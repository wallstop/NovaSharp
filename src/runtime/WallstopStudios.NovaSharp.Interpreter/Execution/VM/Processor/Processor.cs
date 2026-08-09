namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using global::NovaSharp;
    using Debugging;
    using Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Executes bytecode for a script, coordinating stacks, coroutines, and debugger integrations.
    /// </summary>
    internal sealed partial class Processor
    {
        private readonly ByteCode _rootChunk;

        private readonly FastStack<LuaValue> _valueStack;
        private readonly FastStack<CallStackItem> _executionStack;
        private List<Processor> _coroutinesStack;

        private Table _globalTable;
        private readonly Script _script;
        private readonly Processor _parent;
        private CoroutineState _state;
        private bool _canYield = true;
        private int _savedInstructionPtr = -1;
        private readonly DebugContext _debug;
        private LuaValue _lastCloseError = LuaValue.Nil;
        private int _errorHandlerBeforeUnwindScanBoundaryDepth = -1;

        private readonly ref struct ClrCallArguments
        {
            private readonly LuaValue[] _array;
            private readonly ReadOnlySpan<LuaValue> _span;
            private readonly LuaValue _arg0;
            private readonly LuaValue _arg1;
            private readonly LuaValue _arg2;
            private readonly LuaValue _arg3;
            private readonly LuaValue _arg4;
            private readonly LuaValue _arg5;
            private readonly LuaValue _arg6;
            private readonly int _count;
            private readonly bool _hasSpan;

            internal ClrCallArguments(LuaValue[] args)
            {
                _array = args;
                _span = default;
                _arg0 = default;
                _arg1 = default;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = args != null ? args.Length : 0;
                _hasSpan = false;
            }

            internal ClrCallArguments(ReadOnlySpan<LuaValue> args)
            {
                _array = null;
                _span = args;
                _arg0 = default;
                _arg1 = default;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = args.Length;
                _hasSpan = true;
            }

            internal ClrCallArguments(LuaValue arg)
            {
                _array = null;
                _span = default;
                _arg0 = arg;
                _arg1 = default;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 1;
                _hasSpan = false;
            }

            internal ClrCallArguments(LuaValue arg1, LuaValue arg2)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 2;
                _hasSpan = false;
            }

            internal ClrCallArguments(LuaValue arg1, LuaValue arg2, LuaValue arg3)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 3;
                _hasSpan = false;
            }

            internal ClrCallArguments(LuaValue arg1, LuaValue arg2, LuaValue arg3, LuaValue arg4)
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 4;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5
            )
            {
                _array = null;
                _span = default;
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = default;
                _arg6 = default;
                _count = 5;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5,
                LuaValue arg6
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
                _arg6 = default;
                _count = 6;
                _hasSpan = false;
            }

            internal ClrCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5,
                LuaValue arg6,
                LuaValue arg7
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

            internal LuaValue this[int index]
            {
                get
                {
                    LuaValue value;
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

                    return value;
                }
            }

            /// <summary>
            /// Creates the coroutine resume tuple, reusing array-backed caller arguments when available.
            /// </summary>
            internal LuaValue ToTuple()
            {
                if (_array != null)
                {
                    return LuaValue.NewTuple(_array);
                }

                if (_hasSpan)
                {
                    return CreateTupleFromSpan(_span);
                }

                switch (_count)
                {
                    case 0:
                        return LuaValue.EmptyTuple;
                    case 1:
                        return LuaValue.NewTuple(this[0]);
                    case 2:
                        return LuaValue.NewTuple(this[0], this[1]);
                    case 3:
                        return LuaValue.NewTuple(this[0], this[1], this[2]);
                    case 4:
                        return LuaValue.NewTuple(this[0], this[1], this[2], this[3]);
                    case 5:
                        return LuaValue.NewTuple(this[0], this[1], this[2], this[3], this[4]);
                    case 6:
                        LuaValue[] fixedValues =
                        {
                            this[0],
                            this[1],
                            this[2],
                            this[3],
                            this[4],
                            this[5],
                        };
                        return LuaValue.NewTuple(fixedValues);
                    case 7:
                        LuaValue[] fixedSevenValues =
                        {
                            this[0],
                            this[1],
                            this[2],
                            this[3],
                            this[4],
                            this[5],
                            this[6],
                        };
                        return LuaValue.NewTuple(fixedSevenValues);
                    default:
                        LuaValue[] values = new LuaValue[_count];
                        for (int i = 0; i < _count; i++)
                        {
                            values[i] = this[i];
                        }

                        return LuaValue.NewTuple(values);
                }
            }

            private static LuaValue CreateTupleFromSpan(ReadOnlySpan<LuaValue> values)
            {
                switch (values.Length)
                {
                    case 0:
                        return LuaValue.EmptyTuple;
                    case 1:
                        return LuaValue.NewTuple(values[0]);
                    case 2:
                        return LuaValue.NewTuple(values[0], values[1]);
                    case 3:
                        return LuaValue.NewTuple(values[0], values[1], values[2]);
                    case 4:
                        return LuaValue.NewTuple(values[0], values[1], values[2], values[3]);
                    case 5:
                        return LuaValue.NewTuple(
                            values[0],
                            values[1],
                            values[2],
                            values[3],
                            values[4]
                        );
                }

                LuaValue[] copiedValues = new LuaValue[values.Length];
                values.CopyTo(copiedValues);

                return LuaValue.NewTuple(copiedValues);
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
            _valueStack = new FastStack<LuaValue>(
                VmStackDefaults.ValueStackInitialCapacity,
                script.Options.MaxVmValueStackSize
            );
            _executionStack = new FastStack<CallStackItem>(
                VmStackDefaults.ExecutionStackInitialCapacity,
                script.Options.MaxVmCallStackSize
            );
            _coroutinesStack = new List<Processor>();

            _debug = new DebugContext();
            _rootChunk = byteCode;
            _globalTable = globalContext;
            _script = script;
            _state = CoroutineState.Main;
            LuaValue.NewCoroutine(new Coroutine(this)); // creates an associated coroutine for the main processor
        }

        /// <summary>
        /// Creates a child processor that shares the parent's runtime state.
        /// </summary>
        private Processor(Processor parentProcessor)
        {
            // Inherit the ceilings baked into the parent's stacks (ultimately the main processor's, captured
            // at script creation) so every coroutine under a script shares one limit even if ScriptOptions is
            // mutated after the main processor was built.
            _valueStack = new FastStack<LuaValue>(
                VmStackDefaults.ValueStackInitialCapacity,
                parentProcessor._valueStack.MaxCapacity
            );
            _executionStack = new FastStack<CallStackItem>(
                VmStackDefaults.ExecutionStackInitialCapacity,
                parentProcessor._executionStack.MaxCapacity
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
        public LuaValue Call(LuaValue function, LuaValue[] args)
        {
            return Call(function, new ClrCallArguments(args));
        }

        /// <summary>
        /// Invokes a compiled chunk entry point with a fresh closure context.
        /// </summary>
        /// <param name="entryPointAddress">Instruction pointer for the chunk entry point.</param>
        /// <param name="closureScope">Closure context containing the chunk's environment upvalue.</param>
        /// <returns>The return tuple.</returns>
        internal LuaValue CallChunk(int entryPointAddress, ClosureContext closureScope)
        {
            return CallChunkCore(entryPointAddress, closureScope, hasFunction: false, LuaValue.Nil);
        }

        /// <summary>
        /// Invokes a Lua function with no arguments, running the VM until the call completes or throws.
        /// </summary>
        /// <param name="function">Function to invoke.</param>
        /// <returns>The return tuple.</returns>
        internal LuaValue CallFunctionWithoutArguments(LuaValue function)
        {
            if (function.Type != DataType.Function)
            {
                throw new ArgumentException("Value must be a Lua function.", nameof(function));
            }

            Closure closure = function.Function;
            return CallChunkCore(
                closure.EntryPointByteCodeLocation,
                closure.ClosureContext,
                hasFunction: true,
                function
            );
        }

        private LuaValue CallChunkCore(
            int entryPointAddress,
            ClosureContext closureScope,
            bool hasFunction,
            LuaValue function
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
                return coroutinesStack[^1]
                    .CallChunkCore(entryPointAddress, closureScope, hasFunction, function);
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
                    PushChunkEntryPointStackFrame(
                        entryPointAddress,
                        closureScope,
                        hasFunction,
                        function
                    );
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
        public LuaValue Call(LuaValue function, ReadOnlySpan<LuaValue> args)
        {
            return Call(function, new ClrCallArguments(args));
        }

        /// <summary>
        /// Invokes the specified function with one argument.
        /// </summary>
        public LuaValue Call(LuaValue function, LuaValue arg)
        {
            return Call(function, new ClrCallArguments(arg));
        }

        /// <summary>
        /// Invokes the specified function with two arguments.
        /// </summary>
        public LuaValue Call(LuaValue function, LuaValue arg1, LuaValue arg2)
        {
            return Call(function, new ClrCallArguments(arg1, arg2));
        }

        /// <summary>
        /// Invokes the specified function with three arguments.
        /// </summary>
        public LuaValue Call(LuaValue function, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3));
        }

        /// <summary>
        /// Invokes the specified function with four arguments.
        /// </summary>
        public LuaValue Call(
            LuaValue function,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Invokes the specified function with five arguments.
        /// </summary>
        public LuaValue Call(
            LuaValue function,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Invokes the specified function with six arguments.
        /// </summary>
        public LuaValue Call(
            LuaValue function,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Invokes the specified function with seven arguments.
        /// </summary>
        public LuaValue Call(
            LuaValue function,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            return Call(function, new ClrCallArguments(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        private LuaValue Call(LuaValue function, ClrCallArguments args)
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

        /// <summary>
        /// Pushes the stack frame metadata needed to transition from CLR into Lua code.
        /// </summary>
        /// <param name="Flags">Flags describing the call entry point.</param>
        /// <param name="function">Function being invoked.</param>
        /// <param name="args">Arguments to copy.</param>
        /// <returns>The instruction pointer to start executing.</returns>
        private int PushClrToScriptStackFrame(
            CallStackItemFlags Flags,
            LuaValue function,
            ClrCallArguments args
        )
        {
            return PushClrToScriptStackFrameCore(Flags, hasFunction: true, function, args);
        }

        /// <summary>
        /// Pushes a CLR-to-script frame when the function is already at the top of the value stack.
        /// </summary>
        private int PushClrToScriptStackFrame(CallStackItemFlags Flags, ClrCallArguments args)
        {
            return PushClrToScriptStackFrameCore(Flags, hasFunction: false, LuaValue.Nil, args);
        }

        private int PushClrToScriptStackFrameCore(
            CallStackItemFlags Flags,
            bool hasFunction,
            LuaValue function,
            ClrCallArguments args
        )
        {
            // This entry setup runs outside the instruction loop's unwind, so a stack overflow thrown here
            // must leave the value stack pristine: a CLR pcall/xpcall target can catch the error, after which
            // orphaned slots would be popped in place of real call args and corrupt later execution.
            // RentCallFrame() checks the execution-stack ceiling before renting, so nothing is ever rented on
            // the overflow path; only the value slots pushed below need rolling back.
            int valueStackBaseline = _valueStack.Count;
            try
            {
                if (!hasFunction)
                {
                    function = _valueStack.Peek();
                }
                else
                {
                    _valueStack.Push(function); // func val
                }

                int argCount = PushAdjustedArguments(args);
                _valueStack.Push(LuaValue.FromNumber(argCount)); // func args count

                CallStackItem frame = RentCallFrame();
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
            catch (ScriptRuntimeException)
            {
                _valueStack.CropAtCount(valueStackBaseline);
                throw;
            }
        }

        private void PushChunkEntryPointStackFrame(
            int entryPointAddress,
            ClosureContext closureScope,
            bool hasFunction,
            LuaValue function
        )
        {
            // RET cleanup expects the CLR entry layout: function slot followed by argument count.
            // Stack-level debug/getfenv paths read the frame metadata and closure scope instead.
            // As in PushClrToScriptStackFrame, this runs outside the loop's unwind, so roll the value slots
            // back if a stack overflow is thrown during setup (RentCallFrame never rents on that path).
            int valueStackBaseline = _valueStack.Count;
            try
            {
                _valueStack.Push(LuaValue.Void);
                _valueStack.Push(LuaValue.FromNumber(0));

                CallStackItem frame = RentCallFrame();
                frame.BasePointer = _valueStack.Count;
                frame.DebugEntryPoint = entryPointAddress;
                frame.ReturnAddress = -1;
                frame.ClosureScope = closureScope;
                frame.Function = hasFunction ? function : LuaValue.Nil;
                frame.CallingSourceRef = SourceRef.GetClrLocation();
                frame.Flags = CallStackItemFlagsPresets.CallEntryPoint;
                _executionStack.Push(frame);
            }
            catch (ScriptRuntimeException)
            {
                _valueStack.CropAtCount(valueStackBaseline);
                throw;
            }
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

        private int PushAdjustedTrailingValue(LuaValue value, int pushedCount)
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

        private int PushAdjustedTrailingTuple(LuaValue[] tuple, int pushedCount)
        {
            int tupleLength = tuple.Length;
            if (tupleLength == 0)
            {
                return pushedCount;
            }

            for (int i = 0; i < tupleLength - 1; i++)
            {
                _valueStack.Push(tuple[i].ToScalar());
                pushedCount++;
            }

            return PushAdjustedTrailingValue(tuple[tupleLength - 1], pushedCount);
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
