# os.execute() Stub Fixture Fix

**Date**: 2025-12-29
**Initiative**: Lua Comparison Test Mismatch Investigation
**Scope**: os.execute version parity test fixtures

## Summary

Fixed Lua comparison test mismatches for 9 os.execute fixtures that use NovaSharp's stub platform accessor. These fixtures were incorrectly marked as comparable to reference Lua, but they test NovaSharp-specific mocked behavior with fake shell commands.

## Investigation

### Files Investigated

1. [OsExecuteReportsNotSupportedMessageInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReportsNotSupportedMessageInLua52Plus.lua)
2. [OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus.lua)
3. [OsExecuteReturnsNilTupleOnExceptionInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNilTupleOnExceptionInLua52Plus.lua)
4. [OsExecuteReturnsNilTupleOnFailureInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNilTupleOnFailureInLua52Plus.lua)
5. [OsExecuteReturnsTupleInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsTupleInLua52Plus.lua)
6. [OsExecuteReturnsNegativeOneOnExceptionInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNegativeOneOnExceptionInLua51.lua)
7. [OsExecuteReturnsNegativeOneOnNotSupportedInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNegativeOneOnNotSupportedInLua51.lua)
8. [OsExecuteReturnsNonZeroNumberOnFailureInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNonZeroNumberOnFailureInLua51.lua)
9. [OsExecuteReturnsNumberInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNumberInLua51.lua)

### Root Cause

These fixtures use fake shell commands (`'build'`, `'fail'`, `'terminate'`) that **do not exist** on actual systems:

```lua
return os.execute('build')    -- NOT a real shell command
return os.execute('fail')     -- NOT a real shell command
return os.execute('terminate') -- NOT a real shell command
```

The TUnit C# tests mock the platform accessor via `StubPlatformAccessor` to control the exit codes:

```csharp
stub.NextExecuteExitCode = 0;    // Success
stub.NextExecuteExitCode = 7;    // Failure
stub.NextExecuteExitCode = -9;   // Signal (negative)
stub.ExecuteThrows = true;       // Exception
stub.ExecuteNotSupported = true; // PlatformNotSupportedException
```

When run against reference Lua, these commands are **actually executed** and fail:

```
sh: 1: build: Permission denied
Exit code: 127 (command not found)
```

### Output Difference

The comparison showed "Line count differs: Lua=1, Nova=2" because:

1. NovaSharp CLI outputs a `[compatibility] Running '...' with ...` diagnostic message
2. The actual command results differ (fake vs real shell execution)

## Changes Made

### Updated Fixture Metadata (9 files)

Changed `@novasharp-only: false` to `@novasharp-only: true` for all 9 stub-based fixtures:

**Before:**

```lua
-- @novasharp-only: false
```

**After:**

```lua
-- @novasharp-only: true
```

This marks these fixtures as NovaSharp-specific tests that should be skipped when comparing against reference Lua.

### Unmodified Fixture

[OsExecuteWithNoArgsReturnsTrueAllVersions.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteWithNoArgsReturnsTrueAllVersions.lua) remains `@novasharp-only: false` because it calls `os.execute()` with no arguments, which works identically on both NovaSharp and reference Lua.

## Verification

### TUnit Tests Pass

```bash
./scripts/test/quick.sh -c OsExecuteVersionParityTUnitTests
# Result: 29/29 tests passed
```

### Comparison Harness

After the fix, all OsExecute fixtures are properly handled:

```
OsExecuteReportsNotSupportedMessageInLua52Plus.lua: novasharp-only (skipped)
OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus.lua: novasharp-only (skipped)
OsExecuteReturnsNilTupleOnExceptionInLua52Plus.lua: novasharp-only (skipped)
OsExecuteReturnsNilTupleOnFailureInLua52Plus.lua: novasharp-only (skipped)
OsExecuteReturnsTupleInLua52Plus.lua: novasharp-only (skipped)
OsExecuteReturnsNegativeOneOnExceptionInLua51.lua: novasharp-only (skipped)
OsExecuteReturnsNegativeOneOnNotSupportedInLua51.lua: novasharp-only (skipped)
OsExecuteReturnsNonZeroNumberOnFailureInLua51.lua: novasharp-only (skipped)
OsExecuteReturnsNumberInLua51.lua: novasharp-only (skipped)
OsExecuteWithNoArgsReturnsTrueAllVersions.lua: lua=pass nova=pass
```

### Pre-commit Validation

```bash
bash ./scripts/dev/pre-commit.sh
# Result: Completed successfully
```

## Architecture Notes

### Test Organization

NovaSharp has two distinct test fixture directories:

1. **TUnit Fixtures** (`WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/LuaFixtures/`)
   - Better-structured fixtures using real shell commands (`true`, `false`)
   - Used by TUnit C# tests with proper mocking

2. **Comparison Fixtures** (`WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/`)
   - Generated by corpus extractor from C# test code
   - Used for reference Lua comparison

The comparison fixtures sometimes include code that only works with NovaSharp's mocked environment.

### Recommendation for Future

When testing `os.execute()` behavior that requires specific exit codes or error conditions, consider:

1. Using `@novasharp-only: true` for stub-based tests
2. Creating separate comparison-safe fixtures that use portable commands like `true`, `false`, or `exit <code>`

## Files Modified

| File | Change |
|------|--------|
| [OsExecuteReportsNotSupportedMessageInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReportsNotSupportedMessageInLua52Plus.lua) | `@novasharp-only: true` |
| [OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReportsSignalWhenExitCodeNegativeInLua52Plus.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNilTupleOnExceptionInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNilTupleOnExceptionInLua52Plus.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNilTupleOnFailureInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNilTupleOnFailureInLua52Plus.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsTupleInLua52Plus.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsTupleInLua52Plus.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNegativeOneOnExceptionInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNegativeOneOnExceptionInLua51.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNegativeOneOnNotSupportedInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNegativeOneOnNotSupportedInLua51.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNonZeroNumberOnFailureInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNonZeroNumberOnFailureInLua51.lua) | `@novasharp-only: true` |
| [OsExecuteReturnsNumberInLua51.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/OsExecuteVersionParityTUnitTests/OsExecuteReturnsNumberInLua51.lua) | `@novasharp-only: true` |
