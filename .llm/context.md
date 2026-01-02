# AI Assistant Guidelines for NovaSharp

## 🔴 Priority Hierarchy (NEVER Violate)

NovaSharp follows a strict priority order. **NEVER sacrifice a higher priority for a lower one:**

| Priority           | Concern                | Description                                   |
| ------------------ | ---------------------- | --------------------------------------------- |
| **1. CORRECTNESS** | Lua Spec Compliance    | Behavior MUST match reference Lua exactly     |
| **2. SPEED**       | Runtime Performance    | Execute Lua code as fast as possible          |
| **3. MEMORY**      | Minimal Allocations    | Zero-allocation hot paths, aggressive pooling |
| **4. UNITY**       | Platform Compatibility | IL2CPP/AOT, Mono, no runtime code generation  |
| **5. CLARITY**     | Maintainability        | Clean architecture, readability               |

**The Iron Rule**: A performance optimization that breaks Lua spec compliance is REJECTED. A memory optimization that slows down hot paths is REJECTED. See [correctness-then-performance](skills/correctness-then-performance.md) for the complete decision framework.

### ⛔ "Close Enough" is NEVER Acceptable

NovaSharp's goal is **exact Lua spec compliance**, not "something like Lua." When discrepancies are found:

1. **ASSUME NovaSharp is WRONG** — Reference Lua behavior is the source of truth
1. **Investigate deeply** — Never dismiss differences as "acceptable variations"
1. **Fix production code** — Never adjust tests to match NovaSharp's bugs
1. **Verify with `lua5.X`** — Every expected result must be confirmed against reference Lua
1. **"Close enough" = BUG** — Approximate behavior, formatting differences, or edge case mismatches are all bugs

______________________________________________________________________

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave version control to human
1. **NEVER use absolute paths** — Relative to repo root only
1. **NEVER discard output** — No `/dev/null` redirects
1. **Lua Spec Compliance is HIGHEST PRIORITY** — Fix production code, never tests
1. **Zero-Flaky Test Policy** — Every failure is a real bug
1. **Always create BOTH C# tests AND `.lua` fixtures** — Regenerate corpus after
1. **Multi-Version Testing** — Run tests against Lua 5.1-5.5
1. **Lua Fixture Metadata** — Use ONLY `@lua-versions`, `@novasharp-only`, `@expects-error` **at TOP of file**
1. **Pre-Commit Validation** — Run `./scripts/dev/pre-commit.sh` after changes

See individual skills for detailed guidance.

______________________________________________________________________

## Project Overview

NovaSharp is a multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4, 5.5) in C# for .NET, Unity3D (IL2CPP/AOT), Mono, and Xamarin.

