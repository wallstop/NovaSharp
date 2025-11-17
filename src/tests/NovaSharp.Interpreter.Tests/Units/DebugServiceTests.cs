#nullable enable
namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebugServiceTests
    {
        [Test]
        public void ResetBreakPointsMarksExistingLinesAndReturnsFilteredSet()
        {
            Script script = new(CoreModules.PresetComplete);
            BreakpointRecordingDebugger debugger = new();

            script.AttachDebugger(debugger);
            script.DebuggerEnabled = true;

            script.DoString(
                @"
                local a = 1
                local b = 2
                if a < b then
                    return b - a
                end
                return a + b
            "
            );

            Assert.That(debugger.DebugService, Is.Not.Null, "Debugger never received DebugService.");
            Assert.That(debugger.DebugService.OwnerScript, Is.SameAs(script));
            Assert.That(debugger.LastSourceCode, Is.Not.Null, "Debugger never received source code.");

            HashSet<int> applied = debugger.DebugService.ResetBreakPoints(
                debugger.LastSourceCode!,
                new HashSet<int>(debugger.RequestedBreakpoints)
            );

            Assert.That(applied.Count, Is.GreaterThan(0));
            Assert.That(
                applied,
                Is.SubsetOf(debugger.RequestedBreakpoints),
                "ResetBreakPoints should ignore lines with no SourceRef."
            );

            HashSet<int> flaggedLines = debugger.LastSourceCode!.Refs
                .Where(r => r.Breakpoint)
                .Select(r => r.FromLine)
                .ToHashSet();

            Assert.That(flaggedLines, Is.EquivalentTo(applied));
        }

        private sealed class BreakpointRecordingDebugger : IDebugger
        {
            public HashSet<int> RequestedBreakpoints { get; } = new(new[] { 2, 3, 5, 99 });
            public DebugService DebugService { get; private set; } = null!;
            public SourceCode? LastSourceCode { get; private set; }
            private SourceCode? _pendingSourceCode;

            public DebuggerCaps GetDebuggerCaps()
            {
                return DebuggerCaps.HasLineBasedBreakpoints;
            }

            public void SetDebugService(DebugService debugService)
            {
                DebugService = debugService;
                TryCapturePendingSource();
            }

            public void SetSourceCode(SourceCode sourceCode)
            {
                if (LastSourceCode != null)
                {
                    return;
                }

                if (DebugService == null)
                {
                    _pendingSourceCode = sourceCode;
                    return;
                }

                if (sourceCode.OwnerScript != DebugService.OwnerScript)
                {
                    return;
                }

                LastSourceCode = sourceCode;
            }

            private void TryCapturePendingSource()
            {
                if (DebugService == null || _pendingSourceCode == null)
                {
                    return;
                }

                if (_pendingSourceCode.OwnerScript != DebugService.OwnerScript)
                {
                    return;
                }

                LastSourceCode = _pendingSourceCode;
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
                return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

            public List<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
#nullable disable
