# Session 079: Comprehensive Helper Performance Audit

**Date:** 2025-12-22
**Initiative:** Initiative 11 - Helper Performance Audit
**Focus:** `src/runtime/WallstopStudios.NovaSharp.Interpreter/` helper and utility methods

## Summary

Audited all helper methods in the DataStructs, Utilities, and Execution directories for performance issues. Identified and implemented Critical/High priority optimizations by adding `[MethodImpl(AggressiveInlining)]` attributes to frequently-called small methods.

## Files Audited

### DataStructs Directory

| File                     | Lines | Analysis                                              |
| ------------------------ | ----- | ----------------------------------------------------- |
| `FastStack.cs`           | 197   | **Critical** - Core VM stack operations lack inlining |
| `FastStackDynamic.cs`    | 133   | **High** - Alternative stack impl, same issue         |
| `ExtensionMethods.cs`    | 55    | **High** - Dictionary helpers used frequently         |
| `Slice.cs`               | 335   | **High** - Index calculation in tight loops           |
| `HashCodeHelper.cs`      | 438   | ✅ Already optimized with inlining                    |
| `IListSortExtensions.cs` | 597   | ✅ Already optimized with inlining                    |
| `LuaStringPool.cs`       | 253   | ✅ Already optimized with inlining                    |
| `DynValueArrayPool.cs`   | 185   | ✅ Well-designed thread-local pooling                 |
| `ObjectArrayPool.cs`     | 187   | ✅ Well-designed thread-local pooling                 |
| `CollectionPools.cs`     | 219   | ✅ Good design, RAII pattern                          |
| `GenericPool.cs`         | 122   | ✅ Thread-safe generic pooling                        |
| `PooledResource.cs`      | 67    | ✅ Struct-based RAII, efficient                       |
| `ZStringBuilder.cs`      | 179   | ✅ Thin wrapper over ZString                          |
| `MultiDictionary.cs`     | 129   | Low - Not on critical path                            |
| `LinkedListIndex.cs`     | 123   | Low - Not on critical path                            |
| `SystemArrayPool.cs`     | 208   | ✅ Good design                                        |

### Utilities Directory

| File                      | Lines | Analysis                                         |
| ------------------------- | ----- | ------------------------------------------------ |
| `LuaNumberHelpers.cs`     | 242   | Medium - Validation methods, exceptions allocate |
| `StringSpanExtensions.cs` | 46    | **High** - Span helpers lack inlining            |
| `PathSpanExtensions.cs`   | 100   | Medium - Path helpers lack inlining              |

### Execution Directory

| File                  | Lines | Analysis                                       |
| --------------------- | ----- | ---------------------------------------------- |
| `LuaIntegerHelper.cs` | 121   | **Critical** - Shift/conversion ops in VM loop |

## Findings Table

| Priority | File                      | Method(s)                                      | Issue                                      | Status   |
| -------- | ------------------------- | ---------------------------------------------- | ------------------------------------------ | -------- |
| Critical | `FastStack.cs`            | `Push`, `Pop`, `Peek`, `Set`, indexer, `Count` | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| Critical | `LuaIntegerHelper.cs`     | `TryGetInteger`, `ShiftLeft`, `ShiftRight`     | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| High     | `FastStackDynamic.cs`     | `Push`, `Pop`, `Peek`, `Set`                   | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| High     | `ExtensionMethods.cs`     | `GetOrDefault`                                 | Missing inlining, verbose branching        | ✅ Fixed |
| High     | `Slice.cs`                | `CalcRealIndex`, indexer                       | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| High     | `StringSpanExtensions.cs` | `TrimWhitespace`, `HasContent`                 | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| Medium   | `PathSpanExtensions.cs`   | `SliceAfterLastSeparator`                      | Missing `[MethodImpl(AggressiveInlining)]` | ✅ Fixed |
| Medium   | `LuaNumberHelpers.cs`     | Validation methods                             | Exception message string interpolation     | Deferred |
| Low      | `MultiDictionary.cs`      | `Add`, `Find`                                  | Could use collection expressions           | Deferred |

## Changes Implemented

### 1. FastStack.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- Indexer getter/setter
- `Push()` method
- `Pop()` method
- `Peek()` method
- `Set()` method
- `Count` property getter

Also simplified `Peek()` to avoid intermediate variable.

### 2. FastStackDynamic.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- `Set()` method
- `Push()` method
- `Peek()` method
- `Pop()` method

### 3. ExtensionMethods.cs

Added `[MethodImpl(AggressiveInlining)]` to `GetOrDefault()` and simplified the implementation from if-else to ternary.

### 4. LuaIntegerHelper.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- `TryGetInteger(double, out long)` - Core numeric conversion
- `ShiftLeft()` - Bitwise operation
- `ShiftRight()` - Bitwise operation

### 5. StringSpanExtensions.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- `TrimWhitespace()`
- `HasContent()`

### 6. PathSpanExtensions.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- `SliceAfterLastSeparator()`

### 7. Slice.cs

Added `[MethodImpl(AggressiveInlining)]` to:

- Indexer getter/setter
- `CalcRealIndex()` - Called on every index operation

## Deferred Work

### LuaNumberHelpers.cs Exception Messages

The validation methods use string interpolation in exception messages:

```csharp
$"bad argument #{argIndex} to '{functionName}' (number has no integer representation)"
```

This allocates on every throw. However:

1. These are error paths, not hot paths
1. The allocation happens only when throwing
1. Caching would complicate the code

**Recommendation:** Leave as-is. The performance cost is negligible since exceptions are exceptional.

## Test Results

All 11,790 tests pass after changes.

## Performance Impact

The `[MethodImpl(AggressiveInlining)]` attribute hints to the JIT compiler to inline these methods at call sites. For hot-path methods like stack operations that execute millions of times:

- **Eliminates method call overhead** (stack frame setup, parameter passing)
- **Enables further optimizations** (register allocation, constant folding)
- **Reduces branch mispredictions** (no call/ret instructions)

Expected improvement: 5-15% on VM-intensive benchmarks, though actual impact depends on workload.

## Next Steps

1. Run benchmarks to measure actual performance impact
1. Consider similar audit for:
   - `Execution/VM/` bytecode execution methods
   - `Interop/` CLR interop helpers
   - `DataTypes/` DynValue operations
1. Profile to identify remaining allocation hotspots
