# Session 081: Helper Performance Audit Phase 3 - CoreLib Modules

**Date**: 2025-12-22
**Initiative**: 11 (Comprehensive Helper Performance Audit)
**Status**: ✅ Complete

## Summary

Completed Phase 3 of the Helper Performance Audit, focusing on CoreLib module implementations. Applied `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to 18 small, frequently-called helper methods across 8 files.

## Files Modified

| File                          | Methods Inlined | Description                                                                                                         |
| ----------------------------- | --------------- | ------------------------------------------------------------------------------------------------------------------- |
| `ModuleArgumentValidation.cs` | 4               | `ThrowBadArgument`, `ThrowBadArgumentType`, `ThrowNoValue`, `ArgAsInt` — Called at the start of every module method |
| `MathModule.cs`               | 2               | `TryGetIntegerAsDouble`, `TryGetNumberAsDouble` — Small wrappers called in math operations                          |
| `LuaValueConverter.cs`        | 3               | `TryGetInteger` (non-validation), `TryGetNumber` (non-validation), `ToInt32` — Hot path value extraction            |
| `StringModule.cs`             | 2               | `CastStringToNumber`, `NormalizeByte` — String arithmetic and byte normalization helpers                            |
| `TableModule.cs`              | 1               | `LuaComparerToClrComparer` — Comparison helper used in table sorting                                                |
| `Utf8Module.cs`               | 2               | `NormalizeBoundary`, `IsRuneBoundary` — UTF-8 boundary helpers                                                      |
| `BasicModule.cs`              | 3               | `GetDigitValue`, `IsHexDigit`, `IsValidDigit` — Character parsing helpers for tonumber                              |
| `OsTimeModule.cs`             | 1               | `GetDayOfYear` — Simple calculation method                                                                          |

**Total Methods Optimized: 18**

## Files Skipped (with reasons)

| File                     | Reason                                                                  |
| ------------------------ | ----------------------------------------------------------------------- |
| `CoroutineModule.cs`     | No small private helper methods - all methods are public API or complex |
| `DebugModule.cs`         | Complex methods with try/catch blocks and REPL integration              |
| `DynamicModule.cs`       | Public API methods that delegate to script execution context            |
| `LoadModule.cs`          | Complex loading logic with file I/O and error handling                  |
| `IoModule.cs`            | I/O operations with stream handling, not suitable for inlining          |
| `JsonModule.cs`          | Parsing logic with complex branching                                    |
| `OsDateTimeModule.cs`    | Complex time conversion logic                                           |
| `OsSystemModule.cs`      | System interaction with complex error handling                          |
| `MetaTableModule.cs`     | Dynamic typing logic not suitable for inlining                          |
| `ErrorHandlingModule.cs` | Error handling with complex control flow                                |
| `GlobalsModule.cs`       | Already has cached callbacks, methods have complex iteration logic      |
| `IoFile*.cs` files       | Stream and encoding implementations with complex logic                  |

## Optimization Patterns Applied

1. **Argument Validation Helpers** (ModuleArgumentValidation)

   - Called at the entry point of every module method
   - Extremely high-frequency, critical for all stdlib calls

1. **Type Conversion Helpers** (TryGetInteger, ToUInt32, ToInt32)

   - Called repeatedly in numeric operations
   - Simple null/type checks with value extraction

1. **Character/String Helpers** (GetDigitValue, IsHexDigit, NormalizeByte)

   - Called in tight loops during parsing
   - Simple character classification and conversion

1. **Comparison Helpers** (LuaComparerToClrComparer)

   - Used in sorting comparisons
   - Simple delegate wrapper

1. **UTF-8 Position Helpers** (NormalizeBoundary, IsRuneBoundary)

   - Called during string iteration
   - Simple byte-level checks

## Test Results

```
✅ All 11,790 tests passed
   - Total: 11,790
   - Failed: 0
   - Succeeded: 11,790
   - Skipped: 0
```

## Technical Notes

CoreLib modules are the C# implementations of Lua's standard library functions (math, string, table, os, io, etc.). Many of these modules share common helper patterns:

- **Argument validation**: Checking types and ranges of Lua arguments
- **Value extraction**: Converting DynValue to appropriate CLR types
- **Character classification**: Parsing numeric strings and identifiers

By inlining these frequently-called helpers, we reduce function call overhead and enable the JIT to perform better optimizations like constant propagation.

## Impact Assessment

| Category            | Methods | Expected Impact                                    |
| ------------------- | ------- | -------------------------------------------------- |
| Argument Validation | 4       | **High** — Called on every stdlib function call    |
| Type Conversion     | 5       | **High** — Called repeatedly in numeric operations |
| Character Helpers   | 4       | Medium — Called during parsing/conversion          |
| UTF-8 Helpers       | 2       | Medium — Called during string operations           |
| Table Sorting       | 1       | Low — Called only during table.sort                |
| Date/Time           | 1       | Low — Called only during os.time                   |

## Remaining Work

- **Phase 4**: Interop layer helpers (`Interop/`)

## Related

- Previous: [session-080-helper-performance-audit-phase2.md](session-080-helper-performance-audit-phase2.md) (Phase 2)
- Initiative: 11 (Comprehensive Helper Performance Audit)
- PLAN.md: Initiative 11 section
