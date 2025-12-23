# TUnit Multi-Version Coverage Audit - Batch 1 & 2 Progress

**Date**: 2025-12-15
**Tracking**: PLAN.md §8.39 - TUnit Test Multi-Version Coverage Audit

## Summary

This session added explicit `LuaCompatibilityVersion` arguments to ~70 Lua execution tests across 10 test files in the EndToEnd test suite.

## Files Updated - Batch 1

### 1. `ClosureTUnitTests.cs` (11 tests → 55 test cases)

- **Tests Updated**: 11 tests
- **Version Coverage**: All tests now run against Lua 5.1, 5.2, 5.3, 5.4, 5.5
- **Rationale**: Closures are fundamental Lua features that work identically across all versions
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, coreModules)`

### 2. `GotoTUnitTests.cs` (11 tests → 44 test cases)

- **Tests Updated**: 11 tests
- **Version Coverage**: Lua 5.2, 5.3, 5.4, 5.5 only
- **Rationale**: `goto` was introduced in Lua 5.2; not available in 5.1
- **Added Helpers**: `CreateScript(version)`

### 3. `CoroutineTUnitTests.cs` (7 tests → 35-37 test cases)

- **Tests Updated**: 7 tests
- **Version Coverage**:
  - Most tests: All 5 Lua versions
  - `CoroutineVariousErrorHandlingMatchesNunitSuite`: Lua 5.4, 5.5 only
- **Rationale**: Coroutines are fundamental; one test has CLR-boundary detection behavior that requires 5.4+ print/\_\_tostring semantics
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, coreModules)`

### 4. `ErrorHandlingTUnitTests.cs` (4 tests → 20 test cases)

- **Tests Updated**: 4 tests
- **Version Coverage**: All 5 Lua versions
- **Rationale**: Error handling (`pcall`) behavior is consistent across versions
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, coreModules)`

### 5. `TableTUnitTests.cs` (17 tests → 78 test cases)

- **Tests Updated**: 17 Lua-execution tests
- **Version Coverage**:
  - Most tests: All 5 Lua versions
  - `TableUnpackReturnsTuple`: Lua 5.2+ only (`table.unpack` moved from global `unpack` in 5.2)
- **Infrastructure Test**: `TableLengthCalculationsMirrorNunit` left unchanged (tests C# `Table` class, not Lua execution)
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, coreModules)`

## Files Updated - Batch 2

### 6. `MetatableTUnitTests.cs` (10 tests → ~46 test cases)

- **Tests Updated**: 10 tests
- **Version Coverage**:
  - Most tests: All 5 Lua versions
  - `TableIPairsWithMetatable`: Lua 5.2 only (`__ipairs` metamethod was added in 5.2 and deprecated in 5.3+)
- **Rationale**: Metatables work across all versions; `__ipairs` is version-specific
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, modules)`

### 7. `VarargsTupleTUnitTests.cs` (5 tests → 25 test cases)

- **Tests Updated**: 5 tests
- **Version Coverage**: All 5 Lua versions
- **Rationale**: Varargs (`...`) work identically across all Lua versions
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, modules)`

### 8. `DynamicTUnitTests.cs` (5 tests → 24 test cases)

- **Tests Updated**: 5 tests
- **Version Coverage**:
  - Most tests: All 5 Lua versions
  - `DynamicAccessScopeSecurityReturnsNil`: Lua 5.2+ only (`_ENV` is a 5.2+ feature)
- **Rationale**: `dynamic` module is NovaSharp-specific; `_ENV` is version-specific
- **Added Helpers**: `CreateScript(version)`

### 9. `StringLibTUnitTests.cs` (20 tests → ~95 test cases)

- **Tests Updated**: 20 tests
- **Version Coverage**:
  - Most tests: All 5 Lua versions
  - `PrintInvokesToStringMetamethods`: Lua 5.4, 5.5 only (print/\_\_tostring CLR boundary behavior differs in pre-5.4)
