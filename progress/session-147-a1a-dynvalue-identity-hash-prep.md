# Session 147: A1a DynValue Identity and Hash Prep

Date: 2026-07-04

## Summary

- Started Phase A1a prep without changing Lua-visible value semantics.
- Removed the per-instance `DynValue.ReferenceId` field and counter so ordinary `DynValue` wrappers no longer carry debug identity state that will not exist after the struct conversion.
- Kept a no-field `DynValue.ReferenceId` compatibility getter that derives wrapper identity from `RuntimeHelpers.GetHashCode(this)`. This avoids an avoidable source break while still removing the stored identity field and does not introduce obsolete warnings for warning-as-error consumers.
- Kept `RefIdObject.ReferenceId` intact for reference-backed runtime objects such as tables, closures, callbacks, userdata, coroutines, and file handles.
- Added a lazily captured stable userdata hash so table keys do not depend on mutable CLR object hash codes after insertion, while equal static userdata and equal value-style userdata keep matching hash codes. The hash uses descriptor reference identity so custom descriptor virtual hash implementations cannot diverge from userdata equality.
- Replaced `debug.upvalueid` display identity with an ID owned by the debug-only upvalue handle. The Lua identity contract still comes from the cached userdata handle per upvalue slot.
- Updated the VS Code debugger variable inspector so `(val #id)` keeps wrapper identity semantics without depending on a stored `DynValue` field.
- Removed the mutable `DynValue` `_hashCode` cache. `GetHashCode()` now recomputes from current fields, which avoids stale hashes when mutable VM slots are updated through `AssignNumber(...)`.
- Added regression tests proving numeric slot mutation recomputes the current number hash, table value-map keys snapshot mutable numeric/userdata keys before insertion, equal userdata values have equal hash codes, and userdata hash capture is lazy.

## What Remains Open

- `_readOnly`, `ReadOnly`, `AsReadOnly()`, and `CloneAsWritable()` remain in place. They still protect mutable local/upvalue slots, cached singleton values, vararg copies, and table keys stored in `_valueMap`.
- Removing that machinery safely requires a real slot/value split or equivalent explicit key/value snapshotting. A mechanical deletion would let r-values, literals, cached values, and table keys become mutable shared cells.
- A1a remains open until that read-only machinery has a behavior-preserving replacement and the focused r-value/table-key/vararg tests stay green.

## Targeted Validation

- `./scripts/test/quick.sh --full -c DynValueTUnitTests` passed: 95 tests, 0 failures.
- `./scripts/test/quick.sh --full -c VmCorrectnessRegressionTUnitTests` passed: 46 tests, 0 failures.
- `./scripts/test/quick.sh --full -c UserDataTUnitTests` passed: 113 tests, 0 failures.
- `./scripts/test/quick.sh --full -c DebugModuleTUnitTests` passed: 597 tests, 0 failures.
- `./scripts/test/quick.sh --full -c DebugModuleTapParityTUnitTests` passed: 85 tests, 0 failures.
- `./scripts/test/quick.sh --full -c ProcessorStackOperationsTUnitTests` passed: 25 tests, 0 failures.
- `./scripts/test/quick.sh --full -c ProcessorDebuggerRuntimeTUnitTests` passed: 10 tests, 0 failures.
- `./scripts/test/quick.sh --full -c RefIdObjectTUnitTests` passed: 2 tests, 0 failures.
- `./scripts/test/quick.sh --full -c StreamFileUserDataBaseTUnitTests` passed: 76 tests, 0 failures.
- `./scripts/test/quick.sh --full -c VarargsTupleTUnitTests` passed: 50 tests, 0 failures.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed: 14,589 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` passed.

## Diagnostics

- Two earlier debug-module-targeted commands were started in parallel and hit build output file locks in `obj/`/`bin/`. The same targeted classes were then rerun serially and passed, so the failure was a local command scheduling artifact rather than a product failure.

## Next Checks

- Push the scoped commit and wait for PR CI plus Copilot/Cursor feedback.
