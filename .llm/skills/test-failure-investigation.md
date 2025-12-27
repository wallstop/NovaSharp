# Skill: Test Failure Investigation

**When to use**: Any test failure occurs — whether intermittent, unexpected, or seemingly unrelated to current changes.

**Related Skills**: [debugging-interpreter](debugging-interpreter.md) (pipeline debugging), [lua-spec-verification](lua-spec-verification.md) (comparing with reference Lua), [tunit-test-writing](tunit-test-writing.md) (test writing)

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

Before investigating, ensure you can reproduce the failure:

```bash
# Run the specific failing test multiple times
./scripts/test/quick.sh FailingTestName
./scripts/test/quick.sh FailingTestName
./scripts/test/quick.sh FailingTestName

# Run with verbose output
./scripts/test/quick.sh --no-build FailingTestName 2>&1 | tee failure.log
```

If the failure is intermittent:

```bash
# Run in a loop to catch intermittent failures
for i in {1..20}; do
    echo "=== Run $i ===" 
    ./scripts/test/quick.sh --no-build FailingTestName || echo "FAILED on run $i"
done
```

### Step 2: Understand the Test's Intent

Read the test thoroughly:

1. What behavior is it verifying?
1. What Lua version(s) does it target?
1. Is there a corresponding `.lua` fixture?
1. What does the Lua specification say about this behavior?

```bash
# Find the test file
fd "TestClassName" src/tests/

# Find related fixtures
fd "test_name" src/tests/ --extension lua

# Check Lua spec
rg "relevant_function" docs/lua-spec/
```

### Step 3: Determine Failure Category

| Category               | Symptoms                                                              | Investigation Path                          |
| ---------------------- | --------------------------------------------------------------------- | ------------------------------------------- |
| **Production Bug**     | Test expects correct Lua behavior but NovaSharp produces wrong result | Debug interpreter pipeline                  |
| **Test Bug**           | Test expectation is incorrect vs Lua spec                             | Verify against reference Lua, then fix test |
| **Test Isolation Bug** | Test passes alone, fails with others                                  | Check for missing isolation attributes      |
| **Race Condition**     | Intermittent, timing-dependent                                        | Add tracing, review thread safety           |

### Step 4: Production Bug Investigation

If the test correctly expects Lua-compliant behavior:

```bash
# Verify expected behavior with reference Lua
lua5.4 -e "print(test_code_here)"

# Compare with NovaSharp
dotnet run --project src/tooling/NovaSharp.Cli -e "print(test_code_here)"

# Check all Lua versions if version-specific
for v in 5.1 5.2 5.3 5.4; do lua$v -e "print(test_code_here)"; done
```

Then follow [debugging-interpreter](debugging-interpreter.md) to trace through the pipeline.

### Step 5: Test Bug Investigation

If the test expectation might be wrong:

```bash
# Check Lua specification
bat --paging=never docs/lua-spec/lua54-manual.md | rg -A5 "function_name"

# Verify with ALL Lua versions
for v in 5.1 5.2 5.3 5.4; do
    echo "=== Lua $v ==="
    lua$v -e "print(test_code_here)"
done
```

**Important**: If Lua behavior differs from test expectation, you likely found a test bug. But verify thoroughly — the test may be correct and NovaSharp has a bug.

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

| Symptom                   | Likely Cause                       | Where to Look                    |
| ------------------------- | ---------------------------------- | -------------------------------- |
| Wrong numeric result      | `LuaNumber` integer/float handling | `DataTypes/LuaNumber.cs`         |
| String comparison failure | Encoding or escape sequences       | `CoreLib/StringModule.cs`        |
| Table iteration order     | Table implementation               | `DataTypes/Table.cs`             |
| Function call failure     | Argument marshalling               | `Interop/`                       |
| Version-specific failure  | Feature flag or version check      | `LuaCompatibilityVersion` usages |
| Memory/GC issues          | Pooling or closure captures        | Resource management code         |

______________________________________________________________________

## Resources

- [debugging-interpreter](debugging-interpreter.md) — Pipeline debugging
- [lua-spec-verification](lua-spec-verification.md) — Verifying against reference Lua
- [tunit-test-writing](tunit-test-writing.md) — Test patterns and isolation
- [docs/Testing.md](../../docs/Testing.md) — Testing documentation
