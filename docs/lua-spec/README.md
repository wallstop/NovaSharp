# Lua Specification Reference

This directory contains comprehensive Lua language specifications for all versions supported by NovaSharp. These documents are intended for AI assistants and developers to reference when implementing, auditing, or testing Lua compatibility.

## Specification Documents

| Version     | File                                 | Status           | Notes                                                     |
| ----------- | ------------------------------------ | ---------------- | --------------------------------------------------------- |
| **Lua 5.1** | [`lua-5.1-spec.md`](lua-5.1-spec.md) | Released 2006    | Foundation version, widely deployed                       |
| **Lua 5.2** | [`lua-5.2-spec.md`](lua-5.2-spec.md) | Released 2011    | `_ENV`, `goto`, `bit32` library                           |
| **Lua 5.3** | [`lua-5.3-spec.md`](lua-5.3-spec.md) | Released 2015    | Integers, native bitwise operators, `utf8` library        |
| **Lua 5.4** | [`lua-5.4-spec.md`](lua-5.4-spec.md) | Released 2020    | **Primary target**, `<const>`, `<close>`, generational GC |
| **Lua 5.5** | [`lua-5.5-spec.md`](lua-5.5-spec.md) | Work in Progress | `global` keyword, compact arrays, read-only for-loop vars |

## Version Highlights

### Lua 5.1 (Baseline)

- 8 basic types: nil, boolean, number, string, table, function, userdata, thread
- `setfenv`/`getfenv` for function environments
- Global `module()` system
- Global `unpack()` function
- No bitwise operators (use third-party libraries)

### Lua 5.2 (Environment Overhaul)

- `_ENV` replaces `setfenv`/`getfenv`
- `bit32` library for bitwise operations
- `goto` statement and labels
- `rawlen()` function
- `table.pack()`/`table.unpack()` (moved from global)
- Ephemeron tables (weak tables with weak keys)

### Lua 5.3 (Integer Support)

- Dual numeric types: 64-bit integers + 64-bit floats
- `math.type(x)` returns `"integer"` or `"float"`
- `math.maxinteger`, `math.mininteger`, `math.tointeger()`, `math.ult()`
- Native bitwise operators: `&`, `|`, `~`, `<<`, `>>`, `~` (unary NOT)
- Floor division operator: `//`
- `utf8` library
- `string.pack()`/`string.unpack()`/`string.packsize()`
- `bit32` library deprecated

### Lua 5.4 (Resource Management)

- To-be-closed variables: `local x <close> = resource`
- Const variables: `local x <const> = value`
- `__close` metamethod for cleanup
- `coroutine.close()` function
- `warn()` function
- Generational garbage collector
- xoshiro256\*\* random number generator
- `__lt` no longer emulates `__le`
- String-to-number coercion requires metamethods
- `print()` uses `__tostring` directly (not global `tostring`)
- UTF-8 `lax` mode for surrogates

### Lua 5.5 (Upcoming)

- `global` keyword for explicit global declarations
- For-loop control variables are read-only
- `table.create(size, value)` for pre-allocation
- Compact arrays (60% memory reduction)
- Incremental major GC mode

## Usage Guidelines

### For AI Assistants

When working on Lua compatibility tasks:

1. **Read the relevant spec first** before implementing or modifying behavior
1. **Cite specific sections** when documenting changes (e.g., "per §6.4.1")
1. **Check version differences** when behavior varies across versions
1. **Use for test verification** to ensure expected behavior matches spec

### For Developers

1. **Standard library implementation**: Reference exact function signatures and behaviors
1. **Metamethod handling**: Verify metamethod semantics match spec
1. **Error messages**: Match Lua's error message formats where practical
1. **Edge cases**: Specs document behavior for nil, NaN, infinity, etc.

## Key Differences Summary

| Feature             | 5.1                 | 5.2             | 5.3              | 5.4              |
| ------------------- | ------------------- | --------------- | ---------------- | ---------------- |
| Environment         | `setfenv`/`getfenv` | `_ENV`          | `_ENV`           | `_ENV`           |
| Bitwise ops         | None                | `bit32` library | Native operators | Native operators |
| Integers            | Float only          | Float only      | Native 64-bit    | Native 64-bit    |
| Global `unpack`     | Yes                 | No              | No               | No               |
| `goto` statement    | No                  | Yes             | Yes              | Yes              |
| UTF-8 library       | No                  | No              | Yes              | Yes              |
| `<close>` variables | No                  | No              | No               | Yes              |
| Generational GC     | No                  | No              | No               | Yes              |

## Related Documentation

- [PLAN.md](../../PLAN.md) — Implementation roadmap with Lua parity tracking
- [docs/LuaCompatibility.md](../LuaCompatibility.md) — NovaSharp compatibility notes
- [docs/testing/lua-divergences.md](../testing/lua-divergences.md) — Known divergences from reference Lua
- [docs/testing/spec-audit.md](../testing/spec-audit.md) — Feature-by-feature spec compliance audit

## Sources

These specifications are derived from:

- Official Lua manuals at https://www.lua.org/manual/
- Lua source code and test suites
- Lua mailing list announcements for upcoming features
