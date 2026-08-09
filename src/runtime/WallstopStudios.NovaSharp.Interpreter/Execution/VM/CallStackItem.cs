namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using global::NovaSharp;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
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
        /// Lua function currently being executed, materialized on demand for debug APIs.
        /// </summary>
        public LuaValue Function { get; set; }

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
        public LuaValue ErrorHandlerBeforeUnwind { get; private set; } = LuaValue.Nil;

        internal bool HasErrorHandlerBeforeUnwind { get; private set; }

        internal bool ErrorHandlerBeforeUnwindInProgress { get; set; }

        internal void SetErrorHandlerBeforeUnwind(LuaValue handler, bool hasHandler)
        {
            ErrorHandlerBeforeUnwind = hasHandler ? handler : LuaValue.Nil;
            HasErrorHandlerBeforeUnwind = hasHandler;
        }

        /// <summary>
        /// Stack index of the base pointer for the frame.
        /// </summary>
        public int BasePointer { get; set; }

        /// <summary>
        /// Instruction pointer used when returning to the caller.
        /// </summary>
        public int ReturnAddress { get; set; }

        /// <summary>
        /// Mutable cells holding this frame's locals. Entries are <c>null</c> until the local is
        /// first assigned; closures capture the cell itself so later assignments stay visible.
        /// </summary>
        public ValueSlot[] LocalScope { get; set; }

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

        /// <summary>
        /// Resets all fields to their default values for pooling reuse.
        /// </summary>
        internal void Reset()
        {
            DebugEntryPoint = 0;
            DebugSymbols = null;
            CallingSourceRef = default;
            ClrFunction = null;
            Function = LuaValue.Nil;
            Continuation = null;
            ErrorHandler = null;
            SetErrorHandlerBeforeUnwind(LuaValue.Nil, hasHandler: false);
            ErrorHandlerBeforeUnwindInProgress = false;
            BasePointer = 0;
            ReturnAddress = 0;
            if (LocalScope != null)
            {
                SystemArrayPool<ValueSlot>.Return(LocalScope, clearArray: true);
                LocalScope = null;
            }
            ClosureScope = null;
            Flags = default;
            if (BlocksToClose != null)
            {
                // Return all inner lists to their pool first
                foreach (List<SymbolRef> innerList in BlocksToClose)
                {
                    ListPool<SymbolRef>.Return(innerList);
                }
                // Return the outer list to its pool
                ListPool<List<SymbolRef>>.Return(BlocksToClose);
                BlocksToClose = null;
            }
            if (ToBeClosedIndices != null)
            {
                HashSetPool<int>.Return(ToBeClosedIndices);
                ToBeClosedIndices = null;
            }
        }
    }
}
