# AI Assistant Guidelines for NovaSharp

This document provides comprehensive guidance for AI assistants (GitHub Copilot, Claude, and others) working with the NovaSharp codebase. It consolidates information previously spread across `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`.

## Critical Restrictions

**NEVER perform `git add` or `git commit` operations.** Leave all version control operations to the human developer. You may:

- Suggest changes to files
- Create or modify code
- Run builds and tests
- Analyze git status or history

But you must **never** stage files (`git add`) or create commits (`git commit`). The human will handle all version control operations.

**NEVER use absolute paths to local development machines.** All file paths in committed code, configuration, scripts, and documentation must be relative to the repository root. Specifically:

- ❌ Never reference paths like `D:/Code`, `C:/Users`, `/Users/username`, `/home/username`, or any other machine-specific absolute path
- ❌ Never hardcode paths from your local environment into any committed file
- ✅ Use relative paths from repo root (e.g., `src/runtime/...`, `./scripts/...`)
- ✅ Use environment variables or runtime path resolution for dynamic paths
- ✅ Template placeholders like `{path}` in test fixtures are acceptable—these are replaced at runtime

Before suggesting any file changes, verify that no absolute paths to local machines are introduced.

## Project Overview

NovaSharp is a multi-version Lua interpreter (supporting Lua 5.1, 5.2, 5.3, and 5.4) written in C# for .NET Standard 2.1, Mono, Xamarin, and Unity3D (including IL2CPP/AOT). It is an actively maintained continuation of the original interpreter by Marco Mastropaolo targeting modern .NET (6/7/8+), comprehensive Lua compatibility across all major versions, and Unity-first ergonomics.

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

### LuaNumber for Lua Math

- **Always use `LuaNumber`**: When performing any Lua numeric operations, use the `LuaNumber` struct (from `DataTypes/LuaNumber.cs`) instead of raw C# numeric types (`double`, `float`, `int`, `long`)
- **Preserve type information**: `LuaNumber` tracks whether the value is an integer or float subtype—this distinction is critical for Lua 5.3+ semantics
- **Access patterns**:
  - Use `DynValue.LuaNumber` to get the `LuaNumber` from a DynValue (preserves integer vs float)
  - Avoid `DynValue.Number` (returns `double`, loses type information)
  - Check `LuaNumber.IsInteger` before extracting as `AsInteger` or `AsFloat`
- **Validation helpers**: Use `LuaNumberHelpers.RequireIntegerRepresentation()` for functions requiring integer arguments
- **Exception**: Only extract to raw C# types (`double`, `long`) after you have verified the appropriate numeric subtype via `LuaNumber` properties

### Magic String Consolidation

- **No duplicate string literals**: Consolidate all "magic strings" (repeated string literals, error messages, format strings, identifiers) into named constants to ensure a single source of truth
- **Use `const` or `static readonly`**: Define string constants at the class or module level using `const string` for compile-time constants or `static readonly string` when runtime initialization is needed
- **Prefer `nameof()` expressions**: When referencing member names (parameters, properties, methods, types), always use `nameof()` instead of string literals to ensure refactoring safety
- **Organize by domain**: Group related constants in dedicated static classes (e.g., `ErrorMessages`, `LuaKeywords`, `MetamethodNames`) when strings are shared across multiple files
- **Error message consistency**: Error messages should reference a single constant so formatting changes apply everywhere

**Examples**:

```csharp
// CORRECT: Named constants
public static class MetamethodNames
{
    public const string Index = "__index";
    public const string NewIndex = "__newindex";
    public const string Call = "__call";
}

// CORRECT: Use nameof for member references
throw new ArgumentNullException(nameof(script));
throw new ArgumentException($"Invalid value for {nameof(options)}", nameof(options));

// CORRECT: Centralized error messages
public static class ErrorMessages
{
    public const string NumberHasNoIntegerRepresentation = "number has no integer representation";
}

// WRONG: Duplicated magic strings
if (key == "__index") { }  // Use MetamethodNames.Index
if (key == "__index") { }  // Duplicated!

// WRONG: String literal for parameter name
throw new ArgumentNullException("script");  // Use nameof(script)
```

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

**Local Lua Specification References** — Use these for all Lua compatibility work:

