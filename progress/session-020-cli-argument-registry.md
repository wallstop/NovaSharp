# CLI Argument Registry Modularization

**Date**: 2025-12-15
**PLAN.md Item**: §8.31 - CLI Lua Version Propagation & Modularization

## Summary

Implemented a centralized CLI argument registry (`CliArgumentRegistry`) that consolidates all CLI argument definitions, validation, and help text generation. This replaces the ad-hoc argument parsing scattered throughout `Program.cs`.

## Background

The CLI had argument parsing logic scattered across multiple methods:

- `TryRunExecuteChunks()` — Parsed `-e`, `--execute`, `-v`, `--lua-version`
- `TryRunScriptArgument()` — Parsed version options and script paths
- `CheckArgs()` — Handled `-H`, `--help`, `-X`, `-W` and hardwire options
- Multiple helper methods like `IsLuaVersionOption()`, `IsExecuteOption()`, etc.

This made it difficult to:

1. Add new CLI arguments consistently
1. Generate comprehensive help text
1. Maintain consistent error messages
1. Test argument parsing in isolation

## Changes Made

### 1. New File: `CliArgumentRegistry.cs`

Created a centralized registry containing:

#### `CliArgumentDefinition` Class

Represents a single CLI argument with metadata:

- `ShortForm` — Short form (e.g., `-H`)
- `LongForm` — Long form (e.g., `--help`)
- `Description` — Human-readable description for help
- `ValuePlaceholder` — Placeholder for value arguments (e.g., `<version>`)
- `RequiresValue` — Whether the argument needs a value
- `ExitsAfterProcessing` — Whether the program exits after this argument
- `Category` — Grouping for help output

#### `CliArgumentCategory` Enum

Categories for organizing help output:

- `Help` — Help and information options
- `Execution` — Execution mode options
- `Compatibility` — Lua compatibility options
- `Tooling` — Code generation/tooling options

#### `CliParseResult` Class

Structured result of parsing CLI arguments:

- `Success` — Whether parsing succeeded
- `ErrorMessage` — Error message if failed
- `Mode` — Execution mode (`Repl`, `Help`, `Script`, `Execute`, `Hardwire`, `Command`)
- `LuaVersion` — Parsed Lua version if specified
- `ScriptPath` — Script path for Script mode
- `InlineChunks` — Code chunks for Execute mode
- `HardwireArgs` — Arguments for Hardwire mode
- `ReplCommand` — Command for Command mode

#### `CliArgumentRegistry` Static Class

Central registry with:

- Static argument definitions for all CLI options
- `Parse(string[] args)` — Main parsing method
- `TryParseLuaVersion(string, out version)` — Version string parsing
- `GenerateHelpText()` — Comprehensive help generation
- `GetAllDefinitions()` — Enumeration of all arguments

### 2. Refactored `Program.cs`

Replaced scattered parsing with registry usage:

```csharp
// Before: 500+ lines of parsing logic
internal static bool CheckArgs(string[] args, ShellContext shellContext)
{
    if (TryRunExecuteChunks(args)) return true;
    if (TryRunScriptArgument(args)) return true;
    if (args[0] == "-H" || ...) ShowCmdLineHelpBig();
    // ... many more conditions
}

// After: Clean switch on parse result
internal static bool CheckArgs(string[] args, ShellContext shellContext)
{
    CliParseResult parseResult = CliArgumentRegistry.Parse(args);
    
    if (!parseResult.Success)
    {
        Console.WriteLine(parseResult.ErrorMessage);
        ShowCmdLineHelp();
        return true;
    }
    
    switch (parseResult.Mode)
    {
        case CliExecutionMode.Repl: return false;
        case CliExecutionMode.Help: ShowCmdLineHelpBig(); return true;
        case CliExecutionMode.Script: return ExecuteScriptMode(parseResult);
        // ...
    }
}
```

Removed old methods:

- `IsLuaVersionOption()` — Replaced by `CliArgumentRegistry.LuaVersion.Matches()`
- `TryRunExecuteChunks()` — Logic moved to `CliArgumentRegistry.ParseExecuteOrScriptMode()`
- `IsExecuteOption()` — Replaced by registry matching
- `IsKnownNonScriptOption()` — No longer needed
- `TryRunScriptArgument()` — Logic moved to registry

