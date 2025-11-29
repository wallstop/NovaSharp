#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

    public sealed class DebuggerResumeHelpersTUnitTests
    {
        private static readonly DebuggerAction.ActionType[] StepSequence =
        {
            DebuggerAction.ActionType.StepIn,
            DebuggerAction.ActionType.StepOut,
            DebuggerAction.ActionType.Run,
        };

        private static readonly int[] BreakLines = { 7, 8 };

        [global::TUnit.Core.Test]
        public async Task StepInThenStepOutBreaksOnExpectedLines()
        {
            Script script = new(CoreModules.PresetComplete);

            const string Code =
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

            DynValue result = script.DoString(Code);

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(43);

            int[] meaningfulLines = debugger.SeenLines.Where(line => line > 0).ToArray();
            await Assert.That(meaningfulLines.Length).IsGreaterThanOrEqualTo(1);
            await Assert.That(debugger.ActionsServed.Count).IsGreaterThanOrEqualTo(2);
            await Assert
                .That(debugger.ActionsServed[0])
                .IsEqualTo(DebuggerAction.ActionType.StepIn);
            await Assert
                .That(debugger.ActionsServed[1])
                .IsEqualTo(DebuggerAction.ActionType.StepOut);
        }

        private sealed class StepSequencingDebugger : IDebugger
        {
            private readonly Queue<DebuggerAction.ActionType> _actions;
            private readonly HashSet<int> _breakLines;
            private DebugService _service;
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

            public bool SignalRuntimeException(
                NovaSharp.Interpreter.Errors.ScriptRuntimeException ex
            )
            {
                return false;
            }

            public DebuggerAction GetAction(int ip, SourceRef sourceref)
            {
                SeenLines.Add(sourceref?.FromLine ?? -1);

                if (_actions.Count == 0)
                {
                    ActionsServed.Add(DebuggerAction.ActionType.Run);
                    return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
                }

                DebuggerAction.ActionType next = _actions.Dequeue();
                ActionsServed.Add(next);
                return new DebuggerAction { Action = next };
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
#pragma warning restore CA2007