- [`docs/lua-spec/lua-5.1-spec.md`](docs/lua-spec/lua-5.1-spec.md) — Lua 5.1 Reference

- [`docs/lua-spec/lua-5.2-spec.md`](docs/lua-spec/lua-5.2-spec.md) — Lua 5.2 Reference

- [`docs/lua-spec/lua-5.3-spec.md`](docs/lua-spec/lua-5.3-spec.md) — Lua 5.3 Reference

- [`docs/lua-spec/lua-5.4-spec.md`](docs/lua-spec/lua-5.4-spec.md) — Lua 5.4 Reference (primary target)

- [`docs/lua-spec/lua-5.5-spec.md`](docs/lua-spec/lua-5.5-spec.md) — Lua 5.5 (Work in Progress)

- When tests fail, first consult the local specs above, then verify against https://www.lua.org/manual/5.4/ if needed

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

### Lua Fixture Verification Policy

**REQUIRED**: When fixing any Lua semantic issue or discovering a behavioral discrepancy, create a comprehensive suite of standalone Lua files that can be run against both NovaSharp and the official Lua interpreter to verify correctness and prevent regressions.

**Fixture Requirements**:

1. **Create standalone `.lua` files** in the appropriate `LuaFixtures/<TestClass>/` directory
1. **One fixture per behavior variant** — separate files for success cases, error cases, and edge cases
1. **Version-aware naming** — suffix with `_51`, `_52`, `_53plus`, `_54plus`, etc. when behavior differs by version
1. **Self-documenting** — include comments explaining expected behavior and which Lua versions apply
1. **Runnable against real Lua** — fixtures must execute cleanly with `lua5.1`, `lua5.4`, etc.

**Fixture Structure Pattern**:

```lua
-- Test: <description of what's being tested>
-- Expected: <success/error/specific output>
-- Versions: <5.1, 5.2, 5.3, 5.4 or specific subset>
-- Reference: <Lua manual section, e.g., "§6.4.1">

local success, err = pcall(function()
    -- Test code here
end)

if success then
    print("PASS")
else
    print("EXPECTED ERROR: " .. tostring(err))
end
```

**Verification Workflow**:

1. Run fixture against NovaSharp: `nova --lua-version 5.4 fixture.lua`
1. Run fixture against real Lua: `lua5.4 fixture.lua`
1. Compare outputs — they must match exactly
1. Document any intentional divergences in the fixture comments

**Example Fixtures** (from `string.char` fix):

- `CharErrorsOnNegativeValue.lua` — tests error on `string.char(-1)`
- `CharErrorsOnValueAbove255.lua` — tests error on `string.char(256)`
- `CharAcceptsBoundaryValueZero.lua` — tests success on `string.char(0)`
- `CharAcceptsBoundaryValue255.lua` — tests success on `string.char(255)`

This policy ensures every behavioral fix has cross-interpreter verification and guards against future regressions.

### Lua Corpus Regeneration

**REQUIRED**: After adding, updating, or modifying any tests that contain embedded Lua code (via `DoString(...)` calls), regenerate the Lua corpus so the extracted fixtures stay in sync with the test suite.

**Regeneration Commands**:

```bash
# Extract Lua snippets from C# test files to LuaFixtures/
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py

# Dry-run to preview changes without writing files
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --dry-run
```

**Verification Commands** (run after regeneration to ensure parity with reference Lua):

```bash
# Run fixtures against Lua 5.4 (primary target) with parallel execution
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4

# Run against specific Lua version
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.1

# Compare outputs between NovaSharp and reference Lua
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-results
```

**When to Regenerate**:

- After adding new `[Test]` methods with `DoString(...)` calls
- After modifying Lua code in existing test methods
- After fixing Lua semantic issues that change expected behavior
- After adding new Lua fixtures directly to `LuaFixtures/`

The corpus extractor automatically generates version compatibility headers (`@lua-versions`) and source traceability metadata (`@source`, `@test`) for each extracted fixture.

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
- [docs/lua-spec/](docs/lua-spec/) — **Local Lua specifications (5.1, 5.2, 5.3, 5.4, 5.5)** — Use for all compatibility work
- [Lua 5.4 Manual](https://www.lua.org/manual/5.4/) — Online canonical spec reference (backup)
