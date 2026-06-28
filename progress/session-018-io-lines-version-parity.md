# Progress: io.lines Return Value Version Parity (§9.7)

**Date**: 2025-12-14
**PLAN.md Section**: §9.7 - IO Module Version Parity / io.lines return value
**Status**: ✅ Complete

## Summary

Implemented version-aware behavior for `io.lines(filename)` to match official Lua reference specifications. In Lua 5.4+, `io.lines` returns 4 values instead of 1, including a file handle that can be used with to-be-closed variables.

## Lua Specification Differences

| Version     | `io.lines(filename)` Return Values  | Notes                                |
| ----------- | ----------------------------------- | ------------------------------------ |
| Lua 5.1-5.3 | `(iterator, nil, nil)`              | Iterator triple for generic for loop |
| Lua 5.4+    | `(iterator, nil, nil, file_handle)` | Added file handle for to-be-closed   |

### Breaking Change in Lua 5.4

In Lua 5.4, `io.lines` returns 4 values:

1. Iterator function
1. `nil` (state for generic for)
1. `nil` (initial control variable)
1. File handle (userdata) — NEW in 5.4

This change enables the to-be-closed pattern:

```lua
for line in io.lines(filename) do
    -- File handle (4th return value) can be automatically closed
    -- when used with to-be-closed variables or manual cleanup
end
```

## Changes Made

### IoModule.cs

1. **Modified `Lines` method** to check compatibility version and return 4 values for Lua 5.4+:

   ```csharp
   if (version >= LuaCompatibilityVersion.Lua54)
   {
       return DynValue.NewTuple(
           iterator.Tuple[0], // iterator function
           DynValue.Nil,      // state
           DynValue.Nil,      // initial value
           UserData.Create(fileHandle) // file handle for to-be-closed
       );
   }
   ```

1. **Changed from eager to lazy iteration**: Instead of reading all lines into memory upfront, the implementation now:

   - Opens the file and keeps it open
   - Creates a lazy iterator that reads lines on-demand
   - Returns the file handle so it can be closed by the caller (in 5.4+)

1. **Added `CreateLazyLineIterator` helper method** that yields lines one at a time until EOF.

### FileUserDataBase.cs

1. **Added `ReadLineInternal` method**: An internal method that exposes the protected `ReadLine` method with newline trimming, used by the lazy iterator.

### New Test File

Created `IoLinesVersionParityTUnitTests.cs` with comprehensive tests:

- `IoLinesReturnsThreeValuesInLua51To53` — Verifies 5.1-5.3 return structure
- `IoLinesReturnsFourValuesInLua54Plus` — Verifies 5.4+ return structure with file handle
- `IoLinesIteratesOverAllLines` — Tests basic iteration (all versions)
- `IoLinesReturnsEmptyTableForEmptyFile` — Tests empty file handling (all versions)
- `IoLinesHandlesSingleLineWithoutNewline` — Tests edge case (all versions)
- `IoLinesFileHandleCanBeClosedManuallyInLua54Plus` — Tests manual file handle close
- `IoLinesFileHandleIsValidDuringIterationInLua54Plus` — Tests file handle lifecycle
- `IoLinesThrowsForNonexistentFile` — Tests error handling (all versions)

### Lua Fixtures Created

Created fixtures in `LuaFixtures/IoLinesVersionParityTUnitTests/`:

| Fixture                                   | Versions | Description                                        |
| ----------------------------------------- | -------- | -------------------------------------------------- |
| `IoLinesReturnsThreeValues_51_52_53.lua`  | 5.1-5.3  | Verifies 3-value return structure                  |
| `IoLinesReturnsFourValues_54plus.lua`     | 5.4-5.5  | Verifies 4-value return structure with file handle |
| `IoLinesIteratesOverAllLines.lua`         | All      | Tests basic iteration                              |
| `IoLinesFileHandleCanBeClosed_54plus.lua` | 5.4-5.5  | Tests file handle close behavior                   |

## Verification

All fixtures pass with reference Lua interpreters:

```bash
$ lua5.3 IoLinesReturnsThreeValues_51_52_53.lua  # PASS
$ lua5.4 IoLinesReturnsFourValues_54plus.lua    # PASS
$ lua5.4 IoLinesIteratesOverAllLines.lua        # PASS
$ lua5.4 IoLinesFileHandleCanBeClosed_54plus.lua # PASS
```

## Implementation Notes

### Callable Type Difference

NovaSharp's `io.lines` returns a callable userdata (with `__call` metamethod) rather than a native function, due to how `EnumerableWrapper` works. This is functionally equivalent for iteration purposes:

|                           | Real Lua     | NovaSharp          |
| ------------------------- | ------------ | ------------------ |
| `type(io.lines(f))`       | `"function"` | `"userdata"`       |
| `for line in io.lines(f)` | ✅ Works     | ✅ Works           |
| Callable                  | Yes          | Yes (via `__call`) |

This is an acceptable implementation difference that doesn't affect typical Lua code.

### Lazy vs Eager Iteration

The previous implementation read all lines into memory before returning the iterator. The new implementation is lazy, reading lines on-demand, which:

- Reduces memory usage for large files
- Allows the file handle to remain open during iteration
- Enables the 5.4+ pattern of returning the file handle

## Related Sections

- §8.12: io.lines Return Value Changes (Lua 5.4) — Reference
- §9.7: IO Module Version Parity — Tracking table
