namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerResumeHelpersTests
    {
        private static readonly DebuggerAction.ActionType[] StepSequence =
        {
            DebuggerAction.ActionType.StepIn,
            DebuggerAction.ActionType.StepOut,
            DebuggerAction.ActionType.Run,
        };

        private static readonly int[] BreakLines = { 7, 8 };

        [Test]
        public void StepInThenStepOutBreaksOnExpectedLines()
        {
            Script script = new(CoreModules.PresetComplete);

            string code =
                @"function callee()
  local inside = 42
  return inside
end

function caller()
  local before = callee()
  local after = before + 1
  return after
end

return caller()
";

            StepSequencingDebugger debugger = new(StepSequence, BreakLines);

            script.AttachDebugger(debugger);
            script.DebuggerEnabled = true;

            DynValue result = script.DoString(code);

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(43));

            int[] meaningfulLines = debugger.SeenLines.Where(line => line > 0).ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(
                    meaningfulLines.Length,
                    Is.GreaterThanOrEqualTo(1),
                    "Debugger never reported a source line."
                );
                Assert.That(debugger.ActionsServed.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(
                    debugger.ActionsServed[0],
                    Is.EqualTo(DebuggerAction.ActionType.StepIn)
                );
                Assert.That(
                    debugger.ActionsServed[1],
                    Is.EqualTo(DebuggerAction.ActionType.StepOut)
                );
            });
        }

        private sealed class StepSequencingDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction.ActionType> _actions;
            private readonly HashSet<int> _breakLines;
            private DebugService _service = null!;
            private bool _breakpointsInstalled;

            public StepSequencingDebugger(
                IEnumerable<DebuggerAction.ActionType> actions,
                IEnumerable<int> breakLines
            )
            {
                _actions = new Queue<DebuggerAction.ActionType>(actions);
                _breakLines = new HashSet<int>(breakLines);
            }

            public List<int> SeenLines { get; } = new();
            public List<DebuggerAction.ActionType> ActionsServed { get; } = new();

            public DebuggerCaps GetDebuggerCaps()
            {
                return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
            }

            public void SetDebugService(DebugService debugService)
            {
                _service = debugService;
            }

            public void SetSourceCode(SourceCode sourceCode)
            {
                if (_breakpointsInstalled || _service == null)
                {
                    return;
                }

                if (sourceCode.OwnerScript == _service.OwnerScript && sourceCode.Name == "chunk")
                {
                    _service.ResetBreakpoints(sourceCode, _breakLines);
                    _breakpointsInstalled = true;
                }
            }

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
                SeenLines.Add(sourceref?.FromLine ?? -1);

                if (_actions.Count == 0)
                {
                    ActionsServed.Add(DebuggerAction.ActionType.Run);
                    return new DebuggerAction() { Action = DebuggerAction.ActionType.Run };
                }

                DebuggerAction.ActionType next = _actions.Dequeue();
                ActionsServed.Add(next);
                return new DebuggerAction() { Action = next };
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
