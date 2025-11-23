namespace NovaSharp.Interpreter.Execution.Scopes
{
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.Execution.VM;

    internal interface ILoop
    {
        public void CompileBreak(ByteCode bc);
        public bool IsBoundary();
    }

    internal class LoopTracker
    {
        public FastStack<ILoop> Loops { get; } = new(16384);
    }
}
