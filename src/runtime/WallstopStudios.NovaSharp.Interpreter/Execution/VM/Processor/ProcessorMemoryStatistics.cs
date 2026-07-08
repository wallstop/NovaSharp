namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <content>
    /// Provides approximate retained-memory diagnostics for processor-owned stacks.
    /// </content>
    internal sealed partial class Processor
    {
        /// <summary>
        /// Gets the approximate retained bytes for the processor's stack backing storage.
        /// </summary>
        internal long GetEstimatedRetainedStackBytesForMemoryStatistics()
        {
            long valueStackBytes =
                IntPtr.Size
                + ((long)_valueStack.Capacity * PoolElementSize<DynValue>.EstimatedBytes);
            long executionStackBytes = IntPtr.Size + ((long)_executionStack.Capacity * 128L);
            return valueStackBytes + executionStackBytes;
        }
    }
}
