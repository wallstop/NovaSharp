---
name: tunit-test-writing
description: "Write or update NovaSharp TUnit tests with multi-Lua-version coverage, isolation, fixtures, and repository conventions. Use for TUnit, AllLuaVersions, test cases, or interpreter test changes."
metadata:
  category: testing
  priority: core
  related: exhaustive-test-coverage, lua-fixture-creation, lua-spec-verification, test-failure-investigation
---
# Skill: Writing TUnit Tests for NovaSharp

**Related Skills**: [exhaustive-test-coverage](../exhaustive-test-coverage/SKILL.md) (comprehensive testing philosophy), [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (creating .lua fixtures for tests), [lua-spec-verification](../lua-spec-verification/SKILL.md) (verifying behavior), [test-failure-investigation](../test-failure-investigation/SKILL.md) (investigating any test failures)

______________________________________________________________________

## 🔴 Critical: Complete Test Workflow

Every new test requires **THREE deliverables**:

1. **C# TUnit test** — Runs against NovaSharp runtime (this skill)
1. **`.lua` fixture file** — Standalone Lua for cross-interpreter verification (see [lua-fixture-creation](../lua-fixture-creation/SKILL.md))
1. **Regenerate corpus** — Run `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` after adding fixtures

### Workflow Order

```bash
# 1. Create C# test (this skill)
# 2. Create .lua fixture with metadata header (see lua-fixture-creation skill)
# 3. Verify fixture runs against reference Lua
lua5.4 path/to/fixture.lua

# 4. Regenerate corpus to sync fixtures
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py

# 5. Run tests to verify everything works
./scripts/test/quick.sh TestMethodName
```

______________________________________________________________________

## Framework Basics

- **Framework**: TUnit only (`global::TUnit.Core.Test`)
- **Async assertions**: `await Assert.That(...).ConfigureAwait(false)`
- **Method names**: PascalCase, **NO underscores** — `FeatureWorksCorrectly` not `Feature_Works_Correctly`
- **Explicit types**: Never use `var` — always declare types explicitly

______________________________________________________________________

## Required Isolation Attributes

Use these to prevent test interference:

```csharp
[UserDataIsolation]           // Isolates UserData registry
[ScriptGlobalOptionsIsolation] // Isolates global Script options
[PlatformDetectorIsolation]   // Isolates platform detection
```

______________________________________________________________________

## Cleanup Utilities

- `TempFileScope` — Auto-cleanup temporary files
- `SemaphoreSlimScope` — Auto-release semaphores
- `ConsoleTestUtilities` — Capture/restore console output

______________________________________________________________________

## 🔴 Multi-Version Testing (REQUIRED)

**All tests MUST run against all applicable Lua versions (5.1, 5.2, 5.3, 5.4, 5.5).**

### Version Data-Driving Helpers

**⚠️ PREFERRED: Use range-based helpers for future-proof tests.** These automatically include new Lua versions (e.g., 5.6) without requiring test updates.

| Helper                                              | Description              | Use Case                           |
| --------------------------------------------------- | ------------------------ | ---------------------------------- |
| `[AllLuaVersions]`                                  | All Lua versions (5.1+)  | Universal coverage (future-proof)  |
| `[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]`  | Versions from 5.3+       | Features introduced in 5.3         |
| `[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]` | Versions up to 5.2       | Features removed/changed after 5.2 |
| `[LuaVersionRange(Lua52, Lua54)]`                   | Inclusive version window | Focused compatibility spans        |
| `[LuaTestMatrix("input1", "input2")]`               | Versions × inputs        | Comprehensive edge-case testing    |

### ❌ AVOID: Explicit Version Lists

```csharp
// ❌ AVOID: Explicit version lists (not future-proof)
[Arguments(LuaCompatibilityVersion.Lua53)]
[Arguments(LuaCompatibilityVersion.Lua54)]
[Arguments(LuaCompatibilityVersion.Lua55)]
public async Task Feature(LuaCompatibilityVersion version) { }

// ✅ PREFER: Range-based helpers (auto-includes future versions)
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task Feature(LuaCompatibilityVersion version) { }
```

**Why prefer ranges?** When Lua 5.6 is released, tests using `[LuaVersionsFrom]` will automatically include it. Tests using explicit `[Arguments]` for each version will need manual updates.

### Examples

```csharp
// Universal test - runs on all 5 versions
[Test]
[AllLuaVersions]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ...
}

// Matrix test - 5 versions × 2 inputs = 10 test cases
[Test]
[LuaTestMatrix("input1", "input2")]
public async Task FeatureWithInputs(LuaCompatibilityVersion version, string input)
{
    // ...
}
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Version-Specific Features: Test BOTH Scenarios, Version Coverage Checklist, Test Naming Patterns, Data-Driven Test Attributes, and later sections.
