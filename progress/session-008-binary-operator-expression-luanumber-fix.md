# BinaryOperatorExpression LuaNumber Precision Fix

**Date**: 2025-12-13
**Status**: Complete
**PLAN.md Reference**: §8.37 (LuaNumber Usage Audit), §8.36 (Numeric Edge-Case Audit)

## Summary

Fixed critical bugs in `BinaryOperatorExpression.Eval()` (used for dynamic expression evaluation) where numeric comparisons and arithmetic operations used raw `double` arithmetic, causing precision loss for large integers near the 2^53 boundary.

## Root Cause Analysis

The `BinaryOperatorExpression.Eval()` method is used for evaluating Lua expressions outside the main VM loop (via `DynamicExpression.Evaluate()`). The `EvalComparison()`, `EvalArithmetic()`, and `EvalFloorDivision()` helper methods were using:

1. **`l.Number < r.Number`** — Raw double comparison, loses integer precision
1. **`CastToNumber()` → `double`** — Loses integer vs float subtype information
1. **`Math.Floor()` on doubles** — Already lost precision before flooring

While the main VM execution path (`ExecLess`, `ExecLessEq`, `ExecArith`) correctly used `LuaNumber` methods, the dynamic expression path did not.

## Changes Made

### 1. `EvalComparison` — Use `LuaNumber.LessThan/LessThanOrEqual`

**Before:**

```csharp
if (l.Type == DataType.Number && r.Type == DataType.Number)
{
    return (l.Number < r.Number);  // Bug: double precision loss
}
```

**After:**

```csharp
if (l.Type == DataType.Number && r.Type == DataType.Number)
{
    // Use LuaNumber comparison to preserve integer precision at boundaries
    return LuaNumber.LessThan(l.LuaNumber, r.LuaNumber);
}
```

### 2. `EvalArithmetic` — Use `LuaNumber` arithmetic operations

**Before:**

```csharp
double? nd1 = v1.CastToNumber();
double? nd2 = v2.CastToNumber();
// ... double arithmetic ...
return d1 + d2;  // Bug: loses integer subtype
```

**After:**

```csharp
LuaNumber? nd1 = v1.CastToLuaNumber();
LuaNumber? nd2 = v2.CastToLuaNumber();
// ... LuaNumber arithmetic ...
return LuaNumber.Add(n1, n2);  // Preserves integer subtype
```

### 3. `EvalFloorDivision` — Use `LuaNumber.FloorDivide`

**Before:**

```csharp
// NOTE: NovaSharp cannot distinguish integer vs float...
return Math.Floor(nd1.Value / nd2.Value);  // Bug: outdated comment, loses precision
```

**After:**

```csharp
// LuaNumber.FloorDivide preserves integer subtype...
return LuaNumber.FloorDivide(nd1.Value, nd2.Value);
```

## Verification

All 5031 tests pass (6 new tests added).

### New Tests Added

1. `LessThanPreservesIntegerPrecisionAtBoundaries` — Verifies `maxinteger - 1 < maxinteger`
1. `LessOrEqualPreservesIntegerPrecisionAtBoundaries` — Verifies `maxinteger <= maxinteger`
1. `AdditionPreservesIntegerSubtype` — Verifies `integer + integer = integer`
1. `SubtractionPreservesIntegerSubtype` — Verifies `integer - integer = integer`
1. `MultiplicationPreservesIntegerSubtype` — Verifies `integer * integer = integer`
1. `FloorDivisionPreservesIntegerSubtype` — Verifies `integer // integer = integer`

### Lua Fixtures Created

- `LuaFixtures/BinaryOperatorExpressionTUnitTests/LargeIntegerComparisonPrecision.lua`
- `LuaFixtures/BinaryOperatorExpressionTUnitTests/IntegerAdditionPreservesSubtype.lua`
- `LuaFixtures/BinaryOperatorExpressionTUnitTests/IntegerFloorDivisionPreservesSubtype.lua`

All fixtures verified against reference Lua 5.4.

## Impact

This fix ensures that dynamic expression evaluation (used by:

- `DynamicExpression.Evaluate()` API
- Expression parsing/evaluation utilities
- Any code using the Expression AST directly)

now correctly handles:

- Large integer comparisons near `maxinteger`/`mininteger`
- Integer arithmetic preserving the integer subtype
- Floor division preserving integer subtype when both operands are integers

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/BinaryOperatorExpression.cs`
  - `EvalComparison()` — Use `LuaNumber.LessThan/LessThanOrEqual`
  - `EvalArithmetic()` — Use `LuaNumber` arithmetic methods, return `LuaNumber`
  - `EvalFloorDivision()` — Use `LuaNumber.FloorDivide`, return `LuaNumber`

## Related Work

- Previous fix: `progress/2025-12-13-for-loop-luanumber-precision.md` (VM for-loop precision)
- Both fixes are part of §8.37 LuaNumber Usage Audit

## Audit Classification Summary

From Phase 1 grep search results:

| Location                           | Pattern                           | Classification | Action                        |
| ---------------------------------- | --------------------------------- | -------------- | ----------------------------- |
| `ProcessorInstructionLoop.cs:455`  | `(int)(_valueStack.Pop().Number)` | SAFE           | Argument count (small values) |
| `ProcessorInstructionLoop.cs:1191` | `(int)_valueStack.Peek(0).Number` | SAFE           | Argument count (small values) |
| `ProcessorInstructionLoop.cs:1389` | `(int)(_valueStack.Pop().Number)` | SAFE           | Argument count (small values) |
| `ProcessorInstructionLoop.cs:1426` | `(int)(_valueStack.Pop().Number)` | SAFE           | Argument count (small values) |
| `BinaryOperatorExpression.cs:687`  | `l.Number < r.Number`             | **BUG**        | **Fixed**                     |
| `BinaryOperatorExpression.cs:702`  | `l.Number <= r.Number`            | **BUG**        | **Fixed**                     |
| `BinaryOperatorExpression.cs`      | `EvalArithmetic` double ops       | **BUG**        | **Fixed**                     |
| `BinaryOperatorExpression.cs`      | `EvalFloorDivision` double ops    | **BUG**        | **Fixed**                     |
| VM `ExecLess/ExecLessEq`           | Already uses `LuaNumber`          | SAFE           | Already correct               |

## Lua Spec Reference

Lua 5.3 Manual §2.1:

> "The type number uses two internal representations, one called integer and the other called float."

Lua 5.3 Manual §3.4.1:

> "Arithmetic operators work on both integers and floats. The result type depends on the subtype of the operands."
