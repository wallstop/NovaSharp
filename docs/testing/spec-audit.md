# Spec Fidelity Audit (Nov 2025)

NovaSharp aims for Lua 5.4.8 parity. When historic MoonSharp behaviour diverges from the spec, existing tests may accidentally lock in the wrong semantics. This audit captures ongoing verification of high-risk areas so we fix the runtime (or the tests) based on the canonical Lua manuals instead of papering over defects.

## Tracking Table

| Area / File | Representative Tests | Lua 5.4 Reference | Notes | Status / Follow-up |
|-------------|----------------------|-------------------|-------|--------------------|
| BinaryOperatorExpression (`src/tests/NovaSharp.Interpreter.Tests/Units/BinaryOperatorExpressionTests.cs`) | `PowerOperatorIsRightAssociative` | §3.4.1 (Arithmetic Operators) | Direct AST evaluation previously threw `DynamicExpressionException` because `Operator.Power` wasn’t handled in `EvalArithmetic`. Runtime updated (2025‑11‑14) to call `Math.Pow`, matching Lua’s right‑associative power semantics. | ✅ Fixed (tests + runtime) |
| BinaryOperatorExpression (`.../BinaryOperatorExpressionTests.cs`) | `EqualityTreatsNilAndVoidAsEqual` | §3.4.3 (Comparison Operators) | Test asserts `nil == void`. Lua only exposes `nil`; “void” is an internal NovaSharp concept for “no value”. Confirm that scripts can’t observe this behaviour; if they can, Lua parity requires returning `false` when comparing `nil` with any non‑nil value. | 🔍 Needs investigation (file follow-up issue if observable) |

## Next Steps

1. Continue enumerating interpreter/unit tests, cite the relevant Lua manual section for each behaviour, and classify them as **Spec‑Faithful**, **Needs Investigation**, or **Known Divergence**.
2. For every “Needs Investigation” entry, either:
   - prove the behaviour is internal-only and document it, or
   - fix the production code / tests to match Lua, referencing the manual.
3. Record progress in this file and mirror actionable items back to `PLAN.md` and issue trackers so spec deviations are not forgotten.
