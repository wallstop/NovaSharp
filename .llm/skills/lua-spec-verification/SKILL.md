---
name: lua-spec-verification
description: "Determine exact reference Lua behavior and compare NovaSharp against Lua 5.1 through 5.5. Use for spec compliance, semantic ambiguity, version differences, or behavior investigations."
metadata:
  category: lua
  priority: core
  related: lua-fixture-creation, lua-comparison-harness, test-failure-investigation
---
# Skill: Verifying Lua Spec Compliance

**Related Skills**: [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (creating test fixtures), [lua-comparison-harness](../lua-comparison-harness/SKILL.md) (running fixtures), [test-failure-investigation](../test-failure-investigation/SKILL.md) (investigating test failures)

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
1. Record completed evidence in the session progress file; update `PLAN.md` only
   when actionable follow-up remains

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

See [lua-comparison-harness](../lua-comparison-harness/SKILL.md) for full harness usage.

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for What You Should NEVER Do, Investigation Workflow, When Lua Versions Differ, Version-Specific Behavior, and later sections.
