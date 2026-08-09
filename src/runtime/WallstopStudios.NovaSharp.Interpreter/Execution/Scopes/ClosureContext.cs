namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// The scope of a closure (container of upvalues)
    /// </summary>
    /// <remarks>
    /// Upvalues are stored as <see cref="ValueSlot"/> cells so that captured locals keep sharing a
    /// single mutable identity while the values they hold stay immutable and allocation-free to read.
    /// </remarks>
    internal sealed class ClosureContext : IReadOnlyList<LuaValue>
    {
        private static readonly IReadOnlyList<string> EnvironmentSymbols = Array.AsReadOnly(
            new[] { WellKnownSymbols.ENV }
        );

        private ValueSlot _singleSlot;
        private ValueSlot[] _slots;
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
        /// Gets the current value of an upvalue.
        /// </summary>
        public LuaValue this[int index]
        {
            get { return GetSlot(index).Value; }
        }

        /// <summary>
        /// Gets the mutable cell backing an upvalue.
        /// </summary>
        /// <param name="index">The upvalue index.</param>
        /// <returns>The captured <see cref="ValueSlot"/>.</returns>
        internal ValueSlot GetSlot(int index)
        {
            ValidateIndex(index);
            return _count == 1 ? _singleSlot : _slots[index];
        }

        /// <summary>
        /// Rebinds an upvalue to a different cell, making both closures observe the same variable.
        /// </summary>
        /// <param name="index">The upvalue index.</param>
        /// <param name="slot">The cell to bind; <c>null</c> installs a fresh nil cell.</param>
        internal void SetSlot(int index, ValueSlot slot)
        {
            ValidateIndex(index);
            slot ??= new ValueSlot();
            if (_count == 1)
            {
                _singleSlot = slot;
                return;
            }

            _slots[index] = slot;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from a list of captured cells.
        /// This overload avoids enumerator allocation by using the list directly.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="slots">The captured upvalue cells.</param>
        internal ClosureContext(SymbolRef[] symbols, List<ValueSlot> slots)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            Symbols = ExtractSymbolNames(symbols);
            _count = slots.Count;
            InitializeFromList(slots);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from an array of captured cells.
        /// This overload avoids enumerator allocation entirely.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="slots">The captured upvalue cells.</param>
        internal ClosureContext(SymbolRef[] symbols, ValueSlot[] slots)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            Symbols = ExtractSymbolNames(symbols);
            _count = slots.Length;
            InitializeFromArray(slots);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class for a single _ENV upvalue.
        /// </summary>
        /// <param name="environmentValue">The initial environment value for this closure.</param>
        internal ClosureContext(LuaValue environmentValue)
        {
            Symbols = EnvironmentSymbols;
            _singleSlot = new ValueSlot(environmentValue);
            _count = 1;
        }

        internal ClosureContext()
        {
            Symbols = Array.Empty<string>();
            _slots = Array.Empty<ValueSlot>();
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

        private void InitializeFromList(List<ValueSlot> slots)
        {
            if (_count == 0)
            {
                _slots = Array.Empty<ValueSlot>();
                return;
            }

            if (_count == 1)
            {
                _singleSlot = slots[0] ?? new ValueSlot();
                return;
            }

            _slots = new ValueSlot[_count];
            for (int i = 0; i < _count; i++)
            {
                _slots[i] = slots[i] ?? new ValueSlot();
            }
        }

        private void InitializeFromArray(ValueSlot[] slots)
        {
            if (_count == 0)
            {
                _slots = Array.Empty<ValueSlot>();
                return;
            }

            if (_count == 1)
            {
                _singleSlot = slots[0] ?? new ValueSlot();
                return;
            }

            _slots = new ValueSlot[_count];
            for (int i = 0; i < _count; i++)
            {
                _slots[i] = slots[i] ?? new ValueSlot();
            }
        }

        private void ValidateIndex(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>
        /// Returns an enumerator over the current upvalue values.
        /// </summary>
        /// <returns>An enumerator over the current upvalue values.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator over the current upvalue values.
        /// </summary>
        /// <returns>An enumerator over the current upvalue values.</returns>
        IEnumerator<LuaValue> IEnumerable<LuaValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator over the current upvalue values.
        /// </summary>
        /// <returns>An enumerator over the current upvalue values.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates captured upvalue values without allocating when used directly by foreach.
        /// </summary>
        internal struct Enumerator : IEnumerator<LuaValue>
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
            /// Gets the current upvalue value.
            /// </summary>
            public LuaValue Current
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
            /// Gets the current upvalue value.
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