**Key namespaces**: `Execution` (VM), `Tree` (AST), `DataTypes` (DynValue, Table), `Interop` (C# bridge), `CoreLib` (stdlib)

**Pipeline**: Lua Source -> Lexer -> Parser -> AST -> Compiler -> Bytecode -> VM -> Execution

______________________________________________________________________

## Where to Find Things

### Quick Navigation by Task

| I want to...                    | Look in                                             |
| ------------------------------- | --------------------------------------------------- |
| Fix Lua behavior bug            | `src/runtime/.../CoreLib/` (stdlib modules)         |
| Fix parser/lexer issue          | `src/runtime/.../Tree/Lexer/` or `.../Tree/Parser/` |
| Fix VM execution bug            | `src/runtime/.../Execution/VM/`                     |
| Add/modify a data type          | `src/runtime/.../DataTypes/`                        |
| Fix C# interop issue            | `src/runtime/.../Interop/`                          |
| Add a new test                  | `src/tests/.../Tests.TUnit/`                        |
| Add a Lua test fixture          | `src/tests/.../Tests/Fixtures/`                     |
| Check Lua spec                  | `docs/lua-spec/lua5X-manual.md`                     |
| Find pooling utilities          | `src/runtime/.../DataStructs/CollectionPools.cs`    |
| Find string building utilities  | `src/runtime/.../DataStructs/ZStringBuilder.cs`     |
| Check/add performance patterns  | `.llm/skills/high-performance-csharp.md`            |
| Investigate comparison failures | `.llm/skills/lua-comparison-harness.md`             |
| Debug cross-platform issues     | `.llm/skills/test-failure-investigation.md`         |

### Key Files

- `DataTypes/DynValue.cs` — Core Lua value type
- `DataTypes/Table.cs` — Lua table implementation
- `Execution/VM/Processor.cs` — Main VM execution loop
- `CoreLib/*Module.cs` — Standard library implementations
- `DataStructs/CollectionPools.cs` — ListPool, HashSetPool, etc.

______________________________________________________________________

## Commands

```bash
# Build (interpreter only)
./scripts/build/quick.sh

# Build full solution
./scripts/build/quick.sh --all

# Run all tests
./scripts/test/quick.sh

# Run tests by pattern
./scripts/test/quick.sh Floor           # Method name
./scripts/test/quick.sh -c MathModule   # Class name

# Format code
dotnet csharpier format .
```

______________________________________________________________________

## Coding Style

### Essentials

- **`using` directives INSIDE namespace** — Never at file top
- **Explicit types always** — Never use `var`
- **NEVER nullable reference types** — No `#nullable`, `string?`, `?.`, `??`
- **PascalCase** for types/methods, **`_camelCase`** for private fields
- **NO underscores in method names** — `FeatureWorksCorrectly` not `Feature_Works_Correctly`

### Performance (Hot Paths)

- **Use pooled collections** — `ListPool<T>.Get()`, `HashSetPool<T>.Get()` with `using`
- **Use `ZStringBuilder.Create()`** — Never `StringBuilder` or `$"..."` in hot paths
- **NEVER `.ToString()` on enums** — Use `TokenTypeStrings.GetName()` etc.
- **Avoid closures/LINQ** — Use explicit loops in hot paths
- **Use `HashCodeHelper.HashCode()`** — Never bespoke hash patterns

See [high-performance-csharp](skills/high-performance-csharp.md) for detailed patterns.

### Shell Commands

| Tool  | Replaces | Critical Flag                   |
| ----- | -------- | ------------------------------- |
| `rg`  | grep     | -                               |
| `fd`  | find     | `--max-results N`               |
| `bat` | cat      | **`--paging=never`** (CRITICAL) |

______________________________________________________________________

## Commit Style

- Concise, imperative: "Fix parser regression"
- Reference issues: "Fixes #123"

______________________________________________________________________

## Skills Index

Skills are in `.llm/skills/`. Run `python3 tools/LlmSkillIndexer/llm_skill_indexer.py` to see all available skills.

### Core Skills

| Category        | Key Skills                                                                                                                                                                     |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Priority**    | [correctness-then-performance](skills/correctness-then-performance.md), [lua-spec-verification](skills/lua-spec-verification.md)                                               |
| **Performance** | [high-performance-csharp](skills/high-performance-csharp.md), [zstring-migration](skills/zstring-migration.md), [data-structures](skills/data-structures.md)                   |
| **Testing**     | [tunit-test-writing](skills/tunit-test-writing.md), [lua-fixture-creation](skills/lua-fixture-creation.md), [test-failure-investigation](skills/test-failure-investigation.md) |
| **Unity**       | [unity-gc-patterns](skills/unity-gc-patterns.md), [aggressive-inlining](skills/aggressive-inlining.md)                                                                         |
| **Lua**         | [lua-comparison-harness](skills/lua-comparison-harness.md), [adding-opcodes](skills/adding-opcodes.md)                                                                         |
| **Quality**     | [defensive-programming](skills/defensive-programming.md), [documentation-and-changelog](skills/documentation-and-changelog.md)                                                 |

______________________________________________________________________

## Key Utilities

| Utility          | Usage                                     |
| ---------------- | ----------------------------------------- |
| **Pooling**      | `using var lease = ListPool<T>.Get();`    |
| **ZString**      | `using var sb = ZStringBuilder.Create();` |
| **Enum strings** | `TokenTypeStrings.GetName(token)`         |
| **Hash codes**   | `HashCodeHelper.HashCode(a, b, c)`        |

See [use-extension-methods](skills/use-extension-methods.md) for full list.

______________________________________________________________________

## Resources

- [docs/lua-spec/](../docs/lua-spec/) — Local Lua specs
- [docs/Testing.md](../docs/Testing.md) — Testing details
- [PLAN.md](../PLAN.md) — Current plan
