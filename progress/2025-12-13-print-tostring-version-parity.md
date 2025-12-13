# print() Version-Aware tostring Behavior

**Date**: 2025-12-13\
**Initiative**: §9.4 Basic Functions Version Parity\
**Status**: Completed

## Summary

Implemented version-aware behavior for the `print()` function, ensuring NovaSharp matches reference Lua interpreter behavior across all supported versions (5.1, 5.2, 5.3, 5.4).

## Problem Statement

In Lua 5.4, the `print()` function behavior changed:

- **Lua 5.1–5.3**: `print()` calls the global `tostring` function for each argument. If the user overrides the global `tostring`, their custom implementation is used.
- **Lua 5.4+**: `print()` uses the `__tostring` metamethod directly (hardwired behavior), bypassing the global `tostring` function entirely.

This is documented in the Lua 5.4 specification:

> `print(...)` — Prints to stdout. **Changed:** No longer calls tostring (hardwired).

## Verification Against Reference Lua

```bash
# Lua 5.3 behavior (calls global tostring):
$ lua5.3 -e 'function tostring(v) return "CUSTOM:" .. type(v) end; t = setmetatable({}, {__tostring = function() return "META" end}); print(t)'
CUSTOM:table

# Lua 5.4 behavior (uses __tostring directly):
$ lua5.4 -e 'function tostring(v) return "CUSTOM:" .. type(v) end; t = setmetatable({}, {__tostring = function() return "META" end}); print(t)'
META
```

Before this fix, NovaSharp was using Lua 5.4 behavior (`META`) for all versions, which was incorrect for 5.1–5.3.

## Implementation

### Changes to `BasicModule.cs`

Modified the `Print()` function to check the Lua compatibility version:

```csharp
// Lua 5.4+ behavior: print uses __tostring metamethod directly (hardwired)
// Lua 5.1-5.3 behavior: print calls global tostring function (user-overridable)
bool useLua54HardwiredTostring = resolved >= LuaCompatibilityVersion.Lua54;

for (int i = 0; i < args.Count; i++)
{
    if (useLua54HardwiredTostring)
    {
        // Lua 5.4+: Use __tostring metamethod directly
        sb.Append(args.AsStringUsingMeta(executionContext, i, "print"));
    }
    else
    {
        // Lua 5.1-5.3: Call global tostring function (user-overridable)
        sb.Append(CallGlobalTostring(script, args[i], version));
    }
}
```

Added a new helper method `CallGlobalTostring()` that:

- Retrieves the global `tostring` function from the script's globals
- Handles both Lua `Function` and `ClrFunction` types for the override
- Falls back to default formatting if `tostring` doesn't return a string

## Files Modified

### Production Code

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/BasicModule.cs`
  - Updated `Print()` function with version-aware logic
  - Added `CallGlobalTostring()` helper method
  - Added comprehensive XML documentation explaining the version difference

### Test Code

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs`
  - Added 9 new tests covering version-specific `print()` behavior:
    - `PrintCallsGlobalTostringInLua51To53` (3 versions)
    - `PrintUsesTostringMetamethodDirectlyInLua54Plus` (2 versions)
    - `PrintIgnoresGlobalTostringForPlainTablesInLua54Plus` (2 versions)
    - `PrintCallsGlobalTostringForPlainTablesInLua51To53` (3 versions)
    - `PrintCallsGlobalTostringForNumbersInLua51To53` (3 versions)
    - `PrintIgnoresGlobalTostringForNumbersInLua54Plus` (2 versions)
    - `PrintSeparatesArgumentsWithTabs` (2 versions)
    - `PrintWorksWithClrFunctionTostringInLua51To53` (3 versions)

## Test Results

All 4,986 tests pass after these changes (added 9 new test methods × multiple version arguments = ~20 test cases).

## Post-Fix Verification

```bash
# NovaSharp Lua 5.3 mode (now calls global tostring):
$ nova --lua-version 5.3 -e 'function tostring(v) return "CUSTOM:" .. type(v) end; t = setmetatable({}, {__tostring = function() return "META" end}); print(t)'
CUSTOM:table

# NovaSharp Lua 5.4 mode (uses __tostring directly):
$ nova --lua-version 5.4 -e 'function tostring(v) return "CUSTOM:" .. type(v) end; t = setmetatable({}, {__tostring = function() return "META" end}); print(t)'
META
```

## PLAN.md Updates

Updated §9.4 Basic Functions Version Parity table:

- `print(...)` behavior → ✅ Completed

## Related Documentation

- `docs/lua-spec/lua-5.4-spec.md` — Reference for 5.4 `print` behavior change
- `docs/lua-spec/lua-5.3-spec.md` — Reference for 5.1-5.3 behavior
- Lua 5.4 Reference Manual §6.1 — Basic Functions
