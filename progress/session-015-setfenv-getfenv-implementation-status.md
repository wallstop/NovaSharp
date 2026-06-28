# setfenv/getfenv Lua 5.1 Implementation Status

**Date**: 2025-12-13\
**Status**: ✅ **COMPLETE** — Implementation finished and all tests passing\
**Priority**: ✅ DONE — Proper Lua 5.1 compatibility achieved

## Overview

Lua 5.1's `setfenv` and `getfenv` functions for manipulating function environments were removed in Lua 5.2+ (replaced by the `_ENV` mechanism). NovaSharp now implements these functions for Lua 5.1 compatibility mode.

## Implementation Summary

### What Was Done

1. **Implemented `getfenv` in `BasicModule.cs`**:

   - Version-gated with `[LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]`
   - `getfenv()` or `getfenv(nil)` — Returns environment of calling function (defaults to level 1)
   - `getfenv(0)` — Returns global environment (script globals)
   - `getfenv(n)` where n > 0 — Returns environment of function at stack level n
   - `getfenv(f)` where f is function — Returns f's environment table
   - Error handling: "invalid level" for out-of-bounds stack levels

1. **Implemented `setfenv` in `BasicModule.cs`**:

   - Version-gated with `[LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]`
   - `setfenv(f, table)` — Sets environment of function f to table
   - `setfenv(n, table)` where n > 0 — Sets environment of function at stack level n
   - `setfenv(0, table)` — Sets global environment for the thread
   - Returns the function (for level 0, returns nil per Lua 5.1 spec)
   - Error handling: "invalid level" for out-of-bounds stack levels
   - Error handling: "'setfenv' cannot change environment of given object" for C functions

1. **Environment Storage**:

   - Uses the existing `_ENV` upvalue mechanism via `ClosureContext`
   - Environment is stored as the first upvalue (index 0) with symbol `WellKnownSymbols.ENV`
   - Works with existing NovaSharp Closure architecture

1. **Test Suite** (`SetFenvGetFenvTUnitTests.cs`) with 20+ tests covering:

   - Existence tests (Lua 5.1 has them, 5.2+ returns nil)
   - `getfenv()` with no arguments (returns calling function's environment)
   - `getfenv(0)` returns global thread environment
   - `getfenv(1)` returns calling function's environment
   - `getfenv(f)` returns function's environment
   - `setfenv(f, env)` changes function's environment
   - `setfenv` returns the function (for chaining)
   - Error handling for invalid levels
   - Error handling for non-table second argument
   - Negative tests: functions are nil in Lua 5.2+

1. **Lua Fixtures Created** in `LuaFixtures/SetFenvGetFenvTUnitTests/`:

   - Verified against reference Lua 5.1 interpreter
   - Tests for basic operations, error handling, stack levels
   - Negative tests for Lua 5.2+ (getfenv/setfenv are nil)

### Test Results

All 5056 tests pass including the new setfenv/getfenv tests.

### Key Implementation Notes

1. **ScriptOptions Pattern**: When creating a Script for Lua 5.1 tests, use:

   ```csharp
   ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
   {
       CompatibilityVersion = LuaCompatibilityVersion.Lua51,
   };
   ```

   Using `new()` without `Script.DefaultOptions` causes bytecode execution issues.

1. **Stack Walking**: Uses `ScriptExecutionContext.TryGetStackFrame()` to walk the call stack

1. **C Function Handling**: ClrFunction types cannot have their environment changed (matches Lua 5.1 behavior)

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/BasicModule.cs`

  - Added `GetFenv()` method
  - Added `SetFenv()` method
  - Added helper methods: `TryGetLuaStackFrameForGetSetFenv`, `GetEnvironmentFromClosure`, `GetEnvironmentFromClosureContext`, `SetEnvironmentOnClosure`

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs`

  - Complete test suite for setfenv/getfenv

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/SetFenvGetFenvTUnitTests/*.lua`

  - Cross-interpreter verification fixtures

```

## Files to Modify

1. `src/runtime/.../CoreLib/BasicModule.cs` — Add getfenv/setfenv implementations
2. `src/runtime/.../DataTypes/Closure.cs` — May need Environment property (verify)
3. `src/runtime/.../Execution/VM/Processor_*.cs` — May need stack walking helpers

## Verification Steps

1. Run test suite: `dotnet test --filter SetFenvGetFenvTUnitTests`
2. Verify Lua fixtures pass: `python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.1`
3. Verify 5.2+ correctly has nil for these functions

## Related PLAN.md Sections

- §8.19: Environment Changes (Lua 5.2+)
- §9.4: Basic Functions Version Parity (setfenv/getfenv row)
- Recommended Next Steps #1: `setfenv`/`getfenv` for Lua 5.1
```
