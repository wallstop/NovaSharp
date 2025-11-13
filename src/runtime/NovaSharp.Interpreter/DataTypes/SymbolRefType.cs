namespace NovaSharp.Interpreter.DataTypes
{
    using System;

    /// <summary>
    /// Enumeration of the types of SymbolRef
    /// </summary>
    public enum SymbolRefType
    {
        /// <summary>
        /// Legacy placeholder; prefer a concrete symbol type.
        /// </summary>
        [Obsolete("Use a specific SymbolRefType value.", false)]
        Unknown = 0,

        /// <summary>
        /// The symbol ref of a local variable
        /// </summary>
        Local = 1,

        /// <summary>
        /// The symbol ref of an upvalue variable
        /// </summary>
        Upvalue = 2,

        /// <summary>
        /// The symbol ref of a global variable
        /// </summary>
        Global = 3,

        /// <summary>
        /// The symbol ref of the global environment
        /// </summary>
        DefaultEnv = 4,
    }
}
