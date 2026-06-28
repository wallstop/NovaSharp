# Session 068: Metamethod Constants Consolidation

**Date**: 2025-12-21
**Initiative**: 13 - Magic String Consolidation (Phase 1: Metamethods)
**Status**: ✅ Complete

## Summary

Created a `Metamethods` static class with compile-time constant strings for all Lua metamethod names, then migrated ~70 string literals across 15 files to use these constants. This eliminates duplicate magic strings and enables compiler optimizations for string comparisons.

## Problem

The codebase used raw string literals like `"__index"`, `"__newindex"`, `"__call"` etc. scattered across many files:

- No single source of truth for metamethod names
- Typos could go undetected until runtime
- No compiler optimization for string comparisons
- Harder to find all usages of a particular metamethod

## Solution

### 1. Created Metamethods Constants Class

Added `Metamethods` static class in `DataStructs/LuaStringPool.cs`:

```csharp
public static class Metamethods
{
    // Arithmetic operators
    public const string Add = "__add";
    public const string Sub = "__sub";
    public const string Mul = "__mul";
    public const string Div = "__div";
    public const string Mod = "__mod";
    public const string Pow = "__pow";
    public const string Unm = "__unm";
    public const string IDiv = "__idiv";

    // Bitwise operators (Lua 5.3+)
    public const string Band = "__band";
    public const string Bor = "__bor";
    public const string Bxor = "__bxor";
    public const string Bnot = "__bnot";
    public const string Shl = "__shl";
    public const string Shr = "__shr";

    // Comparison operators
    public const string Eq = "__eq";
    public const string Lt = "__lt";
    public const string Le = "__le";

    // Other operators
    public const string Concat = "__concat";
    public const string Len = "__len";

    // Table access
    public const string Index = "__index";
    public const string NewIndex = "__newindex";
    public const string Call = "__call";

    // Iteration (Lua 5.2+)
    public const string Pairs = "__pairs";
    public const string IPairs = "__ipairs";
    public const string Iterator = "__iterator";  // NovaSharp extension

    // Type conversion
    public const string ToStringMeta = "__tostring";

    // Metatable protection
    public const string Metatable = "__metatable";
    public const string Mode = "__mode";
    public const string Name = "__name";

    // Lifecycle
    public const string Gc = "__gc";
    public const string Close = "__close";

    // NovaSharp extensions for CLR interop
    public const string ToBool = "__tobool";
    public const string ToNumber = "__tonumber";
    public const string New = "__new";
}
```

### 2. Migrated Files

| File                                                                                              | Changes                      |
| ------------------------------------------------------------------------------------------------- | ---------------------------- |
| `Execution/VM/Processor/ProcessorInstructionLoop.cs`                                              | 30 replacements              |
| `CoreLib/StringModule.cs`                                                                         | 15 replacements              |
| `CoreLib/BasicModule.cs`                                                                          | 2 replacements               |
| `CoreLib/MetaTableModule.cs`                                                                      | 3 replacements + added using |
| `CoreLib/TableModule.cs`                                                                          | 2 replacements               |
| `CoreLib/TableIteratorsModule.cs`                                                                 | 2 replacements + added using |
| `CoreLib/IoModule.cs`                                                                             | 1 replacement + added using  |
| `Interop/BasicDescriptors/DispatchingUserDataDescriptor.cs`                                       | 13 case labels               |
| `Interop/StandardDescriptors/StandardEnumUserDataDescriptor.cs`                                   | 1 replacement + added using  |
| `Interop/StandardDescriptors/StandardUserDataDescriptor.cs`                                       | 4 replacements + added using |
| `Interop/StandardDescriptors/ReflectionMemberDescriptors/ValueTypeDefaultCtorMemberDescriptor.cs` | 1 replacement + added using  |
| `Interop/PredefinedUserData/EnumerableWrapper.cs`                                                 | 1 replacement + added using  |
| `DataTypes/CallbackArguments.cs`                                                                  | 2 replacements               |
| `Execution/ScriptExecutionContext.cs`                                                             | 1 replacement + added using  |
| `Script.cs`                                                                                       | 2 replacements               |

**Total: ~70 string literal replacements + 8 using statements added**

### 3. Kept as Literals

- `DescriptorHelpers.cs`: `"__to"` prefix for dynamic method name generation
- `ScriptRuntimeException.cs`: `__close` in error message prose for readability

### 4. Updated LuaStringPool

The `LuaStringPool.InitializeCommonStrings()` method now references `Metamethods.*` constants instead of duplicating the strings.

## Benefits

1. **Single source of truth**: All metamethod names defined in one place
1. **Compile-time safety**: Typos are caught at compile time
1. **IDE support**: IntelliSense, Find All References, Rename Refactoring
1. **Compiler optimization**: `const string` enables compile-time string interning
1. **Documentation**: Comments in `Metamethods` class document each metamethod's purpose

## Verification

- **Build**: ✅ Compiles with zero warnings
- **Tests**: ✅ All 11,754 tests pass

## Files Modified

- [DataStructs/LuaStringPool.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/LuaStringPool.cs) - Added `Metamethods` class
- 14 additional files with string literal replacements

## Next Steps (Future Work)

- Create similar constants classes for:
  - Module names (`"string"`, `"table"`, `"math"`, etc.)
  - Common error message prefixes (`"bad argument"`, `"attempt to"`, etc.)
  - Lua type names (`"nil"`, `"boolean"`, `"number"`, etc.)
