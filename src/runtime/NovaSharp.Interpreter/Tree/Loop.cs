using System.Collections.Generic;
using NovaSharp.Interpreter.Execution;
using NovaSharp.Interpreter.Execution.VM;

namespace NovaSharp.Interpreter.Tree
{
    internal class Loop : ILoop
    {
        public RuntimeScopeBlock Scope;
        public List<Instruction> BreakJumps = new();

        public void CompileBreak(ByteCode bc)
        {
            bc.Emit_Exit(Scope);
            BreakJumps.Add(bc.Emit_Jump(OpCode.Jump, -1));
        }

        public bool IsBoundary()
        {
            return false;
        }
    }

    internal class LoopBoundary : ILoop
    {
        public void CompileBreak(ByteCode bc)
        {
            throw new InternalErrorException("CompileBreak called on LoopBoundary");
        }

        public bool IsBoundary()
        {
            return true;
        }
    }
}
