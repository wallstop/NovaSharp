#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

    public sealed class DebugServiceTUnitTests
    {
        private const string SampleScript =
            @"
            local a = 1
            local b = 2
            if a < b then
                return b - a
            end
            return a + b
        ";

        [global::TUnit.Core.Test]
        public async Task ResetBreakpointsMarksExistingLinesAndReturnsFilteredSet()
        {
            (Script script, BreakpointRecordingDebugger debugger) = RunScriptAndAttachDebugger();

            DebugService service =
                debugger.DebugService
                ?? throw new InvalidOperationException("Debugger never received DebugService.");
            await Assert.That(service.OwnerScript).IsSameReferenceAs(script);

            SourceCode source =
                debugger.LastSourceCode
                ?? throw new InvalidOperationException("Debugger never received source code.");

            HashSet<int> requested = new(debugger.RequestedBreakpoints);
            HashSet<int> applied = service.ResetBreakpoints(source, requested);

            await Assert.That(applied.Count).IsGreaterThan(0);
            await Assert.That(applied.All(requested.Contains)).IsTrue();

            HashSet<int> flagged = source
                .Refs.Where(reference => reference.Breakpoint)
                .Select(reference => reference.FromLine)
                .ToHashSet();

            await Assert.That(flagged).IsEquivalentTo(applied);
        }

        [global::TUnit.Core.Test]
        public async Task ResetBreakpointsThrowsWhenSourceCodeIsNull()
        {
            (_, BreakpointRecordingDebugger debugger) = RunScriptAndAttachDebugger();
            DebugService service =
                debugger.DebugService
                ?? throw new InvalidOperationException("Debugger never received DebugService.");

            HashSet<int> lines = new(debugger.RequestedBreakpoints);
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                service.ResetBreakpoints(null, lines)
            );

            await Assert.That(exception.ParamName).IsEqualTo("src");
        }

        [global::TUnit.Core.Test]
        public async Task ResetBreakpointsThrowsWhenLinesSetIsNull()
        {
            (_, BreakpointRecordingDebugger debugger) = RunScriptAndAttachDebugger();
            DebugService service =
                debugger.DebugService
                ?? throw new InvalidOperationException("Debugger never received DebugService.");

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                service.ResetBreakpoints(debugger.LastSourceCode, null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("lines");
        }

        [global::TUnit.Core.Test]
        public async Task ResetBreakpointsThrowsWhenProcessorUnavailable()
        {
            Script script = new(CoreModules.PresetComplete);
            SourceCode source = new("chunk", "return 1", sourceId: 1, script);
            DebugService service = new(script, null);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                service.ResetBreakpoints(source, new HashSet<int> { 1 })
            );

            await Assert.That(exception.Message).Contains("processor");
        }

        private static (
            Script Script,
            BreakpointRecordingDebugger Debugger
        ) RunScriptAndAttachDebugger()
        {
            Script script = new(CoreModules.PresetComplete);
            BreakpointRecordingDebugger debugger = new();

            script.AttachDebugger(debugger);
            script.DebuggerEnabled = true;
            script.DoString(SampleScript);

            return (script, debugger);
        }

        private sealed class BreakpointRecordingDebugger : IDebugger
        {
            public HashSet<int> RequestedBreakpoints { get; } = new(new[] { 2, 3, 5, 99 });

            public DebugService DebugService { get; private set; }

            public SourceCode LastSourceCode { get; private set; }

            private SourceCode _pendingSourceCode;

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

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                return new List<DynamicExpression>();
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }
    }
}
#pragma warning restore CA2007
