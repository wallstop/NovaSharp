namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Expressions;

    internal static class ProcessorDebuggerTestHelpers
    {
        internal static void PrepareCallStack(Processor processor, int frameCount = 1)
        {
            processor.ClearCallStackForTests();

            for (int i = 0; i < frameCount; i++)
            {
                processor.PushCallStackFrameForTests(
                    new CallStackItem()
                    {
                        DebugEntryPoint = 0,
                        CallingSourceRef = SourceRef.GetClrLocation(),
                    }
                );
            }
        }

        internal static SourceRef GetFirstBreakableSourceRef(Script script)
        {
            SourceCode source = script.GetSourceCode(0);
            return source.Refs.First(r => !r.CannotBreakpoint);
        }

        internal sealed class ThrowingExpression : Expression
        {
            private readonly string _message;

            public ThrowingExpression(ScriptLoadingContext context, string message)
                : base(context)
            {
                _message = message;
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                throw new InvalidOperationException(_message);
            }

            public override void Compile(ByteCode bc)
            {
                throw new NotSupportedException("ThrowingExpression cannot compile.");
            }
        }

        internal sealed class StubDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction> _actions = new();

            public bool PauseRequested { get; set; }
            public bool SignalRuntimeExceptionResult { get; set; }
            public bool PauseAfterNextUpdate { get; set; }

            public DebuggerCaps Caps { get; set; } =
                DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;

            public int UpdateCallCount { get; private set; }
            public int RefreshBreakpointsCallCount { get; private set; }
            public int SignalRuntimeExceptionCallCount { get; private set; }
            public List<DynamicExpression> WatchItems { get; } = new();
            public Dictionary<WatchType, List<WatchItem>> LastWatchUpdates { get; } = new();

            public void EnqueueAction(DebuggerAction.ActionType actionType)
            {
                _actions.Enqueue(new DebuggerAction() { Action = actionType });
            }

            public void EnqueueAction(DebuggerAction action)
            {
                _actions.Enqueue(action);
            }

            public DebuggerCaps GetDebuggerCaps()
            {
                return Caps;
            }

            public void SetDebugService(DebugService debugService) { }

            public void SetSourceCode(SourceCode sourceCode) { }

            public void SetByteCode(string[] byteCode) { }

            public bool IsPauseRequested()
            {
                return PauseRequested;
            }

            public bool SignalRuntimeException(ScriptRuntimeException ex)
            {
                SignalRuntimeExceptionCallCount++;
                return SignalRuntimeExceptionResult;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                if (_actions.Count == 0)
                {
                    return new DebuggerAction() { Action = DebuggerAction.ActionType.Run };
                }

                return _actions.Dequeue();
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items)
            {
                UpdateCallCount++;
                LastWatchUpdates[watchType] = items.ToList();
                if (PauseAfterNextUpdate)
                {
                    PauseAfterNextUpdate = false;
                    PauseRequested = true;
                }
            }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return WatchItems.Select(expr => expr).ToList();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
            {
                RefreshBreakpointsCallCount++;
            }
        }
    }
}
