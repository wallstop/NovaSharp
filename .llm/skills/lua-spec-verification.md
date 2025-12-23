# Skill: Verifying Lua Spec Compliance

**When to use**: Investigating whether NovaSharp behavior matches reference Lua.

**Related Skills**: [lua-fixture-creation](lua-fixture-creation.md) (creating test fixtures), [lua-comparison-harness](lua-comparison-harness.md) (running fixtures)

______________________________________________________________________

## Core Principle

**ASSUME NOVASHARP IS WRONG** when behavior differs from reference Lua.

NovaSharp must match official Lua behavior. When behavior differs:

1. Verify against `lua5.1`, `lua5.2`, `lua5.3`, `lua5.4`, `lua5.5`
1. **FIX PRODUCTION CODE** — never adjust tests to match buggy behavior
1. Create standalone `.lua` fixtures runnable against real Lua
1. Update `PLAN.md` §8.38 to document fixes

______________________________________________________________________

## Quick Verification Commands

### Test against reference Lua directly

```bash
# Quick one-liner test
lua5.4 -e "print(your_test_code)"

# Run a fixture file
lua5.4 path/to/fixture.lua
lua5.1 path/to/fixture.lua
```

### Run comparison harness

See [lua-comparison-harness](lua-comparison-harness.md) for full harness usage.

______________________________________________________________________

## What You Should NEVER Do

| ❌ Never                                    | Why                                                                 |
| ------------------------------------------- | ------------------------------------------------------------------- |
| Mark fixtures `@novasharp-only: true`       | Unless testing intentional NovaSharp extensions (CLR interop, `!=`) |
| Change `@expects-error` to match NovaSharp  | You're hiding bugs, not fixing them                                 |
| Skip, disable, or weaken tests              | Tests document expected behavior                                    |
| Adjust test expectations to match NovaSharp | Fix the interpreter instead                                         |

______________________________________________________________________

## Investigation Workflow

### 1. Reproduce with reference Lua

```bash
# Test the behavior in question
lua5.4 -e "print(math.floor(-0.5))"
lua5.1 -e "print(math.floor(-0.5))"
```

### 2. Compare with NovaSharp

```bash
# Run same code in NovaSharp CLI
dotnet run --project src/tooling/NovaSharp.Cli -e "print(math.floor(-0.5))"
```

### 3. Check Lua spec documentation

Local specs are in `docs/lua-spec/`:

- `lua51-manual.md`
- `lua52-manual.md`
- `lua53-manual.md`
- `lua54-manual.md`
- `lua55-manual.md`

### 4. Create a fixture to document expected behavior

See [lua-fixture-creation](lua-fixture-creation.md) for complete fixture template and metadata requirements.

### 5. Fix NovaSharp production code

Locate the relevant implementation and fix it to match reference Lua.

### 6. Document in PLAN.md

Add entry to §8.38 (Lua Spec Compliance Fixes).

______________________________________________________________________

## Version-Specific Behavior

Some behaviors legitimately differ between Lua versions:

| Feature          | 5.1 | 5.2 | 5.3+ |
| ---------------- | --- | --- | ---- |
| Integer subtype  | No  | No  | Yes  |
| `//` operator    | No  | No  | Yes  |
| `math.type()`    | No  | No  | Yes  |
| `utf8` library   | No  | No  | Yes  |
| `goto` statement | No  | Yes | Yes  |

When behavior differs by version:

1. Create version-specific fixtures (`_51.lua`, `_53plus.lua`)
1. Test BOTH positive and negative scenarios
1. Document in `@compat-notes` metadata

______________________________________________________________________

## Resources

- [docs/lua-spec/](../../docs/lua-spec/) — Local Lua reference manuals
- [docs/LuaCompatibility.md](../../docs/LuaCompatibility.md) — Version compatibility matrix
- [PLAN.md §8.38](../../PLAN.md) — Compliance fix tracking
