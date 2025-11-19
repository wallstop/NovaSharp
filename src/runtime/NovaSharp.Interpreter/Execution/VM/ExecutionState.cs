namespace NovaSharp.Interpreter.Execution.VM
{
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;

    internal sealed class ExecutionState
    {
        public FastStack<DynValue> ValueStack { get; } = new(131072);
        public FastStack<CallStackItem> ExecutionStack { get; } = new(131072);
        public int InstructionPtr;
        public CoroutineState State;

        public ExecutionState()
        {
            State = CoroutineState.NotStarted;
        }
    }
}
