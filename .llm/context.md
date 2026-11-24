# NovaSharp repository guide

## Project Overview

NovaSharp is a multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4, 5.5) in C# for .NET, Unity3D (IL2CPP/AOT), Mono, and Xamarin.

**Key namespaces**: `NovaSharp`/`Api` (`LuaValue` public value), `Execution` (VM), `Tree` (AST), `DataTypes` (`Table` and runtime payloads), `Interop` (C# bridge), `CoreLib` (stdlib)

**Pipeline**: Lua Source -> Lexer -> Parser -> AST -> Compiler -> Bytecode -> VM -> Execution

______________________________________________________________________

## Where to Find Things

Runtime paths below are relative to `src/runtime/WallstopStudios.NovaSharp.Interpreter/`.

| Task                           | Start here                                                                                                                                                               |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Investigate Lua behavior       | `CoreLib/`, then the matching [Lua manual](../docs/lua-spec/)                                                                                                            |
| Trace parsing                  | `Tree/Lexer/`, `Tree/Expressions/`, `Tree/Statements/`                                                                                                                   |
| Trace execution                | `Execution/VM/Processor/`                                                                                                                                                |
| Inspect host interop or values | `Interop/`, `Api/LuaValue.cs`, `DataTypes/Table.cs`                                                                                                                      |
| Find pools and string builders | `DataStructs/CollectionPools.cs`, `DataStructs/ZStringBuilder.cs`                                                                                                        |
| Write a regression             | [TUnit tests](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/) and [Lua fixtures](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/) |

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
- **VM opcode and Lua-call paths must be allocation-free** — Use inline `LuaValue` scalars, stack windows, spans, and explicit slow-path allowlists; avoid non-allowlisted `LuaValue[]`, `List<LuaValue>`, and callback context materialization
- **NEVER `.ToString()` on enums** — Use `TokenTypeStrings.GetName()` etc.
- **Avoid closures/LINQ** — Use explicit loops in hot paths
- **Use `HashCodeHelper.HashCode()`** — Never bespoke hash patterns

See [high-performance-csharp](skills/high-performance-csharp/SKILL.md) for detailed patterns.

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

## Skill discovery

Use your client’s skill selector or list the canonical names from the repository root:

```bash
jq -r '.skills[].name' .llm/skills-index.json
```

Open a matching skill for its procedure and consult its references as needed.
The [authoring guide](skills/adding-skills/SKILL.md) covers changes to the catalog.

## Resources

- [docs/lua-spec/](../docs/lua-spec/) — Local Lua specs
- [docs/Testing.md](../docs/Testing.md) — Testing details
- [PLAN.md](../PLAN.md) — Lean execution queue for current and intentionally queued work
