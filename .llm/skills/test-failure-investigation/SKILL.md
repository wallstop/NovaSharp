---
name: test-failure-investigation
description: "Investigate every NovaSharp test failure to root cause under the zero-flaky policy. Use for failing, intermittent, unrelated-looking, platform-specific, or CI-only tests."
metadata:
  category: testing
  priority: core
  related: codebase-navigation, lua-spec-verification, tunit-test-writing, lua-comparison-harness
---
# Skill: Test Failure Investigation

**Related Skills**: [codebase-navigation](../codebase-navigation/SKILL.md) (pipeline debugging), [lua-spec-verification](../lua-spec-verification/SKILL.md) (comparing with reference Lua), [tunit-test-writing](../tunit-test-writing/SKILL.md) (test writing)

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

Verify behavior with reference Lua: `lua5.4 -e "print(...)"`. Compare with NovaSharp. Then follow [codebase-navigation](../codebase-navigation/SKILL.md).

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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Comprehensive Fix Requirements, Cross-Platform Comparison Harness Failures, Escalation Path, Common Root Causes Reference, and later sections.
