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
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMemberDescriptor` | 0.0 | – | 0 / 33 | Interop | `HardwiredDescriptorTests` now exercise read/write conversions and guard rails; rerun coverage export and, if still zero, dig into instrumentation filters for generated descriptors. |
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMethodMemberDescriptor` | 0.0 | – | 0 / 10 | Interop | Same test suite covers argument marshalling + default handling—refresh coverage and ensure hardwired method helpers are not excluded by filters. |
| `NovaSharp.Interpreter.Interop.ReflectionSpecialName` | 0.0 | 0.0 | 0 / 95 | Interop | Extreme-path tests now exercise qualified operator names/null guards, yet coverage remains 0 %; inspect instrumentation or adjust code to ensure lines are tracked. |
| `NovaSharp.Interpreter.Interop.RegistrationPolicies.PermanentRegistrationPolicy` | 0.0 | – | 0 / 2 | Interop | Tests cover all decision paths; coverage still 0 %—confirm instrumentation and consider moving logic into non-inline helpers if needed. |
| `NovaSharp.Interpreter.CoreLib.DebugModule` | 6.6 | 0.0 | 8 / 120 | Runtime | New regression suite covers user values, metatables, upvalues, and traceback; remaining work: tackle `debug.debug` (interactive loop) via injectable prompt/print hooks and cover additional metamethod helpers. |
| `NovaSharp.Interpreter.CoreLib.OsSystemModule` | 20.6 | 20.0 | 12 / 58 | Runtime | Added virtual-platform tests for execute/remove/rename/getenv/tmpname; still need `os.execute` failure surfaces involving stderr/stdout and additional platform edge cases (permissions, path separators).

## Yellow List (line 50–89 %)
- `NovaSharp.Interpreter.CoreLib.MathModule` – 55 % line, 40 % branch. Fresh tests now cover logarithms, power, modf, min/max, ldexp, deterministic random sequences, and NaN/overflow behaviors; remaining gaps are random range bounds and trig conversions under extreme inputs.
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
- Hardwired descriptor helpers (member + method) now covered via `HardwiredDescriptorTests`, validating access checks, conversions, and default-argument marshalling.
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
