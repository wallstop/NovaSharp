# AI Assistant Guidelines for NovaSharp

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave all version control to the human developer
1. **NEVER use absolute paths** — All paths must be relative to repo root (no `D:/Code`, `/Users/...`, etc.)
1. **Lua Spec Compliance is HIGHEST PRIORITY** — When NovaSharp differs from reference Lua, fix production code, never tests
1. **Always create `.lua` test files** — Every test/fix needs standalone Lua fixtures for cross-interpreter verification
1. **Multi-Version Testing** — All TUnit tests MUST use `[Arguments(LuaCompatibilityVersion.LuaXX)]` attributes to run against all applicable Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)

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

**Fixture Template** (harness-compatible metadata):

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/.../TestClass.cs:123
-- @test: TestClass.TestMethod
-- @compat-notes: Optional notes about version-specific behavior

-- Test: <description>
-- Reference: <Lua spec §section>

local result = ...  -- test code
return result       -- or print(result)
```

**Metadata Fields** (required for comparison harness):

| Field             | Description                                                              | Example                          |
| ----------------- | ------------------------------------------------------------------------ | -------------------------------- |
| `@lua-versions`   | Comma-separated compatible Lua versions; use `5.3+` for ranges           | `5.3, 5.4, 5.5` or `5.1+`        |
| `@novasharp-only` | `true` if fixture uses NovaSharp extensions (CLR interop, `!=` operator) | `false`                          |
| `@expects-error`  | `true` if the test expects a runtime error                               | `true`                           |
| `@source`         | Relative path to C# source file with line number                         | `src/tests/.../MyTests.cs:42`    |
| `@test`           | Test class and method name                                               | `MathModuleTUnitTests.TestFloor` |
| `@compat-notes`   | Optional: version-specific behavior notes                                | `Lua 5.3+: integer division`     |

**Error-expecting fixture example**:

```lua
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/.../ErrorTests.cs:99
-- @test: ErrorTests.DivisionByZeroErrors

-- Test: Division by zero should error in integer mode
return 1 // 0  -- integer division by zero
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

### Shell Commands

- **Prefer `rg` (ripgrep) over `grep`** — Faster, respects `.gitignore`, better defaults
- Example: `rg "\.Number" src/runtime/` instead of `grep -rn "\.Number" src/runtime/`

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

### 🔴 Multi-Version Testing Requirements

**NovaSharp supports Lua 5.1, 5.2, 5.3, 5.4, and 5.5.** All new TUnit tests MUST run against all applicable versions using `[Arguments]` attributes. **Tests without version coverage will be rejected.**

**MANDATORY: Always test across all versions unless behavior is version-specific:**

```csharp
[Test]
[Arguments(LuaCompatibilityVersion.Lua51)]
[Arguments(LuaCompatibilityVersion.Lua52)]
[Arguments(LuaCompatibilityVersion.Lua53)]
[Arguments(LuaCompatibilityVersion.Lua54)]
[Arguments(LuaCompatibilityVersion.Lua55)]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // test code
}
```

**For version-specific features, test BOTH positive and negative scenarios:**

```csharp
// POSITIVE: Feature works in versions that support it
[Test]
[Arguments(LuaCompatibilityVersion.Lua53)]
[Arguments(LuaCompatibilityVersion.Lua54)]
[Arguments(LuaCompatibilityVersion.Lua55)]
public async Task MathTypeAvailableInLua53Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type(5)");
    await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
}

// NEGATIVE: Feature is absent/nil in versions that don't support it
[Test]
[Arguments(LuaCompatibilityVersion.Lua51)]
[Arguments(LuaCompatibilityVersion.Lua52)]
public async Task MathTypeShouldBeNilInPreLua53(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type");
    await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
}
```

**Version coverage checklist for every new test:**

1. Does this feature exist in all versions? → Test ALL 5 versions
1. Is this feature version-specific? → Test BOTH:
   - **Positive**: Verify it works in supported versions
   - **Negative**: Verify it's unavailable/nil/errors in unsupported versions
1. Does behavior differ between versions? → Create separate tests per behavior variant

**Test naming patterns:**

- `FeatureWorksAcrossAllVersions` — Universal behavior
- `FeatureAvailableInLua53Plus` — Positive test for newer versions
- `FeatureShouldBeNilInPreLua53` — Negative test for older versions
- `FeatureBehaviorDiffersInLua51` — Version-specific behavior

**Lua fixture naming for version-specific behavior:**

- `feature_test.lua` — Works in all versions
- `feature_test_51.lua` — Lua 5.1 specific
- `feature_test_53plus.lua` — Lua 5.3+
- `feature_test_error_pre53.lua` — Expected error in pre-5.3

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
