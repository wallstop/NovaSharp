# Vestigial Component Inventory (Initial Pass)

Date: 2025-11-10  
Source snapshot: `docs/coverage/latest/Summary.json` + code inspection

## Summary

| Component | Location | Observations | Recommendation |
|-----------|----------|--------------|----------------|
| `PerformanceStopwatch`, `GlobalPerformanceStopwatch`, `DummyPerformanceStopwatch` | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceCounters/*.cs` | Actively referenced via `PerformanceStatistics` (see `PerformanceStatistics.cs:10-88`) and the fast loader/VM hot paths (`LoaderFast.cs`, `Processor.cs`). Provides optional instrumentation when `PerformanceStats.Enabled = true`. | Keep. Consider future upgrade to `System.Diagnostics.Metrics` once we target net8+ exclusively, but no removal at this time. Ensure tests cover behavior (new coverage added in `PerformanceStopwatchTests`). |
| `PerformanceStatistics` | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceStatistics.cs` | Constructed by `Script` (`Script.cs:80`), but instrumentation is opt-in. Supports REPL/CLI perf logging. | Keep. Document usage in developer guide; consider exposing a public hook to attach modern metrics exporters. |
| `ReplHistoryInterpreter` | `src/runtime/NovaSharp.Interpreter/REPL/ReplHistoryNavigator.cs` | No in-repo references to `new ReplHistoryInterpreter(...)`. CLI uses the base `ReplInterpreter`. Appears vestigial legacy feature. | Candidate for removal or relocation to samples. Confirm no downstream consumers; if none, delete and note in release notes. |
| `ReplInterpreterScriptLoader` | `src/runtime/NovaSharp.Interpreter/REPL/ReplInterpreterScriptLoader.cs` | Assigned in CLI (`src/tooling/NovaSharp.Cli/Program.cs:19`) and used in tests/Tutorials. | Keep. Update docs to clarify how REPL loader works. |
| Platform accessors (`LimitedPlatformAccessor`, `StandardPlatformAccessor`) | `src/runtime/NovaSharp.Interpreter/Platforms/*.cs` | Auto-detected via `PlatformAutoDetector` and used in samples. Still relevant for sandboxed scripts. | Keep. Future modernization: use `System.IO.Abstractions` or DI-friendly abstractions. |
| Script loaders (`EmbeddedResourcesScriptLoader`, `UnityAssetsScriptLoader`) | `src/runtime/NovaSharp.Interpreter/Loaders/*.cs` | Referenced by samples/tests. Provide netstandard-friendly file loading. | Keep. Need coverage to guard regressions. |
| `PerformanceCounters` enum values targeting adapter compilation | `src/runtime/NovaSharp.Interpreter/Diagnostics/PerformanceCounter.cs` | Referenced by `PerformanceStatistics` and reflection descriptors. Still used to measure adapter generation. | Keep. |

## Next Steps
1. Verify with maintainers whether `ReplHistoryInterpreter` has external consumers; if not, remove or move to samples to trim core runtime surface.
2. Track follow-up modernization ideas (e.g., replace custom performance counters with BCL metrics once we bump minimum target).
3. Add coverage tasks for remaining zero-coverage classes (Global/Dummy stopwatch, REPL history) so we can confidently refactor or delete.
4. Re-run this inventory after each coverage milestone to ensure no new vestigial code creeps in.

_Prepared by: Codex automation, 2025-11-10_
