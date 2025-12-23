# Session 064: Initiative 12 Phase 3 — Value Type Migration

> **Date**: 2025-12-21\
> **Status**: ✅ **COMPLETE**\
> **Initiative**: [Initiative 12: Deep Codebase Allocation Analysis & Reduction](../PLAN.md#initiative-12-deep-codebase-allocation-analysis--reduction)\
> **Previous**: [Session 062: Phase 2 Quick Wins](session-062-initiative12-phase2-quick-wins.md)

______________________________________________________________________

## Summary

Completed Phase 3 of Initiative 12, focusing on converting suitable types to `readonly struct` for improved performance through compiler optimizations. Analyzed 15+ candidate types, converted 2 types, and documented rationale for types that cannot be converted.

**Results**:

- ✅ All 11,754 tests pass
- ✅ 2 types converted to `readonly struct`
- ✅ No regressions introduced

______________________________________________________________________

## Conversions Implemented

### 1. TablePair → `readonly struct`

**File**: [src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/TablePair.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/TablePair.cs)

**Before**:

```csharp
public struct TablePair : IEquatable<TablePair>
{
    private readonly DynValue _key;
    private readonly DynValue _value;
    // ...
}
```

**After**:

```csharp
public readonly struct TablePair : IEquatable<TablePair>
{
    private readonly DynValue _key;
    private readonly DynValue _value;
    // ...
}
```

**Rationale**:

- Already immutable (all fields are `readonly`)
- 113 usages across the codebase (Table.cs, TableEnumerators, serialization, interop)
- Small size (~32 bytes with two DynValue references)
- Frequently created during table iteration
- Adding `readonly` modifier enables defensive copy elision by compiler

______________________________________________________________________

### 2. ReflectionSpecialName → `readonly struct`

**File**: [src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/ReflectionSpecialNames.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/ReflectionSpecialNames.cs)

**Before**:

```csharp
internal struct ReflectionSpecialName : IEquatable<ReflectionSpecialName>
{
    public ReflectionSpecialNameType Type { get; }
    public string Argument { get; }

    public ReflectionSpecialName(string name)
    {
        // Multiple branches assigning to Type and Argument
        if (name.StartsWith("op_", StringComparison.Ordinal))
        {
            Type = ...;
            Argument = ...;
        }
        else if (...)
        {
            Type = ...;
            Argument = ...;
        }
        // etc.
    }
}
```

**After**:

```csharp
internal readonly struct ReflectionSpecialName : IEquatable<ReflectionSpecialName>
{
    public ReflectionSpecialNameType Type { get; }
    public string Argument { get; }

    public ReflectionSpecialName(string name)
    {
        // Single assignment via tuple deconstruction
        (Type, Argument) = ParseSpecialName(name);
    }

    private static (ReflectionSpecialNameType Type, string Argument) ParseSpecialName(string name)
    {
        if (name.StartsWith("op_", StringComparison.Ordinal))
        {
            return name switch
            {
                "op_Addition" => (ReflectionSpecialNameType.AdditionOperator, null),
                "op_Subtraction" => (ReflectionSpecialNameType.SubtractionOperator, null),
                // ... etc
            };
        }
        // ... rest of parsing logic
    }
}
```

**Rationale**:

- Conceptually immutable (describes a CLR method special name)
- Used in interop reflection path for operator/property/indexer resolution
- Refactored constructor to use static helper returning tuple, enabling single definite assignment
- Small size (~12-16 bytes)

______________________________________________________________________

## Types Analyzed But Not Converted

### Classes That Cannot Become Structs

| Type             | Location                           | Reason                                                                                         |
| ---------------- | ---------------------------------- | ---------------------------------------------------------------------------------------------- |
| `SymbolRef`      | Execution/Scopes/SymbolRef.cs      | Class with internal mutable fields, complex lifetime management, used in debugger and closures |
| `Closure`        | DataTypes/Closure.cs               | Reference semantics required for identity, caches DynValue wrapper                             |
| `ClosureContext` | Execution/Scopes/ClosureContext.cs | Contains arrays, owned by Closure, reference semantics needed                                  |
| `Table`          | DataTypes/Table.cs                 | Large mutable collection, reference semantics essential                                        |
| `DynValue`       | DataTypes/DynValue.cs              | Already optimized class with caching, would be breaking change                                 |

### Structs That Cannot Become `readonly struct`

| Type                       | Location                                | Reason                                                                                  |
| -------------------------- | --------------------------------------- | --------------------------------------------------------------------------------------- |
| `RuntimeScopeBlock`        | Execution/Scopes/RuntimeScopeBlock.cs   | Properties like `From`, `To`, `FromInclusive`, `ToInclusive` are set after construction |
| `SliceEnumerator<T>`       | DataStructs/Slice.cs                    | `MoveNext()` mutates `_index` field                                                     |
| `TablePairEnumerator`      | DataTypes/TableEnumerators.cs           | Mutable iteration state                                                                 |
| `TableValueEnumerator`     | DataTypes/TableEnumerators.cs           | Mutable iteration state                                                                 |
| `PooledResource<T>`        | DataStructs/PooledResource.cs           | Mutable `_disposed` and `_suppressed` flags                                             |
| `DeterministicHashBuilder` | DataStructs/DeterministicHashBuilder.cs | Builder pattern with mutable state                                                      |
| `SourceRef`                | Debugging/SourceRef.cs                  | Has mutable `Breakpoint` property                                                       |
| `Token`                    | Tree/Lexer/Token.cs                     | Has mutable `Text` property                                                             |
| `CallbackArguments`        | DataTypes/CallbackArguments.cs          | Holds reference to mutable array, internal offset tracking                              |

### Already Optimized as `readonly struct`

| Type                      | Location                              |
| ------------------------- | ------------------------------------- |
| `LuaNumber`               | DataTypes/LuaNumber.cs                |
| `SandboxViolationDetails` | Sandboxing/SandboxViolationDetails.cs |
| `AllocationSnapshot`      | Sandboxing/AllocationSnapshot.cs      |
| `ModuleResolutionResult`  | Modules/ModuleResolutionResult.cs     |

______________________________________________________________________

## `in` Parameter Analysis

Analyzed `LuaNumber` for `in` parameter usage:

- **Size**: 9 bytes (8-byte double + 1-byte bool for integer flag)
- **Recommendation**: Too small to benefit from `in` parameters (threshold ~16 bytes)
- **Conclusion**: No `in` parameter changes needed for current codebase

______________________________________________________________________

## Test Results

```
Test run summary: Passed! 
  total: 11754
  failed: 0
  succeeded: 11754
  skipped: 0
  duration: 29s 338ms
```

______________________________________________________________________

## Files Modified

1. **TablePair.cs** — Added `readonly` modifier to struct declaration
1. **ReflectionSpecialNames.cs** — Added `readonly` modifier, refactored constructor with static helper

______________________________________________________________________

## Phase 3 Checklist Status

From [PLAN.md](../PLAN.md):

- [x] Identify candidate classes for struct conversion (analysis tool) — Analyzed 15+ types
- [x] Convert high-impact types to `readonly struct` or `ref struct` — 2 conversions
- [x] Add `in` parameters for large structs passed by value — N/A (no large structs found)
- [ ] Audit generic constraints for missing `struct`/`class` specifiers — Deferred to future session

______________________________________________________________________

## Recommendations for Future Work

1. **Generic Constraint Audit**: Review generic types in `DataStructs/` and `Helpers/` for missing `where T : struct` constraints that could improve JIT optimization
1. **Span-Based Overloads**: Add `ReadOnlySpan<DynValue>` overloads to `Script.Call()` methods (P3.1 from Phase 1 analysis)
1. **KopiLua MatchState**: Consider `ref struct` for pattern matching state (Initiative 10)

______________________________________________________________________

## Related Documents

- [PLAN.md — Initiative 12](../PLAN.md#initiative-12-deep-codebase-allocation-analysis--reduction)
- [Phase 1 Analysis](../docs/performance/allocation-analysis-initiative12-phase1.md)
- [Phase 2 Quick Wins](session-062-initiative12-phase2-quick-wins.md)
- [High-Performance C# Guidelines](../.llm/skills/high-performance-csharp.md)
