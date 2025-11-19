namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;

    internal class CallStackItem
    {
        public int DebugEntryPoint;
        public SymbolRef[] DebugSymbols;

        public SourceRef CallingSourceRef;

        public CallbackFunction ClrFunction;
        public CallbackFunction Continuation;
        public CallbackFunction ErrorHandler;
        public DynValue ErrorHandlerBeforeUnwind;

        public int BasePointer;
        public int ReturnAddress;
        public DynValue[] LocalScope;
        public ClosureContext ClosureScope;

        public CallStackItemFlags Flags;

        public List<List<SymbolRef>> BlocksToClose;
        public HashSet<int> ToBeClosedIndices;
    }
}
