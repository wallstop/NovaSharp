# Progress: JSON Integer/Float Subtype Preservation

**Date**: 2025-12-11
**Task**: Fix JSON serialization to preserve Lua 5.3+ integer vs float distinction
**Status**: ✅ Complete
**Related Sections**: PLAN.md §8.24 (Dual Numeric Type System Phase 4), §8.37 (LuaNumber Usage Audit)

______________________________________________________________________

## Summary

Fixed JSON serialization in `JsonTableConverter` to preserve integer subtype information. Before this fix, all numbers (including integers) were converted to double format during JSON serialization, which caused:

1. Large integers (>2^53) to lose precision
1. Integer values to be serialized with unnecessary decimal points (e.g., `42.0` instead of `42`)
1. Inconsistent JSON output between integer and float DynValues

## Problem Statement

Before this fix:

- `ValueToJson()` called `value.Number.ToString("r", ...)` which converts all numbers to double format
- `ObjectToJsonCore()` converted all CLR numeric types to double before serialization
- Large integers like `9007199254740993` (2^53 + 1) would lose precision when serialized

## Solution

### Modified `ValueToJson()` Method

For `DataType.Number`, now checks `value.IsInteger` and uses the appropriate serialization:

**File**: `JsonTableConverter.cs`

```csharp
case DataType.Number:
    // Preserve integer/float subtype distinction for Lua 5.3+ compliance
    // Integer values serialize without decimal point for better JSON semantics
    if (value.IsInteger)
    {
        sb.Append(value.LuaNumber.AsInteger.ToString(CultureInfo.InvariantCulture));
    }
    else
    {
        sb.Append(value.Number.ToString("r", CultureInfo.InvariantCulture));
    }
    break;
```

### Modified `ObjectToJsonCore()` Method

Replaced bulk numeric type handling with individual type-specific serialization to preserve integer representation:

```csharp
// Handle numeric types - preserve integer representation for better precision
switch (obj)
{
    case sbyte sb1:
        sb.Append(sb1.ToString(CultureInfo.InvariantCulture));
        return;
    case byte b1:
        sb.Append(b1.ToString(CultureInfo.InvariantCulture));
        return;
    case int i1:
        sb.Append(i1.ToString(CultureInfo.InvariantCulture));
        return;
    case long l1:
        sb.Append(l1.ToString(CultureInfo.InvariantCulture));
        return;
    // ... etc for all integer types
    case float f1:
        sb.Append(f1.ToString("r", CultureInfo.InvariantCulture));
        return;
    case double d1:
        sb.Append(d1.ToString("r", CultureInfo.InvariantCulture));
        return;
    case decimal dec1:
        sb.Append(dec1.ToString(CultureInfo.InvariantCulture));
        return;
}
```

### Modified Enum Handling

Enums are now serialized as integers (using `ToInt64`) instead of doubles:

```csharp
if (obj is Enum enumValue)
{
    // Use the underlying integer type for better JSON representation
    sb.Append(
        Convert
            .ToInt64(enumValue, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture)
    );
    return;
}
```

## Files Modified

### Production Code

| File                                       | Change                                                       |
| ------------------------------------------ | ------------------------------------------------------------ |
| `Serialization/Json/JsonTableConverter.cs` | Updated `ValueToJson()` for integer/float preservation       |
| `Serialization/Json/JsonTableConverter.cs` | Updated `ObjectToJsonCore()` for type-specific serialization |
| `Serialization/Json/JsonTableConverter.cs` | Updated enum handling to use integer serialization           |

### Test Code

| File                                                      | Change                                           |
| --------------------------------------------------------- | ------------------------------------------------ |
| `SerializationTests/Json/JsonTableConverterTUnitTests.cs` | Added 6 new tests for integer/float preservation |

## New Tests

Six regression tests were added to verify correct serialization:

1. **TableToJsonPreservesIntegerSubtype**: Verifies integer values serialize without decimal points
1. **TableToJsonPreservesFloatSubtype**: Verifies float values serialize correctly
1. **TableToJsonArrayPreservesIntegerSubtype**: Verifies array elements serialize as integers
1. **TableToJsonLargeIntegerPrecision**: Verifies large integers (>2^53) maintain precision
1. **ObjectToJsonPreservesIntegerTypes**: Verifies CLR integer types serialize correctly
1. **ObjectToJsonPreservesFloatTypes**: Verifies CLR float types serialize correctly

## Test Results

All 4718 tests pass after the changes.

## Related Changes

Also added 7 tests for interop converter integer preservation:

### ClrToScriptConversionsTUnitTests

- `TryObjectToTrivialDynValuePreservesIntegerSubtype`
- `TryObjectToTrivialDynValuePreservesFloatSubtype`
- `TryObjectToSimpleDynValuePreservesIntegerSubtype`
- `ObjectToDynValuePreservesIntegerSubtype`

### ScriptToClrConversionsTUnitTests

- `DynValueToObjectPreservesIntegerSubtype`
- `DynValueToObjectReturnsDoubleForFloatSubtype`
- `DynValueToObjectOfTypeLongPreservesIntegerPrecision`

## PLAN.md Phase 4 Status

This completes the JSON serialization item in §8.24 Phase 4. The interop converters (`FromObject`/`ToObject`) were already correctly handling integer preservation.

**Phase 4 Status**:

- [x] Update binary dump/load format — ✅ DONE (2025-12-11, version 0x151)
- [x] Update `FromObject()` / `ToObject()` for integer preservation — ✅ Already implemented
- [x] Update JSON serialization (integers as JSON integers, not floats) — ✅ DONE (this fix)
- [x] Ensure CLR interop handles `int`, `long`, `float`, `double` correctly — ✅ Verified with tests

## Next Steps

Phase 4 of §8.24 (Interop & Serialization) is now complete. The remaining work in §8.24 is:

**Phase 5: Caching & Performance Validation** (planned)

- Extend `DynValue` caches for common float values (0.0, 1.0, -1.0, etc.)
- Add `FromFloat(double)` cache method for hot paths
- Add negative integer cache (-256 to -1)
- Run Lua comparison harness against reference Lua 5.3/5.4
- Performance benchmarking and memory allocation profiling
