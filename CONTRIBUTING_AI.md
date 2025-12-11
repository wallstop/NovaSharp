# AI Assistant Guidelines for NovaSharp

This document provides comprehensive guidance for AI assistants (GitHub Copilot, Claude, and others) working with the NovaSharp codebase. It consolidates information previously spread across `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md`.

## 🔴 HIGHEST PRIORITY: Lua Spec Compliance

**NovaSharp's PRIMARY GOAL is to be a faithful Lua interpreter that matches the official Lua reference implementation as closely as possible.**

When working on any code that affects Lua semantics:

1. **ASSUME NOVASHARP IS WRONG** when behavior differs from reference Lua
1. **FIX THE PRODUCTION CODE** to match Lua behavior — never adjust tests to accommodate bugs
1. **VERIFY AGAINST REAL LUA** by running the same code against `lua5.1`, `lua5.3`, `lua5.4`, etc.
1. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua interpreters
1. **CITE THE LUA MANUAL** (e.g., "§6.4 String Manipulation") when documenting fixes

### 🚫 What NOT to Do When a Spec Mismatch is Found

**NEVER** do any of the following to "fix" a test failure caused by NovaSharp diverging from Lua:

- ❌ Mark fixture as `@novasharp-only: true` (unless it's testing an intentional NovaSharp extension)
- ❌ Change `@expects-error: true` to `@expects-error: false` to match NovaSharp's incorrect behavior
- ❌ Modify test expectations to match NovaSharp's buggy output
- ❌ Skip or disable the test
- ❌ Add the mismatch to a "known divergences" list

### ✅ What TO Do When a Spec Mismatch is Found

1. **Verify against real Lua** to confirm NovaSharp is wrong:

   ```bash
   lua5.1 -e "print(your_test_code_here)"
   lua5.2 -e "print(your_test_code_here)"
   lua5.3 -e "print(your_test_code_here)"
   lua5.4 -e "print(your_test_code_here)"
   lua5.5 -e "print(your_test_code_here)"
   ```

1. **Find the production code** that implements the incorrect behavior (usually in `CoreLib/*.cs`, `Execution/VM/*.cs`, etc.)

1. **Fix the production code** to match Lua's behavior

1. **Run the test suite** to verify the fix doesn't break other tests

1. **Update PLAN.md** to document the fix

### Exceptions: When `@novasharp-only: true` IS Appropriate

Only mark a fixture as NovaSharp-only when it tests:

- **Intentional NovaSharp extensions** (e.g., prime table syntax `${ }`, compatibility warning system)
- **Platform-dependent behavior** where both NovaSharp and Lua are "correct" but produce different output (e.g., `_VERSION` string, `os.date` with locale-specific formatters where the underlying C library differs)
- **Output format differences** where the semantic result is identical but string representation differs (e.g., number formatting edge cases like `-0` vs `0`)

If you're unsure whether something is an intentional extension or a bug, **assume it's a bug and fix it**.

**Verification Commands**:

```bash
# Test behavior against reference Lua
lua5.1 -e "print(string.byte('hello', 1))"
lua5.3 -e "print(string.byte('hello', 1))"
lua5.4 -e "print(string.byte('hello', 1))"

# Run comprehensive fixture comparison
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.3 -j 8 --output-dir artifacts/lua-comparison-5.3
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.3 --results-dir artifacts/lua-comparison-5.3
```

**When fixture comparisons reveal behavioral differences**:

- These are **production bugs in NovaSharp**, not test issues
- Track all findings in `PLAN.md` §8.38 (Lua Spec Compliance)
- Fix the production code, add regression fixtures, verify against real Lua

See `PLAN.md` §8.38 for the current list of known spec violations and their status.

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
- **Comments**: Use minimal comments - only comment if necessary or if the implementation is unclear

### Type System

- **Explicit types**: Prefer explicit types over `var`; only use `var` for anonymous types
- **No nullable syntax**: Do not use `#nullable`, `string?`, `?.` on reference types, or `null!`
- **Access modifiers**: Always explicit; keep `using` directives minimal

### Code Organization

- **No regions**: Never add `#region`/`#endregion` directives; delete any you encounter
- **Internals access**: Prefer `internal` + `InternalsVisibleTo` over reflection hacks
- **Reflection policy**: Review `docs/modernization/reflection-audit.md` before adding reflection
- **Regions**: Never use regions, in production code or test code

### Enum Conventions

- **Explicit values**: All enum members must have explicit integer values assigned
- **Default sentinel value**: Enums that do **not** require Lua interop must have a default obsolete sentinel value at position 0:
  - Use `[Obsolete("Use a specific <EnumName>.", false)]` attribute (warning, not error)
  - Name it `Unknown = 0` or `None = 0` (use `None` for `[Flags]` enums where "no flags" is a valid state)
- **Exceptions for Lua interop**: Enums like `DataType` where `Nil = 0` is a valid Lua type should **not** have an obsolete sentinel
- **Stable ordering**: Never reorder or renumber existing enum values—add new values at the end with the next available number
- **Flags enums**: Use powers of two (`1 << 0`, `1 << 1`, etc.) and include `None = 0` as a non-obsolete default when "no flags" is semantically valid
- **🔴 Flag enum combined values must be external**: For `[Flags]` enums, combined values produced by **any bitwise operation** (`|`, `&`, `^`, `~`) must **never** be enum members. Instead, define them as `static readonly` or `const` fields in a nearby helper class or extension class. Enum members should **only ever have one bit set** (with the sole exception of `None = 0`). This ensures serialization safety, clean `Enum.GetValues()` iteration, and semantic clarity.
- **InvalidEnumArgumentException for invalid enum values**: When switching on or validating enum values, throw `System.ComponentModel.InvalidEnumArgumentException` for unrecognized/invalid values instead of `ArgumentException`, `ArgumentOutOfRangeException`, or generic exceptions. This exception type is specifically designed for enum validation failures and provides clearer diagnostics:
  ```csharp
  // CORRECT: Use InvalidEnumArgumentException
  switch (dataType)
  {
      case DataType.Nil:
          // handle nil
          break;
      case DataType.Boolean:
          // handle boolean
          break;
      // ... other cases ...
      default:
          throw new InvalidEnumArgumentException(nameof(dataType), (int)dataType, typeof(DataType));
  }

  // WRONG: Using generic exceptions for enum validation
  default:
      throw new ArgumentException($"Unknown data type: {dataType}");  // Use InvalidEnumArgumentException instead
      throw new ArgumentOutOfRangeException(nameof(dataType));         // Use InvalidEnumArgumentException instead
      throw new NotSupportedException($"Unsupported: {dataType}");    // Use InvalidEnumArgumentException instead
  ```

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
- **Lua version**: All new tests should be explicit about any and all Lua versions that the test scenario is valid for. Tests should have both positive and negative cases for features/behavior that do not target all supported Lua versions

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

**CRITICAL**: When NovaSharp's behavior differs from the official Lua interpreter, this is a **production bug in NovaSharp** that must be fixed. Never adjust tests to accommodate bugs.

**The Golden Rule**: If `lua5.X` produces output A and NovaSharp produces output B for the same input, **NovaSharp is wrong and must be fixed** (unless it's platform-dependent behavior like locale-specific date formatting or `_VERSION` strings).

When a test fails or fixture comparison reveals incorrect behavior:

1. **Assume NovaSharp is wrong** until proven otherwise
1. **Verify against real Lua** — run the same code against `lua5.1`, `lua5.3`, `lua5.4`
1. **Fix the production code** to produce correct output
1. **Keep tests unchanged** unless they are demonstrably incorrect
1. **Document the fix** in commit message, PR notes, and `PLAN.md`

**Examples of bugs that MUST be fixed in production code**:

- NovaSharp throws an error where Lua returns `nil` → Fix NovaSharp to return `nil`
- NovaSharp returns a different value than Lua → Fix NovaSharp to return the correct value
- NovaSharp hangs/times out where Lua completes → Fix the infinite loop/performance bug
- NovaSharp crashes where Lua runs successfully → Fix the crash
- NovaSharp silently succeeds where Lua throws an error → Fix NovaSharp to throw

**Lua Fixture Comparison Findings**: When `compare-lua-outputs.py` reveals behavioral differences between NovaSharp and real Lua:

1. **These are production bugs** — NovaSharp is not matching the Lua spec
1. **Do NOT mark fixtures as `@novasharp-only: true`** unless the difference is an intentional NovaSharp extension
1. **Do NOT change `@expects-error`** to match NovaSharp's incorrect behavior
1. **Fix the production code** in `CoreLib/*.cs` or other runtime files
1. **Add regression test fixtures** that verify the fix matches real Lua
1. **Track findings in `PLAN.md` §8.38** (Lua Spec Compliance)

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

## Tool Use

- Prefer ripgrep (`rg`) over `grep` for all string and text searching/filtering (avoid using grep, use rg)

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
