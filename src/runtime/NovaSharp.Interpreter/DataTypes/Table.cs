namespace NovaSharp.Interpreter.DataTypes
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    /// A class representing a Lua table.
    /// </summary>
    public class Table : RefIdObject, IScriptPrivateResource
    {
        private readonly LinkedList<TablePair> _values;
        private readonly LinkedListIndex<DynValue, TablePair> _valueMap;
        private readonly LinkedListIndex<string, TablePair> _stringMap;
        private readonly LinkedListIndex<int, TablePair> _arrayMap;
        private readonly Script _owner;

        private int _initArray = 0;
        private int _cachedLength = -1;
        private bool _containsNilEntries = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="owner">The owner script.</param>
        public Table(Script owner)
        {
            _values = new LinkedList<TablePair>();
            _stringMap = new LinkedListIndex<string, TablePair>(_values);
            _arrayMap = new LinkedListIndex<int, TablePair>(_values);
            _valueMap = new LinkedListIndex<DynValue, TablePair>(_values);
            _owner = owner;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="arrayValues">The values for the "array-like" part of the table.</param>
        public Table(Script owner, params DynValue[] arrayValues)
            : this(owner)
        {
            for (int i = 0; i < arrayValues.Length; i++)
            {
                Set(DynValue.NewNumber(i + 1), arrayValues[i]);
            }
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        public Script OwnerScript
        {
            get { return _owner; }
        }

        /// <summary>
        /// Removes all items from the Table.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            _stringMap.Clear();
            _arrayMap.Clear();
            _valueMap.Clear();
            _cachedLength = -1;
        }

        /// <summary>
        /// Gets the integral key from a double.
        /// </summary>
        private int GetIntegralKey(double d)
        {
            int v = ((int)d);

            if (d >= 1.0 && d == v)
            {
                return v;
            }

            return -1;
        }

        /// <summary>
        /// Gets or sets the
        /// <see cref="System.Object" /> with the specified key(s).
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object" />.
        /// </value>
        /// <param name="keys">The keys to access the table and subtables</param>
        public object this[params object[] keys]
        {
            get { return Get(keys).ToObject(); }
            set { Set(keys, DynValue.FromObject(OwnerScript, value)); }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key(s).
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public object this[object key]
        {
            get { return Get(key).ToObject(); }
            set { Set(key, DynValue.FromObject(OwnerScript, value)); }
        }

        private Table ResolveMultipleKeys(object[] keys, out object key)
        {
            //Contract.Ensures(Contract.Result<Table>() != null);
            //Contract.Requires(keys != null);

            Table t = this;
            key = (keys.Length > 0) ? keys[0] : null;

            for (int i = 1; i < keys.Length; ++i)
            {
                DynValue vt = t.RawGet(key);

                if (vt == null)
                {
                    throw new ScriptRuntimeException("Key '{0}' did not point to anything");
                }

                if (vt.Type != DataType.Table)
                {
                    throw new ScriptRuntimeException("Key '{0}' did not point to a table");
                }

                t = vt.Table;
                key = keys[i];
            }

            return t;
        }

        /// <summary>
        /// Append the value to the table using the next available integer index.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Append(DynValue value)
        {
            this.CheckScriptOwnership(value);
            PerformTableSet(
                _arrayMap,
                Length + 1,
                DynValue.NewNumber(Length + 1),
                value,
                true,
                Length + 1
            );
        }

        #region Set

        private void PerformTableSet<T>(
            LinkedListIndex<T, TablePair> listIndex,
            T key,
            DynValue keyDynValue,
            DynValue value,
            bool isNumber,
            int appendKey
        )
        {
            TablePair prev = listIndex.Set(key, new TablePair(keyDynValue, value));

            // If this is an insert, we can invalidate all iterators and collect dead keys
            if (
                _containsNilEntries
                && value.IsNotNil()
                && (prev.Value == null || prev.Value.IsNil())
            )
            {
                CollectDeadKeys();
            }
            // If this value is nil (and we didn't collect), set that there are nil entries, and invalidate array len cache
            else if (value.IsNil())
            {
                _containsNilEntries = true;

                if (isNumber)
                {
                    _cachedLength = -1;
                }
            }
            else if (isNumber)
            {
                // If this is an array insert, we might have to invalidate the array length
                if (prev.Value == null || prev.Value.IsNilOrNan())
                {
                    // If this is an array append, let's check the next element before blindly invalidating
                    if (appendKey >= 0)
                    {
                        LinkedListNode<TablePair> next = _arrayMap.Find(appendKey + 1);
                        if (next == null || next.Value.Value == null || next.Value.Value.IsNil())
                        {
                            _cachedLength += 1;
                        }
                        else
                        {
                            _cachedLength = -1;
                        }
                    }
                    else
                    {
                        _cachedLength = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value associated to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, DynValue value)
        {
            if (key == null)
            {
                throw ScriptRuntimeException.TableIndexIsNil();
            }

            this.CheckScriptOwnership(value);
            PerformTableSet(_stringMap, key, DynValue.NewString(key), value, false, -1);
        }

        /// <summary>
        /// Sets the value associated to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(int key, DynValue value)
        {
            this.CheckScriptOwnership(value);
            PerformTableSet(_arrayMap, key, DynValue.NewNumber(key), value, true, -1);
        }

        /// <summary>
        /// Sets the value associated to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(DynValue key, DynValue value)
        {
            if (key.IsNilOrNan())
            {
                if (key.IsNil())
                {
                    throw ScriptRuntimeException.TableIndexIsNil();
                }
                else
                {
                    throw ScriptRuntimeException.TableIndexIsNaN();
                }
            }

            if (key.Type == DataType.String)
            {
                Set(key.String, value);
                return;
            }

            if (key.Type == DataType.Number)
            {
                int idx = GetIntegralKey(key.Number);

                if (idx > 0)
                {
                    Set(idx, value);
                    return;
                }
            }

            this.CheckScriptOwnership(key);
            this.CheckScriptOwnership(value);

            PerformTableSet(_valueMap, key, key, value, false, -1);
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(object key, DynValue value)
        {
            if (key == null)
            {
                throw ScriptRuntimeException.TableIndexIsNil();
            }

            if (key is string s)
            {
                Set(s, value);
            }
            else if (key is int i)
            {
                Set(i, value);
            }
            else
            {
                Set(DynValue.FromObject(OwnerScript, key), value);
            }
        }

        /// <summary>
        /// Sets the value associated with the specified keys.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key">The keys.</param>
        /// <param name="value">The value.</param>
        public void Set(object[] keys, DynValue value)
        {
            if (keys == null || keys.Length <= 0)
            {
                throw ScriptRuntimeException.TableIndexIsNil();
            }

            ResolveMultipleKeys(keys, out object key).Set(key, value);
        }

        #endregion

        #region Get

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(string key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key) ?? DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(int key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key) ?? DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(DynValue key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key) ?? DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// (expressed as a <see cref="System.Object"/>).
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(object key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key) ?? DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified keys (expressed as an
        /// array of <see cref="System.Object"/>).
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The keys to access the table and subtables</param>
        public DynValue Get(params object[] keys)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(keys) ?? DynValue.Nil;
        }

        #endregion

        #region RawGet

        private static DynValue RawGetValue(LinkedListNode<TablePair> linkedListNode)
        {
            return (linkedListNode != null) ? linkedListNode.Value.Value : null;
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existant values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(string key)
        {
            return RawGetValue(_stringMap.Find(key));
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existant values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(int key)
        {
            return RawGetValue(_arrayMap.Find(key));
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existant values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(DynValue key)
        {
            if (key.Type == DataType.String)
            {
                return RawGet(key.String);
            }

            if (key.Type == DataType.Number)
            {
                int idx = GetIntegralKey(key.Number);
                if (idx > 0)
                {
                    return RawGet(idx);
                }
            }

            return RawGetValue(_valueMap.Find(key));
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existant values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(object key)
        {
            if (key == null)
            {
                return null;
            }

            if (key is string s)
            {
                return RawGet(s);
            }

            if (key is int i)
            {
                return RawGet(i);
            }

            return RawGet(DynValue.FromObject(OwnerScript, key));
        }

        /// <summary>
        /// Gets the value associated with the specified keys (expressed as an
        /// array of <see cref="System.Object"/>).
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The keys to access the table and subtables</param>
        public DynValue RawGet(params object[] keys)
        {
            if (keys == null || keys.Length <= 0)
            {
                return null;
            }

            return ResolveMultipleKeys(keys, out object key).RawGet(key);
        }

        #endregion

        #region Remove

        private bool PerformTableRemove<T>(
            LinkedListIndex<T, TablePair> listIndex,
            T key,
            bool isNumber
        )
        {
            bool removed = listIndex.Remove(key);

            if (removed && isNumber)
            {
                _cachedLength = -1;
            }

            return removed;
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(string key)
        {
            return PerformTableRemove(_stringMap, key, false);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(int key)
        {
            return PerformTableRemove(_arrayMap, key, true);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(DynValue key)
        {
            if (key.Type == DataType.String)
            {
                return Remove(key.String);
            }

            if (key.Type == DataType.Number)
            {
                int idx = GetIntegralKey(key.Number);
                if (idx > 0)
                {
                    return Remove(idx);
                }
            }

            return PerformTableRemove(_valueMap, key, false);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(object key)
        {
            if (key is string s)
            {
                return Remove(s);
            }

            if (key is int i)
            {
                return Remove(i);
            }

            return Remove(DynValue.FromObject(OwnerScript, key));
        }

        /// <summary>
        /// Remove the value associated with the specified keys from the table.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(params object[] keys)
        {
            if (keys == null || keys.Length <= 0)
            {
                return false;
            }

            return ResolveMultipleKeys(keys, out object key).Remove(key);
        }

        #endregion

        /// <summary>
        /// Collects the dead keys. This frees up memory but invalidates pending iterators.
        /// It's called automatically internally when the semantics of Lua tables allow, but can be forced
        /// externally if it's known that no iterators are pending.
        /// </summary>
        public void CollectDeadKeys()
        {
            for (LinkedListNode<TablePair> node = _values.First; node != null; node = node.Next)
            {
                if (node.Value.Value.IsNil())
                {
                    Remove(node.Value.Key);
                }
            }

            _containsNilEntries = false;
            _cachedLength = -1;
        }

        /// <summary>
        /// Returns the next pair from a value
        /// </summary>
        public TablePair? NextKey(DynValue v)
        {
            if (v.IsNil())
            {
                LinkedListNode<TablePair> node = _values.First;

                if (node == null)
                {
                    return TablePair.Nil;
                }
                else
                {
                    if (node.Value.Value.IsNil())
                    {
                        return NextKey(node.Value.Key);
                    }
                    else
                    {
                        return node.Value;
                    }
                }
            }

            if (v.Type == DataType.String)
            {
                return GetNextOf(_stringMap.Find(v.String));
            }

            if (v.Type == DataType.Number)
            {
                int idx = GetIntegralKey(v.Number);

                if (idx > 0)
                {
                    return GetNextOf(_arrayMap.Find(idx));
                }
            }

            return GetNextOf(_valueMap.Find(v));
        }

        private TablePair? GetNextOf(LinkedListNode<TablePair> linkedListNode)
        {
            while (true)
            {
                if (linkedListNode == null)
                {
                    return null;
                }

                if (linkedListNode.Next == null)
                {
                    return TablePair.Nil;
                }

                linkedListNode = linkedListNode.Next;

                if (!linkedListNode.Value.Value.IsNil())
                {
                    return linkedListNode.Value;
                }
            }
        }

        /// <summary>
        /// Gets the length of the "array part".
        /// </summary>
        public int Length
        {
            get
            {
                if (_cachedLength < 0)
                {
                    _cachedLength = 0;

                    for (
                        int i = 1;
                        _arrayMap.ContainsKey(i) && !_arrayMap.Find(i).Value.Value.IsNil();
                        i++
                    )
                    {
                        _cachedLength = i;
                    }
                }

                return _cachedLength;
            }
        }

        internal void InitNextArrayKeys(DynValue val, bool lastpos)
        {
            if (val.Type == DataType.Tuple && lastpos)
            {
                foreach (DynValue v in val.Tuple)
                {
                    InitNextArrayKeys(v, true);
                }
            }
            else
            {
                Set(++_initArray, val.ToScalar());
            }
        }

        /// <summary>
        /// Gets the meta-table associated with this instance.
        /// </summary>
        public Table MetaTable
        {
            get { return _metaTable; }
            set
            {
                this.CheckScriptOwnership(_metaTable);
                _metaTable = value;
            }
        }
        private Table _metaTable;

        /// <summary>
        /// Enumerates the key/value pairs.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TablePair> Pairs => EnumeratePairs();

        /// <summary>
        /// Enumerates the keys.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DynValue> Keys => EnumerateKeys();

        /// <summary>
        /// Enumerates the values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DynValue> Values => EnumerateValues();

        private IEnumerable<TablePair> EnumeratePairs()
        {
            for (LinkedListNode<TablePair> node = _values.First; node != null; node = node.Next)
            {
                yield return node.Value;
            }
        }

        private IEnumerable<DynValue> EnumerateKeys()
        {
            for (LinkedListNode<TablePair> node = _values.First; node != null; node = node.Next)
            {
                yield return node.Value.Key;
            }
        }

        private IEnumerable<DynValue> EnumerateValues()
        {
            for (LinkedListNode<TablePair> node = _values.First; node != null; node = node.Next)
            {
                yield return node.Value.Value;
            }
        }
    }
}
