namespace NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.DataTypes;

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
            Symbols = symbols.Select(s => s.NameValue).ToArray();
            AddRange(values);
        }

        internal ClosureContext()
        {
            Symbols = Array.Empty<string>();
        }
    }
}
