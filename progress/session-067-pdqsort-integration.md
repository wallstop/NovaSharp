# Session 067: Boxing-Free pdqsort Integration

**Date**: 2025-12-21
**Initiative**: 16 - Boxing-Free pdqsort Integration
**Status**: ✅ Complete

## Summary

Integrated the boxing-free pattern-defeating quicksort (pdqsort) implementation from Initiative 15 into all sorting call sites in the NovaSharp interpreter runtime. This eliminates comparer boxing overhead in hot paths.

## Problem

.NET's built-in `List<T>.Sort(IComparer<T>)` and `Array.Sort(T[], IComparer<T>)` methods box value-type comparers on every call. Even though NovaSharp had converted several comparers to `readonly struct` (Initiative 12 Phase 4), the boxing still occurred at the call site.

## Solution

Used `IListSortExtensions.Sort<T, TComparer>()` which uses generic `TComparer : IComparer<T>` constraints to avoid boxing. This method was implemented in Initiative 15 using pattern-defeating quicksort (pdqsort) for excellent performance.

## Changes Made

### 1. CoreLib/TableModule.cs

**Before:**

```csharp
// Use struct comparer to avoid closure allocation
values.Sort(new LuaSortComparer(executionContext, lt));
```

**After:**

```csharp
// Use struct comparer with boxing-free pdqsort (Initiative 16)
values.Sort<DynValue, LuaSortComparer>(new LuaSortComparer(executionContext, lt));
```

This is a **hot path** - `table.sort` is frequently called in Lua scripts.

### 2. Interop/StandardDescriptors/ReflectionMemberDescriptors/OverloadedMethodMemberDescriptor.cs

**Before:**

```csharp
private sealed class OverloadableMemberDescriptorComparer
    : IComparer<IOverloadableMemberDescriptor>
{
    public static readonly OverloadableMemberDescriptorComparer Instance = new();
    private OverloadableMemberDescriptorComparer() { }
    // ...
}

// Call site:
_overloads.Sort(OverloadableMemberDescriptorComparer.Instance);
```

**After:**

```csharp
private readonly struct OverloadableMemberDescriptorComparer
    : IComparer<IOverloadableMemberDescriptor>
{
    // No singleton needed - struct uses default value
    // ...
}

// Call site:
_overloads.Sort<IOverloadableMemberDescriptor, OverloadableMemberDescriptorComparer>(default);
```

This is a **cold path** (runs once per overloaded method registration), but the conversion:

- Eliminates the singleton instance allocation
- Uses `default` instead of `Instance` for cleaner zero-allocation pattern

## Audit Results

Found **2 sorting call sites** in the runtime codebase:

| Location                              | Comparer Type                                         | Hot/Cold Path | Migrated |
| ------------------------------------- | ----------------------------------------------------- | ------------- | -------- |
| `TableModule.cs`                      | `LuaSortComparer` (struct)                            | Hot           | ✅       |
| `OverloadedMethodMemberDescriptor.cs` | `OverloadableMemberDescriptorComparer` (class→struct) | Cold          | ✅       |

No `Array.Sort()` calls were found. No LINQ ordering or sorted collections found.

## Verification

- **Build**: ✅ Compiles with zero warnings
- **Tests**: ✅ All 11,754 tests pass (including 190 TableModule and OverloadedMethod tests)

## Performance Impact

- **`table.sort`**: Zero boxing allocations per call (previously boxed on every sort)
- **Overload resolution**: Eliminated singleton allocation, zero boxing on sort

## Files Modified

- [CoreLib/TableModule.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/TableModule.cs)
- [Interop/StandardDescriptors/ReflectionMemberDescriptors/OverloadedMethodMemberDescriptor.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/StandardDescriptors/ReflectionMemberDescriptors/OverloadedMethodMemberDescriptor.cs)
