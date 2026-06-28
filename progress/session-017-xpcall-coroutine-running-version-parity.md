# xpcall and coroutine.running Version Parity

**Date**: 2025-12-13\
**Initiative**: §9.4 Basic Functions Version Parity, §9.5 Coroutine Module Version Parity\
**Status**: Completed

## Summary

Implemented version-aware behavior for `xpcall()` extra arguments and `coroutine.running()` return values, ensuring NovaSharp matches reference Lua interpreter behavior across all supported versions (5.1, 5.2, 5.3, 5.4).

## Problem Statement

### xpcall Extra Arguments

The `xpcall()` function signature changed in Lua 5.2:

- **Lua 5.1**: `xpcall(f, err)` — Only two arguments, extra arguments are ignored
- **Lua 5.2+**: `xpcall(f, msgh, [arg1, ...])` — Extra arguments are passed to the called function

This is documented in the Lua specifications:

> Lua 5.2: `xpcall(f, msgh [, arg1, ···])` — Calls function f with the given arguments in protected mode with msgh as the message handler.

Before this fix, NovaSharp was passing extra arguments in all versions, which was incorrect for Lua 5.1.

### coroutine.running Return Value

The `coroutine.running()` function return value changed in Lua 5.2:

- **Lua 5.1**: `coroutine.running()` — Returns only the running coroutine (or nil if main)
- **Lua 5.2+**: `coroutine.running()` — Returns the running coroutine and a boolean (true if main thread)

This is documented in the Lua specifications:

> Lua 5.2: Returns the running coroutine plus a boolean, true when the running coroutine is the main one.

Before this fix, NovaSharp was returning the tuple (co, isMain) in all versions, which was incorrect for Lua 5.1.

## Verification Against Reference Lua

### xpcall

```bash
# Lua 5.1 behavior (ignores extra arguments):
$ lua5.1 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    0

# Lua 5.2+ behavior (passes extra arguments):
$ lua5.2 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    3

$ lua5.3 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    3

$ lua5.4 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    3
```

### coroutine.running

```bash
# Lua 5.1 behavior (returns 1 value from main):
$ lua5.1 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
nil     nil

# Lua 5.2+ behavior (returns 2 values from main):
$ lua5.2 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
thread  boolean

$ lua5.3 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
thread  boolean

$ lua5.4 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
thread  boolean
```

## Implementation

### Changes to `ErrorHandlingModule.cs`

Modified the `Xpcall()` function to check the Lua compatibility version when building arguments:

```csharp
// Lua 5.2+ signature: xpcall(f, msgh, arg1, ...)
// In 5.2+, extra arguments are passed to the function f.
// Lua 5.1 signature: xpcall(f, err)
// In 5.1, only the function and error handler are used; extra args are ignored.
LuaCompatibilityVersion resolved = script.ResolveCompatibilityVersion(version);

DynValue[] fnArgs;
if (resolved >= LuaCompatibilityVersion.Lua52)
{
    // Lua 5.2+: Pass extra arguments to the function
    fnArgs = new DynValue[args.Count - 2];
    for (int i = 0; i < fnArgs.Length; i++)
    {
        fnArgs[i] = args[i + 2];
    }
}
else
{
    // Lua 5.1: Only function and handler, no extra args passed to f
    fnArgs = Array.Empty<DynValue>();
}
```

### Changes to `CoroutineModule.cs`

Modified the `Running()` function to check the Lua compatibility version when building return value:

```csharp
// Lua 5.2+ returns (co, isMain) tuple - co is the running coroutine, isMain is true if main thread
// Lua 5.1 returns only co (or nil if main thread)
LuaCompatibilityVersion resolved = script.ResolveCompatibilityVersion(version);

if (resolved >= LuaCompatibilityVersion.Lua52)
{
    // Lua 5.2+: Return (coroutine, isMain) tuple
    return DynValue.NewTuple(DynValue.NewCoroutine(script.MainCoroutine), DynValue.True);
}
else
{
    // Lua 5.1: Return nil for main thread (only returns single value)
    return DynValue.Nil;
}
```

## Files Modified

### Production Code

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/ErrorHandlingModule.cs`

  - Updated `Xpcall()` function with version-aware argument passing
  - Added comprehensive XML documentation explaining the version difference

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/CoroutineModule.cs`

  - Updated `Running()` function with version-aware return value
  - Added comprehensive XML documentation explaining the version difference

### Test Code

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs`

  - Added 8 new tests covering version-specific `xpcall()` behavior:
    - `XpcallIgnoresExtraArgsInLua51` — Verifies Lua 5.1 ignores extra args
    - `XpcallPassesExtraArgsInLua52Plus` — Verifies Lua 5.2+ passes extra args (4 versions)
    - `XpcallWithNoExtraArgsWorksInAllVersions` — Baseline with no extra args (4 versions)

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs`

  - Added helper method `CreateScriptWithVersion()` for version-specific script creation
  - Added 5 new tests covering version-specific `coroutine.running()` behavior:
    - `RunningReturnsOnlyCoroutineInLua51FromCoroutine` — Verifies 5.1 returns single value inside coroutine
    - `RunningReturnsNilFromMainInLua51` — Verifies 5.1 returns nil from main thread
    - `RunningReturnsTupleFromMainInLua52Plus` — Verifies 5.2+ returns (thread, true) from main (4 versions)
    - `RunningReturnsTupleFromCoroutineInLua52Plus` — Verifies 5.2+ returns (thread, false) inside coroutine (4 versions)

## Test Results

All 5,005 tests pass after these changes (added 11 new test cases across 13 test method invocations).

## Post-Fix Verification

### xpcall

```bash
# NovaSharp Lua 5.1 mode (now ignores extra arguments):
$ nova --lua-version 5.1 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    0

# NovaSharp Lua 5.2+ mode (passes extra arguments):
$ nova --lua-version 5.4 -e 'function f(...) return select("#", ...) end; print(xpcall(f, print, 1, 2, 3))'
true    3
```

### coroutine.running

```bash
# NovaSharp Lua 5.1 mode (now returns nil from main):
$ nova --lua-version 5.1 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
nil     nil

# NovaSharp Lua 5.2+ mode (returns tuple):
$ nova --lua-version 5.4 -e 'local a, b = coroutine.running(); print(type(a), type(b))'
thread  boolean
```

## Known Issue: pcall/xpcall Extra Nil Argument

During investigation, a pre-existing issue was discovered where `pcall` and `xpcall` pass an extra `nil` argument when called without extra args:

```lua
-- Expected: 0 args (Lua behavior)
-- Actual NovaSharp: 1 arg (nil)
function f(...) return select("#", ...) end
print(pcall(f))  -- NovaSharp returns 1 instead of 0
```

This issue affects all Lua versions and is tracked separately. The current fix works around this by comparing relative argument counts rather than absolute counts.

## PLAN.md Updates

Updated the following sections:

- §9.4 Basic Functions Version Parity: `xpcall(f, msgh [,...])` → ✅ Completed
- §9.5 Coroutine Module Version Parity: `coroutine.running()` → ✅ Completed
- Repository Snapshot: Test count updated from 4,986 to 5,005, added xpcall/coroutine.running parity notes

## Related Documentation

- `docs/lua-spec/lua-5.1-spec.md` — Reference for 5.1 behavior
- `docs/lua-spec/lua-5.2-spec.md` — Reference for 5.2+ changes
- Lua 5.1 Reference Manual §5.1 — Error Handling in C
- Lua 5.2 Reference Manual §6.1 — Basic Functions (xpcall changes)
- Lua 5.2 Reference Manual §6.2 — Coroutine Manipulation (running changes)
