namespace NovaSharp.Interpreter.Execution
{
    using System.Collections.Generic;

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
