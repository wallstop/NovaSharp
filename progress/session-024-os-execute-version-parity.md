# os.execute Version Parity Fix

**Date**: 2025-12-15
**Status**: ✅ Complete
**PLAN.md Section**: §9.6 OS Module Version Parity

## Summary

Implemented version-aware return value behavior for `os.execute()` to match Lua specification differences between versions.

## Problem

The `os.execute()` function had different return value formats across Lua versions:

| Version      | Return Value                                        |
| ------------ | --------------------------------------------------- |
| **Lua 5.1**  | Exit status code as a single number (0 for success) |
| **Lua 5.2+** | Tuple `(true\|nil, "exit"\|"signal", code)`         |

NovaSharp was returning the Lua 5.2+ tuple format regardless of the configured `LuaCompatibilityVersion`, which was incorrect for Lua 5.1 mode.

## Changes

### Production Code

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/OsSystemModule.cs`

1. Added import for `WallstopStudios.NovaSharp.Interpreter.Compatibility`
1. Updated `Execute()` method to:
   - Resolve `LuaCompatibilityVersion.Latest` using `LuaVersionDefaults.Resolve()`
   - Return single number for Lua 5.1
   - Return tuple for Lua 5.2+
   - Handle platform exceptions appropriately per version

**Key code change**:

```csharp
LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
    executionContext.Script.CompatibilityVersion
);

// ... platform execution ...

// Lua 5.1: Return just the exit status code
if (version < LuaCompatibilityVersion.Lua52)
{
    return DynValue.NewNumber(exitCode);
}

// Lua 5.2+: Return tuple (true|nil, "exit"|"signal", code)
```

### Test Files

**New Test File**: `src/tests/.../Modules/OsExecuteVersionParityTUnitTests.cs`

Added 11 new tests covering:

- `OsExecuteReturnsNumberInLua51` - Success returns exit code
- `OsExecuteReturnsNonZeroNumberOnFailureInLua51` - Failure returns non-zero exit code
- `OsExecuteReturnsNegativeOneOnExceptionInLua51` - Platform exceptions return -1
- `OsExecuteReturnsNegativeOneOnNotSupportedInLua51` - Not supported returns -1
- `OsExecuteReturnsTupleInLua52Plus` - Success returns (true, "exit", 0)
- `OsExecuteReturnsNilTupleOnFailureInLua52Plus` - Failure returns (nil, "exit", code)
- `OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus` - Signal termination
- `OsExecuteReturnsNilTupleOnExceptionInLua52Plus` - Exception handling
- `OsExecuteReportsNotSupportedMessageInLua52Plus` - Platform not supported
- `OsExecuteWithNoArgsReturnsTrueAllVersions` - Shell availability check

### Lua Fixtures

**Directory**: `LuaFixtures/OsExecuteVersionParityTUnitTests/`

Created 4 Lua fixtures for cross-interpreter verification:

- `OsExecuteReturnsNumberInLua51.lua` - Lua 5.1 number return
- `OsExecuteReturnsTupleInLua52Plus.lua` - Lua 5.2+ tuple return
- `OsExecuteFailureReturnsTupleInLua52Plus.lua` - Failure tuple format
- `OsExecuteWithNoArgsReturnsTrueAllVersions.lua` - No-args behavior

## Bug Fix: Version Resolution

During implementation, discovered a critical bug where `LuaCompatibilityVersion.Latest` (value 0) was not being properly resolved before comparison. The check:

```csharp
if (version < LuaCompatibilityVersion.Lua52)  // Lua52 = 52
```

Would incorrectly evaluate `Latest (0) < Lua52 (52)` as `true`, causing all scripts to use Lua 5.1 behavior regardless of configuration.

**Fix**: Use `LuaVersionDefaults.Resolve()` to convert `Latest` to its concrete version (currently Lua54) before comparison.

## Verification

All 5,259 interpreter tests pass, including:

- 11 new `OsExecuteVersionParityTUnitTests`
- All existing `OsSystemModuleTUnitTests` (which use default `Latest` → Lua 5.4)

## References

- **Lua 5.1 Manual §5.8**: `os.execute([command])` returns status code
- **Lua 5.2+ Manual §6.9**: `os.execute([command])` returns `(true|nil, "exit"|"signal", code)`
