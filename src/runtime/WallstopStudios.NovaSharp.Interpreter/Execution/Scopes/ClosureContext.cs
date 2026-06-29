namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// The scope of a closure (container of upvalues)
    /// </summary>
    internal sealed class ClosureContext : IReadOnlyList<DynValue>
    {
        private static readonly IReadOnlyList<string> EnvironmentSymbols = Array.AsReadOnly(
            new[] { WellKnownSymbols.ENV }
        );

        private DynValue _singleValue;
        private DynValue[] _values;
        private int _count;

        /// <summary>
        /// Gets the symbols.
        /// </summary>
        public IReadOnlyList<string> Symbols { get; private set; }

        /// <summary>
        /// Gets the number of captured upvalue slots.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets or sets an upvalue slot.
        /// </summary>
        public DynValue this[int index]
        {
            get
            {
                ValidateIndex(index);
                return _count == 1 ? _singleValue : _values[index];
            }
            set
            {
                ValidateIndex(index);
                if (_count == 1)
                {
                    _singleValue = value;
                    return;
                }

                _values[index] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from a list of values.
        /// This overload avoids enumerator allocation by using the list directly.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="values">The resolved upvalue values.</param>
        internal ClosureContext(SymbolRef[] symbols, List<DynValue> values)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Symbols = ExtractSymbolNames(symbols);
            _count = values.Count;
            InitializeFromList(values);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from an array of values.
        /// This overload avoids enumerator allocation entirely.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="values">The resolved upvalue values.</param>
        internal ClosureContext(SymbolRef[] symbols, DynValue[] values)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Symbols = ExtractSymbolNames(symbols);
            _count = values.Length;
            InitializeFromArray(values);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class for a single _ENV upvalue.
        /// </summary>
        /// <param name="environmentValue">The mutable environment upvalue slot for this closure.</param>
        internal ClosureContext(DynValue environmentValue)
        {
            Symbols = EnvironmentSymbols;
            _singleValue = environmentValue;
            _count = 1;
        }

        internal ClosureContext(SymbolRef[] symbols, IEnumerable<DynValue> values)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Symbols = ExtractSymbolNames(symbols);
            InitializeFromEnumerable(values);
        }

        internal ClosureContext()
        {
            Symbols = Array.Empty<string>();
            _values = Array.Empty<DynValue>();
        }

        private static string[] ExtractSymbolNames(SymbolRef[] symbols)
        {
            if (symbols.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] names = new string[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                names[i] = symbols[i].NameValue;
            }
            return names;
        }

        private void InitializeFromList(List<DynValue> values)
        {
            if (_count == 0)
            {
                _values = Array.Empty<DynValue>();
                return;
            }

            if (_count == 1)
            {
                _singleValue = values[0];
                return;
            }

            _values = new DynValue[_count];
            for (int i = 0; i < _count; i++)
            {
                _values[i] = values[i];
            }
        }

        private void InitializeFromArray(DynValue[] values)
        {
            if (_count == 0)
            {
                _values = Array.Empty<DynValue>();
                return;
            }

            if (_count == 1)
            {
                _singleValue = values[0];
                return;
            }

            _values = new DynValue[_count];
            Array.Copy(values, _values, _count);
        }

        private void InitializeFromEnumerable(IEnumerable<DynValue> values)
        {
            if (values is DynValue[] array)
            {
                _count = array.Length;
                InitializeFromArray(array);
                return;
            }

            if (values is List<DynValue> list)
            {
                _count = list.Count;
                InitializeFromList(list);
                return;
            }

            List<DynValue> materialized = new();
            foreach (DynValue value in values)
            {
                materialized.Add(value);
            }

            _count = materialized.Count;
            InitializeFromList(materialized);
        }

        private void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Returns an enumerator over the captured upvalue slots.
        /// </summary>
        /// <returns>An enumerator over the captured upvalue slots.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator over the captured upvalue slots.
        /// </summary>
        /// <returns>An enumerator over the captured upvalue slots.</returns>
        IEnumerator<DynValue> IEnumerable<DynValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator over the captured upvalue slots.
        /// </summary>
        /// <returns>An enumerator over the captured upvalue slots.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates captured upvalue slots without allocating when used directly by foreach.
        /// </summary>
        internal struct Enumerator : IEnumerator<DynValue>
        {
            private readonly ClosureContext _context;
            private int _index;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="context">The closure context to enumerate.</param>
            internal Enumerator(ClosureContext context)
            {
                _context = context;
                _index = -1;
            }

            /// <summary>
            /// Gets the current captured upvalue slot.
            /// </summary>
            public DynValue Current
            {
                get
                {
                    if ((uint)_index >= (uint)_context.Count)
                    {
                        throw new InvalidOperationException(
                            "Enumeration has either not started or has already finished."
                        );
                    }

                    return _context[_index];
                }
            }

            /// <summary>
            /// Gets the current captured upvalue slot.
            /// </summary>
            object IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next captured upvalue slot.
            /// </summary>
            /// <returns><c>true</c> when another slot is available; otherwise, <c>false</c>.</returns>
            public bool MoveNext()
            {
                int next = _index + 1;
                if (next >= _context.Count)
                {
                    _index = _context.Count;
                    return false;
                }

                _index = next;
                return true;
            }

            /// <summary>
            /// Resets the enumerator to its initial position.
            /// </summary>
            public void Reset()
            {
                _index = -1;
            }

            /// <summary>
            /// Releases resources held by the enumerator.
            /// </summary>
            public void Dispose() { }
        }
    }
}
