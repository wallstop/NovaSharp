namespace NovaSharp.Interpreter.Execution
{
    using System;

    internal class RuntimeScopeBlock
    {
        public int From { get; internal set; }
        public int To { get; internal set; }
        public int ToInclusive { get; internal set; }

        public override string ToString()
        {
            return $"ScopeBlock : {From} -> {To} --> {ToInclusive}";
        }
    }
}
