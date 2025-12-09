# Copilot Instructions for NovaSharp

> **⚠️ This file is deprecated.** All AI assistant guidelines have been consolidated into [`CONTRIBUTING_AI.md`](../CONTRIBUTING_AI.md). This file is retained for backwards compatibility with GitHub Copilot.

> **🚫 CRITICAL: NEVER perform `git add` or `git commit` operations.** Leave all version control to the human developer.

> **🚫 CRITICAL: NEVER use absolute paths to local development machines.** All file paths must be relative to the repository root. Never reference paths like `D:/Code`, `C:/Users`, `/Users/username`, `/home/username`, or any machine-specific path in committed files.

See [`CONTRIBUTING_AI.md`](../CONTRIBUTING_AI.md) for:

- Project overview and repository structure
- Build, test, and formatting commands
- Architecture overview and key namespaces
- Coding style and testing guidelines
- **LuaNumber usage for Lua math operations**
- **Lua fixture verification policy** (create cross-interpreter test fixtures for all bug fixes)
- Lint guards and commit style

For human contributors, see [`docs/Contributing.md`](../docs/Contributing.md).

______________________________________________________________________

## Legacy Content (Preserved for Reference)

### Project Overview

NovaSharp is a multi-version Lua interpreter (supporting Lua 5.1, 5.2, 5.3, and 5.4) written in C# for .NET Standard 2.1, Mono, Xamarin, and Unity3D (including IL2CPP/AOT). It is an actively maintained continuation of the original interpreter by Marco Mastropaolo targeting modern .NET (6/7/8+), comprehensive Lua compatibility across all major versions, and Unity-first ergonomics.

## Repository Structure

```
src/
  runtime/WallstopStudios.NovaSharp.Interpreter/   # Core interpreter (lexer, parser, compiler, VM)
  tooling/NovaSharp.Cli/           # Command-line interface
  debuggers/                       # VS Code and remote debugger adapters
  tests/
    WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/  # Canonical test runner (TUnit)
    WallstopStudios.NovaSharp.Interpreter.Tests/        # Shared Lua fixtures and helpers
docs/                              # Testing, contributing, and architecture guides
scripts/                           # Build, coverage, and lint helpers
```

## Build Commands

```bash
# Install local CLI tools (CSharpier formatter) — run once per checkout
dotnet tool restore

# Full release build
dotnet build src/NovaSharp.sln -c Release

# Quick iteration on interpreter core
dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/NovaSharp.Interpreter.csproj

# Format all C# code before committing
dotnet csharpier .
```

## Test Commands

```bash
# Run interpreter tests (TUnit, Microsoft.Testing.Platform)
dotnet test --project src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release

# Generate coverage reports
pwsh ./scripts/coverage/coverage.ps1   # or bash ./scripts/coverage/coverage.sh
```

## Architecture

NovaSharp follows a classic interpreter pipeline:

```
Lua Source → [Lexer] → Tokens → [Parser] → AST → [Compiler] → Bytecode → [VM] → Execution
```

### Key Namespaces

| Namespace   | Purpose                                                             |
| ----------- | ------------------------------------------------------------------- |
| `Execution` | VM core: `Processor`, `ByteCode`, `Instruction`, `OpCode`           |
| `Tree`      | AST nodes: `Statement`, `Expression`, parsing logic                 |
| `DataTypes` | Lua values: `DynValue`, `Table`, `Closure`, `Coroutine`, `UserData` |
| `Interop`   | C# bridging: converters, descriptors, type registry                 |
| `CoreLib`   | Built-in Lua modules: `TableModule`, `StringModule`, `MathModule`   |

### DynValue: The Universal Value Type

`DynValue` is the central hub representing all Lua values (nil, boolean, number, string, table, function, etc.). It uses a discriminated union pattern with a `DataType` enum and value storage. Factory methods: `NewString()`, `NewTable()`, `NewClosure()`, etc.

## Coding Style

- **Indentation**: 4 spaces, braces on new lines (Allman style)
- **Naming**: PascalCase for types/methods, `_camelCase` for private fields
- **Formatting**: Run `dotnet csharpier .` before committing — CSharpier is canonical
- **Explicit types**: Prefer explicit types over `var`; only use `var` for anonymous types
- **No nullable syntax**: Do not use `#nullable`, `string?`, `?.` on reference types, or `null!`
- **No regions**: Never add `#region`/`#endregion` directives; delete any you encounter
- **Access modifiers**: Always explicit; keep `using` directives minimal
- **Internals access**: Prefer `internal` + `InternalsVisibleTo` over reflection hacks

## Testing Guidelines

- **Framework**: TUnit only (`global::TUnit.Core.Test`); do not add NUnit fixtures
- **Async assertions**: Use `await Assert.That(...)` with `.ConfigureAwait(false)` on every await
- **Isolation**: Use `[UserDataIsolation]` for tests that mutate `UserData` registry; use `[ScriptGlobalOptionsIsolation]` for `Script.GlobalOptions` changes
- **Method names**: PascalCase without underscores
- **Cleanup**: Use scope helpers (`TempFileScope`, `TempDirectoryScope`, `SemaphoreSlimScope`) instead of `try`/`finally`
- **Console capture**: Use `ConsoleTestUtilities.WithConsoleCaptureAsync` / `WithConsoleRedirectionAsync`
- **Spec alignment**: When tests fail, consult the Lua 5.4 manual; fix runtime code, not tests
- **Production bug policy**: Never adjust tests to accommodate production bugs — fix production code to produce correct output

## Lint Guards (Run Before Pushing)

```bash
python scripts/lint/check-platform-testhooks.py
python scripts/lint/check-console-capture-semaphore.py
python scripts/lint/check-userdata-scope-usage.py
python scripts/lint/check-test-finally.py
python scripts/lint/check-temp-path-usage.py
```

## Commit Style

- Concise, imperative messages: "Fix parser regression"
- Subsystem prefixes when helpful: "debugger: Fix breakpoint handling"
- Reference issues: "Fixes #123"

## Additional Resources

- [AGENTS.md](../AGENTS.md) — Detailed agent guidelines (will be consolidated)
- [CLAUDE.md](../CLAUDE.md) — Claude-specific context (will be consolidated)
- [docs/Contributing.md](../docs/Contributing.md) — Full contributor guide
- [docs/Testing.md](../docs/Testing.md) — Testing infrastructure details
- [Lua 5.4 Manual](https://www.lua.org/manual/5.4/) — Canonical spec reference
