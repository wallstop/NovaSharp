namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Executes bytecode for a script, coordinating stacks, coroutines, and debugger integrations.
    /// </summary>
    internal sealed partial class Processor
    {
        private const int StackSize = 131072;

        private readonly ByteCode _rootChunk;

        private readonly FastStack<DynValue> _valueStack;
        private readonly FastStack<CallStackItem> _executionStack;
        private List<Processor> _coroutinesStack;

        private Table _globalTable;
        private readonly Script _script;
        private readonly Processor _parent;
        private CoroutineState _state;
        private bool _canYield = true;
        private int _savedInstructionPtr = -1;
        private readonly DebugContext _debug;
        private DynValue _lastCloseError = DynValue.Nil;

        /// <summary>
        /// Gets a value indicating whether the currently executing CLR callback can yield back into Lua.
        /// </summary>
        internal bool CanYield
        {
            get { return _canYield; }
        }

        /// <summary>
        /// Initializes the processor for the specified script and installs the global bytecode/root coroutine.
        /// </summary>
        /// <param name="script">Owning script.</param>
        /// <param name="globalContext">Global table visible to the VM.</param>
        /// <param name="byteCode">Root chunk to execute.</param>
        public Processor(Script script, Table globalContext, ByteCode byteCode)
        {
            _valueStack = new FastStack<DynValue>(StackSize);
            _executionStack = new FastStack<CallStackItem>(StackSize);
            _coroutinesStack = new List<Processor>();

            _debug = new DebugContext();
            _rootChunk = byteCode;
            _globalTable = globalContext;
            _script = script;
            _state = CoroutineState.Main;
            DynValue.NewCoroutine(new Coroutine(this)); // creates an associated coroutine for the main processor
        }

        /// <summary>
        /// Creates a child processor that shares the parent's runtime state.
        /// </summary>
        private Processor(Processor parentProcessor)
        {
            _valueStack = new FastStack<DynValue>(StackSize);
            _executionStack = new FastStack<CallStackItem>(StackSize);
            _debug = parentProcessor._debug;
            _rootChunk = parentProcessor._rootChunk;
            _globalTable = parentProcessor._globalTable;
            _script = parentProcessor._script;
            _parent = parentProcessor;
            _state = CoroutineState.NotStarted;
        }

        /// <summary>
        /// Constructs a child processor that reuses the stacks from a recycled processor instance.
        /// </summary>
        /// <param name="parentProcessor">Parent processor to inherit from.</param>
        /// <param name="recycleProcessor">Processor providing the stacks.</param>
        internal Processor(Processor parentProcessor, Processor recycleProcessor)
        {
            _valueStack = recycleProcessor._valueStack;
            _executionStack = recycleProcessor._executionStack;

            _debug = parentProcessor._debug;
            _rootChunk = parentProcessor._rootChunk;
            _globalTable = parentProcessor._globalTable;
            _script = parentProcessor._script;
            _parent = parentProcessor;
            _state = CoroutineState.NotStarted;
        }

        /// <summary>
        /// Invokes the specified function, running the VM until the call completes or throws.
        /// </summary>
        /// <param name="function">Function to invoke.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <returns>The return tuple.</returns>
        public DynValue Call(DynValue function, DynValue[] args)
        {
            List<Processor> coroutinesStack =
                _parent != null ? _parent._coroutinesStack : _coroutinesStack;

            if (coroutinesStack.Count > 0 && coroutinesStack[^1] != this)
            {
                return coroutinesStack[^1].Call(function, args);
            }

            EnterProcessor();

            try
            {
                IDisposable stopwatch = _script.PerformanceStats.StartStopwatch(
                    Diagnostics.PerformanceCounter.Execution
                );

                _canYield = false;

                try
                {
                    int entrypoint = PushClrToScriptStackFrame(
                        CallStackItemFlags.CallEntryPoint,
                        function,
                        args
                    );
                    return ProcessingLoop(entrypoint);
                }
                finally
                {
                    _canYield = true;

                    if (stopwatch != null)
                    {
                        stopwatch.Dispose();
                    }
                }
            }
            finally
            {
                LeaveProcessor();
            }
        }

        // pushes all what's required to perform a clr-to-script function call. function can be null if it's already
        // at vstack top.
        /// <summary>
        /// Pushes the stack frame metadata needed to transition from CLR into Lua code.
        /// </summary>
        /// <param name="Flags">Flags describing the call entry point.</param>
        /// <param name="function">Function being invoked (optional when already on stack).</param>
        /// <param name="args">Arguments to copy.</param>
        /// <returns>The instruction pointer to start executing.</returns>
        private int PushClrToScriptStackFrame(
            CallStackItemFlags Flags,
            DynValue function,
            DynValue[] args
        )
        {
            if (function == null)
            {
                function = _valueStack.Peek();
            }
            else
            {
                _valueStack.Push(function); // func val
            }

            args = InternalAdjustTuple(args);

            for (int i = 0; i < args.Length; i++)
            {
                _valueStack.Push(args[i]);
            }

            _valueStack.Push(DynValue.NewNumber(args.Length)); // func args count

            _executionStack.Push(
                new CallStackItem()
                {
                    BasePointer = _valueStack.Count,
                    DebugEntryPoint = function.Function.EntryPointByteCodeLocation,
                    ReturnAddress = -1,
                    ClosureScope = function.Function.ClosureContext,
                    CallingSourceRef = SourceRef.GetClrLocation(),
                    Flags = Flags,
                }
            );

            return function.Function.EntryPointByteCodeLocation;
        }

        private int _owningThreadId = -1;
        private int _executionNesting;

        /// <summary>
        /// Unwinds processor bookkeeping and signals debugger listeners when execution ends.
        /// </summary>
        private void LeaveProcessor()
        {
            _executionNesting -= 1;
            _owningThreadId = -1;

            if (_parent != null)
            {
                _parent._coroutinesStack.RemoveAt(_parent._coroutinesStack.Count - 1);
            }

            if (
                _executionNesting == 0
                && _debug != null
                && _debug.DebuggerEnabled
                && _debug.DebuggerAttached != null
            )
            {
                _debug.DebuggerAttached.SignalExecutionEnded();
            }
        }

        /// <summary>
        /// Gets the managed thread identifier, returning 1 when the runtime does not expose thread IDs.
        /// </summary>
        private static int GetThreadId()
        {
#if ENABLE_DOTNET || NETFX_CORE
            return 1;
#else
            return Environment.CurrentManagedThreadId;
#endif
        }

        /// <summary>
        /// Validates thread affinity and records nested execution entry.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when multi-threaded access is detected and disabled.</exception>
        private void EnterProcessor()
        {
            int threadId = GetThreadId();

            if (
                _owningThreadId >= 0
                && _owningThreadId != threadId
                && _script.Options.CheckThreadAccess
            )
            {
                string msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Cannot enter the same NovaSharp processor from two different threads : {0} and {1}",
                    _owningThreadId,
                    threadId
                );
                throw new InvalidOperationException(msg);
            }

            _owningThreadId = threadId;

            _executionNesting += 1;

            if (_parent != null)
            {
                _parent._coroutinesStack.Add(this);
            }
        }

        /// <summary>
        /// Gets the source location where the current coroutine last yielded.
        /// </summary>
        internal SourceRef GetCoroutineSuspendedLocation()
        {
            return GetCurrentSourceRef(_savedInstructionPtr);
        }

        /// <summary>
        /// Forces the coroutine state (test-only helper).
        /// </summary>
        internal void ForceStateForTests(CoroutineState state)
        {
            _state = state;
        }

        /// <summary>
        /// Pushes a synthetic call stack frame to aid debugger/tests.
        /// </summary>
        /// <param name="frame">Frame to inject.</param>
        internal void PushCallStackFrameForTests(CallStackItem frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            _executionStack.Push(frame);
        }

        /// <summary>
        /// Clears the execution stack, restoring an idle processor (test-only helper).
        /// </summary>
        internal void ClearCallStackForTests()
        {
            while (_executionStack.Count > 0)
            {
                _executionStack.Pop();
            }
        }
    }
}
