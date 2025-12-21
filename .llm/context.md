# AI Assistant Guidelines for NovaSharp

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave all version control to the human developer
1. **NEVER use absolute paths** — All paths must be relative to repo root (no `D:/Code`, `/Users/...`, etc.)
1. **NEVER discard output** — **NO redirects or pipes to `/dev/null`** (`>/dev/null`, `2>/dev/null`, `&>/dev/null`, `| cat >/dev/null`, etc.), **even in chained commands** (`cmd1 2>/dev/null && cmd2`). Command output is essential for debugging. If a command produces too much output, use `--quiet` flags or filter with `grep`, but NEVER silently discard stderr.
1. **Lua Spec Compliance is HIGHEST PRIORITY** — When NovaSharp differs from reference Lua, fix production code, never tests
1. **Always create `.lua` test files** — Every test/fix needs standalone Lua fixtures for cross-interpreter verification
1. **Multi-Version Testing** — All TUnit tests MUST run against all applicable Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)

______________________________________________________________________

## Project Overview

NovaSharp is a multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4, 5.5) in C# for .NET, Unity3D (IL2CPP/AOT), Mono, and Xamarin.

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

### Quick Scripts (Recommended)

```bash
# Quick build - interpreter only (3-5x faster than full solution)
./scripts/build/quick.sh

# Quick build - full solution
./scripts/build/quick.sh --all

# Quick test - all tests
./scripts/test/quick.sh

# Quick test - filter by method name pattern
./scripts/test/quick.sh Floor              # Methods containing "Floor"

# Quick test - filter by class name pattern
./scripts/test/quick.sh -c MathModule      # Classes containing "MathModule"

# Quick test - combined filters
./scripts/test/quick.sh -c Math -m Floor   # Class AND method filter

# Quick test - skip build (use pre-built binaries)
./scripts/test/quick.sh --no-build Floor

# List available tests
./scripts/test/quick.sh --list
```

### Manual Commands

```bash
# Setup (run once per checkout)
dotnet tool restore

# Full solution build
dotnet build src/NovaSharp.sln -c Release

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

### High-Performance Code

When writing new types and methods, prioritize minimal allocations:

- **Prefer `readonly struct`** for small, immutable data (≤64 bytes)
- **Prefer `ref struct`** for types containing `Span<T>` or stack-only lifetimes
- **Use `ZStringBuilder.Create()`** for string building, never `StringBuilder` or `$"..."` in hot paths
- **Use `ArrayPool<T>.Shared`** or `stackalloc` instead of `new T[]` for temporary buffers
- **Use `ReadOnlySpan<char>`** instead of `string.Substring()` for slicing
- **Avoid boxing** — use generic constraints instead of interface parameters for value types

See [skills/high-performance-csharp.md](skills/high-performance-csharp.md) for detailed guidelines.

______________________________________________________________________

## Commit Style

- Concise, imperative: "Fix parser regression"
- Prefixes when helpful: "debugger: Fix breakpoint handling"
- Reference issues: "Fixes #123"

______________________________________________________________________

## Skills (Task-Specific Guides)

Detailed guides for common tasks are in `.llm/skills/`:

| Skill                                                        | When to Use                                                        |
| ------------------------------------------------------------ | ------------------------------------------------------------------ |
| [high-performance-csharp](skills/high-performance-csharp.md) | Writing new types/methods with minimal allocations                 |
| [lua-fixture-creation](skills/lua-fixture-creation.md)       | Creating `.lua` test files with harness-compatible metadata        |
| [tunit-test-writing](skills/tunit-test-writing.md)           | Writing multi-version TUnit tests with proper isolation            |
| [lua-spec-verification](skills/lua-spec-verification.md)     | Investigating Lua spec compliance, verifying against reference Lua |
| [adding-opcodes](skills/adding-opcodes.md)                   | Adding new bytecode instructions to the VM                         |
| [debugging-interpreter](skills/debugging-interpreter.md)     | Debugging the Lexer → Parser → AST → VM pipeline                   |
| [clr-interop](skills/clr-interop.md)                         | Exposing C# types to Lua, calling Lua from C#                      |
| [coverage-analysis](skills/coverage-analysis.md)             | Running coverage, interpreting reports, finding gaps               |
| [lua-comparison-harness](skills/lua-comparison-harness.md)   | Running fixtures against reference Lua interpreters                |

______________________________________________________________________

## Resources

- [docs/lua-spec/](../docs/lua-spec/) — Local Lua specs (primary reference)
- [docs/Testing.md](../docs/Testing.md) — Testing details
- [PLAN.md](../PLAN.md) — Current plan and known issues
