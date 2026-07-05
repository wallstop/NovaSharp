# Session 149: A1a Literal Boundary Hardening

Date: 2026-07-04

## Summary

- Advanced Phase A1a by sealing writable `DynValue` aliasing at bytecode literal boundaries before the `LuaValue` struct conversion.
- Made direct `LiteralExpression` constants store read-only snapshots instead of caller-owned mutable wrappers.
- Made all bytecode-emitted `Instruction.Value` operands read-only snapshots, including literal loads, metadata payloads, global name operands, and literal index/index-set operands.
- Made binary chunk deserialization return read-only literal values for nil, booleans, numbers, strings, and table environment values.
- Added focused TUnit coverage for direct literal construction, bytecode literal/index/index-set/meta/global operands, and dumped-chunk literals.
- Added a standalone all-version Lua fixture covering repeated numeric literal and literal table-index reuse.
- Addressed Copilot review feedback by making `EmitLiteral` reject null explicitly and routing dumped numeric literals through the cached `DynValue.FromInteger`/`DynValue.FromFloat` factory paths before enforcing read-only storage.

## Adversarial Review

- A sub-agent identified that `EmitIndex(...)` and `EmitIndexSet(...)` still embedded mutable `DynValue` operands after the first patch. Those paths were hardened and covered with mutation-after-emission tests.
- The same review noted missing nil coverage for binary dump read-only constants. The dumped-chunk test now includes `return nil`.

## Validation

- Reference Lua fixture run passed on Lua 5.1, 5.2, 5.3, 5.4, and 5.5 for `InstructionLiteralValuesRemainStableAcrossCalls.lua`.
- `./scripts/test/quick.sh --full -c ByteCodeTUnitTests` passed: 115 tests, 0 failures.
- `./scripts/test/quick.sh -c LiteralExpressionTUnitTests` passed: 6 tests, 0 failures.
- `./scripts/test/quick.sh -c ProcessorBinaryDumpTUnitTests` passed: 55 tests, 0 failures.
- Earlier scoped checks also passed for `BinaryOperatorExpressionTUnitTests` and `UnaryOperatorExpressionTUnitTests` before the final bytecode-operand expansion.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14,826 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` completed successfully; it reported existing documentation and skill metadata warnings.

## Notes

- Running `tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` still rewrites broad unrelated fixture output and leaves unrelated generated files. The generated churn was removed from this PR; only the intentional ByteCode fixture remains.
- Scoped comparison runner execution passed the new fixture on both reference Lua and NovaSharp for Lua 5.1-5.5, but `compare-lua-outputs.py --enforce` reported one-sided keys for the manually scoped fixture runs. This was an artifact path/key mismatch in the manual invocation, not a fixture runtime failure, so the result is not counted as a green comparison check.
- A1a remains open. `_readOnly`/`AsReadOnly()`/`CloneAsWritable()` still protect mutable local/upvalue slots, cached singleton values, vararg copies, and table-key hash stability until a real slot/value split lands.
