# Session 090: Span-Based Array Operation Migration Phase 1

**Date**: 2025-12-22\
**Initiative**: 23 — Span-Based Array Operation Migration\
**Status**: ✅ Phase 1 Complete

## Overview

This session audited and migrated allocating array operations (`Split()`, `Substring()`, `ToCharArray()`) in the runtime code to span-based alternatives where beneficial.

## Audit Results

### string.Split() (3 calls found)

| Priority | File                      | Usage                                          | Status                                      |
| -------- | ------------------------- | ---------------------------------------------- | ------------------------------------------- |
| MEDIUM   | `DescriptorHelpers.cs`    | `NormalizeUppercaseRuns` fuzzy name generation | ✅ Already migrated to span-based iteration |
| LOW      | `ResourceScriptLoader.cs` | Assembly namespace extraction (cold path)      | Keep as-is                                  |
| LOW      | `Hardwire (tooling)`      | Type name parsing (cold path)                  | Keep as-is                                  |

**Finding**: The interpreter's hot paths do not use `string.Split()`. The one warm-path usage was already migrated.

### string.Substring() (21 calls found)

| Priority | Count | Status                                           |
| -------- | ----- | ------------------------------------------------ |
| HIGH     | 3     | ✅ Migrated (StringRange, string.byte)           |
| MEDIUM   | 9     | Defer (LexerUtils, OsTimeModule - warm paths)    |
| LOW      | 9     | Keep as-is (cold paths, result stored as string) |

**High-Impact Migration Completed**:

- Added `StringRange.ApplyToSpan()` returning `ReadOnlySpan<char>` for iteration-only scenarios
- Updated `PerformByteLike()` in `StringModule.cs` to use `ApplyToSpan()` instead of `ApplyToString()`
- Eliminates substring allocation for every `string.byte()` and `string.unicode()` call

### ToCharArray() (2 calls found)

| Priority | File                   | Usage                   | Status                                      |
| -------- | ---------------------- | ----------------------- | ------------------------------------------- |
| MEDIUM   | `StringModule.cs`      | `string.reverse()`      | ✅ Optimized with stackalloc for ≤256 chars |
| LOW      | `CharPtr.cs` (KopiLua) | Pattern matching buffer | Keep as-is (mutable array required)         |

## Changes Made

### 1. StringRange.ApplyToSpan() — NEW METHOD

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringLib/StringRange.cs`

```csharp
/// <summary>
/// Applies the range to a string using Lua's substring semantics (1-based, inclusive, clamped),
/// returning a span instead of allocating a new string.
/// </summary>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public ReadOnlySpan<char> ApplyToSpan(string value)
{
    // Same clamping logic as Apply()...
    return value.AsSpan(i - 1, j - i + 1);
}
```

### 2. string.byte() / string.unicode() — ZERO-ALLOC SUBSTRING

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs`

**Before**:

```csharp
string s = range.ApplyToString(vs.String);  // Allocates substring
```

**After**:

```csharp
ReadOnlySpan<char> span = range.ApplyToSpan(vs.String);  // Zero allocation
```

### 3. string.reverse() — STACKALLOC OPTIMIZATION

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs`

**Before**:

```csharp
char[] elements = argS.String.ToCharArray();  // Always heap allocates
Array.Reverse(elements);
```

**After**:

```csharp
const int StackAllocThreshold = 256;

if (str.Length <= StackAllocThreshold)
{
    Span<char> buffer = stackalloc char[str.Length];  // Stack allocation
    str.AsSpan().CopyTo(buffer);
    buffer.Reverse();  // MemoryExtensions.Reverse
    return DynValue.NewString(new string(buffer));
}
else
{
    // Fallback to heap allocation for large strings
    char[] elements = str.ToCharArray();
    Array.Reverse(elements);
    return DynValue.NewString(new string(elements));
}
```

## Validation

- **All 11,835 tests pass** with zero regressions
- **Code formatted** with CSharpier

## Impact Analysis

| Operation                 | Frequency               | Allocation Saved     |
| ------------------------- | ----------------------- | -------------------- |
| `string.byte(s, i, j)`    | High (string iteration) | 1 substring per call |
| `string.unicode(s, i, j)` | Medium                  | 1 substring per call |
| `string.reverse(s)`       | Low (≤256 chars)        | 1 char[] per call    |

## Deferred Items (Phase 2)

These items were identified but deferred as lower priority:

1. **LexerUtils hex float parsing** (4 Substring calls) — Warm path, one-time per script load
1. **OsTimeModule format parsing** — Low frequency, `os.date` is rarely called
1. **KopiLua str_format precision handling** (2 calls) — Would require broader refactoring

## Recommendations

The high-impact optimizations are complete. The remaining Substring/Split operations are either:

- In cold paths (initialization, error handling)
- Storing results as strings (cannot use spans)
- Part of one-time parsing (script loading)

**Recommendation**: Close Initiative 23 Phase 1 as complete. Phase 2 (LexerUtils) can be done opportunistically.
