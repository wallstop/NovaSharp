# VM Correctness and State Protection Implementation

**Date**: 2025-12-15
**Status**: ✅ **COMPLETE** — Phase 1 (Core Safety) implemented

## Overview

This document details the implementation of the VM Correctness and State Protection initiative (Initiative 12 in PLAN.md). The goal was to make the NovaSharp VM bulletproof against external state corruption while maintaining full Lua compatibility.

## Problem Statement

`DynValue` is a **mutable reference type** by necessity—Lua's closure semantics require that multiple closures can share and mutate the same upvalue. However, this mutability created vectors for external code to corrupt internal VM state.

## Changes Implemented

### 1. Made `DynValue.Assign()` Internal

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/DynValue.cs`

**Change**: Changed `public void Assign(DynValue value)` to `internal void Assign(DynValue value)`

**Rationale**: The `Assign()` method allows mutation of DynValue contents. If the returned `DynValue` from an API call is actually an internal reference (e.g., from a local slot or upvalue), external code could corrupt VM state. By making it internal:

- VM internals (Processor, DebugModule, BasicModule) retain access
- CoreLib modules retain access (same assembly)
- Test projects retain access via `InternalsVisibleTo`
- External user code cannot accidentally corrupt VM state

**Impact**: API breaking change. External code that called `Assign()` must now use `Clone()` and variable assignment instead.

### 2. Fixed `Closure.GetUpValue()` to Return Readonly

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/Closure.cs`

**Changes**:

1. `GetUpValue(int idx)` now returns `AsReadOnly()` instead of the raw internal reference
1. Added `SetUpValue(int idx, DynValue value)` public method for legitimate upvalue modification
1. Added `GetUpValueMutable(int idx)` internal method for VM internals

**Rationale**: Previously, callers could get the internal upvalue reference and mutate it directly:

```csharp
closure.GetUpValue(0).Assign(newValue); // Could corrupt closure state!
```

Now external code must use the explicit `SetUpValue()` method:

```csharp
closure.SetUpValue(0, newValue); // Safe, explicit mutation
```

**Impact**: API behavior change. The doc comment previously encouraged `GetUpValue(idx).Assign(...)` which is now not possible for external callers.

### 3. Fixed Table Key Safety

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/Table.cs`

**Change**: In `Set(DynValue key, DynValue value)`, the key is now made readonly before storage in `_valueMap`:

```csharp
DynValue stableKey = key.ReadOnly ? key : key.AsReadOnly();
PerformTableSet(_valueMap, stableKey, stableKey, value, false, -1);
```

**Rationale**: If a `DynValue` used as a table key is later mutated, the hash table becomes corrupted because the hash code changes but the key remains in its original bucket. Making keys readonly prevents this.

**Impact**: No API change. This is an internal safety improvement.

### 4. Fixed UserData/Thread Hash Codes

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/DynValue.cs`

**Change**: Previously, all `DataType.UserData` and `DataType.Thread` values returned hash code `999`, causing all such values to collide in hash tables. Now:

- `UserData`: Uses the underlying `Object.GetHashCode()` or the `ReferenceId`
- `Thread`: Uses the coroutine's `ReferenceId`

**Rationale**: The constant hash code caused O(n) lookup performance when UserData or Thread values were used as table keys.

**Impact**: Performance improvement. Hash-based lookups are now O(1) for UserData/Thread keys.

### 5. Internal Code Updates

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/BasicModule.cs`

**Change**: Updated `SetEnvironmentOnClosure()` to use `GetUpValueMutable()` instead of `GetUpValue()` since it needs to mutate the internal upvalue.

## Tests Added

**File**: `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs`

New regression test suite with 12 tests covering:

- `GetUpValueReturnsReadonlyCopy` - Verifies GetUpValue returns readonly
- `SetUpValueModifiesClosureUpvalue` - Verifies SetUpValue works correctly
- `SetUpValueThrowsForInvalidIndex` - Verifies bounds checking
- `DebugSetUpValueStillWorks` - Verifies debug.setupvalue compatibility
- `DebugSetLocalIsAvailable` - Verifies debug.setlocal is available
- `TableKeySafetyPreventsHashCorruption` - Verifies table key safety
- `UserDataHashCodesAreDifferent` - Verifies UserData hash codes
- `ThreadHashCodesAreDifferent` - Verifies Thread hash codes
- `AssignIsInternalButAccessibleToTests` - Documents internal access
- `AssignThrowsOnReadonlyValues` - Verifies readonly protection
- `ClosureUpValueSharingStillWorks` - Verifies closure semantics preserved
- `DebugUpValueJoinIsAvailable` - Verifies debug.upvaluejoin available

## Lua Compatibility Verification

All changes preserve Lua semantics:

| Lua Feature               | NovaSharp Behavior                   | Status      |
| ------------------------- | ------------------------------------ | ----------- |
| Closure upvalue sharing   | Multiple closures see mutations      | ✓ Preserved |
| `debug.setupvalue`        | Can modify upvalues                  | ✓ Works     |
| `debug.setlocal`          | Can modify locals                    | ✓ Works     |
| `debug.upvaluejoin`       | Makes closures share upvalues        | ✓ Works     |
| Table reference semantics | Mutations visible through references | ✓ Correct   |
| Table key identity        | Objects use reference identity       | ✓ Correct   |

## API Breaking Changes Summary

| API                         | Old Behavior                       | New Behavior          |
| --------------------------- | ---------------------------------- | --------------------- |
| `DynValue.Assign(DynValue)` | Public method                      | Internal method       |
| `Closure.GetUpValue(int)`   | Returns mutable internal reference | Returns readonly copy |

### New API

| API                                 | Purpose                   |
| ----------------------------------- | ------------------------- |
| `Closure.SetUpValue(int, DynValue)` | Explicit upvalue mutation |

## Test Results

All 5,230+ tests pass, including the new regression tests.

## Related Documents

- [`docs/proposals/vm-correctness.md`](../docs/proposals/vm-correctness.md) - Detailed analysis and design
- PLAN.md §Initiative 12 - VM Correctness and State Protection

## Future Work

The following items from the proposal were not implemented in this phase:

- **Phase 2**: Full public API audit of all methods returning DynValue
- **Phase 3**: Comprehensive documentation of the DynValue contract

These can be addressed in future iterations as needed.
