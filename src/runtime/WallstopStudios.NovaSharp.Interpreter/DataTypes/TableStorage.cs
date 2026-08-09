namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// PUC-Lua-style backing store for <see cref="Table"/>: a contiguous array part holding the dense
    /// positive-integer prefix, plus an insertion-ordered hash part holding every other key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The array part stores values only; the key for slot <c>i</c> is the integer <c>i + 1</c>. A
    /// compact occupancy bitmap records whether a slot is present independently of its value, so
    /// <see cref="LuaValue.Nil"/> remains a storable present value and the value representation can
    /// use default-as-nil without conflating nil with absence.
    /// </para>
    /// <para>
    /// The hash part is a dense <see cref="Node"/> array indexed by a bucket table, in the shape .NET's
    /// own <c>Dictionary</c> uses. Entries are appended, never reordered, so iteration over the hash
    /// part is insertion-ordered. Removal marks a node dead rather than recycling its slot, which keeps
    /// both the iteration order and any in-flight <c>next</c> cursor stable; dead slots are reclaimed
    /// by the next rehash.
    /// </para>
    /// <para>
    /// Keys reach the store through three disjoint routes: positive integers representable as
    /// <see cref="int"/>, strings, and everything else. Each route owns its hash function, so hashes
    /// only ever need to agree with themselves.
    /// </para>
    /// </remarks>
    internal struct TableStorage
    {
        private const int MinHashCapacity = 4;

        /// <summary>
        /// Upper bound on the array part, in slots. Well below the point where the size computation
        /// could overflow, and low enough that a hostile key cannot demand a huge allocation.
        /// </summary>
        private const int MaxArrayCapacity = 1 << 26;

        /// <summary>
        /// Approximate managed overhead of a single array object header plus length field.
        /// </summary>
        private const int ArrayObjectOverhead = 24;

        private struct Node
        {
            /// <summary>The entry key. The hash field records whether the node is live.</summary>
            public LuaValue key;

            /// <summary>The entry value; may be <see cref="LuaValue.Nil"/> for a present-but-nil entry.</summary>
            public LuaValue value;

            /// <summary>Route-specific hash of <see cref="Key"/>, always non-negative.</summary>
            public int hash;

            /// <summary>Index of the next node in the same bucket, or -1 at the end of the chain.</summary>
            public int next;
        }

        private LuaValue[] _array;

        /// <summary>
        /// One presence bit per array slot. Array values cannot encode absence because nil is a
        /// present value until dead-key collection runs.
        /// </summary>
        private uint[] _arrayOccupancy;

        private Node[] _nodes;

        /// <summary>Bucket table holding one-based node indices; zero means the bucket is empty.</summary>
        private int[] _buckets;

        private int _arrayCount;
        private int _nodeCount;
        private int _deadCount;

        /// <summary>
        /// Gets the number of live entries, counting entries whose value is nil.
        /// </summary>
        public int Count => _arrayCount + _nodeCount - _deadCount;

        /// <summary>
        /// Gets the number of slots in the array part.
        /// </summary>
        public int ArrayCapacity => _array == null ? 0 : _array.Length;

        /// <summary>
        /// Gets an estimate of the bytes retained by the array, node, and bucket tables.
        /// </summary>
        public long StructuralBytes
        {
            get
            {
                long bytes = 0;
                if (_array is { Length: > 0 })
                {
                    bytes +=
                        ArrayObjectOverhead
                        + ((long)_array.Length * PoolElementSize<LuaValue>.EstimatedBytes);
                }

                if (_arrayOccupancy is { Length: > 0 })
                {
                    bytes += ArrayObjectOverhead + ((long)_arrayOccupancy.Length * sizeof(uint));
                }

                if (_nodes is { Length: > 0 })
                {
                    // Each node stores two inline DynValues plus its hash and bucket-chain link.
                    bytes +=
                        ArrayObjectOverhead
                        + (
                            (long)_nodes.Length
                            * (
                                (2 * PoolElementSize<LuaValue>.EstimatedBytes)
                                + sizeof(int)
                                + sizeof(int)
                            )
                        );
                }

                if (_buckets is { Length: > 0 })
                {
                    bytes += ArrayObjectOverhead + ((long)_buckets.Length * sizeof(int));
                }

                return bytes;
            }
        }

        /// <summary>
        /// Releases every table, returning the store to its freshly constructed state.
        /// </summary>
        public void Clear()
        {
            _array = null;
            _arrayOccupancy = null;
            _nodes = null;
            _buckets = null;
            _arrayCount = 0;
            _nodeCount = 0;
            _deadCount = 0;
        }

        // ---------------------------------------------------------------------------------------
        // Reads
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Tries to get the value stored under the positive integer <paramref name="key"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInt(int key, out LuaValue value)
        {
            LuaValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                int slot = key - 1;
                if (IsArraySlotOccupied(slot))
                {
                    value = array[slot];
                    return true;
                }

                value = LuaValue.Nil;
                return false;
            }

            int node = FindInt(key);
            if (node >= 0)
            {
                value = _nodes[node].value;
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to get the value stored under the string <paramref name="key"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetString(string key, out LuaValue value)
        {
            int node = FindString(key);
            if (node >= 0)
            {
                value = _nodes[node].value;
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Tries to get the value stored under an arbitrary <paramref name="key"/> that is neither a
        /// string nor a positive integer.
        /// </summary>
        public bool TryGetValue(LuaValue key, out LuaValue value)
        {
            int node = FindValue(key);
            if (node >= 0)
            {
                value = _nodes[node].value;
                return true;
            }

            value = LuaValue.Nil;
            return false;
        }

        // ---------------------------------------------------------------------------------------
        // Writes
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Stores <paramref name="value"/> under the positive integer <paramref name="key"/> and
        /// reports whether <paramref name="previous"/> was present.
        /// </summary>
        public bool SetInt(int key, LuaValue value, out LuaValue previous)
        {
            LuaValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                int slot = key - 1;
                bool replaced = IsArraySlotOccupied(slot);
                previous = replaced ? array[slot] : LuaValue.Nil;
                array[slot] = value;
                if (!replaced)
                {
                    MarkArraySlotOccupied(slot);
                    _arrayCount++;
                }

                return replaced;
            }

            int node = FindInt(key);
            if (node >= 0)
            {
                previous = _nodes[node].value;
                _nodes[node].value = value;
                return true;
            }

            InsertNew(HashInt(key), LuaValue.FromNumber(key), value, key);
            previous = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Stores <paramref name="value"/> under the string <paramref name="key"/> and reports
        /// whether <paramref name="previous"/> was present.
        /// </summary>
        public bool SetString(string key, LuaValue value, out LuaValue previous)
        {
            int node = FindString(key);
            if (node >= 0)
            {
                previous = _nodes[node].value;
                _nodes[node].value = value;
                return true;
            }

            InsertNew(HashString(key), LuaValue.NewString(key), value, 0);
            previous = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Stores <paramref name="value"/> under an arbitrary <paramref name="key"/> that is neither a
        /// string nor a positive integer, and reports whether <paramref name="previous"/> was
        /// present.
        /// </summary>
        public bool SetValue(LuaValue key, LuaValue value, out LuaValue previous)
        {
            int node = FindValue(key);
            if (node >= 0)
            {
                previous = _nodes[node].value;
                _nodes[node].value = value;
                return true;
            }

            InsertNew(HashValue(key), key, value, 0);
            previous = LuaValue.Nil;
            return false;
        }

        // ---------------------------------------------------------------------------------------
        // Removals
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Removes the positive integer <paramref name="key"/>, reporting whether it was present.
        /// </summary>
        public bool RemoveInt(int key)
        {
            LuaValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                int slot = key - 1;
                if (!IsArraySlotOccupied(slot))
                {
                    return false;
                }

                array[slot] = LuaValue.Nil;
                ClearArraySlotOccupied(slot);
                _arrayCount--;
                return true;
            }

            return RemoveNode(FindInt(key));
        }

        /// <summary>
        /// Removes the string <paramref name="key"/>, reporting whether it was present.
        /// </summary>
        public bool RemoveString(string key)
        {
            return RemoveNode(FindString(key));
        }

        /// <summary>
        /// Removes an arbitrary <paramref name="key"/> that is neither a string nor a positive integer,
        /// reporting whether it was present.
        /// </summary>
        public bool RemoveValue(LuaValue key)
        {
            return RemoveNode(FindValue(key));
        }

        /// <summary>
        /// Drops every entry whose value is nil.
        /// </summary>
        public void CollectDeadKeys()
        {
            bool reclaimed = false;

            LuaValue[] array = _array;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    LuaValue value = array[i];
                    if (IsArraySlotOccupied(i) && value.IsNil)
                    {
                        array[i] = LuaValue.Nil;
                        ClearArraySlotOccupied(i);
                        _arrayCount--;
                        reclaimed = true;
                    }
                }
            }

            Node[] nodes = _nodes;
            if (nodes != null)
            {
                for (int i = 0; i < _nodeCount; i++)
                {
                    ref Node node = ref nodes[i];
                    if (IsNodeLive(in node) && node.value.IsNil)
                    {
                        node.key = LuaValue.Nil;
                        node.value = LuaValue.Nil;
                        node.hash = -1;
                        _deadCount++;
                        reclaimed = true;
                    }
                }
            }

            // Rebuild whenever anything was reclaimed, not just when a hash node died. A table whose
            // nils all lived in the array part would otherwise keep an array sized for keys it no
            // longer holds, and keep it charged against the sandbox memory limit. _deadCount is
            // checked too so nodes removed earlier by Remove() are compacted here as well.
            if (reclaimed || _deadCount > 0)
            {
                Rehash(0, hasPendingEntry: false);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Traversal
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Advances a cursor to the next entry, scanning the array part before the hash part.
        /// </summary>
        /// <param name="arrayIndex">Zero-based array slot to resume from; advanced in place.</param>
        /// <param name="nodeIndex">Zero-based hash node to resume from; advanced in place.</param>
        /// <param name="skipNilValues">Whether entries whose value is nil should be skipped.</param>
        /// <param name="pair">The entry found, when this method returns <c>true</c>.</param>
        /// <returns><c>true</c> when an entry was produced; otherwise <c>false</c>.</returns>
        public bool TryAdvance(
            ref int arrayIndex,
            ref int nodeIndex,
            bool skipNilValues,
            out TablePair pair
        )
        {
            LuaValue[] array = _array;
            int arrayLength = array == null ? 0 : array.Length;
            while (arrayIndex < arrayLength)
            {
                int slot = arrayIndex++;
                LuaValue value = array[slot];
                if (!IsArraySlotOccupied(slot) || (skipNilValues && value.IsNil))
                {
                    continue;
                }

                pair = new TablePair(ArrayKeyAt(slot), value);
                return true;
            }

            Node[] nodes = _nodes;
            while (nodeIndex < _nodeCount)
            {
                int slot = nodeIndex++;
                ref Node node = ref nodes[slot];
                if (!IsNodeLive(in node) || (skipNilValues && node.value.IsNil))
                {
                    continue;
                }

                pair = new TablePair(node.key, node.value);
                return true;
            }

            pair = default;
            return false;
        }

        /// <summary>
        /// Resolves the cursor position immediately after the positive integer <paramref name="key"/>.
        /// </summary>
        public bool TryLocateInt(int key, out int arrayIndex, out int nodeIndex)
        {
            LuaValue[] array = _array;
            if (
                array != null
                && (uint)(key - 1) < (uint)array.Length
                && IsArraySlotOccupied(key - 1)
            )
            {
                arrayIndex = key;
                nodeIndex = 0;
                return true;
            }

            return TryLocateNode(FindInt(key), out arrayIndex, out nodeIndex);
        }

        /// <summary>
        /// Resolves the cursor position immediately after the string <paramref name="key"/>.
        /// </summary>
        public bool TryLocateString(string key, out int arrayIndex, out int nodeIndex)
        {
            return TryLocateNode(FindString(key), out arrayIndex, out nodeIndex);
        }

        /// <summary>
        /// Resolves the cursor position immediately after an arbitrary <paramref name="key"/> that is
        /// neither a string nor a positive integer.
        /// </summary>
        public bool TryLocateValue(LuaValue key, out int arrayIndex, out int nodeIndex)
        {
            return TryLocateNode(FindValue(key), out arrayIndex, out nodeIndex);
        }

        private bool TryLocateNode(int node, out int arrayIndex, out int nodeIndex)
        {
            if (node < 0)
            {
                arrayIndex = 0;
                nodeIndex = 0;
                return false;
            }

            arrayIndex = _array == null ? 0 : _array.Length;
            nodeIndex = node + 1;
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // Hash part internals
        // ---------------------------------------------------------------------------------------

        private int FindInt(int key)
        {
            int[] buckets = _buckets;
            if (buckets == null)
            {
                return -1;
            }

            int hash = HashInt(key);
            Node[] nodes = _nodes;
            for (int i = buckets[hash & (buckets.Length - 1)] - 1; i >= 0; i = nodes[i].next)
            {
                ref Node node = ref nodes[i];
                LuaValue nodeKey = node.key;
                if (
                    node.hash == hash
                    && IsNodeLive(in node)
                    && nodeKey.Type == DataType.Number
                    && nodeKey.Number == key
                )
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindString(string key)
        {
            int[] buckets = _buckets;
            if (buckets == null)
            {
                return -1;
            }

            int hash = HashString(key);
            Node[] nodes = _nodes;
            for (int i = buckets[hash & (buckets.Length - 1)] - 1; i >= 0; i = nodes[i].next)
            {
                ref Node node = ref nodes[i];
                LuaValue nodeKey = node.key;
                if (node.hash != hash || !IsNodeLive(in node))
                {
                    continue;
                }

                // Compare the payload by reference first: script constants and repeated field names
                // are usually the very same instance, so the common hit costs one load and no cast.
                object candidate = nodeKey.ReferencePayload;
                if (ReferenceEquals(candidate, key))
                {
                    return i;
                }

                if (candidate is string text && string.Equals(text, key, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindValue(LuaValue key)
        {
            int[] buckets = _buckets;
            if (buckets == null)
            {
                return -1;
            }

            int hash = HashValue(key);
            Node[] nodes = _nodes;
            for (int i = buckets[hash & (buckets.Length - 1)] - 1; i >= 0; i = nodes[i].next)
            {
                ref Node node = ref nodes[i];
                LuaValue nodeKey = node.key;
                if (node.hash == hash && IsNodeLive(in node) && nodeKey.Equals(key))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool RemoveNode(int node)
        {
            if (node < 0)
            {
                return false;
            }

            Node[] nodes = _nodes;
            int hash = nodes[node].hash;
            int[] buckets = _buckets;
            int bucket = hash & (buckets.Length - 1);

            int previous = -1;
            for (int i = buckets[bucket] - 1; i >= 0; i = nodes[i].next)
            {
                if (i == node)
                {
                    if (previous < 0)
                    {
                        buckets[bucket] = nodes[i].next + 1;
                    }
                    else
                    {
                        nodes[previous].next = nodes[i].next;
                    }

                    break;
                }

                previous = i;
            }

            nodes[node].key = LuaValue.Nil;
            nodes[node].value = LuaValue.Nil;
            nodes[node].hash = -1;
            nodes[node].next = -1;
            _deadCount++;
            return true;
        }

        private void InsertNew(int hash, LuaValue key, LuaValue value, int intKeyCandidate)
        {
            Debug.Assert(hash >= 0, "Live node hashes must be non-negative.");

            if (_nodes == null || _nodeCount == _nodes.Length)
            {
                Rehash(intKeyCandidate, hasPendingEntry: true);

                // The rehash may have grown the array part far enough to swallow this key.
                LuaValue[] grown = _array;
                if (
                    intKeyCandidate > 0
                    && grown != null
                    && (uint)(intKeyCandidate - 1) < (uint)grown.Length
                )
                {
                    int slot = intKeyCandidate - 1;
                    if (!IsArraySlotOccupied(slot))
                    {
                        MarkArraySlotOccupied(slot);
                        _arrayCount++;
                    }

                    grown[slot] = value;
                    return;
                }
            }

            int index = _nodeCount++;
            int bucket = hash & (_buckets.Length - 1);
            ref Node node = ref _nodes[index];
            node.key = key;
            node.value = value;
            node.hash = hash;
            node.next = _buckets[bucket] - 1;
            _buckets[bucket] = index + 1;
        }

        /// <summary>
        /// Recomputes the array/hash split using PUC-Lua's sizing heuristic, then rebuilds both parts.
        /// </summary>
        /// <param name="pendingIntKey">
        /// A positive integer key that is about to be inserted and must be counted, or zero when the
        /// pending entry is not an integer key or there is no pending entry at all.
        /// </param>
        /// <param name="hasPendingEntry">
        /// Whether an insert is waiting on this rebuild. Compaction passes <c>false</c>: without it a
        /// rebuild that empties the hash part would still reserve a node and bucket table for an
        /// insert that is never coming, and keep charging it against the sandbox memory limit.
        /// </param>
        private void Rehash(int pendingIntKey, bool hasPendingEntry)
        {
            // nums[i] counts integer keys in (2^(i-1), 2^i]; index 0 counts key 1 alone.
            Span<int> nums = stackalloc int[32];
            nums.Clear();

            int totalIntKeys = 0;
            LuaValue[] array = _array;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (IsArraySlotOccupied(i))
                    {
                        nums[CeilLog2((uint)(i + 1))]++;
                        totalIntKeys++;
                    }
                }
            }

            Node[] nodes = _nodes;
            if (nodes != null)
            {
                for (int i = 0; i < _nodeCount; i++)
                {
                    LuaValue key = nodes[i].key;
                    if (IsNodeLive(in nodes[i]) && TryGetPositiveIntKey(key, out int intKey))
                    {
                        nums[CeilLog2((uint)intKey)]++;
                        totalIntKeys++;
                    }
                }
            }

            if (pendingIntKey > 0)
            {
                nums[CeilLog2((uint)pendingIntKey)]++;
                totalIntKeys++;
            }

            int arrayCapacity = ComputeArrayCapacity(nums, totalIntKeys);

            // Count exactly what Resize will leave in the hash part, so the rebuild is sized once.
            int survivingHashEntries = 0;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (IsArraySlotOccupied(i) && i >= arrayCapacity)
                    {
                        survivingHashEntries++;
                    }
                }
            }

            if (nodes != null)
            {
                for (int i = 0; i < _nodeCount; i++)
                {
                    LuaValue key = nodes[i].key;
                    if (!IsNodeLive(in nodes[i]))
                    {
                        continue;
                    }

                    if (TryGetPositiveIntKey(key, out int intKey) && intKey <= arrayCapacity)
                    {
                        continue;
                    }

                    survivingHashEntries++;
                }
            }

            bool pendingNeedsHashSlot =
                hasPendingEntry && (pendingIntKey <= 0 || pendingIntKey > arrayCapacity);
            Resize(arrayCapacity, survivingHashEntries + (pendingNeedsHashSlot ? 1 : 0));
        }

        /// <summary>
        /// Applies PUC-Lua's <c>computesizes</c> rule: the array part is the largest power of two
        /// <c>n</c> for which more than <c>n / 2</c> of the integer keys in <c>[1, n]</c> are present.
        /// </summary>
        private static int ComputeArrayCapacity(ReadOnlySpan<int> nums, int totalIntKeys)
        {
            int optimal = 0;
            int cumulative = 0;

            for (
                int i = 0, twoToI = 1;
                twoToI > 0 && twoToI <= MaxArrayCapacity && totalIntKeys > twoToI / 2;
                i++, twoToI *= 2
            )
            {
                cumulative += nums[i];
                if (cumulative > twoToI / 2)
                {
                    optimal = twoToI;
                }
            }

            return optimal;
        }

        private void Resize(int arrayCapacity, int hashEntries)
        {
            LuaValue[] oldArray = _array;
            uint[] oldArrayOccupancy = _arrayOccupancy;
            Node[] oldNodes = _nodes;
            int oldNodeCount = _nodeCount;
            int oldArrayLength = oldArray == null ? 0 : oldArray.Length;

            int bucketCapacity = hashEntries <= 0 ? 0 : NextPowerOfTwo(hashEntries);

            _array = arrayCapacity > 0 ? new LuaValue[arrayCapacity] : null;
            _arrayOccupancy = arrayCapacity > 0 ? new uint[(arrayCapacity + 31) >> 5] : null;

            _nodes = bucketCapacity > 0 ? new Node[bucketCapacity] : null;
            _buckets = bucketCapacity > 0 ? new int[bucketCapacity] : null;
            _arrayCount = 0;
            _nodeCount = 0;
            _deadCount = 0;

            // Preserve iteration order: the array prefix first, then surviving nodes in insertion order.
            if (oldArray != null)
            {
                int copyLength = Math.Min(oldArrayLength, arrayCapacity);
                for (int i = 0; i < copyLength; i++)
                {
                    LuaValue value = oldArray[i];
                    if (IsArraySlotOccupied(oldArrayOccupancy, i))
                    {
                        _array[i] = value;
                        MarkArraySlotOccupied(i);
                        _arrayCount++;
                    }
                }

                for (int i = copyLength; i < oldArrayLength; i++)
                {
                    LuaValue value = oldArray[i];
                    if (IsArraySlotOccupied(oldArrayOccupancy, i))
                    {
                        ReinsertInt(i + 1, value);
                    }
                }
            }

            if (oldNodes != null)
            {
                for (int i = 0; i < oldNodeCount; i++)
                {
                    ref Node node = ref oldNodes[i];
                    LuaValue key = node.key;
                    if (!IsNodeLive(in node))
                    {
                        continue;
                    }

                    if (TryGetPositiveIntKey(key, out int intKey))
                    {
                        ReinsertInt(intKey, node.value);
                    }
                    else
                    {
                        ReinsertNode(node.hash, key, node.value);
                    }
                }
            }
        }

        private void ReinsertInt(int key, LuaValue value)
        {
            LuaValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                int slot = key - 1;
                if (!IsArraySlotOccupied(slot))
                {
                    MarkArraySlotOccupied(slot);
                    _arrayCount++;
                }

                array[slot] = value;
                return;
            }

            ReinsertNode(HashInt(key), LuaValue.FromNumber(key), value);
        }

        private void ReinsertNode(int hash, LuaValue key, LuaValue value)
        {
            Debug.Assert(hash >= 0, "Live node hashes must be non-negative.");

            if (_nodes == null || _nodeCount == _nodes.Length)
            {
                GrowNodes();
            }

            int index = _nodeCount++;
            int bucket = hash & (_buckets.Length - 1);
            ref Node node = ref _nodes[index];
            node.key = key;
            node.value = value;
            node.hash = hash;
            node.next = _buckets[bucket] - 1;
            _buckets[bucket] = index + 1;
        }

        /// <summary>
        /// Doubles the hash part in place. Only reachable from a rebuild whose sizing under-counted,
        /// which happens when array slots beyond the new capacity spill back into the hash part.
        /// </summary>
        private void GrowNodes()
        {
            int capacity = _nodes == null ? MinHashCapacity : _nodes.Length * 2;
            Node[] nodes = new Node[capacity];
            int[] buckets = new int[capacity];
            int mask = capacity - 1;

            int count = _nodeCount;
            if (_nodes != null)
            {
                Array.Copy(_nodes, nodes, count);
            }

            for (int i = 0; i < count; i++)
            {
                if (!IsNodeLive(in nodes[i]))
                {
                    continue;
                }

                int bucket = nodes[i].hash & mask;
                nodes[i].next = buckets[bucket] - 1;
                buckets[bucket] = i + 1;
            }

            _nodes = nodes;
            _buckets = buckets;
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Reports whether <paramref name="key"/> is a number that routes through the integer path.
        /// Mirrors <c>Table.GetIntegralKey</c>.
        /// </summary>
        private static bool TryGetPositiveIntKey(LuaValue key, out int intKey)
        {
            if (key.Type == DataType.Number)
            {
                double number = key.Number;
                int candidate = (int)number;
                if (number >= 1.0 && number == candidate)
                {
                    intKey = candidate;
                    return true;
                }
            }

            intKey = 0;
            return false;
        }

        /// <summary>Returns the positive integer key represented by an array slot.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaValue ArrayKeyAt(int slot)
        {
            return LuaValue.FromInteger(slot + 1L);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsArraySlotOccupied(int slot)
        {
            return IsArraySlotOccupied(_arrayOccupancy, slot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsArraySlotOccupied(uint[] occupancy, int slot)
        {
            return occupancy != null && (occupancy[slot >> 5] & (1u << (slot & 31))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkArraySlotOccupied(int slot)
        {
            _arrayOccupancy[slot >> 5] |= 1u << (slot & 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearArraySlotOccupied(int slot)
        {
            _arrayOccupancy[slot >> 5] &= ~(1u << (slot & 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNodeLive(in Node node)
        {
            return node.hash >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashInt(int key)
        {
            return (int)(Avalanche((uint)key) & 0x7FFFFFFF);
        }

        /// <summary>
        /// Per-process seed for string hashing. Randomizing the seed keeps a script from
        /// precomputing colliding table keys, which is the hash-flooding denial-of-service a mod
        /// host has to assume. It is process-scoped, so nothing observable depends on it: iteration
        /// order comes from insertion order, never from bucket layout.
        /// </summary>
        private static readonly uint StringHashSeed = CreateStringHashSeed();

        private static uint CreateStringHashSeed()
        {
            byte[] seed = new byte[sizeof(uint)];
            using (
                System.Security.Cryptography.RandomNumberGenerator random =
                    System.Security.Cryptography.RandomNumberGenerator.Create()
            )
            {
                random.GetBytes(seed);
            }

            return BitConverter.ToUInt32(seed, 0);
        }

        /// <summary>
        /// Seeded string hash consuming four chars per iteration through two independent
        /// accumulators. Materially cheaper than <see cref="string.GetHashCode(StringComparison)"/>
        /// (Marvin) on the short identifiers that dominate Lua field access, while staying
        /// seed-randomized against hash flooding.
        /// </summary>
        private static int HashString(string key)
        {
            unchecked
            {
                uint hash1 = StringHashSeed ^ (uint)key.Length;
                uint hash2 = hash1;

                int length = key.Length;
                int index = 0;
                for (; index + 3 < length; index += 4)
                {
                    hash1 =
                        (((hash1 << 5) | (hash1 >> 27)) + hash1)
                        ^ (key[index] | ((uint)key[index + 1] << 16));
                    hash2 =
                        (((hash2 << 5) | (hash2 >> 27)) + hash2)
                        ^ (key[index + 2] | ((uint)key[index + 3] << 16));
                }

                for (; index < length; index++)
                {
                    hash1 = (((hash1 << 5) | (hash1 >> 27)) + hash1) ^ key[index];
                }

                // Buckets are masked with a power of two, so the accumulator must be avalanched
                // first. Without this, structured keys such as "item1".."item100000" collide
                // heavily in the low bits. (Dictionary sidesteps this with prime-modulo buckets.)
                return (int)(Avalanche(hash1 + (hash2 * 1566083941)) & 0x7FFFFFFF);
            }
        }

        /// <summary>
        /// splitmix32 finalizer: spreads high bits down so power-of-two bucket masking is safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Avalanche(uint hash)
        {
            unchecked
            {
                hash ^= hash >> 16;
                hash *= 0x7feb352d;
                hash ^= hash >> 15;
                hash *= 0x846ca68b;
                hash ^= hash >> 16;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashValue(LuaValue key)
        {
            return key.GetHashCode() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Returns the smallest <c>i</c> with <c>2^i &gt;= value</c>, for <c>value &gt;= 1</c>.
        /// </summary>
        private static int CeilLog2(uint value)
        {
            int result = 0;
            value--;
            while (value != 0)
            {
                value >>= 1;
                result++;
            }

            return result;
        }

        private static int NextPowerOfTwo(int value)
        {
            int result = MinHashCapacity;
            while (result < value)
            {
                result <<= 1;
            }

            return result;
        }
    }
}
