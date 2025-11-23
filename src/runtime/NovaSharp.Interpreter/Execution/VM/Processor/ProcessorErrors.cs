namespace NovaSharp.Interpreter.Execution.VM
{
    using Debugging;
    using NovaSharp.Interpreter.Errors;

    internal sealed partial class Processor
    {
        private SourceRef GetCurrentSourceRef(int instructionPtr)
        {
            if (instructionPtr >= 0 && instructionPtr < _rootChunk.Code.Count)
            {
                return _rootChunk.Code[instructionPtr].SourceCodeRef;
            }
            return null;
        }

        private void FillDebugData(InterpreterException ex, int ip)
        {
            // adjust IP
            if (ip == YieldSpecialTrap)
            {
                ip = _savedInstructionPtr;
            }
            else
            {
                ip -= 1;
            }

            ex.InstructionPtr = ip;

            SourceRef sref = GetCurrentSourceRef(ip);

            ex.DecorateMessage(_script, sref, ip);

            ex.CallStack = GetDebuggerCallStack(sref);
        }
    }
}
