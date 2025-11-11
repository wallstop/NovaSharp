# Coverage Hotspots (baseline: 2025-11-10)

Latest data sourced from `docs/coverage/latest/Summary.json` (generated via `./coverage.ps1`).

## Snapshot
- Overall line coverage: **63.1 %**
- NovaSharp.Interpreter line coverage: **75.4 %**
- NovaSharp.Cli line coverage: **72.2 %**
- NovaSharp.Hardwire line coverage: **22.4 %**
- NovaSharp.RemoteDebugger / NovaSharp.VsCodeDebugger: **0 %** (no tests yet)

## Prioritized Red List (Interpreter < 90 %)

| Class | Line % | Branch % | Covered / Coverable | Owner | Notes |
|-------|-------:|---------:|--------------------:|-------|-------|
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.DefaultValue` | 0.0 | – | 0 / 1 | Interop | Needs a focused unit to verify the sentinel emitted by the hardwire generator; currently dead code until we exercise defaulted CLR parameters. |
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMemberDescriptor` | 81.8 | – | 27 / 33 | Interop | Read/write paths covered; wrap up by validating restricted-type and by-ref conversions so instrumentation reaches ≥90 %. |
| `NovaSharp.Interpreter.Interop.ReflectionSpecialName` | 53.6 | 72.6 | 52 / 97 | Interop | Still lacking coverage for namespace-qualified operator names and fallback behaviour—add table-driven tests to close the gap. |
| `NovaSharp.Interpreter.CoreLib.DebugModule` | 76.6 | 65.7 | 92 / 120 | Runtime | Queued console tests landed; next step is to exercise multiline commands and coroutine-driven sessions to finish the interactive branch coverage. |
| `NovaSharp.Interpreter.CoreLib.IO.FileUserDataBase` | 24.1 | 19.2 | 21 / 87 | Runtime | Introduce tests for `Close`, `Flush`, `Seek`, and failure surfaces (IO exceptions, closed handles) using stubbed streams. |
| `NovaSharp.Interpreter.CoreLib.IO.StreamFileUserDataBase` | 47.2 | 36.3 | 26 / 55 | Runtime | Layer tests for buffered writes, binary/text modes, and repeated dispose calls to drive coverage and validate guard rails. |
| `NovaSharp.Interpreter.CoreLib.DynamicModule` | 72.0 | 75.0 | 18 / 25 | Runtime | Cover `dynamic.prepare`/`dynamic.eval` unhappy paths (syntax errors, sandbox exclusions) to lift the remaining uncovered lines. |
| `NovaSharp.Interpreter.CoreLib.ErrorHandlingModule` | 71.0 | 70.5 | 54 / 76 | Runtime | Add regression tests around nested `pcall`/`xpcall` and message handlers to close out the branch coverage debt. |

## Yellow List (line 50–89 %)
- `NovaSharp.Interpreter.CoreLib.IoModule` – 63.5 % line, 51.7 % branch. Queue tests for `io.lines`, stream reopen failures, and stderr/stdout fallbacks.
- `NovaSharp.Interpreter.CoreLib.JsonModule` – 61.5 % line. Exercise malformed JSON, null handling, and round-trip error cases.
- `NovaSharp.Interpreter.CoreLib.LoadModule` – 56.5 % line, 61.5 % branch. Add coverage for sandboxed `load`/`loadfile` options, mode flags, and environment overrides.
- `NovaSharp.Interpreter.CoreLib.StringModule` – 75 % line. Extend tests for edge-case pattern matching, UTF-8 boundaries, and numeric conversions.
- `NovaSharp.Interpreter.DataStructs.FastStack<T>` – 56.6 % line. Build tests for iterator paths, TrimExcess, and error handling when popping empty stacks.
- `NovaSharp.Interpreter.CoreLib.OsTimeModule` – 80.1 % line. Cover locale/timezone permutations and invalid date tables.

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
- Hardwired descriptor helpers (member + method) now covered via `HardwiredDescriptorTests`, validating access checks, conversions, and default-argument marshalling.
- `OsSystemModule` edge paths validated with platform stub tests (non-zero exits, missing files, rename/delete failures, setlocale placeholder), driving coverage to 98 %.
- `debug.debug` loop now exercised via queued debug console hooks, confirming prompt/print wiring and error reporting without manual REPL interaction.
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
