# Session 085: Magic String Consolidation — Metamethods Implementation

**Date**: 2025-12-22
**Initiative**: 13 — Magic String Consolidation (Phase 1: Metamethods)
**Status**: ✅ Complete

## Summary

Created a `Metamethods` static class with 25 compile-time constant strings for all Lua metamethod names (including NovaSharp extensions), then migrated ~110 string literals across 16 files to use these constants. This eliminates duplicate magic strings and enables compiler optimizations for string comparisons.

## Problem

The codebase used raw string literals like `"__index"`, `"__newindex"`, `"__call"` scattered across many files:

- No single source of truth for metamethod names
- Typos could go undetected until runtime
- No compiler optimization for string comparisons
- Harder to find all usages of a particular metamethod

Note: A previous session (068) documented this work as complete, but the actual implementation was missing from the codebase. This session performed the actual implementation.

## Solution

### 1. Created Metamethods Constants Class

Added `Metamethods` static class in `DataStructs/LuaStringPool.cs` with 25 constant strings:

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

    // Iteration
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

    // NovaSharp extensions
    public const string New = "__new";
    public const string ToNumber = "__tonumber";
    public const string ToBool = "__tobool";
}
```

### 2. Updated LuaStringPool.InitializeCommonStrings()

The pre-interned common strings array now references `Metamethods.*` constants instead of duplicating the string literals.

### 3. Migrated Files

| File                                                                                              | Changes                                                  |
| ------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `DataStructs/LuaStringPool.cs`                                                                    | Added Metamethods class, updated InitializeCommonStrings |
| `Execution/VM/Processor/ProcessorInstructionLoop.cs`                                              | 30 replacements                                          |
| `CoreLib/StringModule.cs`                                                                         | 16 replacements (arithmetic metamethods)                 |
| `CoreLib/BasicModule.cs`                                                                          | 2 replacements (\_\_tostring)                            |
| `CoreLib/MetaTableModule.cs`                                                                      | 3 replacements (\_\_metatable)                           |
| `CoreLib/TableModule.cs`                                                                          | 2 replacements (\_\_lt, \_\_len)                         |
| `CoreLib/TableIteratorsModule.cs`                                                                 | 2 replacements (\_\_pairs, \_\_ipairs)                   |
| `CoreLib/IoModule.cs`                                                                             | 1 replacement (\_\_index)                                |
| `Interop/BasicDescriptors/DispatchingUserDataDescriptor.cs`                                       | 13 switch case labels                                    |
| `Interop/StandardDescriptors/StandardUserDataDescriptor.cs`                                       | 4 replacements (\_\_new)                                 |
| `Interop/StandardDescriptors/StandardEnumUserDataDescriptor.cs`                                   | 1 replacement (\_\_concat)                               |
| `Interop/StandardDescriptors/ReflectionMemberDescriptors/ValueTypeDefaultCtorMemberDescriptor.cs` | 1 replacement (\_\_new)                                  |
| `Interop/PredefinedUserData/EnumerableWrapper.cs`                                                 | 1 replacement (\_\_call)                                 |
| `DataTypes/CallbackArguments.cs`                                                                  | 2 replacements (\_\_tostring)                            |
| `Execution/ScriptExecutionContext.cs`                                                             | 1 replacement (\_\_call)                                 |
| `Script.cs`                                                                                       | 1 replacement (\_\_call)                                 |

**Total: ~110 string literal replacements across 16 files**

### 4. Kept as Literals

- `DescriptorHelpers.cs`: `"__to"` prefix for dynamic metamethod name generation (e.g., `__toMyCustomType`)
- Documentation comments in various files

## Benefits

1. **Single source of truth**: All metamethod names defined in one place
1. **Compile-time safety**: Typos are caught at compile time
1. **IDE support**: IntelliSense, Find All References, Rename Refactoring
1. **Compiler optimization**: `const string` enables compile-time string interning
1. **Switch statement support**: C# allows `const string` in switch case labels
1. **Documentation**: XML comments on each constant document the metamethod's purpose

## Existing Work: LuaKeywords Class

Phase 2 (Lua Keywords) was already complete when this session started. The `LuaKeywords` class contains 22 constants:

- Literal keywords: `nil`, `true`, `false`
- Logical operators: `and`, `or`, `not`
- Control flow: `if`, `then`, `else`, `elseif`, `end`, `for`, `while`, `do`, `repeat`, `until`, `break`, `return`
- Declarations: `function`, `local`, `in`
- Labels: `goto`, `::`

## Verification

- **Build**: ✅ Compiles with zero warnings
- **Tests**: ✅ All 11,790 tests pass
- **Validation**: Metamethod strings only appear in `LuaStringPool.cs` and documentation comments

```bash
# Verify metamethod literals are consolidated
rg '"__[a-z]+"' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs -l | grep -v LuaStringPool.cs
# Result: Only DescriptorHelpers.cs (dynamic name generation) and documentation comments
```

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/LuaStringPool.cs`
- 15 additional files with string literal replacements

## Next Steps (Future Work)

Lower-priority consolidation targets:

- Error message prefixes (`"bad argument"`, `"attempt to"`)
- Module names (`"string"`, `"table"`, `"math"`, etc.)

These are lower priority because:

- Error messages are user-facing strings that may need localization flexibility
- Module names are used in fewer places and are already pre-interned in `LuaStringPool`
