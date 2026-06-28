# Session 069: Metamethod String-to-Enum Investigation

**Date**: 2025-12-21
**Initiative**: 17 - Lua Method String-to-Enum Optimization
**Status**: ❌ Closed (Not Beneficial)

## Summary

Investigated whether replacing metamethod name strings (e.g., `"__index"`, `"__add"`) with enum values would improve performance. The analysis concluded that **enum conversion is NOT beneficial** due to C# string interning already providing efficient comparisons and the fundamental requirement for string-keyed metatable lookups.

## Investigation Scope

### Files Analyzed

- `DataStructs/LuaStringPool.cs` - Metamethods constants class
- `Execution/VM/Processor/ProcessorInstructionLoop.cs` - VM hot path (30 usages)
- `Interop/BasicDescriptors/DispatchingUserDataDescriptor.cs` - Userdata dispatch (13 usages)
- `CoreLib/StringModule.cs` - String metatable registration (15 usages)
- Other CoreLib modules, Table.cs, ScriptExecutionContext.cs

### Usage Pattern Catalog

| Method                             | Location                      | Purpose                                   |
| ---------------------------------- | ----------------------------- | ----------------------------------------- |
| `GetMetamethod()`                  | ScriptExecutionContext        | Probes userdata descriptor then metatable |
| `GetMetamethodRaw()`               | ScriptExecutionContext        | Metatable-only lookup                     |
| `GetBinaryMetamethod()`            | ScriptExecutionContext        | Binary operator metamethod lookup         |
| `InternalInvokeBinaryMetaMethod()` | ProcessorInstructionLoop      | Stack-based binary metamethod invocation  |
| `InternalInvokeUnaryMetaMethod()`  | ProcessorInstructionLoop      | Stack-based unary metamethod invocation   |
| `MetaIndex()`                      | DispatchingUserDataDescriptor | Userdata metamethod lookup                |

## Key Findings

### 1. C# String Interning for `const string`

C# `const string` values are **automatically interned** at compile time:

- All references to `Metamethods.Index` point to the **same string instance** in memory
- String equality comparisons use **reference equality first** (`object.ReferenceEquals`)
- The JIT can optimize `switch` statements on `const string` values

### 2. Current Performance Characteristics

| Aspect            | Current Pattern    | Notes                           |
| ----------------- | ------------------ | ------------------------------- |
| String allocation | **Zero**           | `const string` = no allocations |
| Comparison cost   | O(1) expected      | Hash-based dictionary lookup    |
| Switch statement  | Compiler-optimized | JIT can use jump tables         |

### 3. String Comparisons Per Operation

| Operation Type          | String Lookups | Dictionary Lookups        |
| ----------------------- | -------------- | ------------------------- |
| Arithmetic (fast path)  | 0              | 0                         |
| Arithmetic (metamethod) | 2              | 2 (userdata) or 1 (table) |
| Table index             | 1              | 1                         |
| Function call           | 1              | 1-2                       |

### 4. Critical Blocker: Metatable String Keys

Lua metatables store metamethods with **string keys**. The Table class uses `LinkedListIndex<string, TablePair>` for string-keyed lookups. Even with an enum, we **must** do a string-keyed lookup into the metatable. An enum would only help if we:

1. Changed the internal metatable representation (massive refactor)
1. Added a parallel enum-keyed cache (extra memory + sync overhead)

## Cost-Benefit Analysis

### Potential Benefits of Enum Approach

| Benefit             | Magnitude    | Notes                                |
| ------------------- | ------------ | ------------------------------------ |
| Comparison speed    | **Marginal** | Int comparison vs hash lookup        |
| Memory              | **None**     | Current strings are interned         |
| Type safety         | **Moderate** | Already achieved with `const string` |
| Switch optimization | **Marginal** | Already optimized by JIT             |

### Costs of Enum Approach

| Cost                   | Magnitude            | Notes                                        |
| ---------------------- | -------------------- | -------------------------------------------- |
| API breaking changes   | **High**             | `GetMetamethod()` takes `string`             |
| Conversion overhead    | **Medium**           | String→Enum conversion at boundaries         |
| Maintenance complexity | **Medium**           | Two representations to keep in sync          |
| Table metatable lookup | **Cannot eliminate** | Lua scripts can set arbitrary metatable keys |

## Verdict: Not Recommended

**Converting to enums is NOT beneficial** because:

1. **String interning already provides O(1) reference equality** for the `const string` pattern
1. **Dictionary lookups are already O(1) amortized** with good hash distribution
1. **The metatable lookup cannot be replaced** - Lua tables fundamentally use string keys
1. **The JIT already optimizes string switches** on `const` values
1. **API compatibility would be broken** for `GetMetamethod()`
1. **Marginal gains don't justify complexity** - hot path gains would be nanoseconds

## Alternative Optimizations Identified

These could be pursued separately if needed:

1. **Dictionary Comparer Alignment**: Change `OrdinalIgnoreCase` to `Ordinal` in `DispatchingUserDataDescriptor._metaMembers`
1. **Metatable Caching**: Cache metamethod lookup results for string metatable at script level
1. **Hot Path Specialization**: Hoist type checks before metamethod lookup in arithmetic operations

## Conclusion

Initiative 17 is **closed as won't-implement**. The current `Metamethods` `const string` pattern from Session 068 is the optimal approach given:

- C#'s automatic string interning for compile-time constants
- The fundamental need for string-keyed metatable access in Lua semantics
- JIT optimizations for string switch statements

The investigation confirmed that the metamethod constant consolidation in Session 068 was the right design choice.
