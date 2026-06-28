namespace WallstopStudios.NovaSharp.Interpreter.Tree
{
    using System;
    using System.Collections.Generic;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Represents a concrete loop body that can accept `break` statements and later patch them once the scope exits.
    /// Implements <see cref="IDisposable"/> to ensure the pooled <see cref="BreakJumps"/> list is returned.
    /// </summary>
    internal sealed class Loop : ILoop, IDisposable
    {
        private PooledResource<List<Instruction>> _pooledBreakJumps = ListPool<Instruction>.Get(
            out _
        );
        private bool _disposed;

        /// <summary>
        /// Scope guarded by this loop; the compiler emits `Exit` instructions for any `break`.
        /// </summary>
        public RuntimeScopeBlock Scope { get; set; }

        /// <summary>
        /// Jump instructions emitted for `break` statements that still need to be patched to the loop exit.
        /// </summary>
        public List<Instruction> BreakJumps => _pooledBreakJumps.Resource;

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
        /// Returns the pooled <see cref="BreakJumps"/> list to the pool.
        /// Prefer using the <see cref="Dispose"/> method via a using statement instead.
        /// </summary>
        [Obsolete("Use Dispose() via a using statement instead for automatic resource cleanup.")]
        public void ReturnBreakJumpsToPool()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the loop and returns the pooled list to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _pooledBreakJumps.Dispose();
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
