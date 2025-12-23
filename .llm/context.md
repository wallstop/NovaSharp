# AI Assistant Guidelines for NovaSharp

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave all version control to the human developer
1. **NEVER use absolute paths** — All paths must be relative to repo root (no `D:/Code`, `/Users/...`, etc.)
1. **NEVER discard output** — **NO redirects or pipes to `/dev/null`** (`>/dev/null`, `2>/dev/null`, `&>/dev/null`, `| cat >/dev/null`, etc.), **even in chained commands** (`cmd1 2>/dev/null && cmd2`). Command output is essential for debugging. If a command produces too much output, use `--quiet` flags or filter with `grep`, but NEVER silently discard stderr.
1. **Lua Spec Compliance is HIGHEST PRIORITY** — When NovaSharp differs from reference Lua, fix production code, never tests
1. **Always create `.lua` test files** — Every test/fix needs standalone Lua fixtures for cross-interpreter verification
1. **Multi-Version Testing** — All TUnit tests MUST run against all applicable Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)
1. **Lua Fixture Metadata** — ONLY use `@lua-versions`, `@novasharp-only`, `@expects-error`. Fields like `@min-version`, `@max-version`, `@versions`, `@name`, `@description` are **NOT parsed** by the harness and will be silently ignored

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

### File Structure

- **`using` directives INSIDE namespace** — Never at file top
- **One type per file** — Match filename to type name

### Formatting

- 4-space indent, Allman braces
- Run `dotnet csharpier format .` before commits

### Naming

- **PascalCase**: Types, methods, properties, constants
- **`_camelCase`**: Private fields
- **NO underscores in method names** — Including tests: `FeatureWorksCorrectly` not `Feature_Works_Correctly`

### Types & Variables

- **Explicit types always** — Never use `var` (exception: anonymous types where required)
- **NEVER use nullable reference types** — No `#nullable`, `string?`, `?.`, `??`, `null!`
- **NEVER use `#region`/`#endregion`**
- **Prefer `internal`** + `InternalsVisibleTo` over reflection

### Comments

- **Minimal comments** — Write self-documenting code with clear naming
- **No obvious comments** — Don't explain what code does; explain *why* if non-obvious
- **XML docs only for public API** — Not internal implementation

### Enums

- Explicit integer values for all members
- `None = 0` or `Unknown = 0` sentinel (obsolete) except for Lua interop enums
- **Flag enums**: Combined values (`|`, `&`) must be external helper constants, not enum members
- Invalid values: throw `InvalidEnumArgumentException`
- **NEVER call `.ToString()` on enums** — Use cached string lookups instead (see below)

### Enum String Caching

Calling `.ToString()` on enums allocates a new string every time. Use dedicated cache classes:

```csharp
// ❌ BAD: Allocates on every call
sb.Append(tokenType.ToString());

// ✅ GOOD: Zero allocation
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));  // Pre-cached uppercase
sb.Append(dataType.ToLuaDebuggerString());      // Extension method
```

Available caches: `TokenTypeStrings`, `OpCodeStrings`, `SymbolRefTypeStrings`, `ModLoadStateStrings`, `DebuggerActionTypeStrings`, and `EnumStringCache<TEnum>` for other enums.

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
- **Use `ZString.Concat()`** for simple 2-4 element concatenation
- **Use span-based parsing** instead of `string.Split()` — see [span-optimization](skills/span-optimization.md)
- **Use `ReadOnlySpan<char>`** instead of `string.Substring()` for slicing
- **Reuse existing objects as locks** — never allocate `new object()` for locking; use an existing field
- **Use `HashCodeHelper.HashCode()`** for `GetHashCode()` — never bespoke `hash * 31` patterns or `HashCode.Combine()`
- **Pooled resources** — **ALWAYS use `using` with `Get()`, NEVER manual `Rent()`/`Return()`**:
  - `ListPool<T>.Get()`, `HashSetPool<T>.Get()`, `DictionaryPool<K,V>.Get()` for collections
  - `DynValueArrayPool.Get()`/`ObjectArrayPool.Get()` for fixed exact-size arrays (VM frames, reflection)
  - `SystemArrayPool<T>.Get()` for variable-size buffers
  - `stackalloc` for small compile-time-constant sizes (no pool needed)
  - If type owns pooled resources, implement `IDisposable` and store `PooledResource<T>` as field
- **Avoid boxing** — use generic constraints instead of interface parameters for value types

See [skills/high-performance-csharp.md](skills/high-performance-csharp.md), [skills/zstring-migration.md](skills/zstring-migration.md), and [skills/span-optimization.md](skills/span-optimization.md) for detailed guidelines.

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
| [zstring-migration](skills/zstring-migration.md)             | Migrating string interpolation/concat to zero-allocation ZString   |
| [span-optimization](skills/span-optimization.md)             | Replacing Split/Substring/ToArray with span-based alternatives     |
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
