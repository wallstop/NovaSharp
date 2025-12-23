# TUnit Multi-Version Coverage Audit — Phase 3: Bulk Remediation

**Date**: 2025-12-15\
**Status**: 🟡 In Progress\
**Related**: PLAN.md §8.39

## Summary

Continued bulk remediation of TUnit test files to add `[Arguments(LuaCompatibilityVersion.LuaXX)]` attributes for all Lua versions. Fixed a critical bug in the audit script and remediated 7 major module test files.

## Audit Script Fix

### Bug Discovery

The `check-tunit-version-coverage.py` script was incorrectly looking for `[Arguments]` attributes in the lines BEFORE the `[Test]` attribute, but the actual pattern in TUnit tests is:

```csharp
[Test]                                    // Line i
[Arguments(LuaCompatibilityVersion.Lua51)] // Lines after [Test]
[Arguments(LuaCompatibilityVersion.Lua52)]
[Arguments(LuaCompatibilityVersion.Lua53)]
[Arguments(LuaCompatibilityVersion.Lua54)]
[Arguments(LuaCompatibilityVersion.Lua55)]
public async Task TestMethod(LuaCompatibilityVersion version)
```

### Fix Applied

Changed the script to look at the 20 lines AFTER `[Test]` (up to and including the method signature) instead of before:

```python
# Before (incorrect):
preceding_context = '\n'.join(lines[max(0, i-20):i])

# After (correct):
following_context = '\n'.join(lines[i:min(len(lines), i+20)])
```

## Files Remediated

### Pattern Applied

For each test file:

1. Added `CreateScriptWithVersion(LuaCompatibilityVersion version)` helper if not present
1. Added 5 `[Arguments]` attributes for all Lua versions to each `[Test]` method
1. Changed `CreateScript()` calls to `CreateScriptWithVersion(version)`
1. Added `LuaCompatibilityVersion version` parameter to method signatures

### Module Test Files Updated

| File                          | Tests Updated | Notes                                         |
| ----------------------------- | ------------- | --------------------------------------------- |
| `StringModuleTUnitTests.cs`   | 76            | Already partially done in Phase 2             |
| `DebugModuleTUnitTests.cs`    | 67            | Clean bulk update                             |
| `MathModuleTUnitTests.cs`     | 51            | Had existing `CreateScript(version)` helper   |
| `IoModuleTUnitTests.cs`       | 49            | Had existing `CreateScriptWithVersion` helper |
| `LoadModuleTUnitTests.cs`     | 32            | Added new helper                              |
| `OsSystemModuleTUnitTests.cs` | 27            | Used existing `CreateScriptContext` pattern   |
| `TableModuleTUnitTests.cs`    | 27            | Uses `CreateScript(version)` pattern          |

**Total**: ~329 tests remediated

## Audit Results After Phase 3

```
============================================================
  TUnit Multi-Version Test Coverage Audit
============================================================

Files analyzed:                211
Total tests:                   2,664
Compliant tests:               191 (7.2%)
Lua execution tests needing version: 701
Infrastructure tests (no Lua): 1,772
```

Progress: 1,030 → 701 Lua execution tests needing version coverage (329 fixed)

## Issues Discovered

### CoroutineModuleTUnitTests.cs Requires Manual Handling

Several tests in `CoroutineModuleTUnitTests.cs` test version-specific features that cannot simply have all 5 version arguments added:

| Feature                   | Availability  | Notes                              |
| ------------------------- | ------------- | ---------------------------------- |
| `coroutine.isyieldable()` | Lua 5.3+ only | Does not exist in 5.1/5.2          |
| `coroutine.running()`     | All versions  | Return value differs significantly |

These tests need to be manually split into:

- Positive tests (5.3+ only for `isyieldable`)
- Negative tests (5.1/5.2 verifying `nil` or error)

## Next Steps

1. **Complete Modules/ directory remediation**

   - `BasicModuleTUnitTests.cs`
   - `CoroutineModuleTUnitTests.cs` (manual review required)
   - `OsTimeModuleTUnitTests.cs`
   - `GlobalContextModuleTUnitTests.cs`
   - `Utf8ModuleTUnitTests.cs`
   - `Bit32ModuleTUnitTests.cs`

1. **Units/ directory** — 366 tests, second highest priority batch

1. **CoroutineModule manual review** — Split version-specific tests appropriately

1. **Run full test suite** — Verify all changes don't break existing functionality

## Commands Used

```bash
# Run audit
python3 scripts/lint/check-tunit-version-coverage.py

# Detailed audit showing which tests need work
python3 scripts/lint/check-tunit-version-coverage.py --detailed

# Build to verify changes compile
dotnet build src/NovaSharp.sln -c Release
```

## Related Files

- `scripts/lint/check-tunit-version-coverage.py` — Audit script (fixed)
- `PLAN.md` §8.39 — TUnit Multi-Version Coverage Audit
- `progress/2025-12-15-tunit-version-coverage-audit-phase1.md` — Phase 1 documentation
- `progress/2025-12-15-tunit-version-coverage-phase2-stringmodule.md` — Phase 2 documentation
