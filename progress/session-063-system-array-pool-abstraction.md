# Session 063: SystemArrayPool Abstraction Implementation

**Date**: 2025-12-21\
**Initiative**: 14 - SystemArrayPool Abstraction\
**Status**: ✅ **COMPLETE**

______________________________________________________________________

## Summary

Implemented `SystemArrayPool<T>`, a new array pooling abstraction that wraps `System.Buffers.ArrayPool<T>.Shared` with the project's `PooledResource<T>` disposal pattern. This provides efficient variable-size array pooling for hot paths where the existing exact-size `DynValueArrayPool`/`ObjectArrayPool` are not optimal.

______________________________________________________________________

## Files Created

| File                                                                                                                                                                                                                        | Description                         |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------- |
| [src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/SystemArrayPool.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/SystemArrayPool.cs)                                                     | Main implementation                 |
| [src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataStructs/SystemArrayPoolTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataStructs/SystemArrayPoolTUnitTests.cs) | Comprehensive test suite (41 tests) |

______________________________________________________________________

## Design Decisions

### API Design

The `SystemArrayPool<T>` provides multiple usage patterns:

1. **RAII Pattern (Recommended)** - Automatic cleanup via `PooledResource<T>`:

   ```csharp
   using (PooledResource<char[]> pooled = SystemArrayPool<char>.Get(256, out char[] buffer))
   {
       // buffer.Length >= 256 (may be larger!)
       int written = FormatValue(value, buffer);
       return new string(buffer, 0, written);
   } // Automatically returned to pool
   ```

1. **Manual Lifecycle** - For cases where disposal timing is complex:

   ```csharp
   char[] buffer = SystemArrayPool<char>.Rent(256);
   try
   {
       // Use buffer...
   }
   finally
   {
       SystemArrayPool<char>.Return(buffer);
   }
   ```

1. **Copy-and-Return** - Extract exact-size result and return pooled array:

   ```csharp
   char[] buffer = SystemArrayPool<char>.Rent(1024);
   int actualLength = Fill(buffer);
   return SystemArrayPool<char>.ToArrayAndReturn(buffer, actualLength);
   ```

### Key Semantics

| Aspect                   | Behavior                                                      |
| ------------------------ | ------------------------------------------------------------- |
| **Array Size**           | Arrays may be larger than requested (power-of-2 bucketing)    |
| **Length Tracking**      | Callers must track actual element count separately            |
| **Thread Safety**        | Thread-safe via `ArrayPool<T>.Shared`                         |
| **Clear on Return**      | Default clears arrays for safety; can disable for performance |
| **Large Arrays**         | Arrays >1MB elements are not pooled (prevents memory bloat)   |
| **Zero/Negative Length** | Returns `Array.Empty<T>()` with no-op disposal                |

### Comparison with Existing Pools

| Pool                     | Best For                                   | Allocation Strategy                 |
| ------------------------ | ------------------------------------------ | ----------------------------------- |
| `DynValueArrayPool`      | Fixed small sizes (≤8) for DynValue arrays | Thread-local exact-size caching     |
| `ObjectArrayPool`        | Fixed small sizes (≤8) for object arrays   | Thread-local exact-size caching     |
| **`SystemArrayPool<T>`** | Variable sizes, any element type           | System.Buffers power-of-2 bucketing |

______________________________________________________________________

## Test Coverage

**41 tests** covering:

- Empty/negative length handling (returns `Array.Empty<T>()`)
- Minimum length guarantees across various sizes (1, 5, 10, 16, 32, 100, 256, 1000)
- Value types (`int`, `byte`, `double`)
- Reference types (`string`, `object`)
- Custom struct types
- Null/empty array returns (no-op)
- `PooledResource<T>` disposal and `IsDisposed` state
- Clear-on-return behavior (both enabled and disabled)
- `ToArrayAndReturn` variants (full and partial copy)
- Thread safety (concurrent rent/return cycles from multiple threads)
- Large array handling (1M elements)

______________________________________________________________________

## Use Cases

This pool is designed for:

1. **String Pattern Matching** (KopiLua) - Buffer sizes vary based on pattern complexity
1. **Table Operations** - Dynamic element counts during iteration/mutation
1. **Format String Processing** - Variable output lengths
1. **Any hot path** where exact-size allocation is wasteful

______________________________________________________________________

## Test Results

```
Test run summary: Passed!
  total: 11754
  failed: 0
  succeeded: 11754
  skipped: 0
```

All existing tests continue to pass with the new implementation.

______________________________________________________________________

## Next Steps

1. **Integration** - Use `SystemArrayPool<T>` in KopiLua pattern matching (Initiative 10)
1. **Migration** - Identify existing `new T[]` allocations that could benefit from pooling
1. **Documentation** - Update performance guidelines to reference the new pool

______________________________________________________________________

## Related

- [Initiative 10: KopiLua Performance Hyper-Optimization](../PLAN.md#initiative-10-kopilua-performance-hyper-optimization--high-priority)
- [Initiative 12: Deep Codebase Allocation Analysis](../PLAN.md#initiative-12-deep-codebase-allocation-analysis--reduction--high-priority)
- [High-Performance C# Guidelines](../.llm/skills/high-performance-csharp.md)
