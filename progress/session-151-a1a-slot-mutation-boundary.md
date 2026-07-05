# Session 151: A1a Slot Mutation Boundary

Date: 2026-07-05

## Summary

- Continued A1a prep by making the mutable `DynValue` slot boundary explicit.
- Renamed the internal value-copy mutator from `Assign(...)` to `AssignSlot(...)`.
- Updated all local, upvalue, `_ENV`, debug local/upvalue, and to-be-closed cleanup callsites to use the slot-specific helper.
- Updated existing tests that intentionally exercise internal mutable-slot behavior.

## Rationale

- The remaining `_readOnly` machinery still protects mutable local/upvalue slots, cached singleton values, vararg copies, and table-key/literal snapshots before the A1 struct conversion.
- This slice does not remove that machinery. It narrows the direct whole-slot mutation API so the future slot/value split has an obvious boundary to delete or replace.
- `AssignNumber(...)` remains as a separate intentional numeric-slot mutation path for `ExecIncr` until the value-struct conversion removes wrapper mutation entirely.
- Table keys and instruction literals remain on read-only snapshot paths; this change only touches existing writable slots.

## Validation

- `./scripts/test/quick.sh --full -c DynValueTUnitTests` passed: 95 tests, 0 failures.
- `./scripts/test/quick.sh --full -c VmCorrectnessRegressionTUnitTests` passed: 46 tests, 0 failures.
- `./scripts/test/quick.sh --full -c ByteCodeTUnitTests` passed: 116 tests, 0 failures.
- `./scripts/test/quick.sh --full -c DebugModuleTUnitTests` passed: 597 tests, 0 failures.
- `./scripts/test/quick.sh --full -c SetFenvGetFenvTUnitTests` passed: 25 tests, 0 failures.

## Notes

- No standalone Lua fixture was added because this is an internal slot-boundary refactor with no intended Lua behavior change.
- A1a remains open until the real slot/value split removes `_readOnly`/clone-as-writable responsibilities.
