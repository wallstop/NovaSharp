______________________________________________________________________

triggers:

- "Lua spec"
- "reference Lua"
- "spec compliance"
- "behavior verification"
- "lua5.4"
- "lua5.1"
  category: lua
  related:
- lua-fixture-creation
- lua-comparison-harness
- test-failure-investigation
  priority: core

______________________________________________________________________

# Skill: Verifying Lua Spec Compliance

**When to use**: Investigating whether NovaSharp behavior matches reference Lua.

**Related Skills**: [lua-fixture-creation](lua-fixture-creation.md) (creating test fixtures), [lua-comparison-harness](lua-comparison-harness.md) (running fixtures), [test-failure-investigation](test-failure-investigation.md) (investigating test failures)

______________________________________________________________________

## 🔴 Core Principle: Reference Lua is the Source of Truth

**ASSUME NOVASHARP IS WRONG** when behavior differs from reference Lua.

The output from `lua5.X` defines expected behavior. NovaSharp must match it **exactly** — not "close enough", not "practically the same", not "within tolerance."

______________________________________________________________________

## ⛔ "Close Enough" is NEVER Acceptable

NovaSharp must match reference Lua **EXACTLY**. Approximate behavior is a bug.

### What "Exact Match" Means

| Aspect                 | Requirement                                                             |
| ---------------------- | ----------------------------------------------------------------------- |
| **Output strings**     | Byte-for-byte identical (including whitespace, newlines)                |
| **Numeric results**    | Bit-for-bit identical (same IEEE 754 representation)                    |
| **Numeric formatting** | Character-for-character identical (trailing zeros, scientific notation) |
| **Error types**        | Same error category; message FORMAT may differ                          |
| **Side effects**       | Same order, same targets, same values                                   |
| **Return value count** | Exactly the same number of return values                                |
| **Table iteration**    | Implementation-defined but consistent with Lua spec                     |

### Examples of UNACCEPTABLE "Close Enough"

```lua
-- Reference Lua: print(0.1 + 0.2) → "0.30000000000000004"
-- NovaSharp:     print(0.1 + 0.2) → "0.3"
-- ❌ UNACCEPTABLE — display format differs, FIX NOVASHARP

-- Reference Lua: math.floor(-0.5) → -1.0
-- NovaSharp:     math.floor(-0.5) → -1
-- ❌ UNACCEPTABLE if Lua returns float subtype and we return integer

-- Reference Lua: tostring(1/0) → "inf"
-- NovaSharp:     tostring(1/0) → "Infinity"  
-- ❌ UNACCEPTABLE — string representation differs, FIX NOVASHARP

-- Reference Lua: #t → 3
-- NovaSharp:     #t → 3 (but different internal representation)
-- ✅ ACCEPTABLE — observable behavior matches exactly
```

______________________________________________________________________

## 🔴 Platform-Specific Behavior

NovaSharp MUST produce Lua-spec-compliant behavior on **ALL platforms** (Windows, macOS, Linux). Platform differences in NovaSharp output indicate bugs unless reference Lua also differs.

### Known Platform Variations in Lua

| Area                         | Behavior       | NovaSharp Requirement                          |
| ---------------------------- | -------------- | ---------------------------------------------- |
| `os.execute()` return values | Vary by OS     | Match reference Lua **on the same OS**         |
| `io.popen()` availability    | May not exist  | Match reference Lua availability               |
| Path separators in errors    | `/` vs `\`     | Match reference Lua **on the same OS**         |
| Newline handling             | `\n` vs `\r\n` | Match Lua's output **exactly** per platform    |
| Locale-sensitive functions   | Vary by locale | Match reference Lua **in the same locale**     |
| File system case sensitivity | Varies         | Match OS behavior (NovaSharp doesn't abstract) |

### Investigation Steps for Platform Failures

1. **Run the SAME test on reference Lua on the SAME platform**
1. If reference Lua behavior varies by platform, NovaSharp must match **on each platform**
1. Document platform-specific behavior in tests with appropriate attributes
1. **NEVER accept "works on my machine" as resolution**

### When Platforms Differ and Lua is Consistent

If NovaSharp produces different results on different platforms but reference Lua produces **consistent** results across platforms:

- NovaSharp has a **BUG** on the platform(s) that differ
- Fix NovaSharp to match Lua's cross-platform behavior

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
1. Document in `@compat-notes` metadata

______________________________________________________________________

## Resources

- [docs/lua-spec/](../../docs/lua-spec/) — Local Lua reference manuals
- [docs/LuaCompatibility.md](../../docs/LuaCompatibility.md) — Version compatibility matrix
- [PLAN.md §8.38](../../PLAN.md) — Compliance fix tracking
