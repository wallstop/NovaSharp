namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Debugging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class DebuggerRefreshTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task HardRefreshCapturesStackLocalsAndWatchValues(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            await Assert
                .That(debugger.Updates.ContainsKey(WatchType.CallStack))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(
                    debugger
                        .Updates[WatchType.CallStack]
                        .Any(snapshot =>
                            snapshot.Any(item =>
                                item.Name != null && ContainsOrdinal(item.Name, "target")
                            )
                        )
                )
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(debugger.Updates.ContainsKey(WatchType.Locals))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(debugger.Updates[WatchType.Locals].Count)
                .IsGreaterThan(0)
                .ConfigureAwait(false);

            await Assert
                .That(debugger.Updates.ContainsKey(WatchType.Watches))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(
                    debugger
                        .Updates[WatchType.Watches]
                        .Any(snapshot =>
                            snapshot.Any(item =>
                                item.Name == "sharedValue"
                                && item.Value != null
                                && item.Value.Type == DataType.Number
                                && item.Value.Number == 99
                            )
                        )
                )
                .IsTrue()
                .ConfigureAwait(false);

            await Assert
                .That(debugger.Updates.ContainsKey(WatchType.VStack))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(debugger.Updates[WatchType.VStack].Any(snapshot => snapshot.Count > 0))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(debugger.Updates.ContainsKey(WatchType.Threads))
                .IsTrue()
                .ConfigureAwait(false);
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
