namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

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
        private readonly LinkedList<TablePair> _list;
        private LinkedListNode<TablePair> _current;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablePairsEnumerator"/> struct.
        /// </summary>
        /// <param name="list">The linked list backing the table.</param>
        internal TablePairsEnumerator(LinkedList<TablePair> list)
        {
            _list = list;
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Gets the current key/value pair.
        /// </summary>
        public TablePair Current => _current?.Value ?? default;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (_list == null)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;
                _current = _list.First;
            }
            else
            {
                _current = _current?.Next;
            }

            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _started = false;
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
        private readonly LinkedList<TablePair> _list;
        private LinkedListNode<TablePair> _current;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableKeysEnumerator"/> struct.
        /// </summary>
        /// <param name="list">The linked list backing the table.</param>
        internal TableKeysEnumerator(LinkedList<TablePair> list)
        {
            _list = list;
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Gets the current key.
        /// </summary>
        public DynValue Current => _current?.Value.Key;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (_list == null)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;
                _current = _list.First;
            }
            else
            {
                _current = _current?.Next;
            }

            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _started = false;
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
        private readonly LinkedList<TablePair> _list;
        private LinkedListNode<TablePair> _current;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableValuesEnumerator"/> struct.
        /// </summary>
        /// <param name="list">The linked list backing the table.</param>
        internal TableValuesEnumerator(LinkedList<TablePair> list)
        {
            _list = list;
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public DynValue Current => _current?.Value.Value;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (_list == null)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;
                _current = _list.First;
            }
            else
            {
                _current = _current?.Next;
            }

            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TableValuesEnumerator GetEnumerator() => this;
    }

    /// <summary>
    /// A struct-based enumerator for iterating over table pairs, skipping nil values.
    /// </summary>
    [SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "Enumerator structs are not meant to be compared."
    )]
    public struct TableNonNilPairsEnumerator
    {
        private readonly LinkedList<TablePair> _list;
        private LinkedListNode<TablePair> _current;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableNonNilPairsEnumerator"/> struct.
        /// </summary>
        /// <param name="list">The linked list backing the table.</param>
        internal TableNonNilPairsEnumerator(LinkedList<TablePair> list)
        {
            _list = list;
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Gets the current key/value pair.
        /// </summary>
        public TablePair Current => _current?.Value ?? default;

        /// <summary>
        /// Advances the enumerator to the next non-nil element.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; <c>false</c> if the end was reached.</returns>
        public bool MoveNext()
        {
            if (_list == null)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;
                _current = _list.First;
            }
            else
            {
                _current = _current?.Next;
            }

            // Skip nil values
            while (_current != null && _current.Value.Value != null && _current.Value.Value.IsNil())
            {
                _current = _current.Next;
            }

            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _current = null;
            _started = false;
        }

        /// <summary>
        /// Returns this enumerator (allows foreach usage).
        /// </summary>
        public TableNonNilPairsEnumerator GetEnumerator() => this;
    }
}
