namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerBreakpointTests
    {
        [Test]
        public void DebuggerActionsUpdateBreakpointsAndRefreshes()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                sharedValue = 3
                function target()
                    local localValue = 2
                    return localValue + sharedValue
                end
            "
            );

            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);
            SourceRef breakable = source.Refs.First(sr => !sr.CannotBreakpoint);

            Queue<DebuggerAction> actions = new();
            actions.Enqueue(
                new DebuggerAction
                {
                    Action = DebuggerAction.ActionType.SetBreakpoint,
                    SourceId = source.SourceId,
                    SourceLine = breakable.FromLine,
                    SourceCol = breakable.FromChar,
                }
            );
            actions.Enqueue(
                new DebuggerAction
                {
                    Action = DebuggerAction.ActionType.ToggleBreakpoint,
                    SourceId = source.SourceId,
                    SourceLine = breakable.FromLine,
                    SourceCol = breakable.FromChar,
                }
            );
            actions.Enqueue(
                new DebuggerAction
                {
                    Action = DebuggerAction.ActionType.ClearBreakpoint,
                    SourceId = source.SourceId,
                    SourceLine = breakable.FromLine,
                    SourceCol = breakable.FromChar,
                }
            );
            actions.Enqueue(
                new DebuggerAction
                {
                    Action = DebuggerAction.ActionType.ResetBreakpoints,
                    SourceId = source.SourceId,
                    Lines = new[] { breakable.FromLine },
                }
            );
            actions.Enqueue(
                new DebuggerAction
                {
                    Action = DebuggerAction.ActionType.ToggleBreakpoint,
                    SourceId = source.SourceId,
                    SourceLine = breakable.FromLine + 250,
                    SourceCol = breakable.FromChar,
                }
            );
            actions.Enqueue(new DebuggerAction { Action = DebuggerAction.ActionType.Run });

            BreakpointDebugger debugger = new(actions);
            script.AttachDebugger(debugger);
            script.DebuggerEnabled = true;

            DynValue result = script.Call(script.Globals.Get("target"));
            Assert.That(result.Number, Is.EqualTo(5));

            Assert.That(debugger.BreakpointSnapshots.Count, Is.GreaterThan(0));
            Assert.That(
                debugger.BreakpointSnapshots.Any(snapshot => snapshot.Count > 0),
                Is.True,
                "No breakpoint refresh ever reported an active breakpoint."
            );
        }

        private sealed class BreakpointDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction> _actions;

            public BreakpointDebugger(Queue<DebuggerAction> actions)
            {
                _actions = actions;
            }

            public List<IReadOnlyList<SourceRef>> BreakpointSnapshots { get; } = new();

            public DebuggerCaps GetDebuggerCaps()
            {
                return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
            }

            public void SetDebugService(DebugService debugService) { }

            public void SetSourceCode(SourceCode sourceCode) { }

            public void SetByteCode(string[] byteCode) { }

            public bool IsPauseRequested()
            {
                return false;
            }

            public bool SignalRuntimeException(ScriptRuntimeException ex)
            {
                return false;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                if (_actions.Count == 0)
                {
                    return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
                }

                return _actions.Dequeue();
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
            {
                BreakpointSnapshots.Add(refs.ToList());
            }
        }
    }
}
