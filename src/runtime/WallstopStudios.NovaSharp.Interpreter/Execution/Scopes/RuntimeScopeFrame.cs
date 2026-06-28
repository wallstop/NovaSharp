namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System.Collections.Generic;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Describes the locals owned by a compiled function so the VM can expose debug symbols at runtime.
    /// </summary>
    internal class RuntimeScopeFrame
    {
        /// <summary>
        /// Gets the ordered list of symbols, matching the stack layout used by the VM.
        /// </summary>
        public List<SymbolRef> DebugSymbols { get; private set; }

        /// <summary>
        /// Gets the number of locals tracked by the frame.
        /// </summary>
        public int Count
        {
            get { return DebugSymbols.Count; }
        }

        /// <summary>
        /// Gets the slot index where the first block begins; used to trim locals when unwinding.
        /// </summary>
        public int ToFirstBlock { get; internal set; }

        /// <summary>
        /// Initializes the frame with an empty debug symbol list.
        /// </summary>
        public RuntimeScopeFrame()
        {
            DebugSymbols = new List<SymbolRef>();
        }

        /// <summary>
        /// Returns a textual summary of the frame for diagnostics.
        /// </summary>
        public override string ToString()
        {
            return ZString.Concat("ScopeFrame : #", Count);
        }
    }
}
