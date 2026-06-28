# TUnit Test Multi-Version Coverage Audit - Phase 2 Progress (StringModuleTUnitTests)

**Date**: 2025-12-15
**PLAN.md Section**: §8.39
**Status**: Phase 2 In Progress - Remediation Started

## Summary

Began Phase 2 remediation of the TUnit Test Multi-Version Coverage Audit by updating `StringModuleTUnitTests.cs` with `[Arguments(LuaCompatibilityVersion.*)]` attributes to ensure tests run against all supported Lua versions (5.1, 5.2, 5.3, 5.4, 5.5).

## What Was Done

### Tests Updated in StringModuleTUnitTests.cs

Updated the following universal behavior tests with version arguments for all 5 Lua versions:

**Character Functions (string.char)**:

- `CharProducesStringFromByteValues`
- `CharThrowsWhenArgumentCannotBeCoerced`
- `CharReturnsNullByteForZero`
- `CharReturnsMaxByteValue`
- `CharReturnsEmptyStringWhenNoArgumentsProvided`
- `CharErrorsOnValuesOutsideByteRange`
- `CharAcceptsIntegralFloatValues`
- `CharErrorsOnOutOfRangeValue` (with combined version + value arguments)
- `CharAcceptsBoundaryValues` (with combined version + value arguments)
- `CharAcceptsNumericStringArguments`

**Basic String Functions**:

- `LenReturnsStringLength`
- `LowerReturnsLowercaseString`
- `UpperReturnsUppercaseString`

**Byte Functions (string.byte)**:

- `ByteReturnsByteCodesForSubstring`
- `ByteDefaultsToFirstCharacter`
- `ByteSupportsNegativeIndices`
- `ByteReturnsNilWhenIndexPastEnd`
- `ByteReturnsNilWhenStartExceedsEnd`
- `ByteReturnsNilForEmptySource`
- `ByteAcceptsIntegralFloatIndices`

**Pattern Matching Functions**:

- `UnicodeReturnsFullUnicodeCodePoints`
- `FindReturnsMatchBoundaries`
- `MatchReturnsFirstCapture`
- `ReverseReturnsEmptyStringForEmptyInput`
- `GSubAppliesGlobalReplacement`
- `GMatchIteratesOverMatches`

**Format Functions**:

- `FormatInterpolatesValues`
- `FormatOctalBasic`
- `FormatOctalWithAlternateFlag`
- `FormatOctalAlternateFlagWithZero`
- `FormatOctalWithFieldWidth`
- `FormatOctalWithZeroPadding`
- `FormatOctalWithLeftAlign`
- `FormatOctalWithLeftAlignAndAlternate`
- `FormatOctalZeroPaddingWithAlternate`
- `FormatUnsignedBasic`
- `FormatUnsignedWithFieldWidth`
- `FormatUnsignedWithZeroPadding`

**Other Functions**:

- `SubHandlesNegativeIndices`
- `StartsWithEndsWithContainsTreatNilAsFalse`
- `StartsWithEndsWithContainsReturnTrueWhenMatchesPresent`
- `DumpPrependsNovaSharpBase64Header`

### Version-Specific Behavior Discovered

During remediation, discovered that **`string.rep` with separator parameter** behaves differently across versions:

**Lua 5.1**: Third argument (separator) is ignored

- `string.rep('ab', 3, '-')` → `"ababab"`

**Lua 5.2+**: Third argument specifies separator

- `string.rep('ab', 3, '-')` → `"ab-ab-ab"`

Updated the test to have version-specific expectations:

- `RepSupportsSeparatorsLua52Plus` - Tests 5.2, 5.3, 5.4, 5.5
- `RepIgnoresSeparatorInLua51` - Tests 5.1
- `RepSupportsZeroCount` - Universal test for all versions

### Test Methodology

For each universal behavior test:

1. Added `[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]`
1. Added `[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]`
1. Added `[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]`
1. Added `[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]`
1. Added `[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]`
1. Added `LuaCompatibilityVersion version` parameter to method
1. Changed `CreateScript()` to `CreateScriptWithVersion(version)`

For tests with existing `[Arguments]` attributes (e.g., data-driven tests with value parameters):

- Added version as first argument
- Combined with existing arguments using cross-product

## Remaining Work in StringModuleTUnitTests.cs

Additional tests still need version coverage in this file:

- Hex format tests (`FormatHexBasic`, etc.)
- Integer format tests (`FormatIntegerWithPositiveSign`, etc.)
- Various string.format edge case tests

## Overall Progress

| Metric                               | Before | After  |
| ------------------------------------ | ------ | ------ |
| Total tests needing version coverage | 1,107  | ~1,060 |
| StringModuleTUnitTests tests updated | 0      | ~47    |
| Compliance percentage (overall)      | 1.28%  | ~5%    |

## Files Modified

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs`

## Verification

All tests pass after remediation:

```
Test run summary: Passed!
  total: 5460
  failed: 0
  succeeded: 5460
  skipped: 0
```

## Next Steps

1. Continue updating remaining tests in `StringModuleTUnitTests.cs`
1. Move to other high-priority module test files:
   - `BasicModuleTUnitTests.cs` (partially covered)
   - `TableModuleTUnitTests.cs` (partially covered)
   - `MathModuleTUnitTests.cs`
   - `IoModuleTUnitTests.cs`
1. Update `Units/` directory tests
1. Update `EndToEnd/` directory tests

## Pattern for Version-Parameterized Tests

### Universal Behavior (works same in all versions)

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // test code
}
```

### Version-Specific Behavior (positive test)

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
public async Task FeatureAvailableInLua52Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // test code
}
```

### Version-Specific Behavior (negative test)

```csharp
[global::TUnit.Core.Test]
[global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
public async Task FeatureNotAvailableInLua51(LuaCompatibilityVersion version)
{
    Script script = CreateScriptWithVersion(version);
    // test code demonstrating different behavior
}
```

## Related Documents

- [Session 025 - TUnit Version Coverage Audit Phase 1](session-025-tunit-version-coverage-audit-phase1.md) - Phase 1 audit results
- [`PLAN.md` §8.39](../PLAN.md#--high-priority-tunit-test-multi-version-coverage-audit-839) - Overall tracking
- [`.llm/context.md`](../.llm/context.md) - Multi-version testing guidelines
