# String.gmatch Init Parameter - Lua 5.4 Version Parity

**Date**: 2025-12-13
**Category**: Version-Aware Lua Standard Library Parity (§9.2)
**Status**: ✅ Completed

## Summary

Implemented the optional `init` parameter for `string.gmatch` that was added in Lua 5.4. The parameter specifies the starting position for pattern matching.

## Background

In Lua 5.4, `string.gmatch` gained an optional third parameter `init` that specifies where to start matching in the string:

- **Lua 5.1, 5.2, 5.3**: `string.gmatch(s, pattern)` - always starts at position 1
- **Lua 5.4+**: `string.gmatch(s, pattern [, init])` - starts at position `init` (default 1)

The `init` parameter follows standard Lua string position semantics:

- Positive values: 1-based position from start
- Negative values: position from end of string (e.g., -3 means "3 characters from end")
- Values beyond string length: treated as "end of string" (no matches)

## Changes Made

### 1. KopiLuaStringLib.cs

Modified `str_gmatch` function to:

- Check if Lua version is 5.4+
- If so, read the optional third argument using `LuaLOptInteger`
- Convert the Lua 1-based position to 0-based using `Posrelat`
- Clamp to valid range (0 to string length)
- Set initial position in `GMatchAuxData`

For Lua 5.1-5.3, the third argument is ignored (always starts at position 0).

### 2. StringModuleTUnitTests.cs

Added 6 new tests:

- `GMatchWithInitParameterStartsAtSpecifiedPosition` - Lua 5.4/5.5 with positive init
- `GMatchIgnoresInitParameterInLuaBelow54` - Lua 5.1/5.2/5.3 ignores init
- `GMatchWithNegativeInitStartsFromEnd` - Lua 5.4/5.5 with negative init
- `GMatchWithInitAtExactWordBoundary` - init at word start
- `GMatchWithInitBeyondStringLengthReturnsNoMatches` - init past end

## Reference Lua Verification

Tested against reference Lua interpreters:

```bash
# Lua 5.4 with init=5 (skips 'abc ')
$ lua5.4 -e "for m in string.gmatch('abc def ghi', '%w+', 5) do print(m) end"
def
ghi

# Lua 5.3 ignores init (starts from beginning)
$ lua5.3 -e "for m in string.gmatch('abc def ghi', '%w+', 5) do print(m) end"
abc
def
ghi

# Lua 5.4 negative init
$ lua5.4 -e "for m in string.gmatch('abc def ghi', '%w+', -3) do print(m) end"
ghi
```

## Test Results

All 4,966 tests pass.

## Files Modified

1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs`
1. `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs`
1. `PLAN.md` - Updated §9.2 String Module table and tasks

## PLAN.md Updates

- Marked `string.gmatch` init parameter as ✅ Completed
- Updated task checkbox to completed with date
- Updated Repository Snapshot test count to 4,966
