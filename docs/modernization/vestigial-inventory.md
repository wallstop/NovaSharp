# Vestigial Component Inventory — Historical Snapshot

> **Archive notice:** This inventory records an initial investigation made on
> 2025-11-10 from `docs/coverage/latest/Summary.json` and code inspection at that
> time. Its paths, coverage statements, and recommendations are not current
> backlog or removal authority. Re-run repository usage, behavior, coverage, and
> Lua/platform checks before changing any listed component.

The snapshot predates the `WallstopStudios.NovaSharp` directory/namespace
cutover. A path recorded as `src/runtime/NovaSharp.Interpreter/...` now maps to
`src/runtime/WallstopStudios.NovaSharp.Interpreter/...`; the old path below is
retained only to describe the source snapshot accurately.

## Observations Recorded on 2025-11-10

| Component                                                                         | Snapshot-era location                                                    | Observation recorded at the time                                                                                   | Recorded recommendation                                                   |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------- |
| `PerformanceStopwatch`, `GlobalPerformanceStopwatch`, `DummyPerformanceStopwatch` | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceCounters/*.cs` | Referenced through `PerformanceStatistics` and loader/VM instrumentation when performance statistics were enabled. | Keep; reconsider BCL metrics only after a future target-framework change. |
| `PerformanceStatistics`                                                           | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceStatistics.cs` | Constructed by `Script`; instrumentation was opt-in and used by performance logging.                               | Keep and improve developer documentation.                                 |
| `ReplHistoryInterpreter`                                                          | `src/runtime/NovaSharp.Interpreter/REPL/ReplHistoryNavigator.cs`         | No repository-owned construction was found, and the CLI used `ReplInterpreter`; it appeared potentially vestigial. | Re-investigate before removal or relocation.                              |
| `ReplInterpreterScriptLoader`                                                     | `src/runtime/NovaSharp.Interpreter/REPL/ReplInterpreterScriptLoader.cs`  | Used by the CLI, tests, and tutorials.                                                                             | Keep.                                                                     |
| Platform accessors                                                                | `src/runtime/NovaSharp.Interpreter/Platforms/*.cs`                       | Selected through `PlatformAutoDetector` and used in samples.                                                       | Keep.                                                                     |
| `EmbeddedResourcesScriptLoader`, `UnityAssetsScriptLoader`                        | `src/runtime/NovaSharp.Interpreter/Loaders/*.cs`                         | Referenced by samples and tests for portable resource/file loading.                                                | Keep.                                                                     |
| Adapter-compilation performance counters                                          | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceCounter.cs`    | Used by `PerformanceStatistics` and reflection-backed descriptors.                                                 | Keep.                                                                     |

## Known Later Evidence

The original coverage/removal suggestions are obsolete:

- Dedicated current tests exist for
  [`ReplHistoryInterpreter`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Cli/ReplHistoryInterpreterTUnitTests.cs),
  [`PerformanceStatistics`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Utilities/PerformanceStatisticsTUnitTests.cs),
  and the
  [`PerformanceStopwatch` implementations](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Utilities/PerformanceStopwatchTUnitTests.cs).
- Current loader and platform suites cover the retained implementations under
  [`Loaders`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Loaders/)
  and
  [`Platforms`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Platforms/).
- `ReplHistoryInterpreter` is still present and is now constructed by its test
  suite. That does not prove production demand or justify removal; it invalidates
  the snapshot's zero-usage/zero-coverage premise.

Any renewed vestigial-code investigation should create current evidence and put
selected work in `PLAN.md` or a GitHub issue. Do not treat the 2025 recommendations
as queued work, and do not retain a host API merely for hypothetical external
consumers if a new investigation proves it should be removed.
