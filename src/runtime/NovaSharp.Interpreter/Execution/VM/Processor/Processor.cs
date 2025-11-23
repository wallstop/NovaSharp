namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;

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

        internal bool CanYield
        {
            get { return _canYield; }
        }

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

        //Takes the value and execution stack from recycleProcessor
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

        private static int GetThreadId()
        {
#if ENABLE_DOTNET || NETFX_CORE
            return 1;
#else
            return Environment.CurrentManagedThreadId;
#endif
        }

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

        internal SourceRef GetCoroutineSuspendedLocation()
        {
            return GetCurrentSourceRef(_savedInstructionPtr);
        }

        internal void ForceStateForTests(CoroutineState state)
        {
            _state = state;
        }

        internal void PushCallStackFrameForTests(CallStackItem frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            _executionStack.Push(frame);
        }

        internal void ClearCallStackForTests()
        {
            while (_executionStack.Count > 0)
            {
                _executionStack.Pop();
            }
        }
    }
}
