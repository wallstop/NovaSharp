namespace NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Represents a frame on the NovaSharp execution stack.
    /// </summary>
    internal class CallStackItem
    {
        /// <summary>
        /// Bytecode index where execution should resume for debugger stepping.
        /// </summary>
        public int DebugEntryPoint;

        /// <summary>
        /// Locals captured for debugger inspection.
        /// </summary>
        public SymbolRef[] DebugSymbols;

        /// <summary>
        /// Source reference that initiated the call.
        /// </summary>
        public SourceRef CallingSourceRef;

        /// <summary>
        /// CLR function currently being executed, if any.
        /// </summary>
        public CallbackFunction ClrFunction;

        /// <summary>
        /// Continuation invoked after yielding or tail calls.
        /// </summary>
        public CallbackFunction Continuation;

        /// <summary>
        /// Error handler registered for xpcall style invocations.
        /// </summary>
        public CallbackFunction ErrorHandler;

        /// <summary>
        /// Error handler executed before unwinding (used for message decoration).
        /// </summary>
        public DynValue ErrorHandlerBeforeUnwind;

        /// <summary>
        /// Stack index of the base pointer for the frame.
        /// </summary>
        public int BasePointer;

        /// <summary>
        /// Instruction pointer used when returning to the caller.
        /// </summary>
        public int ReturnAddress;

        /// <summary>
        /// Snapshot of locals stored for debugger inspection or closures.
        /// </summary>
        public DynValue[] LocalScope;

        /// <summary>
        /// Closure context captured by the function.
        /// </summary>
        public ClosureContext ClosureScope;

        /// <summary>
        /// Tracks metadata about the call (entry point, tail-call, etc.).
        /// </summary>
        public CallStackItemFlags Flags;

        /// <summary>
        /// Blocks that have to run __close when the frame unwinds.
        /// </summary>
        public List<List<SymbolRef>> BlocksToClose;

        /// <summary>
        /// Indices of locals that must be closed when unwinding.
        /// </summary>
        public HashSet<int> ToBeClosedIndices;
    }
}
