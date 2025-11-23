namespace NovaSharp.Interpreter.Tree
{
    using System.Collections.Generic;
    using Execution.Scopes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a concrete loop body that can accept `break` statements and later patch them once the scope exits.
    /// </summary>
    internal class Loop : ILoop
    {
        /// <summary>
        /// Scope guarded by this loop; the compiler emits `Exit` instructions for any `break`.
        /// </summary>
        public RuntimeScopeBlock Scope;

        /// <summary>
        /// Jump instructions emitted for `break` statements that still need to be patched to the loop exit.
        /// </summary>
        public List<Instruction> BreakJumps = new();

        /// <summary>
        /// Emits the bytecode for a `break` statement within this loop and records the jump for later patching.
        /// </summary>
        /// <param name="bc">Bytecode stream receiving the exit and jump instructions.</param>
        public void CompileBreak(ByteCode bc)
        {
            bc.EmitExit(Scope);
            BreakJumps.Add(bc.EmitJump(OpCode.Jump, -1));
        }

        /// <summary>
        /// Indicates that this loop can be targeted by `break`.
        /// </summary>
        public bool IsBoundary()
        {
            return false;
        }
    }

    /// <summary>
    /// Loop boundary used to represent constructs where `break` is illegal (e.g., placeholder scopes).
    /// </summary>
    internal class LoopBoundary : ILoop
    {
        /// <summary>
        /// Throws because boundary sentinels should never emit `break` bytecode.
        /// </summary>
        public void CompileBreak(ByteCode bc)
        {
            throw new InternalErrorException("CompileBreak called on LoopBoundary");
        }

        /// <summary>
        /// Indicates that this sentinel exists only to stop `break` propagation.
        /// </summary>
        public bool IsBoundary()
        {
            return true;
        }
    }
}
