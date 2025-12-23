# Session 087: VM Execution Allocation Reduction (Initiative 21 Phase 2)

**Date**: 2025-12-22
**Status**: ✅ Complete
**Initiative**: 21 — Performance Parity Analysis
**Phase**: 2 — VM Execution Allocation Reduction

______________________________________________________________________

## Summary

This session implemented comprehensive pooling for VM execution allocations, reducing per-function-call heap allocations by an estimated 200-500+ bytes. All 11,835 tests pass with zero regressions.

______________________________________________________________________

## Changes Made

### 1. LocalScope Array Pooling (CRITICAL)

**Problem**: Every function call allocated a fresh `DynValue[]` for local variables.

**Solution**:

- **`DynValueArrayPool.cs`**: Increased `MaxSmallArraySize` from 8 to 16 to cover most Lua functions
- **`CallStackItem.cs`**: Added `LocalScopeSize` field to track actual size; updated `Reset()` to return array to pool
- **`ProcessorInstructionLoop.cs`**: Modified `ExecBeginFn` to use `DynValueArrayPool.Rent()` instead of `new DynValue[]`

**Files Modified**:

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ArrayPools/DynValueArrayPool.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/CallStackItem.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`

**Estimated Impact**: 32-256 bytes saved per function call

______________________________________________________________________

### 2. ToBeClosedIndices HashSet Pooling (MEDIUM)

**Problem**: `HashSet<int>` allocated lazily for Lua 5.4's to-be-closed variables.

**Solution**:

- **`HashSetPool.cs`**: Added `Rent()` and `Return()` methods for manual lifetime management
- **`CallStackItem.cs`**: Updated `Reset()` to return HashSet to pool
- **`ProcessorInstructionLoop.cs`**: Modified `ExecBeginFn` and `ExecEnter` to use `HashSetPool<int>.Rent()`

**Files Modified**:

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/Pools/HashSetPool.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/CallStackItem.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`

**Estimated Impact**: 48-96 bytes saved per allocation

______________________________________________________________________

### 3. BlocksToClose List Pooling (HIGH)

**Problem**: Outer and inner lists for block closers were not pooled despite helpers existing.

**Solution**:

- **`ListPool.cs`**: Added `Rent()`, `Rent(capacity)`, and `Return()` methods
- **`CallStackItem.cs`**: Updated `Reset()` to return all inner lists and outer list to pools
- **`ProcessorInstructionLoop.cs`**: Integrated pooling in `ExecEnter`, `ProcessBlockClose`, `ProcessBlockCloseReturn`, and `EndBlockClose`

**Files Modified**:

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/Pools/ListPool.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/CallStackItem.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`

**Edge Cases Handled**:

- Inner lists returned at all exit points (early returns, normal processing)
- Empty lists properly returned, not leaked
- Null checks prevent issues when lists are null

**Estimated Impact**: 64-128 bytes saved per block

______________________________________________________________________

### 4. Varargs Array Pooling (MEDIUM)

**Problem**: Varargs functions allocated a fresh `DynValue[]` for `...` parameters.

**Solution**:

- Modified `ExecBeginFn` to use `DynValueArrayPool.Rent()` as working buffer
- Used `DynValueArrayPool.ToArrayAndReturn()` to create exact-size tuple array and return pooled buffer

**Files Modified**:

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`

**Estimated Impact**: 32+ bytes saved per vararg function call

______________________________________________________________________

## Total Estimated Impact

| Component         | Savings Per Occurrence | Frequency                |
| ----------------- | ---------------------- | ------------------------ |
| LocalScope        | 32-256 bytes           | Every function call      |
| ToBeClosedIndices | 48-96 bytes            | Functions with `<close>` |
| BlocksToClose     | 64-128 bytes           | Every block entry        |
| Varargs           | 32+ bytes              | Vararg function calls    |

**Total**: 200-500+ bytes saved per typical function call

______________________________________________________________________

## Test Results

- ✅ All 11,835 tests pass
- ✅ All 83 vararg-specific tests pass
- ✅ All 180 close-related tests pass
- ✅ All 17 CallStackItemPools tests pass
- Zero regressions

______________________________________________________________________

## API Additions

### HashSetPool\<T>

```csharp
// New methods for manual lifetime management
public static HashSet<T> Rent();
public static void Return(HashSet<T> hashSet);
```

### ListPool\<T>

```csharp
// New methods for manual lifetime management
public static List<T> Rent();
public static List<T> Rent(int capacity);
public static void Return(List<T> list);
```

______________________________________________________________________

## Initiative 21 Phase 2 Status

| Task                                        | Status                      | Impact   |
| ------------------------------------------- | --------------------------- | -------- |
| Pool LocalScope arrays per call             | ✅ Complete                 | CRITICAL |
| Pre-allocate BlocksToClose in CallStackItem | ✅ Complete                 | HIGH     |
| Pool ToBeClosedIndices HashSet              | ✅ Complete                 | MEDIUM   |
| Eliminate bitwise lambda captures           | ✅ Done (Initiative 12)     | MEDIUM   |
| Extend DynValue integer cache               | ✅ Done (0-255, -256 to -1) | LOW      |
| Pool ClosureContext arrays                  | ❌ Deferred                 | MEDIUM   |
| Stack-allocate small DynValue arrays        | 🔲 Future                   | MEDIUM   |
| Pool Varargs arrays                         | ✅ Complete                 | MEDIUM   |

**Phase 2 Status**: Mostly complete. `ClosureContext` pooling deferred due to escaping reference complexity.

______________________________________________________________________

## Related Work

- **Initiative 12**: Deep Allocation Analysis (✅ Complete)
- **Initiative 18**: Token Struct Conversion (✅ Complete)
- **Initiative 21 Phase 1**: Script Caching (✅ Complete)
- **Session 086**: Script Compilation Cache implementation

______________________________________________________________________

## Next Steps

1. Run allocation benchmarks to measure actual improvements
1. Consider Phase 3 structural changes (DynValue struct experiment, register-based VM)
1. Document achievable vs. inherent performance gaps
