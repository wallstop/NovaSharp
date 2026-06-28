# Session 044: Test Data-Driving Helper Migration Phase 6

## Summary

Continued migration of TUnit tests to use `[AllLuaVersions]`, `[LuaVersionsFrom]`, and `[LuaVersionsUntil]` helper attributes for multi-version Lua coverage. This session focused on Spec tests, BinaryDump tests, and various descriptor/loader tests.

## Starting Metrics

- **Lua execution tests needing version coverage**: 266 (from session 043)
- **Compliance**: 45.84%

## Ending Metrics

- **Lua execution tests needing version coverage**: 222
- **Compliance**: 47.25%
- **Tests migrated this session**: 44 tests → 75+ versioned test cases

## Files Modified

### Spec Tests (Converting foreach loops to proper attributes)

1. **[LuaMathMultiVersionSpecTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs)**

   - Converted 8 tests from manual `foreach (version in Lua53PlusVersions)` to `[LuaVersionsFrom(Lua53)]` and `[LuaVersionsUntil(Lua52)]`
   - Removed redundant `Lua53PlusVersions` static array
   - Tests: `MathIntegerHelpersAreUnavailableBeforeLua53`, `MathTypeReportsIntegerAndFloat`, `MathToIntegerConvertsNumbersAndStrings`, etc.

1. **[LuaTableMoveMultiVersionSpecTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaTableMoveMultiVersionSpecTUnitTests.cs)**

   - Converted 4 tests from foreach loops to proper attributes
   - Tests: `TableMoveIsUnavailableBeforeLua53`, `TableMoveReturnsDestinationTable`, `TableMoveHandlesOverlappingRanges`, `TableMoveDefaultsDestinationToSource`

1. **[LuaRandomParityTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs)**

   - Major refactoring: Consolidated 34 tests into proper version-aware structure
   - Unit tests for `Lua51RandomProvider` (15 tests) - no version needed
   - Version-specific tests using `[LuaVersionsFrom(Lua54)]`, `[LuaVersionsUntil(Lua53)]`, `[AllLuaVersions]`
   - Combined duplicate tests like `ScriptWithLua54UsesLuaRandomProvider`/`ScriptWithLua55UsesLuaRandomProvider` into single `ScriptUsesLuaRandomProviderInLua54Plus`

1. **[LuaUtf8MultiVersionSpecTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaUtf8MultiVersionSpecTUnitTests.cs)**

   - Converted 6 tests from foreach loops to proper attributes
   - Tests: `Utf8LibraryIsUnavailableBeforeLua53`, `Utf8LenCountsCharactersAndFlagsInvalidSequences`, `Utf8CodePointDecodesRequestedSlice`, etc.

### EndToEnd Tests

5. **[BinaryDumpTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/BinaryDumpTUnitTests.cs)**
   - Converted 12 tests to use `[AllLuaVersions]`
   - Added version parameter to helper methods `ScriptRunString()` and `ScriptLoadFunc()`
   - Tests: `BinDumpChunkDump`, `BinDumpStringDump`, `BinDumpStandardDumpFunc`, `BinDumpFactorialDumpFunc`, etc.

### Loader Tests

6. **[ReplInterpreterScriptLoaderTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Loaders/ReplInterpreterScriptLoaderTUnitTests.cs)**
   - Converted 4 tests that create `Table` with `new Script()` to use `[AllLuaVersions]`
   - Tests: `ResolveModuleNameUsesLuaPathGlobalWhenPresent`, `ResolveModuleNameFallsBackToModulePathsWhenLuaPathMissing`, `ResolveModuleNameIgnoresNonStringLuaPathGlobal`, `ResolveModuleNameReturnsNullWhenLuaPathCannotResolve`

### Descriptor Tests

7. **[ArrayMemberDescriptorTUnitTests.cs](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs)**
   - Converted 4 tests that use `Script script = new()` to use `[AllLuaVersions]`
   - Tests: `GetterReturnsArrayElement`, `SetterModifiesArrayElement`, `MultiDimensionalArrayAccess`, `MultiDimensionalArraySet`

## Key Patterns Used

### 1. Version-specific feature availability

```csharp
// Positive: Feature available in Lua 5.3+
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task FeatureWorksInLua53Plus(LuaCompatibilityVersion version) { ... }

// Negative: Feature unavailable before Lua 5.3
[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
public async Task FeatureIsNilBeforeLua53(LuaCompatibilityVersion version) { ... }
```

### 2. Universal tests

```csharp
[Test]
[AllLuaVersions]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version) { ... }
```

### 3. Helper method pattern for version-aware Script creation

```csharp
private static DynValue ScriptRunString(string script, LuaCompatibilityVersion version)
{
    Script s1 = new(version);
    // ... test logic
}
```

## Remaining Work

222 Lua execution tests still need version coverage:

- **EventMemberDescriptorTUnitTests**: 11 tests
- **BinaryOperatorExpressionTUnitTests**: 19 tests
- **HardwiredDescriptorsTUnitTests**: 6 tests
- **ParserTUnitTests**: 5 tests
- **BreakStatementTUnitTests**: 5 tests
- Many others scattered across Units, Interop, and EndToEnd folders

## Test Results

All migrated tests pass:

- Spec tests: 180 passed
- BinaryDump tests: 60 passed (12 tests × 5 versions)
- ArrayMemberDescriptor tests: 20 passed (4 Lua tests × 5 versions + 6 infrastructure tests)
- ReplInterpreterScriptLoader tests: 20 passed (4 Lua tests × 5 versions)
- Total test count increased from ~10,904 to ~11,033 (indicating version expansion working)

Note: 6 pre-existing test failures (unrelated to this migration):

- `LocalAssignmentAcceptsConstAndCloseAttributes` - needs Lua 5.4+ version attribute
- `LocalAssignmentRejectsDuplicateAttributes` - needs Lua 5.4+ version attribute
- Plus 4 other unrelated failures

## References

- PLAN.md §8.42: Test Data-Driving Helper Migration
- [LuaVersionDataSourceAttributes.cs](../src/tests/TestInfrastructure/TUnit/LuaVersionDataSourceAttributes.cs): Helper attribute definitions
- Session 043 progress file for context on previous work
