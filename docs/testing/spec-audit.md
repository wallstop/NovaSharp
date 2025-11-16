# Spec Fidelity Audit (Nov 2025)

NovaSharp aims for Lua 5.4.8 parity. When historic legacy behaviour diverges from the spec, existing tests may accidentally lock in the wrong semantics. This audit captures ongoing verification of high-risk areas so we fix the runtime (or the tests) based on the canonical Lua manuals instead of papering over defects.

## Tracking Table

| Area / File | Representative Tests | Lua 5.4 Reference | Notes | Status / Follow-up |
|-------------|----------------------|-------------------|-------|--------------------|
| BinaryOperatorExpression (`src/tests/NovaSharp.Interpreter.Tests/Units/BinaryOperatorExpressionTests.cs`) | `PowerOperatorIsRightAssociative` | §3.4.1 (Arithmetic Operators) | Direct AST evaluation previously threw `DynamicExpressionException` because `Operator.Power` wasn’t handled in `EvalArithmetic`. Runtime updated (2025‑11‑14) to call `Math.Pow`, matching Lua’s right‑associative power semantics. | ✅ Fixed (tests + runtime) |
| BinaryOperatorExpression (`.../BinaryOperatorExpressionTests.cs`) | `EqualityTreatsNilAndVoidAsEqual` | §3.4.3 (Comparison Operators) | Test asserts `nil == void`. `DataType.Void` is an internal sentinel (e.g., `DynValue.ToScalar` returns `Void` when a function yields “no values”). Before user code can observe it, callers either translate `Void` to `Nil` (see `Processor.InstructionLoop` JNil handling) or treat `Void` as equivalent to `Nil` when comparing/assigning. Keeping the test guards against regressions where the sentinel leaks as a distinct Lua-visible value. | ✅ Spec-faithful (internal sentinel) |
| TableModule (`src/tests/NovaSharp.Interpreter.Tests/Units/TableModuleTests.cs`) | `PackPreservesNilAndReportsCount` / `UnpackHonorsExplicitBounds` | §6.6 (Table Manipulation) | NovaSharp’s `table.pack` exposes the Lua-specific `n` field (argument count) and preserves `nil` entries; `table.unpack` respects the optional start/end bounds. Tests exercise both behaviours, matching Lua §6.6. | ✅ Spec-faithful |
| StringModule (`src/tests/NovaSharp.Interpreter.Tests/Units/StringModuleTests.cs`) | `AdjustIndexClampsToBounds` / `StringRangeHandlesNegativeIndices` | §6.4 (String Manipulation) | Tests assert that helper routines mimic Lua’s 1-based indexing and negative-index adjustments. Verified against §6.4 (Lua specifies negative indices count from the end, clamped to string bounds). Implementation currently follows the spec (`StringModule.AdjustIndex` matches Lua’s behaviour). | ✅ Spec-faithful |
| MathModule (`src/tests/NovaSharp.Interpreter.Tests/Units/MathModuleTests.cs`) | `ModfSplitsIntegerAndFractionalComponents` | §6.7 (Mathematical Functions) | Lua’s `math.modf(x)` returns the integer part truncated toward zero and the fractional remainder (`x = i + f`, `f` has same sign as `x`). NovaSharp previously used `Math.Floor`, so `math.modf(-3.25)` yielded `-4` and `0.75` (Lua returns `-3` and `-0.25`). Runtime updated to use `Math.Truncate`, and the test now asserts the Lua-compliant tuple. | ✅ Fixed (runtime + test) |

## Next Steps

1. Continue enumerating interpreter/unit tests, cite the relevant Lua manual section for each behaviour, and classify them as **Spec‑Faithful**, **Needs Investigation**, or **Known Divergence**.
2. For every “Needs Investigation” entry, either:
   - prove the behaviour is internal-only and document it, or
   - fix the production code / tests to match Lua, referencing the manual.
3. Record progress in this file and mirror actionable items back to `PLAN.md` and issue trackers so spec deviations are not forgotten.
