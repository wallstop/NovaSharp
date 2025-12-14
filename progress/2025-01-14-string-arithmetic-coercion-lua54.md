# String-to-Number Coercion Metamethods for Lua 5.4+

**Date**: 2025-01-14
**PLAN.md Item**: §9.4 - String-to-number coercion metamethods for Lua 5.4

## Summary

Implemented Lua 5.4+ string-to-number coercion via string metatable arithmetic metamethods, replacing the built-in operator coercion used in pre-5.4 versions.

## Background

In Lua 5.1-5.3, string-to-number coercion for arithmetic operators (`+`, `-`, `*`, `/`, `%`, `^`) was performed automatically by the operators themselves. Lua 5.4 changed this: arithmetic operators no longer coerce strings to numbers; instead, the string library provides arithmetic metamethods (`__add`, `__sub`, `__mul`, `__div`, `__mod`, `__pow`, `__idiv`, `__unm`) on the string metatable that perform the coercion.

Key Lua 5.4 behavior (from manual §3.4.3):

> "The string library sets metamethods that try to coerce strings to numbers in all arithmetic operations. **If the conversion fails, the library calls the metamethod of the other operand (if present) or it raises an error.**"

This fallback behavior is critical: `string + table` should use the table's `__add` metamethod, not the string's.

## Changes Made

### 1. VM Arithmetic Operations (ProcessorInstructionLoop.cs)

Added version-aware coercion helper `CastToLuaNumberForArithmetic()` that returns null for strings in Lua 5.4+ mode, causing the VM to fall through to metamethod lookup.

Modified arithmetic operations:

- `ExecAdd`, `ExecSub`, `ExecMul`, `ExecMod`, `ExecDiv`, `ExecFloorDiv`, `ExecPower`, `ExecNeg`

### 2. String Metatable Metamethods (StringModule.cs)

In `NovaSharpInit()`, added conditional registration of arithmetic metamethods for Lua 5.4+:

```csharp
if (compatibilityVersion >= LuaCompatibilityVersion.Lua54)
{
    RegisterStringArithmeticMetamethods(stringMetatable);
}
```

Added helper methods:

- `RegisterStringArithmeticMetamethods()` - registers all arithmetic metamethods
- `StringBinaryArithmetic()` - implements binary ops with fallback to other operand's metamethod
- `CoerceToLuaNumber()` - tries to coerce string/number to LuaNumber
- `ArithmeticCoercionError()` - generates appropriate error message

The key implementation detail is in `StringBinaryArithmetic()`:

```csharp
// If coercion fails, fall back to other operand's metamethod
if (nonStringOperand.Type == DataType.Table || nonStringOperand.Type == DataType.UserData)
{
    DynValue otherMetamethod = ctx.GetBinaryMetamethod(nonStringOperand, nonStringOperand, metamethodName);
    if (otherMetamethod != null && otherMetamethod.IsNotNil())
    {
        return ctx.Script.Call(otherMetamethod, left, right);
    }
}
```

### 3. Tests (StringArithmeticCoercionTUnitTests.cs)

Created comprehensive test file with 28 test cases covering:

- String metatable availability (5.4+ has metamethods, pre-5.4 doesn't)
- Basic arithmetic operations with numeric strings
- Metamethod fallback to table's `__add` when string + table
- Custom metamethod override capability
- Error handling for non-numeric strings

### 4. Lua Fixtures

Created test fixtures in `LuaFixtures/StringArithmeticCoercionTUnitTests/`:

- `string_metatable_arithmetic_54plus.lua` - verifies metamethods exist in 5.4+
- `string_metatable_arithmetic_absent_pre54.lua` - verifies metamethods absent in pre-5.4
- `string_arithmetic_basic.lua` - basic arithmetic tests (all versions)
- `string_metamethod_fallback_54plus.lua` - fallback behavior test
- `string_arithmetic_error_nonnumeric.lua` - error case
- `string_floor_division_53plus.lua` - floor division with strings

## Test Results

- All 5084 TUnit tests pass
- Verified against real Lua 5.4 interpreter
- TAP test suite passes (including 231-metatable.t which exercises string + table arithmetic)

## Verification Commands

```bash
# Run all tests
dotnet test src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release

# Run specific tests
dotnet test ... --filter "FullyQualifiedName~StringArithmeticCoercion"
dotnet test ... --filter "FullyQualifiedName~231-metatable"

# Verify with real Lua
lua5.4 src/tests/.../string_metamethod_fallback_54plus.lua
```

## Files Modified

1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`
1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs`
1. `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs` (new)
1. `src/tests/.../LuaFixtures/StringArithmeticCoercionTUnitTests/*.lua` (new, 6 files)

## Status

✅ **COMPLETE** - Ready for PLAN.md update