### 3. New Comprehensive Help Output

The new `GenerateHelpText()` method produces structured help:

```
NovaSharp - A Lua interpreter for .NET

USAGE:
  nova [OPTIONS] [SCRIPT]
  nova -e <code> [-e <code>...] [-v <version>]
  nova -W <dumpfile> <destfile> [--internals] [--vb] [--class:<name>] [--namespace:<name>]
  nova -X <command>

HELP OPTIONS:
  -H, --help                     Display this help message and exit
  -?, /?                         Display this help message and exit (alternative)

EXECUTION OPTIONS:
  -e, --execute <code>           Execute inline Lua code (can be specified multiple times)
  -X <command>                   Execute a REPL command and exit

COMPATIBILITY OPTIONS:
  -v, --lua-version <version>    Set the Lua compatibility version

TOOLING OPTIONS:
  -W <dumpfile> <destfile>       Generate hardwire descriptors from a dump table
  --internals                    Include internal members in hardwire generation
  --vb                           Generate Visual Basic code instead of C#
  --class <name>                 Set the generated class name for hardwire
  --namespace <name>             Set the generated namespace for hardwire

LUA VERSIONS:
  Valid values: 5.1, 5.2, 5.3, 5.4, 5.5, latest

EXAMPLES:
  nova script.lua                    Run a Lua script
  nova -v 5.1 script.lua             Run script in Lua 5.1 mode
  nova -e "print('Hello')"           Execute inline code
  nova -e "x=1" -e "print(x)"        Execute multiple inline chunks
  nova                               Start interactive REPL
```

### 4. New Tests: `CliArgumentRegistryTUnitTests.cs`

Added comprehensive tests covering:

- `TryParseLuaVersion` for all version formats
- Parse results for all execution modes
- Error handling for invalid arguments
- Help text generation
- Argument definition matching

**Test categories:**

- 18 version parsing tests
- 4 help mode tests
- 6 execute mode tests
- 4 script mode tests
- 2 command mode tests
- 3 hardwire mode tests
- 4 error handling tests
- 3 help text tests
- 5 argument definition tests

### 5. Updated Existing Tests

Updated `ProgramTUnitTests.cs` to work with new error messages:

- `CheckArgsHelpFlagWritesUsageAndReturnsTrue` — Now checks for "USAGE:"
- `CheckArgsExecuteCommandFlagWithMissingArgumentShowsError` — Now checks for "-X" in error
- `CheckArgsHardwireFlagWithMissingArgumentsShowsError` — Now checks for file-related error
- `CheckArgsLuaVersionWithMissingValueReportsError` — Now checks for "-v" in error

## Benefits

1. **Single source of truth** — All argument definitions in one place
1. **Comprehensive help** — Auto-generated from definitions
1. **Consistent error messages** — Centralized error handling
1. **Testable parsing** — `CliArgumentRegistry.Parse()` returns structured result
1. **Extensible** — Adding new arguments is trivial
1. **Version propagation** — Clean flow from CLI to Script options

## Files Changed

- `src/tooling/WallstopStudios.NovaSharp.Cli/CliArgumentRegistry.cs` (NEW)
- `src/tooling/WallstopStudios.NovaSharp.Cli/Program.cs` (refactored)
- `src/tests/.../Cli/CliArgumentRegistryTUnitTests.cs` (NEW)
- `src/tests/.../Cli/ProgramTUnitTests.cs` (updated)

## Test Results

All 5218 tests pass, including:

- All new CliArgumentRegistry tests
- All existing Program tests (updated assertions)
- Full test suite

## Usage Examples

```bash
# Show help
nova --help

# Run with specific Lua version
nova -v 5.1 script.lua

# Execute inline code
nova -e "print('Hello World')"

# Multiple chunks with version
nova -v 5.4 -e "x=1" -e "print(x)"
```

## Next Steps

- Integration tests verifying CLI → Script.CompatibilityVersion flow (if not yet complete)
- Consider adding `--quiet` or `--verbose` options
- Consider adding `--strict` option for enabling all warnings
