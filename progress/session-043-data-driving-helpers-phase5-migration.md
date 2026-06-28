# Session 043: Data-Driving Helpers Phase 5 - Migration Continuation

**Date**: 2025-12-19
**Focus**: Continue migrating TUnit tests to use `[AllLuaVersions]` helper attributes

## Overview

This session continues the migration of TUnit tests to use the data-driving helper attributes (`[AllLuaVersions]`, `[LuaVersionsFrom]`, etc.) as part of initiative §8.42. The goal is to reduce the remaining Lua execution tests needing version attributes.

## Starting Metrics

- **Compliant tests**: 1,590 (43.3%)
- **Lua execution tests needing version**: 357
- **Infrastructure tests (exempt)**: 1,727

## Changes Made

### 1. UserDataMethodsTUnitTests.cs (EndToEnd)

Added `[AllLuaVersions]` to 6 tests:

- `InteropTestAutoregisterPolicy`
- `InteropDualInterfaces`
- `InteropTestNamesCamelized`
- `InteropTestSelfDescribingType`
- `InteropTestCustomDescribedType`
- `InteropStaticInstanceAccessRaisesError`

### 2. UserDataMetaTUnitTests.cs (EndToEnd)

Added `[AllLuaVersions]` to 6 tests:

- `InteropMetaPairs`
- `InteropMetaIPairs`
- `InteropMetaIterator`
- `InteropMetaOpLen`
- `InteropMetaEquality`
- `InteropMetaComparisons`

### 3. ProxyObjectsTUnitTests.cs (EndToEnd)

Added `[AllLuaVersions]` to 1 test:

- `ProxySurfaceAllowsAccessToRandom`

### 4. ProxyUserDataDescriptorTUnitTests.cs (Descriptors)

Added `[AllLuaVersions]` to 4 tests:

- `IndexUsesProxyObjectBeforeDelegating`
- `SetIndexReturnsInnerResult`
- `IndexPassesThroughNullInstancesWithoutProxying`
- `MetaIndexAndAsStringProxyValues`

### 5. UserDataNestedTypesTUnitTests.cs (EndToEnd)

Added `[AllLuaVersions]` to 7 tests:

- `InteropNestedTypesPublicEnum`
- `InteropNestedTypesPublicRef`
- `InteropNestedTypesPrivateRef`
- `InteropNestedTypesPrivateRef2`
- `InteropNestedTypesPublicVal`
- `InteropNestedTypesPrivateVal`
- `InteropNestedTypesPrivateVal2`

### 6. DebugModuleTapParityTUnitTests.cs (Modules)

Added `[AllLuaVersions]` to 17 tests:

- `RequireDebugReturnsFunctionTable`
- `RequireDebugReturnsSameInstanceAsGlobal`
- `GetInfoReturnsFunctionMetadata`
- `GetInfoLevelOutOfRangeReturnsNil`
- `GetInfoInvalidArgumentThrows`
- `GetRegistryExposesLoadedTable`
- `SetMetatableRoundTrips`
- `SetMetatableErrorMatchesLuaFormat`
- `SetUserValueRoundTrips`
- `SetUserValueReturnsOriginalHandle`
- `SetUserValueRejectsNonTablesWithLuaMessage`
- `GetUpvalueReturnsTuple`
- `SetupValueUpdatesClosure`
- `UpvalueJoinSharesState`
- `UpvalueIdReturnsUserDataHandles`
- `TracebackIncludesMessage`
- `TracebackUsesLfLineEndings`

Updated the `CreateScript()` helper method to accept `LuaCompatibilityVersion`.

### 7. IoStdHandleUserDataTUnitTests.cs (Modules)

Added `[AllLuaVersions]` to 15 tests:

- `StdInIsFileUserDataHandle`
- `StdInEqualsItselfButNotStdOut`
- `StdOutIsFileUserDataHandle`
- `StdErrIsFileUserDataHandle`
- `RequireIoExposesSameStdHandles`
- `IoInputReturnsCurrentStdInHandle`
- `IoOutputReturnsCurrentStdOutHandle`
- `StdInCannotBeIndexedOrAssigned`
- `StdOutCannotBeIndexedOrAssigned`
- `StdInArithmeticThrows`
- `StdOutArithmeticThrows`
- `StdInConcatenationThrows`
- `StdOutConcatenationThrows`
- `StdInComparisonsThrow`
- `StdOutComparisonsThrow`

Updated the `CreateScript()` helper method to accept `LuaCompatibilityVersion`.

## Ending Metrics

- **Compliant tests**: 1,650 (44.9%)
- **Lua execution tests needing version**: ~297
- **Infrastructure tests (exempt)**: 1,727

## Progress Summary

| File                                 | Tests Added |
| ------------------------------------ | ----------- |
| UserDataMethodsTUnitTests.cs         | 6           |
| UserDataMetaTUnitTests.cs            | 6           |
| ProxyObjectsTUnitTests.cs            | 1           |
| ProxyUserDataDescriptorTUnitTests.cs | 4           |
| UserDataNestedTypesTUnitTests.cs     | 7           |
| DebugModuleTapParityTUnitTests.cs    | 17          |
| IoStdHandleUserDataTUnitTests.cs     | 15          |
| **Total**                            | **56**      |

## Migration Pattern Used

For files using a `CreateScript()` helper method:

```csharp
// Before
private static Script CreateScript()
{
    return new Script(CoreModulePresets.Complete);
}

[Test]
public async Task SomeTest()
{
    Script script = CreateScript();
    // ...
}

// After
private static Script CreateScript(LuaCompatibilityVersion version)
{
    return new Script(version, CoreModulePresets.Complete);
}

[Test]
[AllLuaVersions]
public async Task SomeTest(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ...
}
```

## Next Steps

1. Continue migrating remaining ~297 Lua execution tests
1. Priority files to migrate:
   - Sandbox tests (DeterministicExecutionTUnitTests, SandboxAccessRestrictionTUnitTests, etc.)
   - ReplInterpreterScriptLoaderTUnitTests
   - Spec tests (LuaMathMultiVersionSpec, LuaRandomParity, etc.)
1. Consider creating an automated migration script for common patterns

## Verification

All migrated tests pass:

```bash
./scripts/test/quick.sh -c DebugModuleTapParity --no-build  # 17 tests passed
./scripts/test/quick.sh -c IoStdHandleUserData --no-build   # 15 tests passed
```

## Related Items

- PLAN.md §8.39: TUnit Test Multi-Version Coverage Audit
- PLAN.md §8.42: Test Data-Driving Helper Migration
