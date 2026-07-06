namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Executes bytecode for a script, coordinating stacks, coroutines, and debugger integrations.
    /// </summary>
    internal sealed partial class Processor
    {
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
        private int _errorHandlerBeforeUnwindScanBoundaryDepth = -1;

        private readonly ref struct ClrCallArguments
        {
            private readonly DynValue[] _array;
            private readonly ReadOnlySpan<DynValue> _span;
            private readonly DynValue _arg0;
            private readonly DynValue _arg1;
            private readonly DynValue _arg2;
            private readonly DynValue _arg3;
            private readonly DynValue _arg4;
            private readonly DynValue _arg5;
            private readonly DynValue _arg6;
            private readonly int _count;
            private readonly bool _hasSpan;

            internal ClrCallArguments(DynValue[] args)
            {
                _array = args;
                _span = default;
                _arg0 = null;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = args != null ? args.Length : 0;
                _hasSpan = false;
            }

            internal ClrCallArguments(ReadOnlySpan<DynValue> args)
            {
                _array = null;
                _span = args;
                _arg0 = null;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = args.Length;
                _hasSpan = true;
            }

            internal ClrCallArguments(DynValue arg)
            {
                _array = null;
                _span = default;
                _arg0 = arg;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = 1;
                _hasSpan = false;
            }

            internal ClrCallArguments(DynValue arg1, DynValue arg2)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = 2;
                _hasSpan = false;
            }

            internal ClrCallArguments(DynValue arg1, DynValue arg2, DynValue arg3)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = 3;
                _hasSpan = false;
            }

            internal ClrCallArguments(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                _count = 4;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5
            )
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = null;
                _arg6 = null;
                _count = 5;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5,
                DynValue arg6
            )
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = arg6;
                _arg6 = null;
                _count = 6;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5,
                DynValue arg6,
                DynValue arg7
            )
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = arg6;
                _arg6 = arg7;
                _count = 7;
                _hasSpan = false;
            }

            internal int Count
            {
                get { return _count; }
            }

            internal DynValue this[int index]
            {
                get
                {
                    DynValue value;
                    if (_hasSpan)
                    {
                        value = _span[index];
                    }
                    else if (_array != null)
                    {
                        value = _array[index];
                    }
                    else
                    {
                        value = index switch
                        {
                            0 => _arg0,
                            1 => _arg1,
                            2 => _arg2,
                            3 => _arg3,
                            4 => _arg4,
                            5 => _arg5,
                            6 => _arg6,
                            _ => throw new ArgumentOutOfRangeException(nameof(index)),
                        };
                    }

                    return value ?? DynValue.Nil;
                }
            }

            /// <summary>
            /// Creates the coroutine resume tuple, reusing array-backed caller arguments when available.
            /// </summary>
            internal DynValue ToTuple()
            {
                if (_array != null)
                {
                    if (!ContainsNull(_array))
                    {
                        return DynValue.NewTuple(_array);
                    }

                    DynValue[] values = new DynValue[_count];
                    for (int i = 0; i < _count; i++)
                    {
                        values[i] = _array[i] ?? DynValue.Nil;
                    }

                    return DynValue.NewTuple(values);
                }

                if (_hasSpan)
                {
                    return CreateTupleFromSpan(_span);
                }

                switch (_count)
                {
                    case 0:
                        return DynValue.EmptyTuple;
                    case 1:
                        return DynValue.NewTuple(this[0]);
                    case 2:
                        return DynValue.NewTuple(this[0], this[1]);
                    case 3:
                        return DynValue.NewTuple(this[0], this[1], this[2]);
                    case 4:
                        return DynValue.NewTuple(this[0], this[1], this[2], this[3]);
                    case 5:
                        return DynValue.NewTuple(this[0], this[1], this[2], this[3], this[4]);
                    case 6:
                        DynValue[] fixedValues =
                        {
                            this[0],
                            this[1],
                            this[2],
                            this[3],
                            this[4],
                            this[5],
                        };
                        return DynValue.NewTuple(fixedValues);
                    case 7:
                        DynValue[] fixedSevenValues =
                        {
                            this[0],
                            this[1],
                            this[2],
                            this[3],
                            this[4],
                            this[5],
                            this[6],
                        };
                        return DynValue.NewTuple(fixedSevenValues);
                    default:
                        DynValue[] values = new DynValue[_count];
                        for (int i = 0; i < _count; i++)
                        {
                            values[i] = this[i];
                        }

                        return DynValue.NewTuple(values);
                }
            }

            private static DynValue CreateTupleFromSpan(ReadOnlySpan<DynValue> values)
            {
                switch (values.Length)
                {
                    case 0:
                        return DynValue.EmptyTuple;
                    case 1:
                        return DynValue.NewTuple(values[0]);
                    case 2:
                        return DynValue.NewTuple(values[0], values[1]);
                    case 3:
                        return DynValue.NewTuple(values[0], values[1], values[2]);
                    case 4:
                        return DynValue.NewTuple(values[0], values[1], values[2], values[3]);
                    case 5:
                        return DynValue.NewTuple(
                            values[0],
                            values[1],
                            values[2],
                            values[3],
                            values[4]
                        );
                }

                DynValue[] copiedValues = new DynValue[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    copiedValues[i] = values[i] ?? DynValue.Nil;
                }

                return DynValue.NewTuple(copiedValues);
            }

            private static bool ContainsNull(DynValue[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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
            _valueStack = new FastStack<DynValue>(VmStackDefaults.ValueStackInitialCapacity);
            _executionStack = new FastStack<CallStackItem>(
                VmStackDefaults.ExecutionStackInitialCapacity
            );
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
            _valueStack = new FastStack<DynValue>(VmStackDefaults.ValueStackInitialCapacity);
            _executionStack = new FastStack<CallStackItem>(
                VmStackDefaults.ExecutionStackInitialCapacity
            );
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
            return Call(function, new ClrCallArguments(args));
        }

        /// <summary>
        /// Invokes a compiled chunk entry point with a fresh closure context.
        /// </summary>
        /// <param name="entryPointAddress">Instruction pointer for the chunk entry point.</param>
        /// <param name="closureScope">Closure context containing the chunk's environment upvalue.</param>
        /// <returns>The return tuple.</returns>
        internal DynValue CallChunk(int entryPointAddress, ClosureContext closureScope)
        {
            return CallChunk(entryPointAddress, closureScope, function: null);
        }

        /// <summary>
        /// Invokes a Lua function with no arguments, running the VM until the call completes or throws.
        /// </summary>
        /// <param name="function">Function to invoke.</param>
        /// <returns>The return tuple.</returns>
        internal DynValue CallFunctionWithoutArguments(DynValue function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            Closure closure = function.Function;
            return CallChunk(closure.EntryPointByteCodeLocation, closure.ClosureContext, function);
        }

        private DynValue CallChunk(
            int entryPointAddress,
            ClosureContext closureScope,
            DynValue function
        )
        {
            if (closureScope == null)
            {
                throw new ArgumentNullException(nameof(closureScope));
            }

            List<Processor> coroutinesStack =
                _parent != null ? _parent._coroutinesStack : _coroutinesStack;

            if (coroutinesStack.Count > 0 && coroutinesStack[^1] != this)
            {
                return coroutinesStack[^1].CallChunk(entryPointAddress, closureScope, function);
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
                    PushChunkEntryPointStackFrame(entryPointAddress, closureScope, function);
                    return ProcessingLoop(entryPointAddress);
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

        /// <summary>
        /// Invokes the specified function with caller-owned contiguous arguments.
        /// </summary>
        /// <param name="function">Function to invoke.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <returns>The return tuple.</returns>
        public DynValue Call(DynValue function, ReadOnlySpan<DynValue> args)
        {
            return Call(function, new ClrCallArguments(args));
        }

        /// <summary>
        /// Invokes the specified function with one argument.
        /// </summary>
        public DynValue Call(DynValue function, DynValue arg)
        {
            return Call(function, new ClrCallArguments(arg));
        }

        /// <summary>
        /// Invokes the specified function with two arguments.
        /// </summary>
        public DynValue Call(DynValue function, DynValue arg1, DynValue arg2)
        {
            return Call(function, new ClrCallArguments(arg1, arg2));
        }

        /// <summary>
        /// Invokes the specified function with three arguments.
        /// </summary>
        public DynValue Call(DynValue function, DynValue arg1, DynValue arg2, DynValue arg3)
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3));
        }

        /// <summary>
        /// Invokes the specified function with four arguments.
        /// </summary>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Invokes the specified function with five arguments.
        /// </summary>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Invokes the specified function with six arguments.
        /// </summary>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Invokes the specified function with seven arguments.
        /// </summary>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        private DynValue Call(DynValue function, ClrCallArguments args)
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
                        CallStackItemFlagsPresets.CallEntryPoint,
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
            ClrCallArguments args
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

            int argCount = PushAdjustedArguments(args);
            _valueStack.Push(DynValue.FromNumber(argCount)); // func args count

            CallStackItem frame = CallStackItemPool.Rent();
            frame.BasePointer = _valueStack.Count;
            frame.DebugEntryPoint = function.Function.EntryPointByteCodeLocation;
            frame.ReturnAddress = -1;
            frame.ClosureScope = function.Function.ClosureContext;
            frame.Function = function;
            frame.CallingSourceRef = SourceRef.GetClrLocation();
            frame.Flags = Flags;
            _executionStack.Push(frame);

            return function.Function.EntryPointByteCodeLocation;
        }

        private void PushChunkEntryPointStackFrame(
            int entryPointAddress,
            ClosureContext closureScope,
            DynValue function
        )
        {
            // RET cleanup expects the CLR entry layout: function slot followed by argument count.
            // Stack-level debug/getfenv paths read the frame metadata and closure scope instead.
            _valueStack.Push(DynValue.Void);
            _valueStack.Push(DynValue.FromNumber(0));

            CallStackItem frame = CallStackItemPool.Rent();
            frame.BasePointer = _valueStack.Count;
            frame.DebugEntryPoint = entryPointAddress;
            frame.ReturnAddress = -1;
            frame.ClosureScope = closureScope;
            frame.Function = function;
            frame.CallingSourceRef = SourceRef.GetClrLocation();
            frame.Flags = CallStackItemFlagsPresets.CallEntryPoint;
            _executionStack.Push(frame);
        }

        private int PushAdjustedArguments(ClrCallArguments args)
        {
            int count = args.Count;
            if (count == 0)
            {
                return 0;
            }

            for (int i = 0; i < count - 1; i++)
            {
                _valueStack.Push(args[i].ToScalar());
            }

            return PushAdjustedTrailingValue(args[count - 1], count - 1);
        }

        private int PushAdjustedTrailingValue(DynValue value, int pushedCount)
        {
            if (value.Type == DataType.Void)
            {
                return pushedCount;
            }

            if (value.Type != DataType.Tuple)
            {
                _valueStack.Push(value.ToScalar());
                return pushedCount + 1;
            }

            return PushAdjustedTrailingTuple(value.Tuple, pushedCount);
        }

        private int PushAdjustedTrailingTuple(DynValue[] tuple, int pushedCount)
        {
            int tupleLength = tuple.Length;
            if (tupleLength == 0)
            {
                return pushedCount;
            }

            for (int i = 0; i < tupleLength - 1; i++)
            {
                _valueStack.Push((tuple[i] ?? DynValue.Nil).ToScalar());
                pushedCount++;
            }

            return PushAdjustedTrailingValue(tuple[tupleLength - 1] ?? DynValue.Nil, pushedCount);
        }

        private int _owningThreadId = -1;
        private int _executionNesting;

        /// <summary>
        /// Unwinds processor bookkeeping and signals debugger listeners when execution ends.
        /// </summary>
        private void LeaveProcessor()
        {
            _executionNesting -= 1;
            bool outermostLeave = _executionNesting == 0;

            try
            {
                if (_parent != null)
                {
                    _parent._coroutinesStack.RemoveAt(_parent._coroutinesStack.Count - 1);
                }

                if (
                    outermostLeave
                    && _debug != null
                    && _debug.DebuggerEnabled
                    && _debug.DebuggerAttached != null
                )
                {
                    _debug.DebuggerAttached.SignalExecutionEnded();
                }
            }
            finally
            {
                if (outermostLeave)
                {
                    Volatile.Write(ref _owningThreadId, -1);
                }
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

            if (_script.Options.CheckThreadAccess)
            {
                // Use atomic compare-exchange to prevent TOCTOU race conditions.
                // Try to claim ownership from unowned state (-1 -> threadId).
                int previousOwner = Interlocked.CompareExchange(ref _owningThreadId, threadId, -1);

                // If we didn't get -1, someone already owns the processor
                if (previousOwner != -1 && previousOwner != threadId)
                {
                    string msg = string.Format(
                        CultureInfo.InvariantCulture,
                        "Cannot enter the same NovaSharp processor from two different threads : {0} and {1}",
                        previousOwner,
                        threadId
                    );
                    throw new InvalidOperationException(msg);
                }
            }
            else
            {
                _owningThreadId = threadId;
            }

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
                CallStackItemPool.Return(_executionStack.Pop());
            }
        }
    }
}
