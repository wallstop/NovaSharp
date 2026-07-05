# Session 159: A1a INCR Writable Slot

Date: 2026-07-05

## Summary

- Continued A1a prep by removing the numeric-loop `INCR` dependency on `DynValue.CloneAsWritable()`.
- `ExecIncr(...)` now replaces a read-only top numeric stack value with a new writable numeric slot through `FastStack.Set(...)` before mutating it with `AssignNumber(...)`.
- The focused processor test now asserts that the read-only source wrapper remains read-only and that the incremented stack slot is a distinct writable value.
- Adversarial review found that the first `ExecIncr(...)` edit masked malformed read-only non-number VM state; the helper now rejects that state before replacing the stack slot and has focused coverage for the invariant.
- Escaped vararg capture now reuses already-read-only scalar snapshots and clones only mutable scalar wrappers, preserving caller-slot isolation without cloning cached/literal values.
- After this change, the only production `CloneAsWritable()` call site is mutable escaped vararg capture, which remains intentional until the slot/value split because vararg tuples can outlive the caller frame.

## Validation

- `./scripts/test/quick.sh --full -c ProcessorStackOperationsTUnitTests` completed with exit code 0: 26 tests passed, 0 failed.
- `./scripts/test/quick.sh --full ForLoop` completed with exit code 0: 15 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests` completed with exit code 0: 595 tests passed, 0 failed.
- `./scripts/build/quick.sh` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0: 14,877 tests passed, 0 failed.
- `git diff --check` completed with exit code 0.
- `bash ./scripts/dev/pre-commit.sh` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.1 --output-dir artifacts/lua-comparison-results-5.1` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.1 --results-dir artifacts/lua-comparison-results-5.1 --enforce` completed with exit code 0: 0 mismatch, 0 lua_only, 0 nova_only, 0 missing outputs.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.2 --output-dir artifacts/lua-comparison-results-5.2` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.2 --results-dir artifacts/lua-comparison-results-5.2 --enforce` completed with exit code 0: 0 mismatch, 0 lua_only, 0 nova_only, 0 missing outputs.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.3 --output-dir artifacts/lua-comparison-results-5.3` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.3 --results-dir artifacts/lua-comparison-results-5.3 --enforce` completed with exit code 0: 0 mismatch, 0 lua_only, 0 nova_only, 0 missing outputs.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.4 --output-dir artifacts/lua-comparison-results-5.4` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-results-5.4 --enforce` completed with exit code 0: 0 mismatch, 0 lua_only, 0 nova_only, 0 missing outputs.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.5 --output-dir artifacts/lua-comparison-results-5.5` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.5 --results-dir artifacts/lua-comparison-results-5.5 --enforce` completed with exit code 0: 0 mismatch, 0 lua_only, 0 nova_only, 0 missing outputs.
- Scoped `SimpleTUnitTests` Lua comparison attempts were not usable as validation because the comparison script mapped scoped batch outputs as missing counterpart results.

## Residual Risk

- PR CI and reviewer feedback still need to be observed after the change is pushed.
