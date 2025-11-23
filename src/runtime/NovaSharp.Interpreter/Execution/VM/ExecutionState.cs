namespace NovaSharp.Interpreter.Execution.VM
{
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Captures the processor state so coroutines can suspend/resume later.
    /// </summary>
    internal sealed class ExecutionState
    {
        /// <summary>
        /// Gets the value stack snapshot.
        /// </summary>
        public FastStack<DynValue> ValueStack { get; } = new(131072);

        /// <summary>
        /// Gets the call stack snapshot.
        /// </summary>
        public FastStack<CallStackItem> ExecutionStack { get; } = new(131072);

        /// <summary>
        /// Gets or sets the instruction pointer at the time of suspension.
        /// </summary>
        public int InstructionPtr;

        /// <summary>
        /// Gets or sets the coroutine state associated with the snapshot.
        /// </summary>
        public CoroutineState State;

        /// <summary>
        /// Initializes a new execution state with the default coroutine status.
        /// </summary>
        public ExecutionState()
        {
            State = CoroutineState.NotStarted;
        }
    }
}
