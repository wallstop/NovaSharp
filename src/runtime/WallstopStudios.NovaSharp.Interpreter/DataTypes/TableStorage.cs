namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// PUC-Lua-style backing store for <see cref="Table"/>: a contiguous array part holding the dense
    /// positive-integer prefix, plus an insertion-ordered hash part holding every other key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The array part stores values only; the key for slot <c>i</c> is the integer <c>i + 1</c>. A
    /// <c>null</c> slot means the key is absent, while a non-null slot (including
    /// <see cref="DynValue.Nil"/>) means an entry exists. That distinction mirrors the previous
    /// linked-list storage, where writing nil to a key created a node whose value was nil.
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
            /// <summary>The entry key, or <c>null</c> when the slot has been removed.</summary>
            public DynValue key;

            /// <summary>The entry value; may be <see cref="DynValue.Nil"/> for a present-but-nil entry.</summary>
            public DynValue value;

            /// <summary>Route-specific hash of <see cref="Key"/>, always non-negative.</summary>
            public int hash;

            /// <summary>Index of the next node in the same bucket, or -1 at the end of the chain.</summary>
            public int next;
        }

        private DynValue[] _array;

        /// <summary>
        /// Memoized key values for the array part, where <c>_arrayKeys[i]</c> is the boxed integer
        /// <c>i + 1</c>. Allocated lazily on the first traversal that reaches the array part, so a
        /// table that is only indexed never pays for it, and a table that is traversed repeatedly
        /// pays once instead of once per step.
        /// </summary>
        private DynValue[] _arrayKeys;

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
                    bytes += ArrayObjectOverhead + ((long)_array.Length * IntPtr.Size);
                }

                if (_arrayKeys is { Length: > 0 })
                {
                    bytes += ArrayObjectOverhead + ((long)_arrayKeys.Length * IntPtr.Size);
                }

                if (_nodes is { Length: > 0 })
                {
                    // Two references plus two 32-bit fields per node.
                    bytes +=
                        ArrayObjectOverhead
                        + ((long)_nodes.Length * ((2 * IntPtr.Size) + sizeof(int) + sizeof(int)));
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
            _arrayKeys = null;
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
        /// Gets the value stored under the positive integer <paramref name="key"/>, or <c>null</c>
        /// when the key is absent.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynValue GetInt(int key)
        {
            DynValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                return array[key - 1];
            }

            int node = FindInt(key);
            return node < 0 ? null : _nodes[node].value;
        }

        /// <summary>
        /// Gets the value stored under the string <paramref name="key"/>, or <c>null</c> when the key
        /// is absent.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynValue GetString(string key)
        {
            int node = FindString(key);
            return node < 0 ? null : _nodes[node].value;
        }

        /// <summary>
        /// Gets the value stored under an arbitrary <paramref name="key"/> that is neither a string nor
        /// a positive integer, or <c>null</c> when the key is absent.
        /// </summary>
        public DynValue GetValue(DynValue key)
        {
            int node = FindValue(key);
            return node < 0 ? null : _nodes[node].value;
        }

        // ---------------------------------------------------------------------------------------
        // Writes
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Stores <paramref name="value"/> under the positive integer <paramref name="key"/> and
        /// returns the previous value, or <c>null</c> when the key was absent.
        /// </summary>
        public DynValue SetInt(int key, DynValue value)
        {
            DynValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                DynValue previous = array[key - 1];
                array[key - 1] = value;
                if (previous == null)
                {
                    _arrayCount++;
                }

                return previous;
            }

            int node = FindInt(key);
            if (node >= 0)
            {
                DynValue previous = _nodes[node].value;
                _nodes[node].value = value;
                return previous;
            }

            InsertNew(HashInt(key), DynValue.FromNumber(key), value, key);
            return null;
        }

        /// <summary>
        /// Stores <paramref name="value"/> under the string <paramref name="key"/> and returns the
        /// previous value, or <c>null</c> when the key was absent.
        /// </summary>
        public DynValue SetString(string key, DynValue value)
        {
            int node = FindString(key);
            if (node >= 0)
            {
                DynValue previous = _nodes[node].value;
                _nodes[node].value = value;
                return previous;
            }

            InsertNew(HashString(key), DynValue.NewString(key), value, 0);
            return null;
        }

        /// <summary>
        /// Stores <paramref name="value"/> under an arbitrary <paramref name="key"/> that is neither a
        /// string nor a positive integer, and returns the previous value, or <c>null</c> when the key
        /// was absent.
        /// </summary>
        public DynValue SetValue(DynValue key, DynValue value)
        {
            int node = FindValue(key);
            if (node >= 0)
            {
                DynValue previous = _nodes[node].value;
                _nodes[node].value = value;
                return previous;
            }

            InsertNew(HashValue(key), key, value, 0);
            return null;
        }

        // ---------------------------------------------------------------------------------------
        // Removals
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Removes the positive integer <paramref name="key"/>, reporting whether it was present.
        /// </summary>
        public bool RemoveInt(int key)
        {
            DynValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                if (array[key - 1] == null)
                {
                    return false;
                }

                array[key - 1] = null;
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
        public bool RemoveValue(DynValue key)
        {
            return RemoveNode(FindValue(key));
        }

        /// <summary>
        /// Drops every entry whose value is nil.
        /// </summary>
        public void CollectDeadKeys()
        {
            DynValue[] array = _array;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    DynValue value = array[i];
                    if (value != null && value.IsNil())
                    {
                        array[i] = null;
                        _arrayCount--;
                    }
                }
            }

            Node[] nodes = _nodes;
            if (nodes != null)
            {
                for (int i = 0; i < _nodeCount; i++)
                {
                    ref Node node = ref nodes[i];
                    if (node.key != null && node.value.IsNil())
                    {
                        node.key = null;
                        node.value = null;
                        _deadCount++;
                    }
                }

                if (_deadCount > 0)
                {
                    Rehash(0, hasPendingEntry: false);
                }
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
            DynValue[] array = _array;
            int arrayLength = array == null ? 0 : array.Length;
            while (arrayIndex < arrayLength)
            {
                int slot = arrayIndex++;
                DynValue value = array[slot];
                if (value == null || (skipNilValues && value.IsNil()))
                {
                    continue;
                }

                pair = new TablePair(ArrayKeyAt(slot, arrayLength), value);
                return true;
            }

            Node[] nodes = _nodes;
            while (nodeIndex < _nodeCount)
            {
                int slot = nodeIndex++;
                ref Node node = ref nodes[slot];
                if (node.key == null || (skipNilValues && node.value.IsNil()))
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
            DynValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length && array[key - 1] != null)
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
        public bool TryLocateValue(DynValue key, out int arrayIndex, out int nodeIndex)
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
                DynValue nodeKey = node.key;
                if (
                    node.hash == hash
                    && nodeKey != null
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
                DynValue nodeKey = node.key;
                if (node.hash != hash || nodeKey == null)
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

        private int FindValue(DynValue key)
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
                DynValue nodeKey = node.key;
                if (node.hash == hash && nodeKey != null && nodeKey.Equals(key))
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

            nodes[node].key = null;
            nodes[node].value = null;
            nodes[node].next = -1;
            _deadCount++;
            return true;
        }

        private void InsertNew(int hash, DynValue key, DynValue value, int intKeyCandidate)
        {
            if (_nodes == null || _nodeCount == _nodes.Length)
            {
                Rehash(intKeyCandidate, hasPendingEntry: true);

                // The rehash may have grown the array part far enough to swallow this key.
                DynValue[] grown = _array;
                if (
                    intKeyCandidate > 0
                    && grown != null
                    && (uint)(intKeyCandidate - 1) < (uint)grown.Length
                )
                {
                    if (grown[intKeyCandidate - 1] == null)
                    {
                        _arrayCount++;
                    }

                    grown[intKeyCandidate - 1] = value;
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
            DynValue[] array = _array;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != null)
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
                    DynValue key = nodes[i].key;
                    if (key != null && TryGetPositiveIntKey(key, out int intKey))
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
                    if (array[i] != null && i >= arrayCapacity)
                    {
                        survivingHashEntries++;
                    }
                }
            }

            if (nodes != null)
            {
                for (int i = 0; i < _nodeCount; i++)
                {
                    DynValue key = nodes[i].key;
                    if (key == null)
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
            DynValue[] oldArray = _array;
            Node[] oldNodes = _nodes;
            int oldNodeCount = _nodeCount;
            int oldArrayLength = oldArray == null ? 0 : oldArray.Length;

            int bucketCapacity = hashEntries <= 0 ? 0 : NextPowerOfTwo(hashEntries);

            _array = arrayCapacity > 0 ? new DynValue[arrayCapacity] : null;

            // Slot-to-key mapping is stable, so a memo that still fits is reused across a grow; a
            // shrink drops it rather than paying to copy keys the table no longer addresses.
            if (_arrayKeys != null && _arrayKeys.Length > arrayCapacity)
            {
                _arrayKeys = null;
            }

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
                    DynValue value = oldArray[i];
                    if (value != null)
                    {
                        _array[i] = value;
                        _arrayCount++;
                    }
                }

                for (int i = copyLength; i < oldArrayLength; i++)
                {
                    DynValue value = oldArray[i];
                    if (value != null)
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
                    DynValue key = node.key;
                    if (key == null)
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

        private void ReinsertInt(int key, DynValue value)
        {
            DynValue[] array = _array;
            if (array != null && (uint)(key - 1) < (uint)array.Length)
            {
                if (array[key - 1] == null)
                {
                    _arrayCount++;
                }

                array[key - 1] = value;
                return;
            }

            ReinsertNode(HashInt(key), DynValue.FromNumber(key), value);
        }

        private void ReinsertNode(int hash, DynValue key, DynValue value)
        {
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
                if (nodes[i].key == null)
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
        private static bool TryGetPositiveIntKey(DynValue key, out int intKey)
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

        /// <summary>
        /// Returns the key value for array slot <paramref name="slot"/>, materializing and memoizing
        /// it on first use so repeated traversals of the same table do not re-allocate keys.
        /// </summary>
        private DynValue ArrayKeyAt(int slot, int arrayLength)
        {
            DynValue[] keys = _arrayKeys;
            if (keys == null || keys.Length < arrayLength)
            {
                DynValue[] grown = new DynValue[arrayLength];
                if (keys != null)
                {
                    Array.Copy(keys, grown, keys.Length);
                }

                _arrayKeys = keys = grown;
            }

            DynValue key = keys[slot];
            if (key == null)
            {
                key = DynValue.FromNumber(slot + 1);
                keys[slot] = key;
            }

            return key;
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
        private static int HashValue(DynValue key)
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
