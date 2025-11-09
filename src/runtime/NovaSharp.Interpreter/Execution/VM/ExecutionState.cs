namespace NovaSharp.Interpreter.Execution.VM
{
    using DataStructs;

    internal sealed class ExecutionState
    {
        public FastStack<DynValue> valueStack = new(131072);
        public FastStack<CallStackItem> executionStack = new(131072);
        public int instructionPtr = 0;
        public CoroutineState state = CoroutineState.NotStarted;
    }
}
