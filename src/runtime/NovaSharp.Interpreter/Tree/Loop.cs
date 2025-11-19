namespace NovaSharp.Interpreter.Tree
{
    using System.Collections.Generic;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class Loop : ILoop
    {
        public RuntimeScopeBlock Scope;
        public List<Instruction> BreakJumps = new();

        public void CompileBreak(ByteCode bc)
        {
            bc.EmitExit(Scope);
            BreakJumps.Add(bc.EmitJump(OpCode.Jump, -1));
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
