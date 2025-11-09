using NovaSharp.Interpreter.DataStructs;
using NovaSharp.Interpreter.Execution.VM;

namespace NovaSharp.Interpreter.Execution
{
    interface ILoop
    {
        void CompileBreak(ByteCode bc);
        bool IsBoundary();
    }

    internal class LoopTracker
    {
        public FastStack<ILoop> Loops = new(16384);
    }
}
