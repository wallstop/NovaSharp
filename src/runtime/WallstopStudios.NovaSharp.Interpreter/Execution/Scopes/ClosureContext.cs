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

        internal ClosureContext(SymbolRef[] symbols, IEnumerable<DynValue> values)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            string[] names = new string[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                names[i] = symbols[i].NameValue;
            }

            Symbols = names;
            AddRange(values);
        }

        internal ClosureContext()
        {
            Symbols = Array.Empty<string>();
        }
    }
}
