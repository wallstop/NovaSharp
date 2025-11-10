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
| `NovaSharp.Interpreter.CoreLib.IO.BinaryEncoding` | 0.0 | 0.0 | 0 / 11 | Runtime | Add read/write regression tests for binary IO edge cases (little/big endian conversions). |
| `NovaSharp.Interpreter.Loaders.InvalidScriptLoader` | 0.0 | 0.0 | 0 / 9 | Tooling | Exercise failure paths to verify loader diagnostics/messages stay stable. |
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMemberDescriptor` | 0.0 | – | 0 / 33 | Interop | Cover hardwired property dispatch before Roslyn generator removes reflection. |
| `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.HardwiredMethodMemberDescriptor` | 0.0 | – | 0 / 10 | Interop | Add invocation tests to validate method wiring without reflection. |
| `NovaSharp.Interpreter.Interop.ReflectionSpecialName` | 0.0 | 0.0 | 0 / 95 | Interop | Build targeted tests for conversion/operator metadata; feeds the reflection-free effort. |
| `NovaSharp.Interpreter.Interop.RegistrationPolicies.PermanentRegistrationPolicy` | 0.0 | – | 0 / 2 | Interop | Ensure registration policies stay covered as Roslyn-generated descriptors replace reflection.

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
