namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Captures the processor state so coroutines can suspend/resume later.
    /// </summary>
    internal sealed class ExecutionState
    {
        /// <summary>
        /// Gets the value stack snapshot.
        /// </summary>
        public FastStack<DynValue> ValueStack { get; } =
            new(VmStackDefaults.ValueStackInitialCapacity, VmStackDefaults.ValueStackMaxCapacity);

        /// <summary>
        /// Gets the call stack snapshot.
        /// </summary>
        public FastStack<CallStackItem> ExecutionStack { get; } =
            new(
                VmStackDefaults.ExecutionStackInitialCapacity,
                VmStackDefaults.ExecutionStackMaxCapacity
            );

        /// <summary>
        /// Gets or sets the instruction pointer at the time of suspension.
        /// </summary>
        public int InstructionPtr { get; set; }

        /// <summary>
        /// Gets or sets the coroutine state associated with the snapshot.
        /// </summary>
        public CoroutineState State { get; set; }

        /// <summary>
        /// Initializes a new execution state with the default coroutine status.
        /// </summary>
        public ExecutionState()
        {
            State = CoroutineState.NotStarted;
        }
    }
}
