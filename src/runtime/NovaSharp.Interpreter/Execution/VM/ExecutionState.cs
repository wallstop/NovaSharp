namespace NovaSharp.Interpreter.Execution.VM
{
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;

    internal sealed class ExecutionState
    {
        public FastStack<DynValue> valueStack = new(131072);
        public FastStack<CallStackItem> executionStack = new(131072);
        public int instructionPtr = 0;
        public CoroutineState state = CoroutineState.NotStarted;
    }
}
