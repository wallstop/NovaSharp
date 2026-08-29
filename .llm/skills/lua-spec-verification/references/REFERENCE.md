# Verifying Lua Spec Compliance Reference

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

See [lua-fixture-creation](../../lua-fixture-creation/SKILL.md) for complete fixture template and metadata requirements.

### 5. Fix NovaSharp production code

Locate the relevant implementation and fix it to match reference Lua.

For stateful standard-library behavior, probe more than the happy path: verify
the initial state, every control transition, unknown controls, invalid arguments
before and after transitions, output formatting and destination, and isolation
between separate Lua states. A function-presence check or one successful call
cannot establish semantic parity.

### 6. Record the result

Put the investigation, fix, and verification receipt in the current
`progress/session-NNN-*.md`. Keep only unresolved, selected follow-up in
`PLAN.md`; route significant version behavior to `docs/LuaCompatibility.md`.

______________________________________________________________________

## When Lua Versions Differ

NovaSharp is a **MULTI-VERSION interpreter**. When Lua versions behave differently, NovaSharp must:

### Match Each Version Exactly

```csharp
// Example: math.log takes 1 arg in 5.1, 1-2 args in 5.2+
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
public async Task MathLogWithBase(LuaCompatibilityVersion v) { /* ... */ }

[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
public async Task MathLogSingleArg(LuaCompatibilityVersion v) { /* ... */ }
```

### Investigation Checklist for Version Differences

1. Test code against **ALL Lua versions** (5.1, 5.2, 5.3, 5.4, 5.5)
1. Document which versions have which behavior
1. Create **SEPARATE test cases** for each behavior variant
1. Ensure NovaSharp matches **EACH version** when running in that mode
1. Add entry to `docs/LuaCompatibility.md` for significant differences

### 🔴 NEVER Do These

- Pick one version's behavior and apply to all
- "Average" or interpolate between versions
- Choose "the most sensible" behavior over spec compliance
- Ignore older version differences
- Assume Lua 5.4 behavior is "correct" for all versions

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
1. Document the version-specific behavior in a plain fixture comment or nearby test documentation; do not invent extra fixture metadata keys

______________________________________________________________________

## Resources

- [docs/lua-spec/](../../../../docs/lua-spec/) — Local Lua reference manuals
- [docs/LuaCompatibility.md](../../../../docs/LuaCompatibility.md) — Version compatibility matrix
- [plan-maintenance](../../plan-maintenance/SKILL.md) — Routing active work and completed evidence
