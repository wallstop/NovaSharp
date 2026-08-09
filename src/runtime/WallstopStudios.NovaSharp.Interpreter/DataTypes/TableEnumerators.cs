namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using global::NovaSharp;

    /// <summary>
    /// A struct-based enumerator for iterating over table key/value pairs without heap allocation.
    /// </summary>
    /// <remarks>
    /// This enumerator avoids the allocation that would occur when using the <see cref="Table.Pairs"/>
    /// property which returns <see cref="IEnumerable{T}"/>. Use <see cref="Table.GetPairsEnumerator"/>
    /// in hot paths where allocation matters.
    /// </remarks>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Enumerator structs are not meant to be compared."
    )]
    public struct TablePairsEnumerator
    {
        private readonly Table _table;
        private int _arrayIndex;
        private int _nodeIndex;
        private TablePair _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablePairsEnumerator"/> struct.
        /// </summary>
        /// <param name="table">The table to iterate.</param>
        internal TablePairsEnumerator(Table table)
        {
            _table = table;
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = default;
        }

        /// <summary>
        /// Gets the current key/value pair.
        /// </summary>
        public TablePair Current => _current;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (
                _table != null
                && _table.TryAdvanceEntry(ref _arrayIndex, ref _nodeIndex, false, out _current)
            )
            {
                return true;
            }

            _current = default;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = default;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TablePairsEnumerator GetEnumerator() => this;
    }

    /// <summary>
    /// A struct-based enumerator for iterating over table keys without heap allocation.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Enumerator structs are not meant to be compared."
    )]
    public struct TableKeysEnumerator
    {
        private readonly Table _table;
        private int _arrayIndex;
        private int _nodeIndex;
        private LuaValue _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableKeysEnumerator"/> struct.
        /// </summary>
        /// <param name="table">The table to iterate.</param>
        internal TableKeysEnumerator(Table table)
        {
            _table = table;
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = LuaValue.Nil;
        }

        /// <summary>
        /// Gets the current key.
        /// </summary>
        public LuaValue Current => _current;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (
                _table != null
                && _table.TryAdvanceEntry(
                    ref _arrayIndex,
                    ref _nodeIndex,
                    false,
                    out TablePair pair
                )
            )
            {
                _current = pair.Key;
                return true;
            }

            _current = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = LuaValue.Nil;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TableKeysEnumerator GetEnumerator() => this;
    }

    /// <summary>
    /// A struct-based enumerator for iterating over table values without heap allocation.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Enumerator structs are not meant to be compared."
    )]
    public struct TableValuesEnumerator
    {
        private readonly Table _table;
        private int _arrayIndex;
        private int _nodeIndex;
        private LuaValue _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableValuesEnumerator"/> struct.
        /// </summary>
        /// <param name="table">The table to iterate.</param>
        internal TableValuesEnumerator(Table table)
        {
            _table = table;
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = LuaValue.Nil;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public LuaValue Current => _current;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (
                _table != null
                && _table.TryAdvanceEntry(
                    ref _arrayIndex,
                    ref _nodeIndex,
                    false,
                    out TablePair pair
                )
            )
            {
                _current = pair.Value;
                return true;
            }

            _current = LuaValue.Nil;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = LuaValue.Nil;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TableValuesEnumerator GetEnumerator() => this;
    }

    /// <summary>
    /// A struct-based enumerator for iterating over non-nil key/value pairs without heap allocation.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Enumerator structs are not meant to be compared."
    )]
    public struct TableNonNilPairsEnumerator
    {
        private readonly Table _table;
        private int _arrayIndex;
        private int _nodeIndex;
        private TablePair _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableNonNilPairsEnumerator"/> struct.
        /// </summary>
        /// <param name="table">The table to iterate.</param>
        internal TableNonNilPairsEnumerator(Table table)
        {
            _table = table;
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = default;
        }

        /// <summary>
        /// Gets the current key/value pair.
        /// </summary>
        public TablePair Current => _current;

        /// <summary>
        /// Advances the enumerator to the next non-nil element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (
                _table != null
                && _table.TryAdvanceEntry(ref _arrayIndex, ref _nodeIndex, true, out _current)
            )
            {
                return true;
            }

            _current = default;
            return false;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _arrayIndex = 0;
            _nodeIndex = 0;
            _current = default;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TableNonNilPairsEnumerator GetEnumerator() => this;
    }
}
