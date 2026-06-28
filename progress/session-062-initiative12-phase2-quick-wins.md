# Session 062: Initiative 12 Phase 2 Quick Wins — Allocation Reduction

> **Date**: December 21, 2025
> **Status**: ✅ Complete
> **Related**: [PLAN.md Initiative 12](../PLAN.md), [Phase 1 Analysis](../docs/performance/allocation-analysis-initiative12-phase1.md)

______________________________________________________________________

## Summary

Implemented the **Phase 2 Immediate (High-Impact, Low-Risk)** allocation reductions from Initiative 12. All 11,713 tests pass.

## Changes Made

### Files Modified

1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs`
1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/CallStackItem.cs`
1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`
1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs`

______________________________________________________________________

## Priority Items Completed

### P1.1: Pre-allocate BlocksToClose Inner Lists ✅

**Problem**: `ExecEnter` allocated `new List<SymbolRef>()` for every block entry with closers.

**Solution**:

- `ExecEnter`: Changed to use `ListPool<SymbolRef>.Rent()` for pooled list allocation
- `ExecBeginFn`: Changed to use `ListPool<SymbolRef>.Rent()` for pooled list allocation
- `CloseCurrentBlock`: Added `ListPool<SymbolRef>.Return(list)` to return inner lists to pool after use
- `CloseAllPendingBlocks`: Added `ListPool<SymbolRef>.Return(list)` to return inner lists to pool after use
- `CallStackItem.Reset()`: Added loop to return all inner `List<SymbolRef>` lists to pool before clearing
- **Added Rent/Return API to `ListPool<T>`**: New `Rent()` and `Return()` methods for manual lifecycle management

**Estimated Savings**: 32+ bytes per block entry

______________________________________________________________________

### P1.2: Replace Lambda Delegates with Static Delegates for Bitwise Ops ✅

**Problem**: `ExecBitwiseBinary` used inline lambdas like `(x, y) => x & y` which allocated closures.

**Solution**:
Added static readonly delegates:

- `s_bitwiseAnd` — `(x, y) => x & y`
- `s_bitwiseOr` — `(x, y) => x | y`
- `s_bitwiseXor` — `(x, y) => x ^ y`
- `s_leftShift` — `(x, y) => x << (int)y`
- `s_rightShift` — `(x, y) => x >> (int)y`

Updated `ExecBNot`, `ExecBAnd`, `ExecBOr`, `ExecBXor`, `ExecShl`, `ExecShr` to use static delegates.

**Estimated Savings**: 256-512 bytes total (one-time)

______________________________________________________________________

### P1.3: Use DynValueArrayPool for varargs and pcall/xpcall ⏸️ (Deferred)

**Analysis**: The arrays in varargs (`ProcessorInstructionLoop.cs#L1209`) and pcall/xpcall (`ErrorHandlingModule.cs`) have lifetimes that extend beyond the method scope — they're stored in `DynValue.NewTuple` results. Direct pooling would require copying, which may not provide net benefits.

**Resolution**: Deferred to Phase 3 for deeper analysis of pooled-to-exact-array conversion patterns.

______________________________________________________________________

### P1.4: Use DynValueArrayPool for Coroutine.Resume args ⏸️ (Deferred)

**Analysis**: Similar to P1.3, the `DynValue[]` array is passed to `coroutineStack.Push()` and may be stored via tuple creation in the coroutine resume flow.

**Resolution**: Deferred to Phase 3.

______________________________________________________________________

### P1.5: Use Static Delegates for String Metatable Operations ✅

**Problem**: `StringModule.SetupMathOperators` used inline lambdas for `__add`, `__sub`, etc., allocating closures each time.

**Solution**:
Added 8 static readonly callback delegates:

- `s_add`, `s_sub`, `s_mul`, `s_div`
- `s_mod`, `s_pow`, `s_unm`, `s_idiv`

Converted `SetupMathOperators` to use pre-allocated static callbacks with corresponding static callback methods.

**Estimated Savings**: ~512 bytes (one-time)

______________________________________________________________________

## Build & Test Status

| Check           | Result                                |
| --------------- | ------------------------------------- |
| Build (Release) | ✅ Successful                         |
| All Tests       | ✅ 11,713 passed, 0 failed, 0 skipped |

______________________________________________________________________

## Estimated Allocation Savings

| Item                              | Savings                        |
| --------------------------------- | ------------------------------ |
| P1.1 (BlocksToClose inner lists)  | 32+ bytes per block entry      |
| P1.2 (Bitwise static delegates)   | 256-512 bytes total (one-time) |
| P1.5 (String metatable delegates) | ~512 bytes (one-time)          |

**Total**:

- **One-time initialization**: ~768-1024 bytes saved
- **Per-operation**: 32+ bytes per block entry (significant for deep nesting or loops)

______________________________________________________________________

## Issues Encountered

1. **ListPool\<T> API missing Rent/Return**: Added manual `Rent()` and `Return()` methods to `ListPool<T>` to support scenarios where lists have longer lifetimes than a single scope.

1. **P1.3/P1.4 lifetime issues**: Arrays created in varargs, pcall/xpcall, and coroutine resume flows are stored in tuples or other data structures that outlive the method. Pooling would require copying to new arrays, which could negate the benefits. Deferred for Phase 3 evaluation.

1. **CallStackItem.Reset() update**: The `Reset()` method needed to return inner `List<SymbolRef>` lists to the pool before clearing the outer list, preventing pool leakage.

______________________________________________________________________

## Next Steps (Phase 3)

From the analysis document:

| Priority | Site       | File                                           | Change                                  |
| -------- | ---------- | ---------------------------------------------- | --------------------------------------- |
| P2.1     | 19, 20     | KopiLuaStringLib.cs                            | Pool `MatchState` and char buffers      |
| P2.2     | 4          | ProcessorInstructionLoop.cs                    | Pool `LocalScope` arrays for ≤16 locals |
| P2.3     | 13, 14, 18 | TableModule.cs, StringModule.cs, Utf8Module.cs | Pool result arrays                      |
| P2.4     | 21         | LuaState.cs                                    | Use `DynValueArrayPool` for PopResults  |
| P2.5     | 22-26      | Interop descriptors                            | Verify `ObjectArrayPool` usage          |
