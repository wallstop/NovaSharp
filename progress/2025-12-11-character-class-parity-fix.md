# Character Class Parity Fix

**Date**: 2025-12-11
**Priority**: 🔴 HIGH (Initiative 8.4)
**Status**: ✅ COMPLETE

## Summary

Fixed a critical divergence in NovaSharp's `%p` (punctuation) character class that caused pattern matching to behave differently from reference Lua interpreters.

## Problem

NovaSharp's `IsPunctuation(char c)` function was using .NET's `Char.IsPunctuation()`, which has a narrower definition than C's `ispunct()` function used by Lua.

### Missing Characters

The following ASCII characters were NOT being recognized as punctuation by NovaSharp but ARE punctuation in reference Lua:

| Code | Character | Description  |
| ---- | --------- | ------------ |
| 36   | `$`       | Dollar sign  |
| 43   | `+`       | Plus sign    |
| 60   | `<`       | Less than    |
| 61   | `=`       | Equals       |
| 62   | `>`       | Greater than |
| 94   | `^`       | Caret        |
| 96   | `` ` ``   | Backtick     |
| 124  | `\|`      | Pipe         |
| 126  | `~`       | Tilde        |

### Root Cause

C's `ispunct()` returns true for any printable character that is not a space or alphanumeric character. .NET's `Char.IsPunctuation()` has a more restrictive Unicode-based definition that excludes "symbol" characters.

## Solution

Changed `IsPunctuation(char c)` in `LuaBaseCLib.cs` to use the C-standard definition:

```csharp
// Before (wrong):
internal static bool IsPunctuation(char c)
{
    return Char.IsPunctuation(c);
}

// After (correct):
internal static bool IsPunctuation(char c)
{
    // C ispunct(): printable, not space, not alphanumeric
    // ASCII printable range: 0x21-0x7E (33-126), excludes space (0x20)
    return c >= 0x21 && c <= 0x7E && !Char.IsLetterOrDigit(c);
}
```

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/LuaStateInterop/LuaBaseCLib.cs` - Fixed `IsPunctuation(char c)`

## Tests Added

Created comprehensive character class parity tests in:

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/PatternMatching/CharacterClassParityTUnitTests.cs`

### Test Coverage (158 test cases across 16 test methods)

All tests run against **all supported Lua versions** (5.1, 5.2, 5.3, 5.4, 5.5) to ensure consistent behavior.

1. **Full ASCII range tests** for each character class (×5 Lua versions each):

   - `%a` (alpha) - 52 characters (A-Z, a-z)
   - `%c` (control) - 33 characters (0-31, 127)
   - `%d` (digit) - 10 characters (0-9)
   - `%g` (graph) - 94 characters (33-126) - **Lua 5.2+ only** (×4 versions)
   - `%l` (lower) - 26 characters (a-z)
   - `%p` (punct) - 32 characters (all punctuation)
   - `%s` (space) - 6 characters (tab, newline, vtab, ff, cr, space)
   - `%u` (upper) - 26 characters (A-Z)
   - `%w` (alnum) - 62 characters (0-9, A-Z, a-z)
   - `%x` (xdigit) - 22 characters (0-9, A-F, a-f)

1. **Negated class tests** (×5 Lua versions each) for `%A`, `%D`, `%L`, `%S`, `%U`

1. **Specific punctuation character tests** - Comprehensive character-by-character tests across all Lua versions, with special focus on previously-missing characters: `$`, `+`, `<`, `=`, `>`, `^`, `` ` ``, `|`, `~`

## Verification

### Reference Lua 5.4 Output

```
%p (punct): 33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,58,59,60,61,62,63,64,91,92,93,94,95,96,123,124,125,126
```

### NovaSharp Output (After Fix)

```
%p (punct): 33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,58,59,60,61,62,63,64,91,92,93,94,95,96,123,124,125,126
```

✅ **Exact match across all Lua versions**

## Test Results

- **Before**: 4,741 tests passing
- **After**: 4,899 tests passing (+158 character class parity tests)
- **Regressions**: 0

## Lua Fixtures Created

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/CharacterClassParityTUnitTests/CharacterClassComprehensive.lua` - Comprehensive test script runnable against reference Lua interpreters, with version checking for `%g` (Lua 5.2+)

## Related PLAN.md Sections

- **§8.4**: String and Pattern Matching - ✅ `%p` divergence fixed
- **Initiative 8.4**: String/Pattern Matching verification - Partial progress

## Remaining Work (§8.4)

The following items from Initiative 8.4 still need verification:

1. Compare remaining character classes for non-ASCII characters (Unicode range)
1. Verify `string.format` output for edge cases (NaN, Inf, large numbers)
1. Test pattern matching with non-ASCII characters
1. Document any intentional Unicode-aware divergences
