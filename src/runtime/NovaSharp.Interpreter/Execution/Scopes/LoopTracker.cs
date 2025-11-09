namespace NovaSharp.Interpreter.Execution
{
    using DataStructs;
    using VM;

    internal interface ILoop
    {
        public void CompileBreak(ByteCode bc);
        public bool IsBoundary();
    }

    internal class LoopTracker
    {
        public FastStack<ILoop> loops = new(16384);
    }
}
