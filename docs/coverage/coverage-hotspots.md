# Coverage Hotspots (baseline: 2025-11-10)

Latest data sourced from `docs/coverage/latest/Summary.json` (generated via `./coverage.ps1`).

## Snapshot
- Overall line coverage: **60.3 %**
- NovaSharp.Interpreter line coverage: **71.9 %**
- NovaSharp.Cli line coverage: **72.2 %**
- NovaSharp.Hardwire line coverage: **22.4 %**
- NovaSharp.RemoteDebugger / NovaSharp.VsCodeDebugger: **0 %** (no tests yet)

## Prioritized Red List (Interpreter < 90 %)

| Class | Line % | Branch % | Covered / Coverable | Owner | Notes |
|-------|-------:|---------:|--------------------:|-------|-------|
| `NovaSharp.Interpreter.REPL.ReplInterpreterScriptLoader` | 0.0 | 0.0 | 0 / 64 | Tooling | Add REPL loader tests for inline script execution and error propagation. |
| `NovaSharp.Interpreter.DataStructs.FastStackDynamic<T>` | 0.0 | 0.0 | 0 / 55 | Runtime | Exercise dynamic stack resize logic via unit tests to replace reflection-based call sites. |
| `NovaSharp.Interpreter.Serialization.SerializationExtensions` | 0.0 | 0.0 | 0 / 68 | Runtime | Roundtrip tests landed; rerun coverage to capture the new baseline and confirm Lua formatter output. |
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredUserDataDescriptor` | 0.0 | – | 0 / 84 | Interop | Cover descriptor invocation paths ahead of Roslyn generator replacement. |
| `NovaSharp.Interpreter.CoreLib.OsTimeModule` | 0.0 | 0.0 | 0 / 96 | Runtime | New NUnit regression tests exercise `os.time`, `os.difftime`, and `os.date`; rerun coverage to capture the lift. |
| `NovaSharp.Interpreter.Loaders.UnityAssetsScriptLoader` | 0.0 | 0.0 | 0 / 52 | Tooling | Unit tests now cover dictionary-backed loading, path trimming, errors, and enumeration; rerun coverage to record improvements. |
| `NovaSharp.Interpreter.LinqHelpers` | 0.0 | 0.0 | 0 / 19 | Runtime | LINQ-free iterators landed with unit tests; rerun coverage to record the bump and verify analyzer enforcement potential. |
| `NovaSharp.Interpreter.Interop.Attributes.NovaSharpHideMemberAttribute` | 0.0 | – | 0 / 12 | Interop | Tests now verify hidden methods/properties (with inheritance). Refresh coverage snapshot to mark complete. |

## Yellow List (line 50–89 %)
- `NovaSharp.Interpreter.CoreLib.MathModule` – 55 % line, 40 % branch. Fresh tests now cover logarithms, power, modf, min/max, ldexp, deterministic random sequences, and NaN/overflow behaviors. Run `./coverage.ps1` to capture the uplift.
- `NovaSharp.Interpreter.Tree.Expressions.FunctionDefinitionExpression` – 63 % line. Add parser/compiler unit tests covering variadic arguments + local closures.
- `NovaSharp.Interpreter.Execution.Processors.Processor` – 71 % line. Expand VM opcode coverage, especially coroutine resume/yield sequences.
- `NovaSharp.Commands.Program` – 57 % line. Already in CLI suite; add tests for error exit codes and argument parsing of `--interactive`.

(Review full list in `docs/coverage/latest/Summary.json`.)

## Action Items
1. Assign owners for each red-listed class (default owner noted above until explicit assignment).
2. Add issue/project board entries mirroring this table so progress is tracked.
3. Update this document after each `./coverage.ps1` run (include new timestamp + notes).
4. When a class crosses 90 %, move it to the green archive section (to be added) and celebrate the win.

## Recently Covered
- `PerformanceStopwatch`, `GlobalPerformanceStopwatch`, and `DummyPerformanceStopwatch` now covered by dedicated stopwatch unit tests.
- `PerformanceStatistics` exercises enabling/disabling counters and global aggregation.
- `ReplHistoryInterpreter` navigation (prev/next) verified via tests.
- Platform accessors (`LimitedPlatformAccessor`, `StandardPlatformAccessor`) guarded with sandbox/full IO tests.
- `EmbeddedResourcesScriptLoader` validated against embedded Lua fixture.
- `InternalErrorException` constructors covered by direct unit tests.
- `SerializationExtensions` exercised with prime/nested table scenarios and tuple/string escaping; serializer fixed to emit Lua-compliant braces/newlines.

## Updating the Snapshot
```powershell
./coverage.ps1
# Copy docs/coverage/latest/Summary.json entries into the tables above.
```

_Last updated: 2025-11-10_
