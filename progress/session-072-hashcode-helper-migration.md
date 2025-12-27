# Session 072: HashCodeHelper Migration

**Date**: 2025-12-21
**Initiative**: 19 - HashCodeHelper Migration
**Status**: ✅ Complete

## Summary

Surveyed all `GetHashCode()` implementations in the runtime code and migrated bespoke hash algorithms (`hash * 31`, inline `StringComparer.Ordinal.GetHashCode()`) to use the centralized `HashCodeHelper` class for consistent, deterministic hashing.

## Background

The project has a `HashCodeHelper` class providing:

- **Deterministic**: Results stable across process boundaries and .NET versions (unlike `System.HashCode`)
- **Efficient**: FNV-1a algorithm with aggressive inlining and cached `TypeTraits<T>`
- **Zero-allocation**: No boxing for value types; uses `EqualityComparer<T>.Default` caching
- **Optimized primitives**: `AddInt()`, `AddLong()`, `AddDouble()` avoid boxing entirely

## Survey Results

All `override int GetHashCode()` implementations in `src/runtime/WallstopStudios.NovaSharp.Interpreter/`:

| File                                       | Type                      | Status                                                 |
| ------------------------------------------ | ------------------------- | ------------------------------------------------------ |
| `DataTypes/DynValue.cs`                    | `DynValue`                | ✅ Already compliant (uses `DeterministicHashBuilder`) |
| `DataTypes/LuaNumber.cs`                   | `LuaNumber`               | ✅ Already compliant (uses `DeterministicHashBuilder`) |
| `DataStructs/TablePair.cs`                 | `TablePair`               | ✅ Already compliant (uses `HashCodeHelper.HashCode`)  |
| `DataStructs/Slice.cs`                     | `Slice`                   | ✅ Already compliant (uses `HashCodeHelper.HashCode`)  |
| `Sandbox/SandboxViolationDetails.cs`       | `SandboxViolationDetails` | ✅ Already compliant (uses `HashCodeHelper.HashCode`)  |
| `Diagnostics/AllocationSnapshot.cs`        | `AllocationSnapshot`      | ✅ Already compliant (uses `HashCodeHelper.HashCode`)  |
| `LuaPort/JacksonSoft.Json/JsonPosition.cs` | `JsonPosition`            | ✅ Already compliant (uses `HashCodeHelper.HashCode`)  |
| `Tree/Token.cs`                            | `Token`                   | ✅ **MIGRATED**                                        |
| `Loaders/ModuleResolutionResult.cs`        | `ModuleResolutionResult`  | ✅ **MIGRATED**                                        |
| `Tree/Expressions/DynamicExpression.cs`    | `DynamicExpression`       | ✅ **MIGRATED**                                        |
| `LuaPort/CharPtr.cs`                       | `CharPtr`                 | ⏭️ Intentionally unchanged (returns 0)                 |

## Migrations Performed

### 1. Token.GetHashCode()

**Before** (`hash * 31` pattern):

```csharp
public override int GetHashCode()
{
    int hash = 17;
    hash = hash * 31 + SourceId;
    hash = hash * 31 + FromCol;
    hash = hash * 31 + FromLine;
    hash = hash * 31 + ToCol;
    hash = hash * 31 + ToLine;
    hash = hash * 31 + PrevCol;
    hash = hash * 31 + PrevLine;
    hash = hash * 31 + (int)Type;
    hash = hash * 31 + (Text != null ? Text.GetHashCode() : 0);
    return hash;
}
```

**After** (HashCodeHelper):

```csharp
public override int GetHashCode()
{
    int baseHash = HashCodeHelper.HashCode(SourceId, FromCol, FromLine, ToCol, ToLine, PrevCol, PrevLine, (int)Type);
    return Text != null ? baseHash ^ HashCodeHelper.HashCode(Text) : baseHash;
}
```

### 2. ModuleResolutionResult.GetHashCode()

**Before** (`hash * 31` pattern):

```csharp
public override int GetHashCode()
{
    int hash = 17;
    hash = hash * 31 + (ModuleName != null ? ModuleName.GetHashCode() : 0);
    hash = hash * 31 + (ResolvedPath != null ? ResolvedPath.GetHashCode() : 0);
    return hash;
}
```

**After** (HashCodeHelper):

```csharp
public override int GetHashCode()
{
    return HashCodeHelper.HashCode(ModuleName, ResolvedPath);
}
```

### 3. DynamicExpression.GetHashCode()

**Before** (inline `StringComparer.Ordinal.GetHashCode()`):

```csharp
public override int GetHashCode()
{
    return Name != null ? StringComparer.Ordinal.GetHashCode(Name) : 0;
}
```

**After** (HashCodeHelper):

```csharp
public override int GetHashCode()
{
    return HashCodeHelper.HashCode(Name);
}
```

## Intentionally Left Unchanged

### CharPtr.GetHashCode() returns 0

This is intentional and correct:

- `CharPtr` is a mutable **class** (not struct) from the KopiLua port
- It simulates C pointer semantics where the `Index` field changes as the "pointer" advances
- Returning 0 is a deliberate design choice to prevent this mutable type from being used as a dictionary key
- Using a real hash would cause bugs if the CharPtr was modified after insertion into a hash-based collection

## Test Results

All 11,754 tests pass:

```
Test run summary: Passed!
  total: 11754
  failed: 0
  succeeded: 11754
  skipped: 0
  duration: 21s
```

## Benefits

1. **Consistency**: All GetHashCode implementations now use a single, well-tested algorithm
1. **Determinism**: Results are stable across process boundaries (important for serialization, caching)
1. **Efficiency**: FNV-1a with aggressive inlining vs manual `hash * 31` multiplication
1. **Maintainability**: Centralized implementation easier to optimize/audit

## Next Steps

- Initiative 19 is now complete
- Consider adding a lint script to detect bespoke hash patterns in new code
- The `HashCodeHelper` guidelines are already documented in `.llm/skills/high-performance-csharp.md`
