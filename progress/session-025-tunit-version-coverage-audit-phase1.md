# TUnit Test Multi-Version Coverage Audit - Phase 1 Complete

**Date**: 2025-12-15
**PLAN.md Section**: §8.39
**Status**: Phase 1 Complete (Automated Audit)

## Summary

Completed Phase 1 of the TUnit Test Multi-Version Coverage Audit. Created an automated audit script that scans all TUnit test files and identifies tests missing `LuaCompatibilityVersion` `[Arguments]` attributes.

## What Was Done

### 1. Created Audit Script

Created `scripts/lint/check-tunit-version-coverage.py` with the following capabilities:

- Scans all `*TUnitTests.cs` files in the TUnit test directory
- Identifies tests with `[global::TUnit.Core.Test]` attribute
- Detects existing version coverage via `[Arguments(LuaCompatibilityVersion.*)]`
- Detects alternative coverage via `[MethodDataSource]` and `[CombinedDataSources]`
- **NEW**: Distinguishes between Lua execution tests and infrastructure tests by analyzing method bodies
- Outputs human-readable or JSON reports
- Supports `--detailed` flag for full test listing

### 2. Lua Execution Detection

The script analyzes each test method body for patterns indicating Lua code execution:

- `.DoString()`
- `.DoFile()`
- `.RunString()`
- `.DoChunk()`
- `new Script()`
- `CreateScript()`
- `script.Globals`
- `script.Call()`

This enables prioritization: Lua execution tests are higher priority for version coverage than infrastructure tests.

## Audit Results

### Overall Statistics

| Metric                              | Count |
| ----------------------------------- | ----- |
| Files analyzed                      | 211   |
| Total tests                         | 2,664 |
| Compliant tests (have version args) | 34    |
| Non-compliant tests                 | 2,630 |
| Compliance percentage               | 1.28% |

### Non-Compliant Test Breakdown

| Category                               | Count | Priority |
| -------------------------------------- | ----- | -------- |
| 🔴 Lua execution tests needing version | 1,107 | **HIGH** |
| ⚪ Infrastructure tests (no Lua)       | 1,523 | LOW      |

### Lua Execution Tests by Directory

| Directory   | Tests Needing Version |
| ----------- | --------------------- |
| Modules     | 505                   |
| Units       | 366                   |
| EndToEnd    | 98                    |
| Descriptors | 80                    |
| Spec        | 35                    |
| Cli         | 18                    |
| Loaders     | 4                     |
| Platforms   | 1                     |
| **Total**   | **1,107**             |

### Infrastructure Tests by Directory

| Directory          | Count |
| ------------------ | ----- |
| Units              | 1,080 |
| Descriptors        | 145   |
| Cli                | 98    |
| Modules            | 68    |
| EndToEnd           | 42    |
| Loaders            | 29    |
| Spec               | 27    |
| Platforms          | 22    |
| SerializationTests | 12    |

### Current Version Coverage

Tests with explicit `[Arguments(LuaCompatibilityVersion.*)]`:

| Version | Test Count |
| ------- | ---------- |
| Lua51   | 13         |
| Lua52   | 19         |
| Lua53   | 16         |
| Lua54   | 15         |
| Lua55   | 15         |

## Usage

```bash
# Basic summary
python3 scripts/lint/check-tunit-version-coverage.py

# Detailed output with all non-compliant tests
python3 scripts/lint/check-tunit-version-coverage.py --detailed

# JSON output for automation
python3 scripts/lint/check-tunit-version-coverage.py --json

# Fail CI if non-compliant tests found
python3 scripts/lint/check-tunit-version-coverage.py --fail-on-noncompliant

# Only consider Lua execution tests for compliance
python3 scripts/lint/check-tunit-version-coverage.py --lua-only --fail-on-noncompliant
```

## Recommendations

### High Priority (1,107 tests)

**Lua execution tests** should be remediated first. These tests execute actual Lua code and their behavior may differ across Lua versions.

Recommended approach:

1. Start with `Modules/` directory (505 tests) - core Lua stdlib tests
1. Then `Units/` directory (366 tests) - unit tests for interpreter components
1. Then `EndToEnd/` (98 tests) - integration tests
1. Finally `Descriptors/` (80 tests) - interop tests

### Medium Priority (35 tests)

**Spec tests** in `Spec/` directory should be next - these are specification compliance tests.

### Low Priority (1,523 tests)

**Infrastructure tests** that don't execute Lua code don't strictly need version arguments, but adding them ensures tests run against all versions for consistency.

### Pattern for Remediation

For universal Lua features (work same in all versions):

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
public async Task TestMethod(Compatibility.LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ... test code
}
```

For version-specific features:

```csharp
// Positive test - feature available
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
public async Task FeatureAvailableInLua53Plus(Compatibility.LuaCompatibilityVersion version)
{
    // Test feature works
}

// Negative test - feature unavailable
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
[global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
public async Task FeatureShouldBeNilInPreLua53(Compatibility.LuaCompatibilityVersion version)
{
    // Test feature is nil/absent
}
```

## Next Steps

1. **Phase 2: Classification** — Review audit results, categorize tests by feature area
1. **Phase 3: Remediation** — Add `[Arguments]` attributes to Lua execution tests
1. **Phase 4: Negative Test Gap Analysis** — Identify version-specific features missing negative tests
1. **Phase 5: Negative Test Implementation** — Add negative test cases
1. **Phase 6: CI Integration** — Add lint check to CI pipeline

## Files Changed

- Created: `scripts/lint/check-tunit-version-coverage.py`

## Related

- PLAN.md §8.39: TUnit Test Multi-Version Coverage Audit
- CONTRIBUTING_AI.md: Multi-Version Testing Requirements
- docs/Testing.md: Testing documentation
