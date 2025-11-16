namespace NovaSharp.Interpreter.Execution.Scopes
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;

    internal class RuntimeScopeFrame
    {
        public List<SymbolRef> DebugSymbols { get; private set; }
        public int Count
        {
            get { return DebugSymbols.Count; }
        }
        public int ToFirstBlock { get; internal set; }

        public RuntimeScopeFrame()
        {
            DebugSymbols = new List<SymbolRef>();
        }

        public override string ToString()
        {
            return $"ScopeFrame : #{Count}";
        }
    }
}
