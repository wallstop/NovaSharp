namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataTypes;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebuggerRefreshTests
    {
        [Test]
        public void HardRefreshCapturesStackLocalsAndWatchValues()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString(
                @"
                sharedValue = 99
                function target()
                    local localValue = 42
                    local other = 'value'
                    return localValue + 1
                end
            "
            );

            RecordingDebugger debugger = new();
            script.AttachDebugger(debugger);
            script.DebuggerEnabled = true;

            script.Call(script.Globals.Get("target"));

            Assert.That(
                debugger.Updates.ContainsKey(WatchType.CallStack),
                "Call stack snapshot missing"
            );
            Assert.That(
                debugger
                    .Updates[WatchType.CallStack]
                    .Any(snapshot =>
                        snapshot.Any(item =>
                            item.Name != null && ContainsOrdinal(item.Name, "target")
                        )
                    ),
                Is.True,
                "Target frame not found in call stack updates"
            );

            Assert.That(debugger.Updates.ContainsKey(WatchType.Locals), "Locals snapshot missing");
            Assert.That(
                debugger.Updates[WatchType.Locals].Count,
                Is.GreaterThan(0),
                "No locals snapshots were recorded"
            );

            Assert.That(
                debugger.Updates.ContainsKey(WatchType.Watches),
                "Watches snapshot missing"
            );
            Assert.That(
                debugger
                    .Updates[WatchType.Watches]
                    .Any(snapshot =>
                        snapshot.Any(item =>
                            item.Name == "sharedValue"
                            && item.Value != null
                            && item.Value.Type == DataType.Number
                            && item.Value.Number == 99
                        )
                    ),
                Is.True,
                "Watch expression did not resolve sharedValue"
            );

            Assert.That(debugger.Updates.ContainsKey(WatchType.VStack), "VStack snapshot missing");
            Assert.That(
                debugger.Updates[WatchType.VStack].Any(snapshot => snapshot.Count > 0),
                Is.True,
                "Value stack snapshot empty"
            );

            Assert.That(
                debugger.Updates.ContainsKey(WatchType.Threads),
                "Threads snapshot missing"
            );
        }

        private sealed class RecordingDebugger : IDebugger
        {
            private DebugService _service;
            private int _remainingRefreshes = 10;

            public Dictionary<WatchType, List<IReadOnlyList<WatchItem>>> Updates { get; } = new();

            public DebuggerCaps GetDebuggerCaps()
            {
                return DebuggerCaps.CanDebugSourceCode | DebuggerCaps.HasLineBasedBreakpoints;
            }

            public void SetDebugService(DebugService debugService)
            {
                _service = debugService;
            }

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
                if (_remainingRefreshes-- > 0)
                {
                    return new DebuggerAction { Action = DebuggerAction.ActionType.HardRefresh };
                }

                return new DebuggerAction { Action = DebuggerAction.ActionType.Run };
            }

            public void SignalExecutionEnded() { }

            public void Update(WatchType watchType, IEnumerable<WatchItem> items)
            {
                if (!Updates.TryGetValue(watchType, out List<IReadOnlyList<WatchItem>> snapshots))
                {
                    snapshots = new List<IReadOnlyList<WatchItem>>();
                    Updates[watchType] = snapshots;
                }

                snapshots.Add(items.ToList());
            }

            public IReadOnlyList<DynamicExpression> GetWatchItems()
            {
                if (_service == null)
                {
                    return new List<DynamicExpression>();
                }

                return new List<DynamicExpression>
                {
                    _service.OwnerScript.CreateDynamicExpression("sharedValue"),
                };
            }

            public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
        }

        private static bool ContainsOrdinal(string text, string value)
        {
            return text != null && text.Contains(value, StringComparison.Ordinal);
        }
    }
}
