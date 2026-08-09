namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Compatibility;
    using DataStructs;
    using Errors;
    using Sandboxing;

    /// <summary>
    /// A class representing a Lua table.
    /// </summary>
    public class Table : RefIdObject, IScriptPrivateResource
    {
        // Estimated fixed cost of an empty Table: object header, field storage, and the empty
        // TableStorage. The array/node/bucket tables are accounted separately as they are allocated.
        private const int BaseTableOverhead = 96;

        private TableStorage _storage;
        private readonly Script _owner;

        private int _initArray;
        private int _constructorArrayLength;
        private int _cachedLength = -1;
        private bool _containsNilEntries;
        private long _trackedBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="owner">The owner script.</param>
        public Table(Script owner)
        {
            _owner = owner;

            // Track initial allocation if memory tracking is enabled
            AllocationTracker tracker = owner?.AllocationTracker;
            if (tracker != null)
            {
                tracker.RecordAllocation(BaseTableOverhead);
            }

            _trackedBytes = BaseTableOverhead;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="arrayValues">The values for the "array-like" part of the table.</param>
        public Table(Script owner, params DynValue[] arrayValues)
            : this(owner)
        {
            if (arrayValues == null)
            {
                throw new ArgumentNullException(nameof(arrayValues));
            }

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
            _storage.Clear();
            _initArray = 0;
            _constructorArrayLength = 0;
            _cachedLength = -1;
            _containsNilEntries = false;
            SyncTrackedMemory();
        }

        /// <summary>
        /// Reports the storage currently retained by this table to the owner's allocation tracker.
        /// </summary>
        /// <remarks>
        /// The tracker reflects the array, node, and bucket tables actually held, so a table that
        /// stays large after its entries are nil'd keeps counting against a sandbox memory limit.
        /// </remarks>
        private void SyncTrackedMemory()
        {
            AllocationTracker tracker = _owner?.AllocationTracker;
            if (tracker == null)
            {
                return;
            }

            long current = BaseTableOverhead + _storage.StructuralBytes;
            long delta = current - _trackedBytes;
            if (delta > 0)
            {
                tracker.RecordAllocation(delta);
            }
            else if (delta < 0)
            {
                tracker.RecordDeallocation(-delta);
            }

            _trackedBytes = current;
        }

        /// <summary>
        /// Gets the integral key from a double.
        /// </summary>
        private static int GetIntegralKey(double d)
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
        [SuppressMessage(
            "Design",
            "CA1043:Use Integral Or String Argument For Indexers",
            Justification = "Lua tables support arbitrary key sequences and the indexer mirrors that flexibility."
        )]
        public object this[params object[] keys]
        {
            get { return Get(keys).ToObject(); }
            set { Set(keys, DynValue.FromObject(OwnerScript, value)); }
        }

        /// <summary>
        /// Gets or sets the
        /// <see cref="System.Object" /> with the specified nested keys.
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object" />.
        /// </value>
        /// <param name="key1">The key used to locate the nested table.</param>
        /// <param name="key2">The key to access in the nested table.</param>
        [SuppressMessage(
            "Design",
            "CA1043:Use Integral Or String Argument For Indexers",
            Justification = "Lua tables support arbitrary key sequences and the indexer mirrors that flexibility."
        )]
        public object this[object key1, object key2]
        {
            get { return Get(key1, key2).ToObject(); }
            set { Set(key1, key2, DynValue.FromObject(OwnerScript, value)); }
        }

        /// <summary>
        /// Gets or sets the
        /// <see cref="System.Object" /> with the specified nested keys.
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object" />.
        /// </value>
        /// <param name="key1">The first key used to locate the nested table.</param>
        /// <param name="key2">The second key used to locate the nested table.</param>
        /// <param name="key3">The key to access in the nested table.</param>
        [SuppressMessage(
            "Design",
            "CA1043:Use Integral Or String Argument For Indexers",
            Justification = "Lua tables support arbitrary key sequences and the indexer mirrors that flexibility."
        )]
        public object this[object key1, object key2, object key3]
        {
            get { return Get(key1, key2, key3).ToObject(); }
            set { Set(key1, key2, key3, DynValue.FromObject(OwnerScript, value)); }
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

        private static Table ResolveNextTable(Table table, object key)
        {
            if (!table.TryRawGet(key, out DynValue value))
            {
                throw new ScriptRuntimeException("Key '{0}' did not point to anything", key);
            }

            if (value.Type != DataType.Table)
            {
                throw new ScriptRuntimeException("Key '{0}' did not point to a table", key);
            }

            return value.Table;
        }

        private Table ResolveNestedKeys(object key1, object key2, out object key)
        {
            key = key2;
            return ResolveNextTable(this, key1);
        }

        private Table ResolveNestedKeys(object key1, object key2, object key3, out object key)
        {
            Table table = ResolveNextTable(this, key1);
            key = key3;
            return ResolveNextTable(table, key2);
        }

        private Table ResolveMultipleKeys(object[] keys, out object key)
        {
            //Contract.Ensures(Contract.Result<Table>() != null);
            //Contract.Requires(keys != null);

            return ResolveMultipleKeys(keys.AsSpan(), out key);
        }

        private Table ResolveMultipleKeys(ReadOnlySpan<object> keys, out object key)
        {
            Table t = this;
            key = (keys.Length > 0) ? keys[0] : null;

            for (int i = 1; i < keys.Length; ++i)
            {
                t = ResolveNextTable(t, key);
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
            int appendKey = Length + 1;
            bool hadPrevious = _storage.SetInt(appendKey, value, out DynValue previous);
            OnEntryWritten(hadPrevious, previous, value, true, appendKey, appendKey);
        }

        /// <summary>
        /// Updates the derived border/nil bookkeeping after an entry has been written to storage.
        /// </summary>
        /// <param name="hadPrevious">Whether the key was present before the write.</param>
        /// <param name="previous">The value the key held before the write.</param>
        /// <param name="value">The value just written.</param>
        /// <param name="isNumber">Whether the key travelled the integer route.</param>
        /// <param name="numericKey">The integer key when <paramref name="isNumber"/> is set; otherwise zero.</param>
        /// <param name="appendKey">The key being appended for the array fast path, or -1.</param>
        /// <param name="isConstructorField">Whether the write came from a table constructor.</param>
        private void OnEntryWritten(
            bool hadPrevious,
            DynValue previous,
            DynValue value,
            bool isNumber,
            int numericKey,
            int appendKey,
            bool isConstructorField = false
        )
        {
            bool writesNilToMissingKey = value.IsNil() && (!hadPrevious || previous.IsNil());
            bool targetsConstructorArrayField =
                !isConstructorField
                && _constructorArrayLength > 0
                && isNumber
                && numericKey > 0
                && numericKey <= _constructorArrayLength
                && hadPrevious;
            bool preservesLua54AbsentNilWrite =
                !isConstructorField
                && _constructorArrayLength > 0
                && writesNilToMissingKey
                && ResolveCompatibilityVersion() == LuaCompatibilityVersion.Lua54;
            bool clearsAbsentNumericNilWrite =
                !isConstructorField
                && _constructorArrayLength > 0
                && isNumber
                && value.IsNil()
                && !hadPrevious
                && ResolveCompatibilityVersion() != LuaCompatibilityVersion.Lua54;
            bool preservesConstructorArrayLength =
                !clearsAbsentNumericNilWrite
                && (
                    isConstructorField
                    || targetsConstructorArrayField
                    || preservesLua54AbsentNilWrite
                );

            if (
                !isConstructorField
                && !preservesConstructorArrayLength
                && _constructorArrayLength > 0
            )
            {
                _constructorArrayLength = 0;
                _cachedLength = -1;
            }

            SyncTrackedMemory();

            // If this is an insert, we can invalidate all iterators and collect dead keys
            if (
                !isConstructorField
                && !preservesConstructorArrayLength
                && _containsNilEntries
                && value.IsNotNil()
                && (!hadPrevious || previous.IsNil())
            )
            {
                CollectDeadKeys();
            }
            // If this value is nil (and we didn't collect), set that there are nil entries, and invalidate array len cache
            else if (value.IsNil())
            {
                _containsNilEntries = true;

                if (isNumber && !preservesLua54AbsentNilWrite)
                {
                    _cachedLength = -1;
                }
            }
            else if (isNumber)
            {
                // If this is an array insert, we might have to invalidate the array length
                if (!hadPrevious || previous.IsNilOrNan())
                {
                    // If this is an array append, let's check the next element before blindly invalidating
                    if (appendKey >= 0)
                    {
                        bool hasNext = _storage.TryGetInt(appendKey + 1, out DynValue next);
                        if (_cachedLength >= 0 && (!hasNext || next.IsNil()))
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
            bool hadPrevious = _storage.SetString(key, value, out DynValue previous);
            OnEntryWritten(hadPrevious, previous, value, false, 0, -1);
        }

        /// <summary>
        /// Sets the value associated to the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(int key, DynValue value)
        {
            this.CheckScriptOwnership(value);

            if (key <= 0)
            {
                // Non-positive integers are not part of the array key space; route them exactly like
                // the equivalent Lua key so host and script writes address the same entry.
                DynValue nonPositiveKey = DynValue.FromNumber(key);
                bool replaced = _storage.SetValue(nonPositiveKey, value, out DynValue previous);
                OnEntryWritten(replaced, previous, value, false, 0, -1);
                return;
            }

            bool hadPrevious = _storage.SetInt(key, value, out DynValue previousInt);
            OnEntryWritten(hadPrevious, previousInt, value, true, key, -1);
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

            bool hadPrevious = _storage.SetValue(key, value, out DynValue previous);
            OnEntryWritten(hadPrevious, previous, value, false, 0, -1);
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

            switch (key)
            {
                case string s:
                    Set(s, value);
                    break;
                case int i:
                    Set(i, value);
                    break;
                default:
                    Set(DynValue.FromObject(OwnerScript, key), value);
                    break;
            }
        }

        /// <summary>
        /// Sets the value associated with the specified keys.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="value">The value.</param>
        public void Set(object[] keys, DynValue value)
        {
            if (keys == null || keys.Length == 0)
            {
                throw ScriptRuntimeException.TableIndexIsNil();
            }

            ResolveMultipleKeys(keys, out object key).Set(key, value);
        }

        /// <summary>
        /// Sets the value associated with the specified caller-owned nested keys.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The keys to access the table and subtables.</param>
        /// <param name="value">The value.</param>
        public void Set(ReadOnlySpan<object> keys, DynValue value)
        {
            if (keys.Length == 0)
            {
                throw ScriptRuntimeException.TableIndexIsNil();
            }

            ResolveMultipleKeys(keys, out object key).Set(key, value);
        }

        /// <summary>
        /// Sets the value associated with the specified nested keys.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The key used to locate the nested table.</param>
        /// <param name="key2">The key to set in the nested table.</param>
        /// <param name="value">The value.</param>
        public void Set(object key1, object key2, DynValue value)
        {
            ResolveNestedKeys(key1, key2, out object key).Set(key, value);
        }

        /// <summary>
        /// Sets the value associated with the specified nested keys.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The first key used to locate the nested table.</param>
        /// <param name="key2">The second key used to locate the nested table.</param>
        /// <param name="key3">The key to set in the nested table.</param>
        /// <param name="value">The value.</param>
        public void Set(object key1, object key2, object key3, DynValue value)
        {
            ResolveNestedKeys(key1, key2, key3, out object key).Set(key, value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(string key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(int key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(DynValue key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// (expressed as a <see cref="System.Object"/>).
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue Get(object key)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Tries to get the value associated with a string key without conflating an absent entry
        /// with a present entry whose value is nil.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The stored value, or nil when the key is absent.</param>
        public bool TryRawGet(string key, out DynValue value)
        {
            if (key == null)
            {
                value = DynValue.Nil;
                return false;
            }

            return _storage.TryGetString(key, out value);
        }

        /// <summary>
        /// Tries to get the value associated with an integer key without conflating an absent entry
        /// with a present entry whose value is nil.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The stored value, or nil when the key is absent.</param>
        public bool TryRawGet(int key, out DynValue value)
        {
            return key > 0
                ? _storage.TryGetInt(key, out value)
                : _storage.TryGetValue(DynValue.FromNumber(key), out value);
        }

        /// <summary>
        /// Tries to get the value associated with a Lua key without conflating an absent entry with
        /// a present entry whose value is nil.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The stored value, or nil when the key is absent.</param>
        public bool TryRawGet(DynValue key, out DynValue value)
        {
            switch (key.Type)
            {
                case DataType.String:
                    return TryRawGet(key.String, out value);
                case DataType.Number:
                {
                    int index = GetIntegralKey(key.Number);
                    if (index > 0)
                    {
                        return TryRawGet(index, out value);
                    }

                    break;
                }
            }

            return _storage.TryGetValue(key, out value);
        }

        /// <summary>
        /// Tries to get the value associated with a CLR key without conflating an absent entry with
        /// a present entry whose value is nil.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The stored value, or nil when the key is absent.</param>
        public bool TryRawGet(object key, out DynValue value)
        {
            switch (key)
            {
                case null:
                    value = DynValue.Nil;
                    return false;
                case string text:
                    return TryRawGet(text, out value);
                case int integer:
                    return TryRawGet(integer, out value);
                default:
                    return TryRawGet(DynValue.FromObject(OwnerScript, key), out value);
            }
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
            return RawGet(keys);
        }

        /// <summary>
        /// Gets the value associated with the specified caller-owned nested keys.
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The keys to access the table and subtables.</param>
        public DynValue Get(ReadOnlySpan<object> keys)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(keys);
        }

        /// <summary>
        /// Gets the value associated with the specified nested keys.
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The key used to locate the nested table.</param>
        /// <param name="key2">The key to access in the nested table.</param>
        public DynValue Get(object key1, object key2)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key1, key2);
        }

        /// <summary>
        /// Gets the value associated with the specified nested keys.
        /// This will marshall CLR and NovaSharp objects in the best possible way.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The first key used to locate the nested table.</param>
        /// <param name="key2">The second key used to locate the nested table.</param>
        /// <param name="key3">The key to access in the nested table.</param>
        public DynValue Get(object key1, object key2, object key3)
        {
            //Contract.Ensures(Contract.Result<DynValue>() != null);
            return RawGet(key1, key2, key3);
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(string key)
        {
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(int key)
        {
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(DynValue key)
        {
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
        }

        /// <summary>
        /// Gets the value associated with the specified key,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key">The key.</param>
        public DynValue RawGet(object key)
        {
            return TryRawGet(key, out DynValue value) ? value : DynValue.Nil;
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
            if (keys == null || keys.Length == 0)
            {
                return DynValue.Nil;
            }

            return ResolveMultipleKeys(keys, out object key).RawGet(key);
        }

        /// <summary>
        /// Gets the value associated with the specified caller-owned nested keys,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="keys">The keys to access the table and subtables.</param>
        public DynValue RawGet(ReadOnlySpan<object> keys)
        {
            if (keys.Length == 0)
            {
                return DynValue.Nil;
            }

            return ResolveMultipleKeys(keys, out object key).RawGet(key);
        }

        /// <summary>
        /// Gets the value associated with the specified nested keys,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key1">The key used to locate the nested table.</param>
        /// <param name="key2">The key to access in the nested table.</param>
        public DynValue RawGet(object key1, object key2)
        {
            return ResolveNestedKeys(key1, key2, out object key).RawGet(key);
        }

        /// <summary>
        /// Gets the value associated with the specified nested keys,
        /// without bringing to Nil the non-existent values.
        /// </summary>
        /// <param name="key1">The first key used to locate the nested table.</param>
        /// <param name="key2">The second key used to locate the nested table.</param>
        /// <param name="key3">The key to access in the nested table.</param>
        public DynValue RawGet(object key1, object key2, object key3)
        {
            return ResolveNestedKeys(key1, key2, key3, out object key).RawGet(key);
        }

        private bool OnEntryRemoved(bool removed, bool isNumber)
        {
            if (!removed)
            {
                return false;
            }

            if (_constructorArrayLength > 0)
            {
                _constructorArrayLength = 0;
                _cachedLength = -1;
            }
            else if (isNumber)
            {
                _cachedLength = -1;
            }

            if (_storage.Count == 0)
            {
                // An emptied table should not keep holding its tables against a memory limit.
                _storage.Clear();
                _containsNilEntries = false;
            }

            SyncTrackedMemory();
            return true;
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(string key)
        {
            return key != null && OnEntryRemoved(_storage.RemoveString(key), false);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(int key)
        {
            // Non-positive keys are not part of the array key space, so they route -- and report --
            // exactly as Set(int) and Remove(DynValue) do for the same key.
            bool isArrayKey = key > 0;
            bool removed = isArrayKey
                ? _storage.RemoveInt(key)
                : _storage.RemoveValue(DynValue.FromNumber(key));
            return OnEntryRemoved(removed, isArrayKey);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(DynValue key)
        {
            switch (key.Type)
            {
                case DataType.String:
                    return Remove(key.String);
                case DataType.Number:
                {
                    int idx = GetIntegralKey(key.Number);
                    if (idx > 0)
                    {
                        return Remove(idx);
                    }

                    break;
                }
            }

            return OnEntryRemoved(_storage.RemoveValue(key), false);
        }

        /// <summary>
        /// Remove the value associated with the specified key from the table.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(object key)
        {
            return key switch
            {
                string s => Remove(s),
                int i => Remove(i),
                _ => Remove(DynValue.FromObject(OwnerScript, key)),
            };
        }

        /// <summary>
        /// Remove the value associated with the specified keys from the table.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(params object[] keys)
        {
            return keys is { Length: > 0 } && ResolveMultipleKeys(keys, out object key).Remove(key);
        }

        /// <summary>
        /// Remove the value associated with the specified caller-owned nested keys from the table.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="keys">The key.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(ReadOnlySpan<object> keys)
        {
            return keys.Length > 0 && ResolveMultipleKeys(keys, out object key).Remove(key);
        }

        /// <summary>
        /// Remove the value associated with the specified nested keys from the table.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The key used to locate the nested table.</param>
        /// <param name="key2">The key to remove from the nested table.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(object key1, object key2)
        {
            return ResolveNestedKeys(key1, key2, out object key).Remove(key);
        }

        /// <summary>
        /// Remove the value associated with the specified nested keys from the table.
        /// Multiple keys can be used to access subtables.
        /// </summary>
        /// <param name="key1">The first key used to locate the nested table.</param>
        /// <param name="key2">The second key used to locate the nested table.</param>
        /// <param name="key3">The key to remove from the nested table.</param>
        /// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(object key1, object key2, object key3)
        {
            return ResolveNestedKeys(key1, key2, key3, out object key).Remove(key);
        }

        /// <summary>
        /// Collects the dead keys. This frees up memory but invalidates pending iterators.
        /// It's called automatically internally when the semantics of Lua tables allow, but can be forced
        /// externally if it's known that no iterators are pending.
        /// </summary>
        public void CollectDeadKeys()
        {
            _storage.CollectDeadKeys();

            if (_storage.Count == 0)
            {
                _storage.Clear();
            }

            _containsNilEntries = false;
            _constructorArrayLength = 0;
            _cachedLength = -1;
            SyncTrackedMemory();
        }

        /// <summary>
        /// Returns the next pair from a value
        /// </summary>
        /// <returns>
        /// The next non-nil pair, <see cref="TablePair.Nil"/> once the traversal is exhausted, or
        /// <c>null</c> when <paramref name="v"/> is not a key of this table.
        /// </returns>
        public TablePair? NextKey(DynValue v)
        {
            int arrayIndex;
            int nodeIndex;
            if (v.IsNil())
            {
                arrayIndex = 0;
                nodeIndex = 0;
            }
            else if (!TryLocateKey(v, out arrayIndex, out nodeIndex))
            {
                return null;
            }

            return _storage.TryAdvance(
                ref arrayIndex,
                ref nodeIndex,
                skipNilValues: true,
                out TablePair pair
            )
                ? pair
                : TablePair.Nil;
        }

        /// <summary>
        /// Resolves the traversal cursor that sits immediately after <paramref name="key"/>.
        /// </summary>
        private bool TryLocateKey(DynValue key, out int arrayIndex, out int nodeIndex)
        {
            switch (key.Type)
            {
                case DataType.String:
                    return _storage.TryLocateString(key.String, out arrayIndex, out nodeIndex);
                case DataType.Number:
                {
                    int idx = GetIntegralKey(key.Number);
                    if (idx > 0)
                    {
                        return _storage.TryLocateInt(idx, out arrayIndex, out nodeIndex);
                    }

                    break;
                }
            }

            return _storage.TryLocateValue(key, out arrayIndex, out nodeIndex);
        }

        /// <summary>
        /// Advances a traversal cursor, used by the allocation-free table enumerators.
        /// </summary>
        internal bool TryAdvanceEntry(
            ref int arrayIndex,
            ref int nodeIndex,
            bool skipNilValues,
            out TablePair pair
        )
        {
            return _storage.TryAdvance(ref arrayIndex, ref nodeIndex, skipNilValues, out pair);
        }

        /// <summary>
        /// Gets the length of the "array part".
        /// </summary>
        public int Length
        {
            get
            {
                if (_cachedLength >= 0)
                {
                    return _cachedLength;
                }

                LuaCompatibilityVersion version = ResolveCompatibilityVersion();

                if (_constructorArrayLength > 0 && version != LuaCompatibilityVersion.Lua55)
                {
                    _cachedLength =
                        version == LuaCompatibilityVersion.Lua54
                            ? CalculateLua54ConstructorLength()
                            : CalculatePreLua54ConstructorLength();
                    return _cachedLength;
                }

                _cachedLength = CalculatePrefixLength();
                return _cachedLength;
            }
        }

        private LuaCompatibilityVersion ResolveCompatibilityVersion()
        {
            return _owner == null
                ? LuaVersionDefaults.CurrentDefault
                : LuaVersionDefaults.Resolve(_owner.Options.CompatibilityVersion);
        }

        private int CalculatePrefixLength()
        {
            int length = 0;
            while (IsArrayValueNotNil(length + 1))
            {
                length++;
            }

            return length;
        }

        private int CalculatePreLua54ConstructorLength()
        {
            if (IsArrayValueNotNil(_constructorArrayLength))
            {
                return _constructorArrayLength;
            }

            int low = 0;
            int high = _constructorArrayLength;

            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (IsArrayValueNotNil(middle))
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private int CalculateLua54ConstructorLength()
        {
            for (int i = _constructorArrayLength; i > 0; i--)
            {
                if (IsArrayValueNotNil(i))
                {
                    return i;
                }
            }

            return 0;
        }

        private bool IsArrayValueNotNil(int index)
        {
            if (index < 1)
            {
                return false;
            }

            return _storage.TryGetInt(index, out DynValue value) && value.IsNotNil();
        }

        /// <summary>
        /// Initializes a keyed field while a table constructor is still being evaluated.
        /// </summary>
        internal void InitNextKey(DynValue key, DynValue value)
        {
            if (key.IsNilOrNan())
            {
                if (key.IsNil())
                {
                    throw ScriptRuntimeException.TableIndexIsNil();
                }

                throw ScriptRuntimeException.TableIndexIsNaN();
            }

            value = value.ToScalar();

            if (key.Type == DataType.String)
            {
                this.CheckScriptOwnership(value);
                bool hadPrevious = _storage.SetString(
                    key.String,
                    value,
                    out DynValue previousString
                );
                OnEntryWritten(
                    hadPrevious,
                    previousString,
                    value,
                    false,
                    0,
                    -1,
                    isConstructorField: true
                );
                return;
            }

            if (key.Type == DataType.Number)
            {
                int idx = GetIntegralKey(key.Number);

                if (idx > 0)
                {
                    this.CheckScriptOwnership(value);
                    bool hadPrevious = _storage.SetInt(idx, value, out DynValue previousInt);
                    OnEntryWritten(
                        hadPrevious,
                        previousInt,
                        value,
                        true,
                        idx,
                        -1,
                        isConstructorField: true
                    );
                    ExtendConstructorArrayLengthThroughContiguousFields();
                    return;
                }
            }

            this.CheckScriptOwnership(key);
            this.CheckScriptOwnership(value);

            bool replaced = _storage.SetValue(key, value, out DynValue previous);
            OnEntryWritten(replaced, previous, value, false, 0, -1, isConstructorField: true);
        }

        /// <summary>
        /// Initializes the hidden array iteration keys used by `next`/`ipairs` while inserting complex values (tables/functions).
        /// </summary>
        internal void InitNextArrayKeys(DynValue val, bool lastPosition)
        {
            if (val.Type == DataType.Tuple && lastPosition)
            {
                foreach (DynValue v in val.Tuple)
                {
                    InitNextArrayKeys(v, true);
                }
            }
            else
            {
                DynValue value = val.ToScalar();
                this.CheckScriptOwnership(value);
                _initArray++;
                bool hadPrevious = _storage.SetInt(_initArray, value, out DynValue previous);
                OnEntryWritten(
                    hadPrevious,
                    previous,
                    value,
                    true,
                    _initArray,
                    -1,
                    isConstructorField: true
                );
                _constructorArrayLength = _initArray;
                ExtendConstructorArrayLengthThroughContiguousFields();
            }
        }

        private void ExtendConstructorArrayLengthThroughContiguousFields()
        {
            if (_constructorArrayLength > 0 && !IsArrayValueNotNil(_constructorArrayLength))
            {
                return;
            }

            while (IsArrayValueNotNil(_constructorArrayLength + 1))
            {
                _constructorArrayLength++;
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
                this.CheckScriptOwnership(value);
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
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                yield return pair;
            }
        }

        private IEnumerable<DynValue> EnumerateKeys()
        {
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                yield return pair.Key;
            }
        }

        private IEnumerable<DynValue> EnumerateValues()
        {
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                yield return pair.Value;
            }
        }

        /// <summary>
        /// Gets a struct-based enumerator for iterating over key/value pairs without heap allocation.
        /// </summary>
        /// <returns>A <see cref="TablePairsEnumerator"/> that can be used in foreach loops.</returns>
        /// <remarks>
        /// Use this method in hot paths where avoiding allocations is important.
        /// For general use, the <see cref="Pairs"/> property is more convenient.
        /// </remarks>
        [SuppressMessage(
            "Design",
            "CA1024:Use properties where appropriate",
            Justification = "Method returns a new struct enumerator instance each call for foreach pattern."
        )]
        public TablePairsEnumerator GetPairsEnumerator()
        {
            return new TablePairsEnumerator(this);
        }

        /// <summary>
        /// Gets a struct-based enumerator for iterating over keys without heap allocation.
        /// </summary>
        /// <returns>A <see cref="TableKeysEnumerator"/> that can be used in foreach loops.</returns>
        [SuppressMessage(
            "Design",
            "CA1024:Use properties where appropriate",
            Justification = "Method returns a new struct enumerator instance each call for foreach pattern."
        )]
        public TableKeysEnumerator GetKeysEnumerator()
        {
            return new TableKeysEnumerator(this);
        }

        /// <summary>
        /// Gets a struct-based enumerator for iterating over values without heap allocation.
        /// </summary>
        /// <returns>A <see cref="TableValuesEnumerator"/> that can be used in foreach loops.</returns>
        [SuppressMessage(
            "Design",
            "CA1024:Use properties where appropriate",
            Justification = "Method returns a new struct enumerator instance each call for foreach pattern."
        )]
        public TableValuesEnumerator GetValuesEnumerator()
        {
            return new TableValuesEnumerator(this);
        }

        /// <summary>
        /// Gets a struct-based enumerator for iterating over non-nil key/value pairs without heap allocation.
        /// </summary>
        /// <returns>A <see cref="TableNonNilPairsEnumerator"/> that can be used in foreach loops.</returns>
        [SuppressMessage(
            "Design",
            "CA1024:Use properties where appropriate",
            Justification = "Method returns a new struct enumerator instance each call for foreach pattern."
        )]
        public TableNonNilPairsEnumerator GetNonNilPairsEnumerator()
        {
            return new TableNonNilPairsEnumerator(this);
        }

        /// <summary>
        /// Fills the destination span with key/value pairs from the table.
        /// </summary>
        /// <param name="destination">The span to fill.</param>
        /// <returns>The number of pairs written to the span.</returns>
        /// <remarks>
        /// This method does not allocate and is suitable for hot paths.
        /// If the destination is smaller than the table, only the first entries are copied.
        /// </remarks>
        public int FillPairs(Span<TablePair> destination)
        {
            int index = 0;
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (
                index < destination.Length
                && _storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair)
            )
            {
                destination[index++] = pair;
            }
            return index;
        }

        /// <summary>
        /// Fills the destination span with keys from the table.
        /// </summary>
        /// <param name="destination">The span to fill.</param>
        /// <returns>The number of keys written to the span.</returns>
        public int FillKeys(Span<DynValue> destination)
        {
            int index = 0;
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (
                index < destination.Length
                && _storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair)
            )
            {
                destination[index++] = pair.Key;
            }
            return index;
        }

        /// <summary>
        /// Fills the destination span with values from the table.
        /// </summary>
        /// <param name="destination">The span to fill.</param>
        /// <returns>The number of values written to the span.</returns>
        public int FillValues(Span<DynValue> destination)
        {
            int index = 0;
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (
                index < destination.Length
                && _storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair)
            )
            {
                destination[index++] = pair.Value;
            }
            return index;
        }

        /// <summary>
        /// Fills the destination collection with key/value pairs, clearing it first.
        /// </summary>
        /// <typeparam name="TCollection">The type of the collection.</typeparam>
        /// <param name="destination">The collection to fill.</param>
        /// <returns>The collection for fluent chaining.</returns>
        public TCollection FillPairs<TCollection>(TCollection destination)
            where TCollection : ICollection<TablePair>
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                destination.Add(pair);
            }
            return destination;
        }

        /// <summary>
        /// Fills the destination collection with keys, clearing it first.
        /// </summary>
        /// <typeparam name="TCollection">The type of the collection.</typeparam>
        /// <param name="destination">The collection to fill.</param>
        /// <returns>The collection for fluent chaining.</returns>
        public TCollection FillKeys<TCollection>(TCollection destination)
            where TCollection : ICollection<DynValue>
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                destination.Add(pair.Key);
            }
            return destination;
        }

        /// <summary>
        /// Fills the destination collection with values, clearing it first.
        /// </summary>
        /// <typeparam name="TCollection">The type of the collection.</typeparam>
        /// <param name="destination">The collection to fill.</param>
        /// <returns>The collection for fluent chaining.</returns>
        public TCollection FillValues<TCollection>(TCollection destination)
            where TCollection : ICollection<DynValue>
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            int arrayIndex = 0;
            int nodeIndex = 0;
            while (_storage.TryAdvance(ref arrayIndex, ref nodeIndex, false, out TablePair pair))
            {
                destination.Add(pair.Value);
            }
            return destination;
        }

        /// <summary>
        /// Gets the total number of entries in the table, including nil entries.
        /// </summary>
        /// <remarks>
        /// This count includes entries where the value has been set to nil.
        /// For the "array length" (consecutive non-nil integer keys starting at 1), use <see cref="Length"/>.
        /// </remarks>
        public int Count => _storage.Count;
    }
}
