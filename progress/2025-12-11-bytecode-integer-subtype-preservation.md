# Progress: Bytecode Integer/Float Subtype Preservation

**Date**: 2025-12-11
**Task**: Fix bytecode serialization to preserve Lua 5.3+ integer vs float distinction
**Status**: ✅ Complete
**Related Sections**: PLAN.md §8.37 (LuaNumber Usage Audit), §8.24 (Dual Numeric Type System Phase 4)

______________________________________________________________________

## Summary

Fixed a critical bug where bytecode dump/load operations lost the integer subtype information, converting all numbers to doubles. This caused precision loss for large integers (>2^53) and violated Lua 5.3+ semantics that distinguish between integer and float subtypes.

## Problem Statement

Before this fix:

- `DumpValue()` called `wr.Write(value.Number)` which converts to double, losing integer subtype
- `ReadValue()` called `DynValue.NewNumber(rd.ReadDouble())` which uses `FromDouble()` auto-detection
- Large integers like `9007199254740993` (2^53 + 1) would lose precision when round-tripped
- Negative zero (`-0.0`) might incorrectly convert to integer zero

## Solution

### Bytecode Format Version Bump

Changed `DumpChunkVersion` from `0x150` to `0x151` to indicate the new format that preserves numeric subtypes.

**File**: `ProcessorBinaryDump.cs`

```csharp
/// Dump format version. Version history:
/// - 0x150: Original format (all numbers as double, loses integer subtype)
/// - 0x151: LuaNumber format (preserves integer vs float subtype for Lua 5.3+ semantics)
private const int DumpChunkVersion = 0x151;
```

### Modified `DumpValue()` Method

For `DataType.Number`, now writes:

1. A type flag byte: `0` for integer, `1` for float
1. The appropriate value: `Int64` for integers, `Double` for floats

**File**: `Instruction.cs`

```csharp
case DataType.Number:
    // Write integer/float subtype flag and appropriate value type
    // to preserve Lua 5.3+ integer vs float distinction
    if (value.LuaNumber.IsInteger)
    {
        wr.Write((byte)0); // Integer subtype flag
        wr.Write(value.LuaNumber.AsInteger);
    }
    else
    {
        wr.Write((byte)1); // Float subtype flag
        wr.Write(value.LuaNumber.AsFloat);
    }
    break;
```

### Modified `ReadValue()` Method

Reads the type flag and calls the appropriate factory method:

```csharp
case DataType.Number:
    // Read the integer/float subtype flag (0 = integer, 1 = float)
    byte numSubtype = rd.ReadByte();
    if (numSubtype == 0)
    {
        // Integer subtype - read as Int64 to preserve full precision
        return DynValue.NewInteger(rd.ReadInt64());
    }
    else
    {
        // Float subtype - read as double
        return DynValue.NewFloat(rd.ReadDouble());
    }
```

## Files Modified

### Production Code

| File                     | Change                                                                 |
| ------------------------ | ---------------------------------------------------------------------- |
| `ProcessorBinaryDump.cs` | Bumped `DumpChunkVersion` to `0x151` with documentation                |
| `Instruction.cs`         | Updated `DumpValue()` and `ReadValue()` for integer/float preservation |

### Test Code

| File                                                                  | Change                                             |
| --------------------------------------------------------------------- | -------------------------------------------------- |
| `Units/Execution/ProcessorBinaryDumpTUnitTests.cs`                    | Updated version constant, added 4 round-trip tests |
| `Units/Execution/ProcessorExecution/ProcessorBinaryDumpTUnitTests.cs` | Updated version constant                           |

## New Tests

Four regression tests were added to verify correct subtype preservation:

1. **`DumpLoadRoundTripPreservesIntegerSubtype`**

   - Tests `return 9007199254740993` (2^53 + 1)
   - Verifies integer subtype preserved after dump/load cycle

1. **`DumpLoadRoundTripPreservesFloatSubtype`**

   - Tests `return 3.14159`
   - Verifies float subtype preserved after dump/load cycle

1. **`DumpLoadRoundTripPreservesNegativeZeroAsFloat`**

   - Tests `return -0.0`
   - Verifies negative zero remains float (important for IEEE 754 semantics)

1. **`DumpLoadRoundTripPreservesLargeIntegerPrecision`**

   - Tests `return 9223372036854775807` (long.MaxValue, 2^63-1)
   - Verifies exact integer preservation for values that cannot be represented exactly as double

## Test Results

```
Test run summary: Passed! - 4,705 tests
  failed: 0
  succeeded: 4,705
  skipped: 0
  duration: 24s
```

All existing tests continue to pass, and the 4 new tests verify the fix.

## Breaking Change Notice

The bytecode dump format changed from version `0x150` to `0x151`. Old bytecode dumps (version `0x150`) **cannot be loaded** by this version. However:

- This is expected behavior - version checks exist to reject incompatible dumps
- The old format had a bug (precision loss) that makes it unsuitable for correct Lua 5.3+ semantics
- Scripts should be recompiled from source when upgrading

## Impact on PLAN.md

- **§8.37**: Marked "Bug 3: Bytecode serialization" as complete
- **§8.24 Phase 4**: Marked "Update binary dump/load format" task as done
- **Repository Snapshot**: Updated test count from 4,635 to 4,705, added bytecode format note

## Next Steps

Remaining items in §8.24 Phase 4:

- Update `FromObject()` / `ToObject()` for integer preservation
- Update JSON serialization (integers as JSON integers, not floats)
- Ensure CLR interop handles `int`, `long`, `float`, `double` correctly
