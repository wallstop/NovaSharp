# Session 041: TUnit Data-Driving Helpers — Phase 3 UserData Migration

**Date**: 2025-12-19
**Focus**: Test Data-Driving Helper Infrastructure (§8.42) — Phase 3 UserData Tests Migration
**Status**: ✅ Significant Progress

## Summary

This session continued the Phase 3 migration work on the data-driving helpers initiative. We converted 3 UserData-related test files from tests without version parameters to using the consolidated `[AllLuaVersions]` helper attribute, ensuring all C# interop tests run across all 5 Lua versions.

## Converted Files

| File                                          | Tests Migrated | Test Cases | Attribute Used     | Notes                                            |
| --------------------------------------------- | -------------- | ---------- | ------------------ | ------------------------------------------------ |
| `DispatchingUserDataDescriptorTUnitTests.cs`  | 22             | 123        | `[AllLuaVersions]` | Updated helper method to accept version          |
| `StandardEnumUserDataDescriptorTUnitTests.cs` | 21             | 114        | `[AllLuaVersions]` | Updated TestHelpers.CreateExecutionContext calls |
| `UserDataEventsTUnitTests.cs`                 | 7              | 35         | `[AllLuaVersions]` | Event handling tests                             |

**Total Tests Converted**: 50 tests → 272 test cases (5.44× coverage increase)

## Coverage Metrics

| Metric                              | Before | After  | Change |
| ----------------------------------- | ------ | ------ | ------ |
| Compliant tests                     | 1,497  | 1,553  | +56    |
| Lua execution tests needing version | 436    | 395    | -41    |
| Compliance %                        | 40.72% | 42.25% | +1.53% |

## Migration Patterns Applied

### Pattern 1: Helper Method with Version Parameter

For files that use a helper method to create Script instances, we added a version parameter:

**Before:**

```csharp
private static Script CreateScriptWithHosts(out DispatchHost hostAdd, ...)
{
    Script script = new(CoreModulePresets.Complete);
    // ...
}

[global::TUnit.Core.Test]
public async Task SomeTest()
{
    Script script = CreateScriptWithHosts(out _, ...);
    // ...
}
```

**After:**

```csharp
private static Script CreateScriptWithHosts(
    LuaCompatibilityVersion version,
    out DispatchHost hostAdd, ...)
{
    Script script = new(version, CoreModulePresets.Complete);
    // ...
}

[global::TUnit.Core.Test]
[AllLuaVersions]
public async Task SomeTest(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithHosts(version, out _, ...);
    // ...
}
```

### Pattern 2: Direct TestHelpers.CreateExecutionContext Calls

For tests using `TestHelpers.CreateExecutionContext(new Script())`:

**Before:**

```csharp
[global::TUnit.Core.Test]
public async Task SomeTest()
{
    ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script());
    // ...
}
```

**After:**

```csharp
[global::TUnit.Core.Test]
[AllLuaVersions]
public async Task SomeTest(LuaCompatibilityVersion version)
{
    ScriptExecutionContext context = TestHelpers.CreateExecutionContext(new Script(version));
    // ...
}
```

### Pattern 3: Lambda-Based Tests

For tests that create Script inside lambdas:

**Before:**

```csharp
[global::TUnit.Core.Test]
public async Task InteropEventSimple()
{
    await WithRegisteredEventTypes(async () =>
    {
        Script script = new(default(CoreModules));
        // ...
    });
}
```

**After:**

```csharp
[global::TUnit.Core.Test]
[AllLuaVersions]
public async Task InteropEventSimple(LuaCompatibilityVersion version)
{
    await WithRegisteredEventTypes(async () =>
    {
        Script script = new(version, default(CoreModules));
        // ...
    });
}
```

## Files Modified

1. **DispatchingUserDataDescriptorTUnitTests.cs**

   - Added imports for `LuaCompatibilityVersion` and helper attribute namespace
   - Updated `CreateScriptWithHosts` helper to accept version parameter
   - Added `[AllLuaVersions]` and version parameter to 22 test methods

1. **StandardEnumUserDataDescriptorTUnitTests.cs**

   - Added imports for `LuaCompatibilityVersion` and helper attribute namespace
   - Updated all `new Script()` calls to `new Script(version)`
   - Added `[AllLuaVersions]` and version parameter to 21 test methods

1. **UserDataEventsTUnitTests.cs**

   - Added imports for `LuaCompatibilityVersion` and helper attribute namespace
   - Updated all `new Script(default(CoreModules))` calls to include version
   - Added `[AllLuaVersions]` and version parameter to 7 test methods

## Tests Verified

All converted tests pass:

- DispatchingUserDataDescriptorTUnitTests: 123 test cases (22 tests × 5 versions + variants)
- StandardEnumUserDataDescriptorTUnitTests: 114 test cases (21 tests × 5 versions + infrastructure)
- UserDataEventsTUnitTests: 35 test cases (7 tests × 5 versions)

## Remaining Work

### High-Priority Files (Per §8.42 Migration Status)

- UserDataMethodsTUnitTests.cs (~42 tests) - Most tests have different InteropAccessMode variants
- UserDataPropertiesTUnitTests.cs (~37 tests)
- UserDataFieldsTUnitTests.cs (~37 tests)
- BinaryDumpTUnitTests.cs (~11 tests)
- UserDataMetaTUnitTests.cs (~9 tests)

### Other Non-Compliant Lua Execution Tests

- Approximately 345 tests across various modules still need version coverage

## Next Steps

1. Continue migrating remaining UserData test files
1. Create automated migration script for common patterns (helper method, direct instantiation)
1. Update PLAN.md with current metrics
1. Add CI lint rule to flag tests without version coverage

## Commands

```bash
# Build and test migrated files
./scripts/test/quick.sh -c DispatchingUserDataDescriptor
./scripts/test/quick.sh -c StandardEnumUserDataDescriptor
./scripts/test/quick.sh -c UserDataEvents

# Check coverage metrics
python3 scripts/lint/check-tunit-version-coverage.py

# See detailed non-compliant tests
python3 scripts/lint/check-tunit-version-coverage.py --detailed
```
