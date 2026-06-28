______________________________________________________________________

triggers:

- "test failure"
- "failing test"
- "flaky test"
- "test investigation"
- "root cause"
- "test debugging"
  category: testing
  related:
- codebase-navigation
- lua-spec-verification
- tunit-test-writing
- lua-comparison-harness
  priority: core

______________________________________________________________________

# Skill: Test Failure Investigation

**When to use**: Any test failure occurs — whether intermittent, unexpected, or seemingly unrelated to current changes.

**Related Skills**: [codebase-navigation](codebase-navigation.md) (pipeline debugging), [lua-spec-verification](lua-spec-verification.md) (comparing with reference Lua), [tunit-test-writing](tunit-test-writing.md) (test writing)

______________________________________________________________________

## 🔴 Zero-Flaky Test Policy

**NovaSharp maintains a strict zero-flaky test policy.** Every test failure indicates a real bug that must be comprehensively investigated and fixed.

### Core Principles

1. **Every failure is meaningful** — Test failures always indicate either a production bug OR a test bug
1. **Never "make tests pass"** — Understand the root cause before making any changes
1. **Never ignore failing tests** — Do not skip, disable, weaken, or mark tests as flaky
1. **Fix the bug, not the symptom** — Comprehensive fixes prevent regressions

### What "Flaky" Really Means

When a test appears flaky, it means one of:

| Apparent Symptom      | Actual Cause                      | Required Action                           |
| --------------------- | --------------------------------- | ----------------------------------------- |
| Random failures       | Race condition in production code | Fix thread safety in interpreter          |
| Intermittent failures | Non-deterministic test setup      | Fix test isolation properly               |
| Environment-dependent | Platform-specific bug             | Fix production code for all platforms     |
| Order-dependent       | Test pollution or shared state    | Fix test isolation with proper attributes |
| Timing-dependent      | Concurrency bug                   | Fix synchronization in production code    |

______________________________________________________________________

## Investigation Workflow

### Step 1: Reproduce Reliably

Run the test multiple times: `./scripts/test/quick.sh FailingTestName`. For intermittent failures, run in a loop.

### Step 2: Understand the Test's Intent

Read the test. What behavior is it verifying? What Lua version(s)? Is there a `.lua` fixture? Check Lua spec: `rg "function_name" docs/lua-spec/`

### Step 3: Determine Failure Category

| Category               | Symptoms                                                              | Investigation Path                          |
| ---------------------- | --------------------------------------------------------------------- | ------------------------------------------- |
| **Production Bug**     | Test expects correct Lua behavior but NovaSharp produces wrong result | Debug interpreter pipeline                  |
| **Test Bug**           | Test expectation is incorrect vs Lua spec                             | Verify against reference Lua, then fix test |
| **Test Isolation Bug** | Test passes alone, fails with others                                  | Check for missing isolation attributes      |
| **Race Condition**     | Intermittent, timing-dependent                                        | Add tracing, review thread safety           |

### Step 4: Production Bug Investigation

Verify behavior with reference Lua: `lua5.4 -e "print(...)"`. Compare with NovaSharp. Then follow [codebase-navigation](codebase-navigation.md).

### Step 5: Test Bug Investigation

**🔴 PRESUME NOVASHARP IS WRONG**: First verify with reference Lua. If Lua matches test expectation, NovaSharp has a BUG. Only consider a test bug if ALL applicable Lua versions differ from test expectation.

### Step 6: Test Isolation Investigation

If tests pass individually but fail together:

```csharp
// Check for missing isolation attributes
[Test]
[UserDataIsolation]            // Missing this?
[ScriptGlobalOptionsIsolation] // Missing this?
[PlatformDetectorIsolation]    // Missing this?
public async Task TheFailingTest(...)
```

Common isolation issues:

- Global `Script.GlobalOptions` modifications
- `UserData` type registrations persisting
- Static state in production code
- Console output capture conflicts

### Step 7: Race Condition Investigation

If failures are timing-dependent:

1. Add logging to suspect areas
1. Review any `async`/`await` patterns
1. Check for shared mutable state
1. Review any timer or delay usage
1. Check for proper `ConfigureAwait(false)` usage

______________________________________________________________________

## 🚫 What You Should NEVER Do

| ❌ NEVER                                     | Why                                         |
| -------------------------------------------- | ------------------------------------------- |
| Add `[Skip]` or `[Ignore]` attributes        | Hides bugs, creates technical debt          |
| Mark tests as "flaky"                        | There are no flaky tests, only unfound bugs |
| Weaken assertions                            | Changes expected behavior silently          |
| Add retry logic to tests                     | Masks intermittent production bugs          |
| Adjust expected values without investigation | May accept incorrect behavior               |
| Comment out failing assertions               | Silent correctness regression               |
| Add arbitrary delays/sleeps                  | Masks timing bugs, slows CI                 |
| Delete failing tests                         | Loses coverage of important behavior        |

______________________________________________________________________

## Comprehensive Fix Requirements

When you find the root cause, the fix must be **comprehensive**:

### For Production Bugs

1. Fix the root cause in production code
1. Add regression test(s) covering the fix
1. Create `.lua` fixture verifying behavior against reference Lua
1. Regenerate corpus: `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`
1. Verify fix across ALL applicable Lua versions
1. Document in `PLAN.md` if it's a spec compliance fix

### For Test Bugs

