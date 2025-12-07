# AI Assistant Guidelines for NovaSharp

This document provides comprehensive guidance for AI assistants (GitHub Copilot, Claude, and others) working with the NovaSharp codebase. It consolidates information previously spread across `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`.

## Project Overview

NovaSharp is a multi-version Lua interpreter (supporting Lua 5.1, 5.2, 5.3, and 5.4) written in C# for .NET Standard 2.1, Mono, Xamarin, and Unity3D (including IL2CPP/AOT). It is an actively maintained fork of MoonSharp targeting modern .NET (6/7/8+), comprehensive Lua compatibility across all major versions, and Unity-first ergonomics.

## Repository Structure

```
src/
  runtime/WallstopStudios.NovaSharp.Interpreter/        # Core interpreter (lexer, parser, compiler, VM)
  tooling/NovaSharp.Cli/                # Command-line interface
  debuggers/
    NovaSharp.VsCodeDebugger/           # VS Code debugger adapter (DAP)
    NovaSharp.RemoteDebugger/           # Web-based remote debugger
  tests/
    WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/  # Canonical test runner (TUnit)
    WallstopStudios.NovaSharp.Interpreter.Tests/        # Shared Lua fixtures and helpers
docs/                                   # Testing, contributing, and architecture guides
scripts/                                # Build, coverage, and lint helpers
tools/                                  # Python utilities (audits, extractors)
```

## Build and Test Commands

### Initial Setup

```bash
# Install local CLI tools (CSharpier formatter) — run once per checkout
dotnet tool restore
```

### Building

```bash
# Full release build of all targets
dotnet build src/NovaSharp.sln -c Release

# Quick iteration on interpreter core
dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/NovaSharp.Interpreter.csproj
```

### Testing

```bash
# Run interpreter tests (TUnit, Microsoft.Testing.Platform)
dotnet test --project src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release

# Generate coverage reports
pwsh ./scripts/coverage/coverage.ps1   # or bash ./scripts/coverage/coverage.sh on macOS/Linux
```

### Formatting

```bash
# Format all C# code before committing — CSharpier is canonical
dotnet csharpier format .
```

## Architecture Overview

NovaSharp follows a classic interpreter pipeline:

```
Lua Source → [Lexer] → Tokens → [Parser] → AST → [Compiler] → Bytecode → [VM] → Execution
```

Entry point is the `Script` class, which coordinates the entire pipeline.

### Key Namespaces

| Namespace   | Purpose                                                             |
| ----------- | ------------------------------------------------------------------- |
| `Execution` | VM core: `Processor`, `ByteCode`, `Instruction`, `OpCode`           |
| `Tree`      | AST nodes: `Statement`, `Expression`, parsing logic                 |
| `DataTypes` | Lua values: `DynValue`, `Table`, `Closure`, `Coroutine`, `UserData` |
| `Interop`   | C# bridging: converters, descriptors, type registry                 |
| `CoreLib`   | Built-in Lua modules: `TableModule`, `StringModule`, `MathModule`   |
| `Debugging` | Debug infrastructure: `IDebugger`, `DebugService`, `SourceCode`     |

### DynValue: The Universal Value Type

`DynValue` is the **central hub** of NovaSharp's architecture:

- Single unified type representing all Lua values (nil, boolean, number, string, table, function, etc.)
- Used everywhere: function parameters, return values, table contents, variables
- Contains a discriminated union: `DataType` enum + value storage
- Factory methods for each type: `NewString()`, `NewTable()`, `NewClosure()`, etc.
- Provides type-safe interop via `FromObject()` and `ToObject()`

### Execution Flow Example

```csharp
Script.DoString("return x + 1")
  → Loader_Fast.LoadChunk()
      → Lexer tokenizes source
      → Parser builds AST (ChunkStatement)
      → AST.Compile() emits bytecode
  → Script.Call(closure)
      → Processor.Call() sets up stack frame
      → Processing_Loop() executes bytecode instruction-by-instruction
  → Returns DynValue result
```

## Coding Style

### Formatting

- **Indentation**: 4 spaces, braces on new lines (Allman style)
- **Naming**: PascalCase for types/methods, `_camelCase` for private fields
- **Formatting**: Run `dotnet csharpier format .` before committing — CSharpier is canonical
- **Lua fixtures**: Preserve 2-space indentation for easier diffing with upstream

### Type System

- **Explicit types**: Prefer explicit types over `var`; only use `var` for anonymous types
- **No nullable syntax**: Do not use `#nullable`, `string?`, `?.` on reference types, or `null!`
- **Access modifiers**: Always explicit; keep `using` directives minimal

### Code Organization

- **No regions**: Never add `#region`/`#endregion` directives; delete any you encounter
- **Internals access**: Prefer `internal` + `InternalsVisibleTo` over reflection hacks
- **Reflection policy**: Review `docs/modernization/reflection-audit.md` before adding reflection

### Enum Conventions

- **Explicit values**: All enum members must have explicit integer values assigned
- **Default sentinel value**: Enums that do **not** require Lua interop must have a default obsolete sentinel value at position 0:
  - Use `[Obsolete("Use a specific <EnumName>.", false)]` attribute (warning, not error)
  - Name it `Unknown = 0` or `None = 0` (use `None` for `[Flags]` enums where "no flags" is a valid state)
