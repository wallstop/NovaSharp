# Session 156: A1a Argument Slot Copy

Date: 2026-07-05

## Summary

- Continued A1a prep by removing the redundant `CloneAsWritable()` from fixed-parameter binding in `ExecArgs`.
- Kept vararg capture cloning in place after adversarial review showed escaped vararg tuples/tables can otherwise expose caller-owned mutable `DynValue` wrappers.
- Added focused C# coverage that `AssignSlot(...)` copies read-only source values without locking destination slots.
- Added script-call coverage for mutable fixed arguments, escaped vararg scalar snapshots, `table.pack(...)`, and table reference sharing.
- Added a standalone all-version Lua fixture for reference-visible argument rebinding and table-sharing semantics.

## Validation

- Reference Lua fixture run passed for Lua 5.1, 5.2, 5.3, 5.4, and 5.5:
  - `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/ScriptCallTUnitTests/ArgumentRebindingAndVarargCaptureKeepScalarValues.lua`
- NovaSharp CLI fixture run passed for Lua 5.1, 5.2, 5.3, 5.4, and 5.5.
- `bash scripts/tests/run-lua-fixtures-fast.sh --fixtures-dir src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/ScriptCallTUnitTests ...` passed the selected fixtures for reference Lua and NovaSharp across Lua 5.1-5.5.
- `./scripts/test/quick.sh --full -c DynValueTUnitTests` passed: 96 tests, 0 failures.
- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests` passed: 595 tests, 0 failures.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14,876 tests, 0 failures.

## Notes

- An initial parallel targeted-test run produced a build output file-lock diagnostic. The same targeted suites were rerun serially and passed cleanly, so the diagnostic was a local command scheduling artifact.
- `compare-lua-outputs.py --enforce` was attempted against the scoped `ScriptCallTUnitTests` fixture output, but the comparator reported one-sided keys because the narrowed fixture directory made reference-Lua and NovaSharp batch outputs use different relative paths while still applying the full-corpus ratchet. Direct reference Lua, NovaSharp CLI, and the scoped fixture runner all executed the new fixture successfully on Lua 5.1-5.5, so this was treated as an invocation-scoping issue rather than a runtime mismatch.
- `CloneAsWritable()` now remains in production only for `ExecIncr()` and vararg capture. `ExecIncr()` is still the numeric-loop slot mutation boundary, and vararg capture still needs wrapper snapshots while tuples can escape before the full slot/value split.
