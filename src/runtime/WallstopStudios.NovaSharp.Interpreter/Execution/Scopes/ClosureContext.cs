namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// The scope of a closure (container of upvalues)
    /// </summary>
    internal class ClosureContext : List<DynValue>
    {
        /// <summary>
        /// Gets the symbols.
        /// </summary>
        public string[] Symbols { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from a list of values.
        /// This overload avoids enumerator allocation by using the list directly.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="values">The resolved upvalue values.</param>
        internal ClosureContext(SymbolRef[] symbols, List<DynValue> values)
            : base(values.Count)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            Symbols = ExtractSymbolNames(symbols);

            // Use direct indexing to avoid List<T>.Enumerator boxing through IEnumerable<T>
            for (int i = 0; i < values.Count; i++)
            {
                Add(values[i]);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureContext"/> class from an array of values.
        /// This overload avoids enumerator allocation entirely.
        /// </summary>
        /// <param name="symbols">The symbol references for each upvalue.</param>
        /// <param name="values">The resolved upvalue values.</param>
        internal ClosureContext(SymbolRef[] symbols, DynValue[] values)
            : base(values.Length)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            Symbols = ExtractSymbolNames(symbols);

            // Direct array iteration - no allocation
            for (int i = 0; i < values.Length; i++)
            {
                Add(values[i]);
            }
        }

        internal ClosureContext(SymbolRef[] symbols, IEnumerable<DynValue> values)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            Symbols = ExtractSymbolNames(symbols);
            AddRange(values);
        }

        internal ClosureContext()
        {
            Symbols = Array.Empty<string>();
        }

        private static string[] ExtractSymbolNames(SymbolRef[] symbols)
        {
            string[] names = new string[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                names[i] = symbols[i].NameValue;
            }
            return names;
        }
    }
}
