# Session 076: KopiLua Phase 3 Optimization — Final Allocation Hot-Path Elimination

**Date**: 2025-12-21
**Initiative**: 10 (KopiLua Performance Hyper-Optimization)
**Status**: ✅ Complete — Initiative 10 now fully implemented

______________________________________________________________________

## Summary

This session completes Phase 3 of Initiative 10, implementing the final set of allocation optimizations in the KopiLua-derived string library. Combined with Phase 1 (baseline benchmarks) and Phase 2 (`CharPtr` struct conversion), the string pattern matching subsystem is now significantly more allocation-efficient.

______________________________________________________________________

## Changes Made

### 1. `Capture` Class → Struct Conversion

**File**: [KopiLuaStringLib.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs)

**Before**:

```csharp
public class Capture
{
    public CharPtr Init;
    public int Len;
}
```

**After**:

```csharp
public struct Capture
{
    public CharPtr Init;
    public int Len;
}
```

**Impact**: Each `MatchState` contains a `Capture[]` array with 32 elements (`LuaPatternMaxCaptures`). With `Capture` as a class, this created 32 separate heap objects per `MatchState`. As a struct, the array is now contiguous memory with no individual allocations.

**Allocations Eliminated**: ~32 objects × ~24 bytes = **~768 bytes per MatchState creation**

______________________________________________________________________

### 2. `MatchState` Thread-Local Pooling

**File**: [KopiLuaStringLib.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs)

**Implementation**:

```csharp
[ThreadStatic]
private static MatchState t_cachedMatchState;

private static MatchState RentMatchState()
{
    MatchState ms = t_cachedMatchState;
    if (ms != null)
    {
        t_cachedMatchState = null!;
        ms.Reset();
        return ms;
    }
    return new MatchState();
}

private static void ReturnMatchState(MatchState ms)
{
    t_cachedMatchState = ms;
}
```

Added `Reset()` method to `MatchState`:

```csharp
public void Reset()
{
    matchdepth = MaxRecursion;
    endOnly = false;
    level = 0;
    // Capture array is reused; struct values are overwritten per-capture
}
```

**Usage Sites Updated**:

- `str_find_aux()` — Pattern find/match operations
- `gmatch_aux()` — Global match iterator
- `str_gsub()` — Global substitution

**Impact**: After the first pattern operation per thread, subsequent operations reuse the same `MatchState` instead of allocating a new one.

**Allocations Eliminated**: **~1KB per pattern operation** (after first call)

______________________________________________________________________

### 3. `str_format` Thread-Local Buffer Caching

**File**: [KopiLuaStringLib.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs)

**Before** (allocating per call):

```csharp
char[] formBuffer = new char[MaxFormat];   // ← Allocated every str_format call
char[] buffBuffer = new char[MAX_ITEM];    // ← Allocated every str_format call
```

**After** (thread-local cached):

```csharp
// Thread-local cached buffers at class level
[ThreadStatic]
private static char[] t_formatFormBuffer; // MaxFormat size

[ThreadStatic]
private static char[] t_formatBuffBuffer; // MAX_ITEM size

private static void GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer)
{
    formBuffer = t_formatFormBuffer ??= new char[MaxFormat];
    buffBuffer = t_formatBuffBuffer ??= new char[MAX_ITEM];
}

// In str_format():
GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer);
```

**Impact**: The `MaxFormat` (~25 bytes) and `MAX_ITEM` (512 bytes) buffers are now allocated once per thread and reused across all `string.format` calls.

**Allocations Eliminated**: **~540 bytes per `string.format` call** (after first call per thread)

______________________________________________________________________

### 4. `addquoted()` Pre-Computed Escape Arrays

**File**: [KopiLuaStringLib.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs)

**Before**:

```csharp
if (isfollowedbynum)
    LuaLAddString(b, $"\\{(int)s[0]:D3}");  // ← String interpolation allocation
else
    LuaLAddString(b, $"\\{(int)s[0]}");      // ← String interpolation allocation
```

**After**:

```csharp
// Pre-computed arrays at class level
private static readonly string[] EscapeSequencesWithDigit = new string[256];
private static readonly string[] EscapeSequencesWithoutDigit = new string[256];

static KopiLuaStringLib()
{
    for (int i = 0; i < 256; i++)
    {
        EscapeSequencesWithDigit[i] = $"\\{i:D3}";
        EscapeSequencesWithoutDigit[i] = $"\\{i}";
    }
}

// In addquoted():
if (isfollowedbynum)
    LuaLAddString(b, EscapeSequencesWithDigit[(int)s[0]]);
else
    LuaLAddString(b, EscapeSequencesWithoutDigit[(int)s[0]]);
```

**Impact**: Escape sequences are now simple array lookups instead of runtime string interpolation.

**Allocations Eliminated**: **~24-40 bytes per escaped character**

______________________________________________________________________

## Test Results

| Test Suite                 | Result                  |
| -------------------------- | ----------------------- |
| **StringModuleTUnitTests** | ✅ 613/613 passed       |
| **Full Test Suite**        | ✅ 11,755/11,755 passed |

______________________________________________________________________

## Files Modified

| File                                                                                                                        | Changes                                                                   |
| --------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| [KopiLuaStringLib.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs)                     | Capture→struct, MatchState pooling, buffer reuse, escape arrays           |
| [CharPtrTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Utilities/CharPtrTUnitTests.cs) | Updated tests for struct semantics                                        |
| Multiple test files                                                                                                         | Updated `Token` constructor calls for struct conversion (Phase 2 cleanup) |

______________________________________________________________________

## Initiative 10 Complete Summary

### All Phases

| Phase       | Description                                                           | Status      |
| ----------- | --------------------------------------------------------------------- | ----------- |
| **Phase 1** | Baseline benchmarks established                                       | ✅ Complete |
| **Phase 2** | `CharPtr` class → `readonly struct` conversion                        | ✅ Complete |
| **Phase 3** | `Capture` → struct, `MatchState` pooling, buffer reuse, escape arrays | ✅ Complete |

### Combined Performance Impact

| Scenario         | Latency Improvement | Allocation Reduction |
| ---------------- | ------------------- | -------------------- |
| MatchSimple      | 24-31%              | 58%                  |
| MatchComplex     | 53-56%              | **85%**              |
| GsubSimple       | **60-63%**          | 81%                  |
| GsubWithCaptures | 54-58%              | 79%                  |

### Key Achievements

1. **`CharPtr`**: Converted from class to `readonly struct` — eliminates ~50+ allocations per pattern match
1. **`Capture`**: Converted from class to struct — eliminates 32 allocations per `MatchState`
1. **`MatchState`**: Thread-local pooling — reuses ~1KB object across pattern operations
1. **`str_format` buffers**: Thread-local caching — reuses ~540 bytes across all format calls per thread
1. **`addquoted`**: Pre-computed escape sequences — eliminates per-character string allocations

______________________________________________________________________

## Related Sessions

- [session-074-kopilua-optimization-phase1.md](session-074-kopilua-optimization-phase1.md) — Baseline benchmarks
- [session-075-kopilua-charptr-struct.md](session-075-kopilua-charptr-struct.md) — CharPtr struct conversion

______________________________________________________________________

## PLAN.md Updates

- ✅ Updated Initiative 10 status to **COMPLETE**
- ✅ Updated test count to 11,755
- ✅ Added Phase 3 completion details and final results table
