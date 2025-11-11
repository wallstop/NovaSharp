# Coverage Hotspots (baseline: 2025-11-10)

Latest data sourced from `docs/coverage/latest/Summary.json` (generated via `./coverage.ps1`).

## Snapshot
- Overall line coverage: **66.3 %**
- NovaSharp.Interpreter line coverage: **79.5 %**
- NovaSharp.Cli line coverage: **72.2 %**
- NovaSharp.Hardwire line coverage: **22.3 %**
- NovaSharp.RemoteDebugger / NovaSharp.VsCodeDebugger: **0 %** (no tests yet)

## Prioritized Red List (Interpreter < 90 %)

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
- `ErrorHandlingModule` now sits at 100 % line / 94 % branch thanks to nested `pcall`/`xpcall` regression tests (tail-call continuations, yield requests, CLR handlers).
- `HardwiredMemberDescriptor` base getters/setters now throw under coverage, shrinking the remaining uncovered lines to the by-ref conversion paths (currently at 100 % line coverage in the latest report).
- `DynValueMemberDescriptor` now covered across read access, execution flags, setter guard, and wiring paths (primitive, table, userdata, unsupported types).
- `DebugModule` interactive loop now handles returned values, CLR exceptions, and null-input exits under test, bumping branch coverage to 77 %.
- Expanded `ReflectionSpecialName` operator mapping tests (additional arithmetic/relational cases) to drive branch coverage; rerun `coverage.ps1` to confirm the updated instrumentation.

## Updating the Snapshot
```powershell
./coverage.ps1
# Copy docs/coverage/latest/Summary.json entries into the tables above.
```

_Last updated: 2025-11-11_
