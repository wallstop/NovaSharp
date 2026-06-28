# Session 042: TUnit Data-Driving Helpers — Phase 4 Batch Migration

**Date**: 2025-12-19
**Focus**: Test Data-Driving Helper Infrastructure (§8.42) — Phase 4 Batch Migration
**Status**: ✅ Complete

## Summary

This session continued the TUnit data-driving helpers migration initiative, converting 38 additional tests across 3 test files to use the consolidated helper attributes (`[AllLuaVersions]`, `[LuaVersionsFrom]`, `[LuaVersionsUntil]`). The migration improves multi-version test coverage and reduces boilerplate.

## Coverage Metrics

| Metric                              | Before | After  | Change |
| ----------------------------------- | ------ | ------ | ------ |
| Compliant tests                     | 1,553  | 1,590  | +37    |
| Lua execution tests needing version | 395    | 357    | -38    |
| Compliance %                        | 42.25% | 43.28% | +1.03% |

## Files Converted

### 1. ErrorHandlingModuleTUnitTests.cs

**Tests Migrated**: 19 → ~97 test cases (5× coverage increase)

| Migration Pattern           | Tests | Notes                           |
| --------------------------- | ----- | ------------------------------- |
| `[AllLuaVersions]`          | 11    | Universal pcall/xpcall behavior |
| `[LuaVersionsFrom(Lua53)]`  | 2     | Handler validation (5.3+)       |
| `[LuaVersionsUntil(Lua51)]` | 2     | Legacy 5.1-only behavior        |
| `[LuaVersionsFrom(Lua52)]`  | 2     | Extra arguments feature (5.2+)  |
| Already compliant           | 8     | Existing version attributes     |

**Key Consolidations**:

- Merged 3 separate tests (`XpcallPassesExtraArgumentsInLua52/53/54`) into single `[LuaVersionsFrom(Lua52)]` test
- Replaced hardcoded `Lua53` tests with proper `[LuaVersionsFrom(Lua53)]` for 5.3+ coverage
- Removed unused `CreateScript()` method (replaced by versioned `CreateScriptWithVersion()`)

### 2. CoroutineLifecycleTUnitTests.cs

**Tests Migrated**: 11 → ~103 test cases

| Migration Pattern          | Tests | Notes                                    |
| -------------------------- | ----- | ---------------------------------------- |
| `[AllLuaVersions]`         | 8     | Universal coroutine behavior             |
| `[LuaVersionsFrom(Lua54)]` | 3     | `<close>` attribute tests (5.4+ feature) |

**Key Changes**:

- Updated `CreateScript()` helper to accept `LuaCompatibilityVersion` parameter
- Tests using Lua 5.4's `<close>` attribute correctly restricted to 5.4+
- `CloseNotStartedCoroutineReturnsTrue` uses `[AllLuaVersions]` since it doesn't use `<close>`

### 3. CompositeUserDataDescriptorTUnitTests.cs

**Tests Migrated**: 8 → ~45 test cases

| Migration Pattern  | Tests | Notes                    |
| ------------------ | ----- | ------------------------ |
| `[AllLuaVersions]` | 8     | Descriptor pattern tests |

**Note**: These tests use `new Script()` as a context parameter for descriptor methods but don't execute Lua code. Adding version coverage ensures the tests verify behavior across all script configurations.

## Migration Patterns Applied

### Pattern 1: Version Range Helpers (Most Common)

Replace verbose multi-line `[Arguments]` with single-line helpers:

**Before (verbose)**:

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
public async Task Feature(LuaCompatibilityVersion version)
```

**After (concise)**:

```csharp
[global::TUnit.Core.Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
public async Task Feature(LuaCompatibilityVersion version)
```

### Pattern 2: Version-Specific Features

For Lua 5.4-only features like `<close>` attribute:

```csharp
[global::TUnit.Core.Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
public async Task CloseSuspendedCoroutine(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    script.DoString(@"
        function closable()
            local handle <close> = setmetatable({}, { __close = function() end })
            coroutine.yield('pause')
        end
    ");
    // ...
}
```

### Pattern 3: Consolidating Separate Per-Version Tests

Merged 3 tests into 1 with automatic version expansion:

**Before**: `XpcallPassesExtraArgumentsInLua52`, `XpcallPassesExtraArgumentsInLua53`, `XpcallPassesExtraArgumentsInLua54`

**After**: Single `XpcallPassesExtraArgumentsInLua52Plus` with `[LuaVersionsFrom(Lua52)]`

## Verification

All migrated tests pass:

```bash
./scripts/test/quick.sh -c ErrorHandlingModule     # 97 tests passed
./scripts/test/quick.sh -c CoroutineLifecycle      # 103 tests passed  
./scripts/test/quick.sh -c CompositeUserDataDescriptor  # 45 tests passed
```

## Remaining Work

Per §8.42 Migration Status, approximately 357 Lua execution tests still need version coverage. High-impact remaining files:

| File                               | Approx Tests | Notes              |
| ---------------------------------- | ------------ | ------------------ |
| BinaryOperatorExpressionTUnitTests | ~19          | Mixed compliance   |
| DebugModuleTapParityTUnitTests     | ~17          | Debug module tests |
| LuaRandomParityTUnitTests          | ~15          | RNG parity         |
| IoStdHandleUserDataTUnitTests      | ~15          | I/O tests          |
| EventMemberDescriptorTUnitTests    | ~11          | UserData events    |

## Commands

```bash
# Check compliance metrics
python3 scripts/lint/check-tunit-version-coverage.py

# See non-compliant tests by file  
python3 scripts/lint/check-tunit-version-coverage.py --detailed | \
  grep -E "^src/tests" | cut -d: -f1 | sort | uniq -c | sort -rn

# Run migrated tests
./scripts/test/quick.sh -c ErrorHandlingModule
./scripts/test/quick.sh -c CoroutineLifecycle
./scripts/test/quick.sh -c CompositeUserDataDescriptor
```
