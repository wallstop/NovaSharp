# Session 159: A1a INCR Writable Slot

Date: 2026-07-05

## Summary

- Continued A1a prep by removing the numeric-loop `INCR` dependency on `DynValue.CloneAsWritable()`.
- `ExecIncr(...)` now replaces a read-only top numeric stack value with a new writable numeric slot through `FastStack.Set(...)` before mutating it with `AssignNumber(...)`.
- The focused processor test now asserts that the read-only source wrapper remains read-only and that the incremented stack slot is a distinct writable value.
- Escaped vararg capture now reuses already-read-only scalar snapshots and clones only mutable scalar wrappers, preserving caller-slot isolation without cloning cached/literal values.
- After this change, the only production `CloneAsWritable()` call site is mutable escaped vararg capture, which remains intentional until the slot/value split because vararg tuples can outlive the caller frame.

## Validation

- `./scripts/test/quick.sh -c ProcessorStackOperationsTUnitTests` completed with exit code 0.
- `./scripts/test/quick.sh -c ScriptCallTUnitTests` completed with exit code 0.
- `./scripts/test/quick.sh -c DynValueTUnitTests` completed with exit code 0.
- `./scripts/test/quick.sh -c VmCorrectnessRegressionTUnitTests` completed with exit code 0.
- `./scripts/build/quick.sh` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0.
- `dotnet tool restore` completed with exit code 0.
- `dotnet tool run csharpier format src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorStackOperationsTUnitTests.cs` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.1 --output-dir artifacts/lua-comparison-results-5.1` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.1 --results-dir artifacts/lua-comparison-results-5.1 --enforce` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.2 --output-dir artifacts/lua-comparison-results-5.2` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.2 --results-dir artifacts/lua-comparison-results-5.2 --enforce` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.3 --output-dir artifacts/lua-comparison-results-5.3` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.3 --results-dir artifacts/lua-comparison-results-5.3 --enforce` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.4 --output-dir artifacts/lua-comparison-results-5.4` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-results-5.4 --enforce` completed with exit code 0.
- `bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.5 --output-dir artifacts/lua-comparison-results-5.5` plus `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.5 --results-dir artifacts/lua-comparison-results-5.5 --enforce` completed with exit code 0.

## Residual Risk

- PR CI and reviewer feedback still need to be observed after the change is pushed.
