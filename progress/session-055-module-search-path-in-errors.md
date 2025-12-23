# Session 055: Module Search Path in Errors

**Date**: 2024-12-21
**Focus**: Implement Lua-compatible "module not found" error messages that list search paths tried, controlled by `LuaCompatibleErrors` flag

## Summary

Extended the `LuaCompatibleErrors` feature (added in Session 054) to include module loading errors. When this flag is enabled and `require()` fails to find a module, the error message now lists all search paths tried, matching reference Lua's behavior.

## Changes

### Core Implementation

Modified `FormatModuleNotFoundError` in [Script.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs) to respect the `LuaCompatibleErrors` flag:

```csharp
private static string FormatModuleNotFoundError(
    string modname,
    IReadOnlyList<string> searchedPaths,
    bool luaCompatibleErrors
)
{
    // When LuaCompatibleErrors is disabled, return a simple message for backward compatibility
    if (!luaCompatibleErrors)
    {
        return $"module '{modname}' not found";
    }

    // When enabled, list all search paths matching reference Lua format
    // module 'foo' not found:
    //     no field package.preload['foo']
    //     no file './foo.lua'
    //     ...
}
```

### Test Updates

1. **Renamed and updated test** in [LoadModuleTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs):

   - `RequireErrorListsSearchedPaths` → `RequireErrorListsSearchedPathsWhenLuaCompatibleErrorsEnabled`
   - Now explicitly enables `LuaCompatibleErrors` and verifies detailed path listing

1. **Added new test** `RequireErrorShowsSimpleMessageWhenLuaCompatibleErrorsDisabled`:

   - Verifies that when `LuaCompatibleErrors = false` (default), error is simple: `module 'X' not found`

1. **Fixed StubScriptLoader** in both ScriptCallTUnitTests files:

   - Added `TryResolveModuleName` override to properly delegate to `ResolveModuleName`
   - This was needed because the error formatting code path now uses `TryResolveModuleName` for path tracking

### Lua Fixtures

Updated and created test fixtures:

| File                                                                                                                                                               | Description                                                                 |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------- |
| [RequireErrorListsSearchedPaths.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua) | Tests detailed error format with `LuaCompatibleErrors=true`                 |
| [RequireErrorShowsSimpleMessage.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/LoadModuleTUnitTests/RequireErrorShowsSimpleMessage.lua) | Tests simple error format with `LuaCompatibleErrors=false` (NovaSharp-only) |

## Behavior Summary

| `LuaCompatibleErrors` | Error Message Format                                                                 |
| --------------------- | ------------------------------------------------------------------------------------ |
| `false` (default)     | `module 'X' not found`                                                               |
| `true`                | `module 'X' not found:\n\tno field package.preload['X']\n\tno file './X.lua'\n\t...` |

## Files Modified

| File                                                                                                                                                                | Changes                                                              |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| [Script.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs)                                                                                         | Added `luaCompatibleErrors` parameter to `FormatModuleNotFoundError` |
| [LoadModuleTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs)                                           | Updated and added tests for both behaviors                           |
| [ScriptCallTUnitTests.cs (Execution)](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs)                       | Fixed `StubScriptLoader` to override `TryResolveModuleName`          |
| [ScriptCallTUnitTests.cs (ScriptExecution)](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs) | Fixed `StubScriptLoader` to override `TryResolveModuleName`          |
| [RequireErrorListsSearchedPaths.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua)  | Added `@novasharp-options: LuaCompatibleErrors=true`                 |
| [RequireErrorShowsSimpleMessage.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/LoadModuleTUnitTests/RequireErrorShowsSimpleMessage.lua)  | New fixture for simple message format                                |

## Test Results

```bash
# Module error tests (10 tests)
./scripts/test/quick.sh RequireError
✅ Tests passed

# All LoadModule tests (111 tests)
./scripts/test/quick.sh -c LoadModuleTUnitTests
✅ Tests passed

# All RequireModule tests (80 tests)
./scripts/test/quick.sh RequireModule
✅ Tests passed

# All LuaCompatibleErrors tests (37 tests)
./scripts/test/quick.sh LuaCompatible
✅ Tests passed
```

## Related

- **Session 054**: Added `LuaCompatibleErrors` flag for variable names in error messages
- **PLAN.md §8.44**: Error message parity tracking