- **Rationale**: String library is mostly consistent; print/\_\_tostring metamethod invocation has version-specific CLR boundary detection
- **Added Helpers**: `CreateScript(version)` and `CreateScript(version, modules)`

### 10. `RealWorldScriptTUnitTests.cs` (2 tests → 10 test cases)

- **Tests Updated**: 2 tests
- **Version Coverage**: All 5 Lua versions
- **Rationale**: Real-world Lua libraries (json.lua, inspect.lua) work across all versions
- **Added Helpers**: `CreateScript(version)`

## Metrics

### Before Session

- Compliant tests: 778
- Non-compliant Lua tests: 820
- Compliance: ~21.76%

### After Batch 1

- Compliant tests: 827 (+49)
- Non-compliant Lua tests: 772 (-48)
- Compliance: ~23.13%

### After Batch 2

- Compliant tests: 852 (+25)
- Non-compliant Lua tests: 754 (-18)
- Compliance: ~23.83%

### Total Progress

- Tests with version coverage: +74
- Non-compliant Lua tests reduced: -66

### Version Coverage Growth

| Version | Before | After Batch 1 | After Batch 2 |
| ------- | ------ | ------------- | ------------- |
| Lua51   | 884    | 934           | 966           |
| Lua52   | 914    | 980           | 1,014         |
| Lua53   | 1,109  | 1,176         | 1,211         |
| Lua54   | 1,127  | 1,202         | 1,238         |
| Lua55   | 1,143  | 1,220         | 1,260         |

## Test Results

All tests pass: **7,894 passed, 0 failed**

## Key Findings

### Version-Specific Behaviors Discovered

1. **`__ipairs` metamethod**: Added in Lua 5.2, deprecated/removed in 5.3+
1. **`_ENV` variable**: Introduced in Lua 5.2 for explicit environment control
1. **`table.unpack`**: Moved from global `unpack` to `table.unpack` in Lua 5.2
1. **`goto`**: Introduced in Lua 5.2
1. **print/\_\_tostring CLR boundary**: Different behavior in pre-5.4 vs 5.4+ when calling `__tostring` metamethod from `print`

### Pattern for CreateScript Helper

**IMPORTANT**: Use `new ScriptOptions(Script.DefaultOptions)` as base, NOT `new ScriptOptions()`:

```csharp
private static Script CreateScript(LuaCompatibilityVersion version)
{
    ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
    {
        CompatibilityVersion = version,
    };
    return new Script(CoreModulePresets.Complete, options);
}
```

This ensures all default options (including necessary configuration) are preserved.

## Next Steps

- Continue with remaining EndToEnd test files
- Move to Modules/ test files
- Target: Reduce non-compliant Lua tests to \<500
  ```csharp
  Script script = CreateScript(version);
  DynValue result = script.DoString(code);
  ```

## Next Steps

~772 Lua execution tests still need version coverage. Priority areas:

- `EndToEnd/` directory: Many more files with similar patterns
- `Modules/` directory: Module-specific tests that need appropriate version gating
- `Units/` directory: Unit tests that execute Lua code

## Version-Specific Feature Reference

| Feature                                     | First Available |
| ------------------------------------------- | --------------- |
| `goto` statement                            | Lua 5.2         |
| `table.unpack` (instead of global `unpack`) | Lua 5.2         |
| `table.pack`                                | Lua 5.2         |
| `table.move`                                | Lua 5.3         |
| `math.type`                                 | Lua 5.3         |
| `math.tointeger`                            | Lua 5.3         |
| `math.ult`                                  | Lua 5.3         |
| Integer division `//`                       | Lua 5.3         |
| Bitwise operators                           | Lua 5.3         |
| `utf8` library                              | Lua 5.3         |
| `coroutine.isyieldable`                     | Lua 5.3         |
| `coroutine.close`                           | Lua 5.4         |
| `io.lines` 4-value return                   | Lua 5.4         |
