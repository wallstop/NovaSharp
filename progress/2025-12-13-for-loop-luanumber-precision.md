# For-Loop LuaNumber Precision and Overflow Fix

**Date**: 2025-12-13
**Status**: Complete
**PLAN.md Reference**: §8.37 (LuaNumber Usage Audit), §8.11 (Numerical For Loop Semantics)

## Summary

Fixed critical bugs in the numeric `for`-loop implementation where large integer values near `maxinteger`/`mininteger` caused:

1. **Precision loss** — Integer values converted to floats, losing precision
1. **Infinite loops** — Loop counter became stuck due to float precision limits or overflow wrap-around

## Root Cause Analysis

The numeric for-loop (`for i = start, stop, step do`) uses three VM operations:

1. `ToNum` — Converts loop bounds/step to numbers
1. `JFor` — Conditional jump (loop test)
1. `Incr` — Increment the loop counter

All three were using `double` arithmetic, which:

1. **Loses integer precision** for values beyond 2^53
1. **Cannot distinguish integer vs float subtype** (important for Lua 5.3+)
1. **Wraps silently** on integer overflow without detection

## Changes Made

### 1. `ExecToNum` — Use `LuaNumber` instead of `double`

**Before:**

```csharp
double? v = _valueStack.Pop().ToScalar().CastToNumber();
if (v.HasValue)
{
    _valueStack.Push(DynValue.NewNumber(v.Value));
}
```

**After:**

```csharp
LuaNumber? v = _valueStack.Pop().ToScalar().CastToLuaNumber();
if (v.HasValue)
{
    _valueStack.Push(DynValue.NewNumber(v.Value));
}
```

### 2. `ExecJFor` — Use `LuaNumber` comparisons + overflow detection

**Before:**

```csharp
double val = _valueStack.Peek(0).Number;
double step = _valueStack.Peek(1).Number;
double stop = _valueStack.Peek(2).Number;
bool whileCond = (step > 0) ? val <= stop : val >= stop;
```

**After:**

```csharp
LuaNumber val = _valueStack.Peek(0).LuaNumber;
LuaNumber step = _valueStack.Peek(1).LuaNumber;
LuaNumber stop = _valueStack.Peek(2).LuaNumber;

// Overflow detection for integer for-loops (Lua 5.3+ §3.3.5)
if (val.IsInteger && step.IsInteger && stop.IsInteger)
{
    // Detect wrap-around from positive to negative (or vice versa)
    if (stepVal > 0 && stopVal >= 0 && valVal < 0)
        return i.NumVal; // Overflow - stop loop
    if (stepVal < 0 && stopVal <= 0 && valVal > 0)
        return i.NumVal; // Overflow - stop loop
}

bool stepPositive = LuaNumber.LessThan(LuaNumber.Zero, step);
bool whileCond = stepPositive
    ? LuaNumber.LessThanOrEqual(val, stop)
    : LuaNumber.LessThanOrEqual(stop, val);
```

### 3. `ExecIncr` — Use `LuaNumber.Add` instead of double addition

**Before:**

```csharp
top.AssignNumber(top.Number + btm.Number);
```

**After:**

```csharp
top.AssignNumber(LuaNumber.Add(top.LuaNumber, btm.LuaNumber));
```

## Verification

### Before Fix

```lua
local max = math.maxinteger
for i = max - 2, max do
  print(i)
end
-- Output: 9223372036854780000.0 (repeated infinitely, float, wrong value)
```

### After Fix

```lua
local max = math.maxinteger
for i = max - 2, max do
  print(i)
end
-- Output:
-- 9223372036854775805 (integer)
-- 9223372036854775806 (integer)
-- 9223372036854775807 (integer)
-- count = 3
```

Matches reference Lua 5.4 exactly.

## Test Coverage

- All 5025 existing tests pass
- Verified behavior matches reference Lua 5.4 for:
  - `maxinteger` upper bound loops
  - `mininteger` lower bound loops (negative step)
  - Normal integer loops
  - Float loops (preserve float subtype)
  - Empty loops (start > stop with positive step)

## Lua Spec Reference

Lua 5.4 Manual §3.3.5:

> "In the case of an integer for loop, the *control variable* is guaranteed not to wrap."

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`
  - `ExecToNum()` — Use `CastToLuaNumber()` instead of `CastToNumber()`
  - `ExecJFor()` — Use `LuaNumber` comparisons and add overflow detection
  - `ExecIncr()` — Use `LuaNumber.Add()` instead of double addition