- **Exceptions for Lua interop**: Enums like `DataType` where `Nil = 0` is a valid Lua type should **not** have an obsolete sentinel
- **Stable ordering**: Never reorder or renumber existing enum values—add new values at the end with the next available number
- **Flags enums**: Use powers of two (`1 << 0`, `1 << 1`, etc.) and include `None = 0` as a non-obsolete default when "no flags" is semantically valid

## Testing Guidelines

### Framework

- **TUnit only**: Use `global::TUnit.Core.Test`; do not add NUnit fixtures
- **Async assertions**: Use `await Assert.That(...)` with `.ConfigureAwait(false)` on every await
- **CA2007 enforcement**: This analyzer is enforced as an error; append `.ConfigureAwait(false)` to all awaits

### Test Organization

- **Method names**: PascalCase without underscores
- **Organization**: Keep fixtures in descriptive folders (`Units`, `EndToEnd`, feature-specific)
- **Naming**: Use `<Feature>TUnitTests.cs` convention

### Isolation Attributes

- **`[UserDataIsolation]`**: For tests that mutate `UserData` registry
- **`[ScriptGlobalOptionsIsolation]`**: For tests that modify `Script.GlobalOptions`
- **`[PlatformDetectorIsolation]`**: For tests that override platform detection

### Cleanup Helpers

Use scope helpers instead of `try`/`finally`:

- `TempFileScope` / `TempDirectoryScope` for temporary files
- `SemaphoreSlimScope` for semaphore management
- `DeferredActionScope` for custom cleanup
- `ConsoleTestUtilities.WithConsoleCaptureAsync` for console capture

### TUnit Data-Driven Tests

- Use `[Arguments(...)]` for literal inputs
- Use `[MethodDataSource]` / `[ClassDataSource]` for complex objects
- Use `[CombinedDataSources]` for Cartesian products
- Reference: https://tunit.dev/llms.txt

### Spec Alignment

- When tests fail, consult the Lua 5.4 manual at https://www.lua.org/manual/5.4/
- Fix runtime code to match spec; don't weaken tests
- Cite manual sections (e.g., "§6.4 String Manipulation") in tests/PR notes

### Production Bug Policy

**CRITICAL**: Never adjust tests to accommodate production bugs. When a test fails or exposes incorrect behavior:

1. **Assume production is wrong** until proven otherwise
1. **Fix the production code** to produce correct output
1. **Keep tests unchanged** unless they are demonstrably incorrect
1. **Verify against specification** (Lua manual, DAP protocol spec, etc.)
1. **Document the fix** in commit message and PR notes

This applies to all bugs, including:

- Serialization issues (e.g., JSON outputting `{}` instead of `[]` for empty arrays)
- Protocol violations (e.g., DAP responses not matching spec)
- Lua semantics diverging from the official interpreter
- API contracts not being honored

If you discover a production bug while writing tests, fix the production code first, then verify the test passes with correct behavior. Never work around bugs with test accommodations.

## Lint Guards

Run these before pushing:

```bash
python scripts/lint/check-platform-testhooks.py
python scripts/lint/check-console-capture-semaphore.py
python scripts/lint/check-userdata-scope-usage.py
python scripts/lint/check-test-finally.py
python scripts/lint/check-temp-path-usage.py
```

## Implementation Notes

### Working with the VM

- Bytecode compilation happens **per-function**: each function has its own `ByteCode` object
- Stack frames managed via `CallStackItem` - tracks locals, return address, closure context
- Tail call optimization critical for Lua compatibility - implemented via `TailCallRequest`

### Working with Interop

- Register types via `UserData.RegisterType<T>()` before passing C# objects to Lua
- Use `InteropAccessMode.LazyOptimized` for best performance
- Standard library modules (`CoreLib/`) show idiomatic patterns

### Working with Tables

- Tables use 1-based indexing (Lua convention)
- Metatables control operator overloading (`__index`, `__newindex`, `__add`, etc.)
- `LinkedListIndex` maintains insertion order with O(1) lookup

### Adding Opcodes

1. Update `OpCode` enum in `Execution/VM/OpCode.cs`
1. Implement handler in `Processor.Processing_Loop()`
1. Add compiler emission in relevant AST node's `Compile()` method
1. Test both direct execution and bytecode serialization/deserialization

## Commit Style

- **Format**: Concise, imperative messages (e.g., "Fix parser regression")
- **Prefixes**: Use subsystem prefixes when helpful (e.g., "debugger: Fix breakpoint handling")
- **Issues**: Reference with `Fixes #ID` to auto-close on merge
- **Breaking changes**: Document prominently and coordinate release notes

## Additional Resources

- [docs/Contributing.md](docs/Contributing.md) — Full contributor guide
- [docs/Testing.md](docs/Testing.md) — Testing infrastructure details
- [docs/Modernization.md](docs/Modernization.md) — Modernization roadmap
- [PLAN.md](PLAN.md) — Current testing and coverage plan
- [Lua 5.4 Manual](https://www.lua.org/manual/5.4/) — Canonical spec reference
