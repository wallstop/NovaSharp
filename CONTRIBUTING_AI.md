# AI Assistant Guidelines for NovaSharp

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave all version control to the human developer
1. **NEVER use absolute paths** — All paths must be relative to repo root (no `D:/Code`, `/Users/...`, etc.)
1. **Lua Spec Compliance is HIGHEST PRIORITY** — When NovaSharp differs from reference Lua, fix production code, never tests

## 🔴 Lua Spec Compliance

**NovaSharp must match official Lua behavior.** When behavior differs from reference Lua:

1. **ASSUME NOVASHARP IS WRONG** — verify against `lua5.1`, `lua5.3`, `lua5.4`, `lua5.5`
1. **FIX PRODUCTION CODE** — never adjust tests to match buggy behavior
1. **CREATE LUA TEST FILES** — every fix needs standalone `.lua` fixtures runnable against real Lua
1. **UPDATE PLAN.md** — document fixes in §8.38

**NEVER**:

- Mark fixtures `@novasharp-only: true` (unless testing intentional extensions)
- Change `@expects-error` to match NovaSharp's incorrect behavior
- Skip, disable, or weaken tests

**Verification**:

```bash
lua5.4 -e "print(your_test_code)"
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-5.4
```

## 🔴 Testing: Always Create Lua Files

**Every test, bug fix, or behavioral change MUST include standalone `.lua` files** that can be verified against real Lua interpreters.

**Requirements**:

1. Create `.lua` files in `LuaFixtures/<TestClass>/` directory
1. One file per behavior variant (success, error, edge cases)
1. Version-aware naming: `_51`, `_52`, `_53plus`, `_54plus` suffixes when behavior differs
1. Files must run cleanly with `lua5.1`, `lua5.4`, etc.

**Fixture Template**:

```lua
-- Test: <description>
-- Expected: <success/error/output>
-- Versions: <5.1, 5.2, 5.3, 5.4>
-- Reference: <§section>

local ok, err = pcall(function()
    -- test code
end)
print(ok and "PASS" or ("ERROR: " .. tostring(err)))
```

**Regenerate corpus after test changes**:

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

______________________________________________________________________

## Project Overview

NovaSharp is a multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4) in C# for .NET, Unity3D (IL2CPP/AOT), Mono, and Xamarin.

**Repository Structure**:

```
src/
  runtime/WallstopStudios.NovaSharp.Interpreter/  # Core interpreter
  tooling/NovaSharp.Cli/                          # CLI
  debuggers/                                       # VS Code & remote debuggers
  tests/
    WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/  # Test runner (TUnit)
    WallstopStudios.NovaSharp.Interpreter.Tests/        # Shared fixtures
docs/lua-spec/                                    # Local Lua specs (5.1-5.5)
```

**Architecture**:

```
Lua Source → Lexer → Parser → AST → Compiler → Bytecode → VM → Execution
```

Key namespaces: `Execution` (VM), `Tree` (AST), `DataTypes` (DynValue, Table), `Interop` (C# bridge), `CoreLib` (stdlib)

______________________________________________________________________

## Commands

```bash
# Setup
dotnet tool restore

# Build
dotnet build src/NovaSharp.sln -c Release

# Test
dotnet test --project src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release

# Format (required before commits)
dotnet csharpier format .

# Coverage
bash ./scripts/coverage/coverage.sh
```

______________________________________________________________________

## Coding Style

- **Formatting**: 4-space indent, Allman braces, run `dotnet csharpier format .`
- **Naming**: PascalCase types/methods, `_camelCase` private fields
- **Types**: Explicit types preferred; no `var` unless required (anonymous types)
- **Nullable**: Never use `#nullable`, `string?`, `?.`, `null!`
- **Regions**: Never use `#region`/`#endregion`
- **Internals**: Prefer `internal` + `InternalsVisibleTo` over reflection

### Enums

- Explicit integer values for all members
- `None = 0` or `Unknown = 0` sentinel (obsolete) except for Lua interop enums
- **Flag enums**: Combined values (`|`, `&`) must be external helper constants, not enum members
- Invalid values: throw `InvalidEnumArgumentException`

### LuaNumber

- Use `LuaNumber` struct for all Lua numeric operations (preserves integer vs float subtype)
- Access via `DynValue.LuaNumber`, not `DynValue.Number`

### Magic Strings

- Consolidate repeated strings into `const` or `static readonly` constants
- Use `nameof()` for parameter/member references

______________________________________________________________________

## Testing

**Framework**: TUnit only (`global::TUnit.Core.Test`)

**Requirements**:

- Async assertions: `await Assert.That(...).ConfigureAwait(false)`
- Method names: PascalCase, no underscores
- Isolation: `[UserDataIsolation]`, `[ScriptGlobalOptionsIsolation]`, `[PlatformDetectorIsolation]`
- Cleanup: Use `TempFileScope`, `SemaphoreSlimScope`, `ConsoleTestUtilities`
- **Every test needs corresponding `.lua` files** for cross-interpreter verification

**Data-driven tests**: `[Arguments]`, `[MethodDataSource]`, `[CombinedDataSources]`

**Lint guards** (run before push):

```bash
python scripts/lint/check-platform-testhooks.py
python scripts/lint/check-console-capture-semaphore.py
python scripts/lint/check-userdata-scope-usage.py
python scripts/lint/check-test-finally.py
python scripts/lint/check-temp-path-usage.py
```

______________________________________________________________________

## Implementation Notes

### VM

- Per-function bytecode, stack frames via `CallStackItem`, tail call via `TailCallRequest`

### Interop

- Register types with `UserData.RegisterType<T>()` before use
- `InteropAccessMode.LazyOptimized` for performance

### Tables

- 1-based indexing, metatables for operator overloading

### Adding Opcodes

1. Update `OpCode` enum
1. Implement in `Processor.Processing_Loop()`
1. Emit in AST node's `Compile()`
1. Test execution and serialization

______________________________________________________________________

## Commit Style

- Concise, imperative: "Fix parser regression"
- Prefixes when helpful: "debugger: Fix breakpoint handling"
- Reference issues: "Fixes #123"

## Resources

- [docs/lua-spec/](docs/lua-spec/) — Local Lua specs (primary reference)
- [docs/Testing.md](docs/Testing.md) — Testing details
- [PLAN.md](PLAN.md) — Current plan and known issues
