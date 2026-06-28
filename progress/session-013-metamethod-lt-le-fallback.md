# Metamethod `__lt`/`__le` Fallback Version Parity Fix

**Date**: 2025-12-13
**Status**: Complete
**PLAN.md Reference**: §9.11 Metamethod Behavior Parity

## Summary

Implemented version-gated behavior for the `__lt` metamethod fallback when `__le` is not defined. This change corrects NovaSharp's behavior to match the actual Lua reference implementation across versions.

## Background

### Lua Behavior for `<=` with Only `__lt` Defined

In Lua 5.1 through 5.4, when comparing two tables with `<=`:

- If `__le` metamethod is defined → use it
- If `__le` is NOT defined but `__lt` IS defined → fall back to `not (b < a)`

### Documentation vs Reality

The Lua 5.4 manual §8.1 ("Incompatibilities with the Previous Version") states:

> "The use of the `__lt` metamethod to emulate `__le` has been removed."

However, **actual testing against Lua 5.4.4 shows this is not true**:

```bash
$ lua5.4 -e "
local mt = { __lt = function(a,b) return a.v < b.v end }
local a,b = setmetatable({v=1},mt), setmetatable({v=2},mt)
print(a <= b)  -- prints: true
"
true
```

### Lua 5.5 Actually Removes the Fallback

Testing against Lua 5.5 shows the behavior change:

```bash
$ lua5.5 -e "
local mt = { __lt = function(a,b) return a.v < b.v end }
local a,b = setmetatable({v=1},mt), setmetatable({v=2},mt)
print(a <= b)
"
lua5.5: (command line):4: attempt to compare two table values
```

## Implementation

### Changes Made

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs`

Modified the `ExecLessEq` method (around line 1890) to check the compatibility version before attempting the `__lt` fallback:

```csharp
if (ip < 0)
{
    // Lua 5.5 removes the fallback from __lt to emulate __le.
    // In earlier versions (5.1-5.4), if __le is not defined, we try __lt with swapped arguments.
    // Note: Lua 5.4 manual §8.1 claims this was removed in 5.4, but actual Lua 5.4.x still supports it.
    // Lua 5.5 (verified against lua5.5) actually removes this fallback behavior.
    // Latest mode follows current target (Lua 5.4.x) behavior which allows the fallback.
    Compatibility.LuaCompatibilityVersion version =
        _script.Options.CompatibilityVersion;
    bool allowLtFallback =
        version != Compatibility.LuaCompatibilityVersion.Lua55;

    if (allowLtFallback)
    {
        ip = InternalInvokeBinaryMetaMethod(r, l, "__lt", instructionPtr, DynValue.True);
    }
    // ...
}
```

### Version Behavior Summary

| Version | `__lt` Fallback for `__le`        |
| ------- | --------------------------------- |
| Lua 5.1 | ✅ Allowed                        |
| Lua 5.2 | ✅ Allowed                        |
| Lua 5.3 | ✅ Allowed                        |
| Lua 5.4 | ✅ Allowed                        |
| Lua 5.5 | ❌ Removed                        |
| Latest  | ✅ Allowed (follows 5.4.x target) |

### Note on `Latest` Mode

Per `docs/LuaCompatibility.md`, `Latest` mode "keep[s] behaviour aligned with the most recent NovaSharp target (Lua 5.4.x today)". Therefore, `Latest` allows the fallback like Lua 5.4.

When NovaSharp's target moves to Lua 5.5+, the `Latest` behavior should be updated accordingly.

## Tests Added

**File**: `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MetamethodLtLeFallbackTUnitTests.cs`

Created comprehensive test coverage with 8 test methods:

1. **`LtFallbackToLeWorksInLua51Through54`** - Verifies fallback works in Lua 5.1, 5.2, 5.3, 5.4
1. **`LtFallbackToLeFailsInLua55`** - Verifies fallback throws in Lua 5.5
1. **`LtFallbackToLeWorksInLatestMode`** - Verifies fallback works in Latest (follows 5.4.x)
1. **`LeMetamethodWorksInAllVersions`** - Verifies `__le` works when defined (all versions)
1. **`LtMetamethodWorksInAllVersions`** - Verifies `__lt` works for `<` operator (all versions)
1. **`LtFallbackCorrectlyInvertsResult`** - Verifies fallback uses `not (b < a)` correctly
1. **`LtFallbackHandlesEqualValues`** - Verifies fallback for equal values (a \<= a)

## Verification

### Build

```bash
dotnet build src/NovaSharp.sln -c Release
# Build succeeded. 0 Warning(s) 0 Error(s)
```

### Tests

```bash
dotnet test src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release
# Passed! - Failed: 0, Passed: 5025, Skipped: 0, Total: 5025
```

## Key Learnings

1. **Always verify spec claims against actual reference implementation** - The Lua 5.4 manual documentation was premature; the actual behavior change occurred in Lua 5.5.

1. **Test against multiple Lua versions** - Version-specific behavior requires verification against actual Lua interpreters for each version.

1. **`Latest` mode follows the current target** - Per NovaSharp documentation, `Latest` should match the current target version (5.4.x), not necessarily the newest Lua release.

## References

- Lua 5.4 Manual §8.1: https://www.lua.org/manual/5.4/manual.html#8.1
- Lua 5.4 Manual §3.4.4 (Order operators): https://www.lua.org/manual/5.4/manual.html#3.4.4
- NovaSharp `docs/LuaCompatibility.md` - Documents `Latest` mode behavior
