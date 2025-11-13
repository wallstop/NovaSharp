namespace NovaSharp.Interpreter.Execution.Scopes
{
    using System;
    using NovaSharp.Interpreter.DataTypes;

    internal class RuntimeScopeBlock
    {
        public int From { get; internal set; }
        public int To { get; internal set; }
        public int ToInclusive { get; internal set; }
        public SymbolRef[] ToBeClosed { get; internal set; } = Array.Empty<SymbolRef>();

        public override string ToString()
        {
            return $"ScopeBlock : {From} -> {To} --> {ToInclusive}";
        }
    }
}
