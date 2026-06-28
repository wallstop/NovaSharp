# TUnit Test Multi-Version Coverage Audit - Phase 3 Progress (Coroutine Module)

**Date**: 2025-12-15
**PLAN.md Section**: §8.39
**Status**: CoroutineModuleTUnitTests.cs Remediation Complete

## Summary

Completed remediation of `CoroutineModuleTUnitTests.cs` with proper version-specific test coverage. This file required special attention due to version-specific behavior differences in:

- `coroutine.running()` — Return value differs between Lua 5.1 and 5.2+
- `coroutine.isyieldable()` — Only exists in Lua 5.3+

## Metrics Update

| Metric                              | Before Session | After Session | Change |
| ----------------------------------- | -------------- | ------------- | ------ |
| Total tests                         | 2,723          | 2,725         | +2     |
| Compliant tests                     | 491            | 503           | +12    |
| Compliance %                        | 18.03%         | 18.46%        | +0.43% |
| Lua execution tests needing version | 701            | 691           | -10    |

### Version Coverage

| Version | Tests Before | Tests After |
| ------- | ------------ | ----------- |
| Lua51   | 683          | 682         |
| Lua52   | 720          | 734         |
| Lua53   | 746          | 753         |
| Lua54   | 765          | 772         |
| Lua55   | 780          | 790         |

## CoroutineModuleTUnitTests.cs Changes

### Version-Specific Test Patterns Applied

1. **`coroutine.running()` Tests**

   - **Lua 5.2+**: Returns `(coroutine, isMain)` tuple
   - Tests `RunningFromMainReturnsMainCoroutine` and `RunningInsideCoroutineReturnsFalse` now have `[Arguments]` for Lua52, Lua53, Lua54, Lua55 only

1. **`coroutine.isyieldable()` Tests**

   - Only available in Lua 5.3+
   - Tests for this feature have `[Arguments]` for Lua53, Lua54, Lua55 only

1. **Universal Coroutine Tests**

   - `coroutine.create()`, `coroutine.resume()`, `coroutine.yield()`, `coroutine.status()`, `coroutine.wrap()`
   - All have full 5-version coverage: `[Arguments(LuaCompatibilityVersion.Lua51)]` through `Lua55`

### Test Structure

```csharp
// Version-specific: Lua 5.2+ coroutine.running() behavior
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
public async Task RunningFromMainReturnsMainCoroutine(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // Test verifies (coroutine, isMain) tuple return
}

// Universal: All versions
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
public async Task StatusReflectsLifecycleAndForceSuspendedStates(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // Test works identically across all versions
}
```

## Version-Specific Behavior Documentation

| Feature                   | Lua 5.1                              | Lua 5.2                             | Lua 5.3+                            |
| ------------------------- | ------------------------------------ | ----------------------------------- | ----------------------------------- |
| `coroutine.running()`     | Returns coroutine only (nil on main) | Returns `(coroutine, isMain)` tuple | Returns `(coroutine, isMain)` tuple |
| `coroutine.isyieldable()` | ❌ Does not exist                    | ❌ Does not exist                   | ✅ Returns boolean                  |
| `coroutine.close()`       | ❌ Does not exist                    | ❌ Does not exist                   | ✅ Lua 5.4+ only                    |

## Remaining Work

### Immediate Next Steps

1. **`Bit32ModuleTUnitTests.cs`** — Requires special handling:

   - `bit32` library only exists in Lua 5.2
   - Tests should have `[Arguments(LuaCompatibilityVersion.Lua52)]` only
   - Need negative tests for other versions (bit32 should be nil)

1. **Remaining Module Files** — ~150 tests

   - Apply standard 5-version coverage pattern
   - Use `scripts/dev/add-version-coverage.py` automation tool

1. **Units/ Directory** — 366 tests (second priority)

### CI Integration Target

- Current compliance: 18.46%
- Target for CI enforcement: >50%
- Estimated tests needed: ~300 more tests with version coverage

## Files Modified This Session

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs`

## Related Progress Files

- [Session 025 - TUnit Version Coverage Audit Phase 1](session-025-tunit-version-coverage-audit-phase1.md)
- [Session 028 - TUnit Version Coverage Phase 2 StringModule](session-028-tunit-version-coverage-phase2-stringmodule.md)
- [Session 029 - TUnit Version Coverage Phase 3 Bulk](session-029-tunit-version-coverage-phase3-bulk.md)
- [Session 031 - TUnit Version Coverage Phase 3 Modules](session-031-tunit-version-coverage-phase3-modules.md)
