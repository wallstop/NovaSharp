# Lua Semantic Divergences

> **Status**: Reference documentation for the lua-comparison CI gate.

This document catalogs known semantic differences between NovaSharp and reference Lua interpreters that cause fixtures to produce different outputs. These divergences are categorized by their nature and severity.

## Summary Statistics

| Category                        | Count | CI Treatment    |
| ------------------------------- | ----- | --------------- |
| NovaSharp-only fixtures (CLR)   | 539   | Skipped         |
| Version-incompatible            | 68    | Skipped         |
| Both error (different messages) | ~53   | Expected        |
| Output format differences       | ~23   | Document/Ignore |

**Effective match rate**: ~66% of comparable fixtures (those runnable by both interpreters without CLR dependencies).

## Categories of Divergence

### 1. NovaSharp-Only Features (Intentional)

These fixtures exercise CLR interop, userdata binding, and NovaSharp extensions that have no Lua equivalent:

- **CLR type registration** (`UserData.RegisterType<T>()`)
- **C# object indexing** (accessing .NET properties/fields)
- **Event subscription** (`obj.Event:add()` / `:remove()`)
- **Extension methods** (registered via `ExtensionMethodsRegistry`)
- **NovaSharp `!=` operator** (non-standard syntax extension)

**CI treatment**: Marked `@novasharp-only: true` in fixture headers; skipped by comparison runner.

### 2. Error Message Format Differences (Expected)

Both interpreters error on invalid code, but error messages differ:

| Pattern               | Lua Output                            | NovaSharp Output                                |
| --------------------- | ------------------------------------- | ----------------------------------------------- |
| Nil index             | `attempt to index a nil value (name)` | `attempt to index a nil value`                  |
| Stack trace format    | `[C]: in ?`                           | `.NET stack trace with line numbers`            |
| Module not found      | Lists search paths                    | `module 'X' not found`                          |
| Break outside loop    | `break outside loop at line N`        | `<break> at line N not inside a loop`           |
| Syntax error location | `unexpected symbol near 'X'`          | `unexpected symbol near 'X'` (same, line diffs) |

**CI treatment**: Classified as `both_error` in comparison; expected behavior—both correctly reject invalid code.

### 3. Output Format Differences (Normalization Candidates)

#### Table/Function Address Representation

- **Lua**: `table: 0x7f5a3c001234`
- **NovaSharp**: `table: 00000BD3ABCD1234` (different hex format)

The normalization script replaces addresses with `<addr>`, but the regex needs to handle NovaSharp's format.

**Affected fixtures**: `BinaryDumpTUnitTests/LoadChangeEnvWithDebugSetUpValue.lua`

#### Debug Module Prompt

- **Lua**: `lua_debug>`
- **NovaSharp**: Full path prefix `/workspaces/.../`

**Affected fixtures**: `DebugModuleTUnitTests/DebugDebug*.lua`, multiple `DebugModuleTapParityTUnitTests/Unknown*.lua`

#### Debug Module Availability

NovaSharp's debug module is a partial implementation. Some fixtures using `require 'debug'` fail because:

- **Lua 5.4**: Full debug library available
- **NovaSharp**: `module 'debug' not found` (not registered by default)

**Affected fixtures**: `DebugModuleTapParityTUnitTests/Unknown.lua`, `Unknown_1.lua`, `Unknown_5.lua`

### 4. Semantic Differences (Potential Bugs)

These require investigation to determine if NovaSharp behavior should be fixed:

#### `<close>` Reassignment Behavior

- **Fixture**: `CloseAttributeTUnitTests/ReassignmentClosesPreviousValueImmediately.lua`
- **Lua 5.4**: Reports error with full path in error message
- **NovaSharp**: Produces different output

**Status**: Needs investigation.

#### xpcall Behavior

- **Fixtures**: `ErrorHandlingModuleTUnitTests/Xpcall*.lua`
- **Difference**: Line count differs (Lua=5, Nova=1)

**Status**: Needs investigation to verify error handler semantics match Lua 5.4.

#### IO Module Behavior

- **Fixtures**: `IoModuleTUnitTests/Close*.lua`, `Input*.lua`
- **Difference**: Line count differs (Lua=4, Nova=1)

**Status**: Needs investigation for file handle output format.

## CI Gate Configuration

The `lua-comparison` job runs with semantic normalization enabled. The current normalizations:

```python
# Memory addresses: 0x7f... → <addr>
result = re.sub(r'0x[0-9a-fA-F]+', '<addr>', result)

# Line numbers in errors: file.lua:123: → file.lua:<line>:
result = re.sub(r'(\.lua):(\d+):', r'\1:<line>:', result)

# NovaSharp CLI compatibility info removed
result = re.sub(r'^\[compatibility\].*$\n?', '', result, flags=re.MULTILINE)
```

### Recommended Additional Normalizations

1. **NovaSharp hex addresses**: `00000BD3...` format (no `0x` prefix)
1. **Debug prompt**: Normalize `lua_debug>` and full-path prefixes
1. **Stack trace format**: Normalize .NET stack traces to Lua format

## Fixture Metadata

Each fixture contains metadata headers for version compatibility:

```lua
-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: path/to/TestClass.cs:123
-- @test: TestClass.TestMethod
```

### Updating Fixture Metadata

When a fixture is determined to be NovaSharp-specific:

1. Update the header: `-- @novasharp-only: true`
1. The runner will skip it in Lua-vs-NovaSharp comparisons
1. Document the reason in this file if it represents a known divergence

## Future Work

1. **Improve normalization**: Update `compare-lua-outputs.py` to handle NovaSharp-specific output formats
1. **Debug module**: Consider implementing more of the debug library
1. **Semantic alignment**: Investigate potential bugs in `<close>`, xpcall, and IO modules
1. **Multi-version matrix**: Document version-specific divergences (5.1 vs 5.4 syntax)

## Related Documentation

- [Lua Comparison Harness](lua-comparison-harness.md) – Full harness documentation
- [Lua Compatibility](../LuaCompatibility.md) – Feature support matrix
- [PLAN.md](../../PLAN.md) – Current development priorities
