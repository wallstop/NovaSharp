namespace NovaSharp.Interpreter.Execution.VM
{
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;

    internal class CallStackItem
    {
        public int debugEntryPoint;
        public SymbolRef[] debugSymbols;

        public SourceRef callingSourceRef;

        public CallbackFunction clrFunction;
        public CallbackFunction continuation;
        public CallbackFunction errorHandler;
        public DynValue errorHandlerBeforeUnwind;

        public int basePointer;
        public int returnAddress;
        public DynValue[] localScope;
        public ClosureContext closureScope;

        public CallStackItemFlags flags;
    }
}
