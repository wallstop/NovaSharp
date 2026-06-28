# Progress: load/loadstring Version Parity (§9.4)

**Date**: 2025-12-14
**PLAN.md Section**: §9.4 - load/loadfile signature verification per version
**Status**: ✅ Complete

## Summary

Implemented version-aware behavior for `load`, `loadsafe`, and `loadstring` functions to match official Lua reference specifications.

## Issues Discovered

1. **`loadstring` missing in Lua 5.1 mode**: The `loadstring` function was not available when running in Lua 5.1 compatibility mode, but it should be (it was removed in Lua 5.2).

1. **`load` incorrectly accepting strings in Lua 5.1 mode**: The `load` function accepted string arguments in all versions, but Lua 5.1's `load(func [, chunkname])` only accepts reader functions, not strings.

## Lua Specification Differences

| Function     | Lua 5.1                                           | Lua 5.2+                                                             |
| ------------ | ------------------------------------------------- | -------------------------------------------------------------------- |
| `load`       | `load(func [, chunkname])` - reader function only | `load(chunk [, chunkname [, mode [, env]]])` - strings and functions |
| `loadstring` | Available                                         | Removed (use `load` instead)                                         |
| `loadsafe`   | NovaSharp extension - follows `load` rules        | NovaSharp extension - follows `load` rules                           |

## Changes Made

### LoadModule.cs

1. **Added `loadstring` function** with `[LuaCompatibility(Lua51, Lua51)]` attribute to make it available only in Lua 5.1 mode.

1. **Modified `LoadCore` method** to accept `allowStrings` parameter that determines whether string arguments are accepted.

1. **Modified `Load` method** to check compatibility version:

   ```csharp
   LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
   bool allowStrings = LuaVersionDefaults.Resolve(version) >= LuaCompatibilityVersion.Lua52;
   return LoadCore(executionContext, args, null, allowStrings);
   ```

1. **Modified `LoadSafe` method** similarly to respect version constraints.

### New Test File

Created `LoadModuleVersionParityTUnitTests.cs` with comprehensive tests:

- `LoadstringIsAvailableInLua51` - Positive test: loadstring exists in 5.1
- `LoadstringIsNilInLua52Plus` - Negative test: loadstring is nil in 5.2+
- `LoadstringCompilesAndExecutesInLua51` - Functionality test
- `LoadstringSyntaxErrorReturnsNilAndMessageInLua51` - Error handling test
- `LoadstringWithChunknameInLua51` - Optional parameter test
- `LoadRejectsStringArgumentInLua51` - Negative test: strings rejected in 5.1
- `LoadAcceptsStringArgumentInLua52Plus` - Positive test: strings accepted in 5.2+
- `LoadAcceptsReaderFunctionInAllVersions` - Universal behavior test
- `LoadEnvParameterWorksInLua52Plus` - 5.2+ specific parameter test
- `LoadSafeRejectsStringArgumentInLua51` - Extension follows same rules
- `LoadSafeAcceptsStringArgumentInLua52Plus` - Extension follows same rules

### Lua Fixtures Created

Created 8 Lua fixture files in `LuaFixtures/LoadModuleVersionParityTUnitTests/`:

- `loadstring_available_51.lua`
- `loadstring_nil_52plus.lua`
- `loadstring_compiles_51.lua`
- `loadstring_with_chunkname_51.lua`
- `loadstring_syntax_error_51.lua`
- `load_rejects_string_51.lua`
- `load_accepts_string_52plus.lua`
- `load_accepts_reader_all.lua`
- `load_env_parameter_52plus.lua`

## Test Results

All 5121 tests pass after the changes.

## Key Implementation Notes

### Version Check Pattern

The `[LuaCompatibility]` attribute is used for functions that exist in specific versions only. For functions that exist in all versions but have different behavior, we check the version at runtime:

```csharp
LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
bool allowStrings = LuaVersionDefaults.Resolve(version) >= LuaCompatibilityVersion.Lua52;
```

### ScriptOptions Pattern

Tests must set `CompatibilityVersion` in `ScriptOptions` before creating the `Script` instance. Setting it after creation doesn't affect module registration:

```csharp
// CORRECT
ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
{
    CompatibilityVersion = version,
};
Script script = new Script(CoreModulePresets.Complete, options);

// WRONG - version not applied to module registration
Script script = new Script();
script.Options.CompatibilityVersion = version; // Too late!
```

## Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/LoadModule.cs`
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs` (new)
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/LoadModuleVersionParityTUnitTests/*.lua` (new, 8 files)
