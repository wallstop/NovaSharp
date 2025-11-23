namespace NovaSharp.Interpreter.Execution.VM
{
    using Debugging;
    using NovaSharp.Interpreter.Errors;

    /// <content>
    /// Contains helpers for decorating exceptions with source information.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Retrieves the source reference corresponding to the specified instruction pointer.
        /// </summary>
        private SourceRef GetCurrentSourceRef(int instructionPtr)
        {
            if (instructionPtr >= 0 && instructionPtr < _rootChunk.Code.Count)
            {
                return _rootChunk.Code[instructionPtr].SourceCodeRef;
            }
            return null;
        }

        /// <summary>
        /// Populates debugger metadata for the provided interpreter exception.
        /// </summary>
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
