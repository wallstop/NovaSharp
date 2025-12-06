namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Represents a frame on the NovaSharp execution stack.
    /// </summary>
    internal class CallStackItem
    {
        /// <summary>
        /// Bytecode index where execution should resume for debugger stepping.
        /// </summary>
        public int DebugEntryPoint { get; set; }

        /// <summary>
        /// Locals captured for debugger inspection.
        /// </summary>
        public SymbolRef[] DebugSymbols { get; set; }

        /// <summary>
        /// Source reference that initiated the call.
        /// </summary>
        public SourceRef CallingSourceRef { get; set; }

        /// <summary>
        /// CLR function currently being executed, if any.
        /// </summary>
        public CallbackFunction ClrFunction { get; set; }

        /// <summary>
        /// Continuation invoked after yielding or tail calls.
        /// </summary>
        public CallbackFunction Continuation { get; set; }

        /// <summary>
        /// Error handler registered for xpcall style invocations.
        /// </summary>
        public CallbackFunction ErrorHandler { get; set; }

        /// <summary>
        /// Error handler executed before unwinding (used for message decoration).
        /// </summary>
        public DynValue ErrorHandlerBeforeUnwind { get; set; }

        /// <summary>
        /// Stack index of the base pointer for the frame.
        /// </summary>
        public int BasePointer { get; set; }

        /// <summary>
        /// Instruction pointer used when returning to the caller.
        /// </summary>
        public int ReturnAddress { get; set; }

        /// <summary>
        /// Snapshot of locals stored for debugger inspection or closures.
        /// </summary>
        public DynValue[] LocalScope { get; set; }

        /// <summary>
        /// Closure context captured by the function.
        /// </summary>
        public ClosureContext ClosureScope { get; set; }

        /// <summary>
        /// Tracks metadata about the call (entry point, tail-call, etc.).
        /// </summary>
        public CallStackItemFlags Flags { get; set; }

        /// <summary>
        /// Blocks that have to run __close when the frame unwinds.
        /// </summary>
        public List<List<SymbolRef>> BlocksToClose { get; set; }

        /// <summary>
        /// Indices of locals that must be closed when unwinding.
        /// </summary>
        public HashSet<int> ToBeClosedIndices { get; set; }
    }
}
