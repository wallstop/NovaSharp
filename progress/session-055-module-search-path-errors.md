# Session 055: §8.44 Phase 3 — Module Search Path in Errors

**Date**: 2025-12-21
**Status**: ✅ **COMPLETE**

## Objective

Implement Lua-compatible "module not found" error messages that list all search paths tried, matching Lua's format when `ScriptOptions.LuaCompatibleErrors` is enabled.

## Background

When `require("nonexistent")` fails:

- NovaSharp (before): `module 'nonexistent' not found`
- Lua: Lists all search paths tried:
  ```
  module 'nonexistent' not found:
      no file './nonexistent.lua'
      no file '/usr/local/share/lua/5.4/nonexistent.lua'
      ...
  ```

## Implementation Summary

### 1. New ModuleResolutionResult Struct

Created `ModuleResolutionResult` (readonly struct for minimal allocations) that tracks:

- `ResolvedPath`: The resolved file path if found, otherwise `null`
- `SearchedPaths`: List of all paths that were searched
- `IsResolved`: Whether the module was successfully resolved

```csharp
// ModuleResolutionResult.cs
public readonly struct ModuleResolutionResult
{
    public string ResolvedPath { get; }
    public IReadOnlyList<string> SearchedPaths { get; }
    public bool IsResolved => ResolvedPath != null;
}
```

### 2. Updated IScriptLoader Interface

Added `ResolveModuleNameWithDetails()` method to return `ModuleResolutionResult`:

```csharp
// IScriptLoader.cs
ModuleResolutionResult ResolveModuleNameWithDetails(
    string modname, 
    Table globalContext);
```

### 3. Updated ScriptLoaderBase

Modified `ResolveModuleName()` to track all searched paths and return them via the new `ResolveModuleNameWithDetails()` method.

### 4. Updated LoadModule Error Formatting

Modified `LoadModule.GetModuleNotFoundErrorMessage()` to accept a `luaCompatibleErrors` parameter:

- When `false` (default): Returns simple `module 'X' not found` message
- When `true`: Returns detailed Lua-style format with all search paths listed

```csharp
public static string GetModuleNotFoundErrorMessage(
    string modname, 
    IReadOnlyList<string> searchedPaths, 
    bool luaCompatibleErrors)
{
    if (!luaCompatibleErrors || searchedPaths == null || searchedPaths.Count == 0)
    {
        return $"module '{modname}' not found";
    }
    
    var sb = ZStringBuilder.Create();
    sb.Append("module '");
    sb.Append(modname);
    sb.Append("' not found:");
    
    foreach (var path in searchedPaths)
    {
        sb.AppendLine();
        sb.Append("\tno file '");
        sb.Append(path);
        sb.Append("'");
    }
    
    return sb.ToString();
}
```

## Files Modified

| File                                                                                                                | Changes                                                           |
| ------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| [ModuleResolutionResult.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Loaders/ModuleResolutionResult.cs) | New readonly struct for tracking resolution results               |
| [IScriptLoader.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Loaders/IScriptLoader.cs)                   | Added `ResolveModuleNameWithDetails()` method                     |
| [ScriptLoaderBase.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Loaders/ScriptLoaderBase.cs)             | Implemented path tracking in resolution                           |
| [LoadModule.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/LoadModule.cs)                         | Updated error message formatting with `luaCompatibleErrors` param |

## Test Files Modified

| File                                                                                                                                                                | Changes                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| [LoadModuleTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs)                                           | Added tests for both error formats                       |
| [ScriptCallTUnitTests.cs (Execution)](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs)                       | Fixed stub to implement `ResolveModuleNameWithDetails()` |
| [ScriptCallTUnitTests.cs (ScriptExecution)](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs) | Fixed stub to implement `ResolveModuleNameWithDetails()` |

## Test Results

All tests pass:

```bash
./scripts/test/quick.sh RequireError
# 10 tests ✅

./scripts/test/quick.sh LuaCompatible
# 37 tests ✅
```

## Example Output

With `ScriptOptions.LuaCompatibleErrors = true`:

```
module 'nonexistent' not found:
    no file './nonexistent.lua'
    no file '/usr/local/share/lua/5.4/nonexistent.lua'
    no file '/usr/local/lib/lua/5.4/nonexistent/init.lua'
```

With `ScriptOptions.LuaCompatibleErrors = false` (default):

```
module 'nonexistent' not found
```

## Design Notes

1. **Backward Compatible**: Default behavior unchanged (simple error message)
1. **Opt-in via `LuaCompatibleErrors`**: Same flag used for variable names in errors (Session 054)
1. **Minimal Allocations**: Uses `ModuleResolutionResult` readonly struct
1. **Lua-Matching Format**: Tab indentation, "no file" prefix matches reference Lua

## Related Sessions

- [Session 053](session-053-lua-output-format-alignment.md): §8.44 Phase 1 — Address Format
- [Session 054](session-054-lua-variable-names-in-errors.md): §8.44 Phase 2 — Variable Names in Errors

## Next Steps

- [ ] Phase 4: Debug Prompt format (trivial, low priority)
- [ ] Add CI job to run `compare-lua-outputs.py --enforce` on PRs
