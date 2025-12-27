# TUnit Test Multi-Version Coverage Bulk Remediation

**Date**: 2025-12-15
**Status**: ✅ Significant progress made
**Related**: PLAN.md §8.39

## Summary

Performed bulk remediation of TUnit tests to add `[Arguments(LuaCompatibilityVersion.LuaXX)]` attributes, enabling multi-version test coverage across Lua 5.1, 5.2, 5.3, 5.4, and 5.5.

## Results

| Metric                  | Before | After | Change |
| ----------------------- | ------ | ----- | ------ |
| Compliant tests         | 852    | 1,341 | +489   |
| Non-compliant Lua tests | 754    | 465   | -289   |
| Compliance %            | 23.8%  | 37.6% | +13.8% |

## Files Transformed

The following test files were successfully transformed to add version coverage:

### Spec Tests

- `Lua55SpecTUnitTests.cs` - 37 tests, Lua 5.5 specific
- `ScriptConstructorConsistencyTUnitTests.cs` - 14 tests, all versions
- `LuaBasicMultiVersionSpecTUnitTests.cs` - 17 tests, all versions

### Execution Tests

- `ScriptCallTUnitTests.cs` (both paths) - ~34 tests, all versions
- `ScriptLoadTUnitTests.cs` - 15 tests, all versions
- `ScriptOptionsTUnitTests.cs` - ~10 tests, all versions
- `ProcessorCoroutineApiTUnitTests.cs` - 20 tests, all versions
- `ProcessorCoroutineModuleTUnitTests.cs` - 14 tests, all versions
- `ProcessorCoroutineCloseTUnitTests.cs` - 12 tests, all versions
- `CoroutineLifecycleIntegrationTUnitTests.cs` - 11 tests, all versions
- `ByteCodeTUnitTests.cs` - 19 tests, all versions
- `BitwiseOperatorTUnitTests.cs` - 10 tests, all versions

### DataTypes Tests

- `TableTUnitTests.cs` - 25 tests, all versions
- `CoroutineLifecycleTUnitTests.cs` - 11 tests, all versions

### Descriptors Tests

- `OverloadedMethodMemberDescriptorTUnitTests.cs` - 18 tests, all versions
- `PropertyMemberDescriptorTUnitTests.cs` - 14 tests, all versions
- `FieldMemberDescriptorTUnitTests.cs` - 14 tests, all versions
- `DispatchingUserDataDescriptorTUnitTests.cs` - 12 tests, all versions

### Sandbox Tests

- `SandboxMemoryLimitTUnitTests.cs` - 22 tests, all versions

### CLI Tests

- `ReplInterpreterTUnitTests.cs` - 13 tests, all versions

### Expression Tests

- `BinaryOperatorExpressionTUnitTests.cs` - 22 tests, all versions
- `UnaryOperatorExpressionTUnitTests.cs` - 2 tests, all versions
- `DynamicExpressionTUnitTests.cs` - 4 tests, all versions

## Transformation Approach

1. Created Python script (`/tmp/transform_tests.py`) to:

   - Add `[Arguments(LuaCompatibilityVersion.Lua51/52/53/54/55)]` attributes after `[Test]`
   - Add version parameter to test method signatures
   - Replace `new Script(CoreModulePresets.Complete)` with `new Script(version, CoreModulePresets.Complete)`
   - Replace `new Script()` with `new Script(version)`

1. Created supplementary script (`/tmp/fix_usings.py`) to add missing `using WallstopStudios.NovaSharp.Interpreter.Compatibility;` statements.

1. Handled both `[Test]` and `[global::TUnit.Core.Test]` attribute formats.

## Test Results

After transformation:

- **9,642 tests passed** (99.76%)
- **23 tests failed** (0.24%)

The failing tests are expected - they reveal version-specific behavior differences that were previously hidden. For example:

- `coroutine.running()` returns `nil` in Lua 5.1 but returns `(thread, boolean)` in 5.2+
- Some tests expected 5.2+ behavior but are now also running against 5.1

## Remaining Work

1. **465 Lua tests still need version coverage** - These include:

   - Tests with existing parameters that need careful merging
   - Tests using helper methods that abstract Script creation
   - Infrastructure tests that may not need version coverage

1. **23 failing tests need version-specific fixes**:

   - `ProcessorCoroutineModuleTUnitTests.RunningFromMainReturnsMainCoroutine` - should be 5.2+ only
   - `ProcessorCoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse` - should be 5.2+ only
   - `ProcessorCoroutineModuleTUnitTests.ResumeFlattensNestedTupleResults` - version-specific tuple handling
   - And similar version-dependent tests

1. **Files not transformed** (use helper methods):

   - `IoStdHandleUserDataTUnitTests.cs` - uses `CreateScript()` helper
   - `DebugModuleTapParityTUnitTests.cs` - uses helper method
   - `ErrorHandlingModuleTUnitTests.cs` - uses helper method
   - These require manual refactoring

## Next Steps

1. Fix the 23 failing tests by restricting their version coverage appropriately
1. Manually refactor tests using `CreateScript()` helper methods
1. Add negative tests for version-specific features (e.g., test that `coroutine.running()` returns `nil` in 5.1)
1. Continue transforming remaining 465 non-compliant Lua tests
1. Add CI lint rule to prevent regression

## Commands Used

```bash
# Run audit
python3 scripts/lint/check-tunit-version-coverage.py --lua-only

# Build verification
dotnet build src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release

# Run tests
dotnet test src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj -c Release --no-build
```