1. Verify correct behavior against reference Lua
1. Fix test expectation to match Lua spec
1. Update corresponding `.lua` fixture if exists
1. Ensure test runs on all applicable versions
1. Add version-specific tests if behavior differs by version

### For Isolation Bugs

1. Add missing isolation attributes
1. Review similar tests for same issue
1. Consider if production code needs thread-safety improvements
1. Document any new isolation patterns discovered

______________________________________________________________________

## 🔴 Cross-Platform Comparison Harness Failures

The Lua comparison harness runs fixtures against reference Lua on 3 platforms (macOS, Windows, Ubuntu) × 5 versions (5.1-5.5) in a CI lane that depends on `lint`, not the unit-test OS matrix. Treat comparison failures as independent Lua-spec signals even if a unit-test job also failed.

### Step 1: Categorize the Failure Pattern

| Pattern                               | Diagnosis                                             |
| ------------------------------------- | ----------------------------------------------------- |
| Fails on ALL platforms + ALL versions | Likely NovaSharp bug                                  |
| Fails on ONE platform only            | Platform-specific C library difference                |
| Fails on ONE version only             | Version-specific behavior (check `@lua-versions`)     |
| Fails on Windows only                 | Windows C library (MSVCRT) quirk                      |
| NovaSharp output is consistent        | Platform Lua varies; possibly `@novasharp-only: true` |

### Step 2: Check for Metadata Location Issues

**The #1 cause of unexpected failures**: Metadata placed incorrectly.

```bash
# Check if metadata is at the TOP of the file (no blank lines before it)
head -1 path/to/fixture.lua
# Should show: -- @lua-versions: ...

# If it shows a blank line or code, metadata is being SILENTLY IGNORED!
```

The harness parser **STOPS at the first non-comment line**. Metadata after a blank line is never read.

### Step 3: Check for Platform-Specific C Library Differences

Common platform differences that are NOT NovaSharp bugs:

| Function                          | Issue                                                          | Solution                |
| --------------------------------- | -------------------------------------------------------------- | ----------------------- |
| `os.date()` with POSIX specifiers | Windows doesn't support %C, %D, %F, %R, %T, %V, %u, %e, %n, %t | `@novasharp-only: true` |
| `tostring(0/0)`                   | Windows outputs `-nan(ind)`, others output `nan`               | Harness normalizes this |
| `math.pow`, `math.log10`, etc.    | Windows Lua may be built without compat flags                  | `@novasharp-only: true` |
| `__le` metamethod fallback        | Lua 5.4 Windows build may lack this                            | `@novasharp-only: true` |

### Step 4: Document the Reason

When marking tests as `@novasharp-only: true`, the fixture must document the
NovaSharp extension or platform C-library/spec implementation-defined behavior
that makes comparison against the local reference Lua inappropriate. Use a
plain comment next to the metadata; do not invent extra fixture metadata keys.

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- Windows strftime does not support %C; NovaSharp is POSIX-compliant on all platforms.

print(os.date("%C"))
```

### Step 5: Distinguish NovaSharp Bugs from Platform Quirks

**Key insight**: NovaSharp uses pure C# implementations that are often MORE correct (POSIX-compliant) than platform-specific reference Lua.

| Scenario                                        | Action                                                |
| ----------------------------------------------- | ----------------------------------------------------- |
| NovaSharp is MORE correct than platform Lua     | `@novasharp-only: true` with a plain explanatory note |
| NovaSharp differs from ALL platforms + versions | **Fix NovaSharp** - this is a real bug                |
| NovaSharp matches some platforms but not others | Check if NovaSharp matches the POSIX standard         |
| Reference Lua varies between platforms          | NovaSharp should be consistently correct              |

______________________________________________________________________

## Escalation Path

If you cannot determine root cause after thorough investigation:

1. **Document everything** — What you tried, what you observed
1. **Create minimal reproduction** — Smallest possible test case
1. **Check similar tests** — Are related tests also problematic?
1. **Review recent changes** — Did something in the area change recently?
1. **Ask for help** — With full documentation of investigation

Never leave a failing test unresolved. If investigation is incomplete, document current state and continue investigating.

______________________________________________________________________

## Common Root Causes Reference

| Symptom                       | Likely Cause                                       | Where to Look                    |
| ----------------------------- | -------------------------------------------------- | -------------------------------- |
| Wrong numeric result          | `LuaNumber` integer/float handling                 | `DataTypes/LuaNumber.cs`         |
| String comparison failure     | Encoding or escape sequences                       | `CoreLib/StringModule.cs`        |
| Table iteration order         | Table implementation                               | `DataTypes/Table.cs`             |
| Function call failure         | Argument marshalling                               | `Interop/`                       |
| Version-specific failure      | Feature flag or version check                      | `LuaCompatibilityVersion` usages |
| Memory/GC issues              | Pooling or closure captures                        | Resource management code         |
| Fixture skipped/wrong version | **Metadata location** (blank line before metadata) | Check first line of .lua file    |
| Platform-specific failure     | C library differences                              | See lua-comparison-harness skill |
| Windows-only failures         | MSVCRT strftime/NaN differences                    | Mark `@novasharp-only: true`     |

______________________________________________________________________

## Resources

- [codebase-navigation](codebase-navigation.md) — Pipeline debugging
- [lua-spec-verification](lua-spec-verification.md) — Verifying against reference Lua
- [tunit-test-writing](tunit-test-writing.md) — Test patterns and isolation
- [docs/Testing.md](../../docs/Testing.md) — Testing documentation
