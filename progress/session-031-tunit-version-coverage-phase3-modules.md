# TUnit Test Multi-Version Coverage Audit - Phase 3 Progress (Module Tests)

**Date**: 2025-12-15
**PLAN.md Section**: §8.39
**Status**: Phase 3 In Progress - Major remediation batch complete

## Summary

Continued Phase 3 remediation of the TUnit Test Multi-Version Coverage Audit by adding `[Arguments(LuaCompatibilityVersion.*)]` attributes to 10 module test files, significantly improving test coverage compliance.

## Metrics

| Metric                              | Before | After  | Change  |
| ----------------------------------- | ------ | ------ | ------- |
| Total tests                         | 2,723  | 2,723  | —       |
| Compliant tests                     | 124    | 491    | +367    |
| Compliance %                        | 4.55%  | 18.03% | +13.48% |
| Lua execution tests needing version | 1,030  | 701    | -329    |
| Infrastructure tests (no Lua)       | 1,569  | 1,531  | -38     |

### Version Coverage

| Version | Tests Before | Tests After |
| ------- | ------------ | ----------- |
| Lua51   | 126          | 683         |
| Lua52   | 142          | 720         |
| Lua53   | 148          | 746         |
| Lua54   | 148          | 765         |
| Lua55   | 141          | 780         |

## Files Modified

### Module Test Files Updated

1. **`DebugModuleTUnitTests.cs`** — 67 tests

   - Added `using WallstopStudios.NovaSharp.Interpreter.Compatibility;`
   - Added `CreateScriptWithVersion` helper
   - Added version arguments to all 67 test methods

1. **`MathModuleTUnitTests.cs`** — 51 tests

   - Used existing `CreateScript(version)` helper
   - Added version arguments to tests missing coverage

1. **`IoModuleTUnitTests.cs`** — 49 tests

   - Used existing `CreateScriptWithVersion` helper
   - Added version arguments to tests missing coverage
   - Preserved helper methods that don't need version param

1. **`Utf8ModuleTUnitTests.cs`** — 37 tests

   - Added version arguments using existing helper

1. **`CoroutineModuleTUnitTests.cs`** — 34 tests

   - Added version arguments using existing helper

1. **`OsSystemModuleTUnitTests.cs`** — 32 tests

   - Added `using WallstopStudios.NovaSharp.Interpreter.Compatibility;`
   - Added `CreateScriptWithVersion` helper
   - Fixed `CreateScriptContext` method to handle nullable version

1. **`OsTimeModuleTUnitTests.cs`** — 22 tests

   - Added version arguments using existing helper

1. **`TableModuleTUnitTests.cs`** — 16 tests

   - Added `using WallstopStudios.NovaSharp.Interpreter.Compatibility;`
   - Used existing `CreateScript(version)` helper

1. **`BasicModuleTUnitTests.cs`** — 35 tests

   - Added version arguments using existing helper

1. **`LoadModuleTUnitTests.cs`** — 20 tests

   - Added `using WallstopStudios.NovaSharp.Interpreter.Compatibility;`
   - Added `CreateScriptWithVersion` helper

## Tools Created

### `/scripts/dev/add-version-coverage.py`

Automated script to add version coverage to TUnit test files:

```bash
python3 scripts/dev/add-version-coverage.py <file_path>
```

Features:

- Detects tests without version Arguments
- Adds all 5 Lua version Arguments (5.1-5.5)
- Modifies method signatures to accept `LuaCompatibilityVersion version`
- Replaces `CreateScript()` with `CreateScriptWithVersion(version)` in modified methods
- Preserves helper methods that shouldn't be modified

### Lint Script Fix

Fixed `/scripts/lint/check-tunit-version-coverage.py` to correctly detect version Arguments that appear AFTER the `[Test]` attribute (the original script only looked at lines BEFORE `[Test]`).

## Test Pattern Used

For each universal behavior test:

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
public async Task TestMethodName(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // test code
}
```

Helper method pattern:

```csharp
private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
{
    ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
    {
        CompatibilityVersion = version,
    };
    Script script = new(CoreModulePresets.Complete, options);
    script.Options.DebugPrint = _ => { };
    return script;
}
```

## Build Status

All modified files compile successfully:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Known Issues

One test failure observed during testing:

- `IsYieldableInsideXpcallErrorHandlerWithinCoroutine` - Stack overflow in error handler recursion
- Investigation needed to determine if this is a version-specific behavior or existing bug

## Next Steps

1. Investigate and fix the test failure in `CoroutineModuleTUnitTests`
1. Continue remediation with remaining high-priority files:
   - `Units/DataTypes/TableTUnitTests.cs` (25 tests)
   - `Descriptors/StandardEnumUserDataDescriptorTUnitTests.cs` (22 tests)
   - `Units/Tree/Expressions/BinaryOperatorExpressionTUnitTests.cs` (22 tests)
   - `Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs` (20 tests)
1. Update PLAN.md with current metrics
1. Continue until all 701 Lua execution tests have version coverage

## Related Files

- `PLAN.md` §8.39 — TUnit Test Multi-Version Coverage Audit
- `scripts/lint/check-tunit-version-coverage.py` — Audit script
- `scripts/dev/add-version-coverage.py` — Automation script
- `progress/2025-12-15-tunit-version-coverage-audit-phase1.md` — Phase 1 progress
- `progress/2025-12-15-tunit-version-coverage-phase2-stringmodule.md` — Phase 2 progress
