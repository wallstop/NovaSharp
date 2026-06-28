______________________________________________________________________

triggers:

- "test coverage"
- "write tests"
- "test cases"
- "edge cases"
- "test patterns"
  category: testing
  related:
- tunit-test-writing
- lua-fixture-creation
- test-failure-investigation
  priority: core

______________________________________________________________________

# Skill: Exhaustive Test Coverage

**When to use**: Writing tests for any new feature or bug fix.

**Code Samples**: [test-patterns](../code-samples/test-patterns.md)

**Related Skills**: [tunit-test-writing](tunit-test-writing.md), [lua-fixture-creation](lua-fixture-creation.md), [test-failure-investigation](test-failure-investigation.md)

______________________________________________________________________

## Philosophy: Test Everything, Trust Nothing

Every feature and bug fix requires **exhaustive testing**. Tests are documentation, specification, and regression prevention.

### Coverage Goals

- **Normal cases** - The happy path works
- **Edge cases** - Boundaries, limits, unusual inputs
- **Error cases** - Invalid inputs, exceptional conditions
- **Negative cases** - What SHOULDN'T work doesn't
- **Version-specific** - Behavior differences across Lua versions
- **"The Impossible"** - Scenarios that "can't happen" (they will)

______________________________________________________________________

## Test Categories

### 1. Normal/Happy Path

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorReturnsCorrectValue(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.floor(3.7)");
    await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
}
```

### 2. Edge Cases

```csharp
[Test]
[AllLuaVersions]
[Arguments(0.0)]
[Arguments(-0.0)]
[Arguments(double.MaxValue)]
[Arguments(double.MinValue)]
[Arguments(double.Epsilon)]
public async Task MathFloorHandlesEdgeCases(LuaCompatibilityVersion version, double input)
{
    Script script = CreateScript(version);
    double expected = Math.Floor(input);
    DynValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
}
```

### 3. Error Cases

```csharp
[Test]
[AllLuaVersions]
[Arguments("nil")]
[Arguments("'hello'")]
[Arguments("{}")]
[Arguments("true")]
public async Task MathFloorThrowsOnInvalidTypes(LuaCompatibilityVersion version, string arg)
{
    Script script = CreateScript(version);
    await Assert.ThrowsAsync<ScriptRuntimeException>(
        () => Task.FromResult(script.DoString($"return math.floor({arg})"))
    ).ConfigureAwait(false);
}
```

### 4. Negative Tests (Verify Absence)

```csharp
// Feature should NOT be available in older versions
[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
public async Task MathTypeDoesNotExistInLua52AndEarlier(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type");
    await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
}
```

### 5. Special Value Tests

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorHandlesInfinity(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue posInf = script.DoString("return math.floor(math.huge)");
    await Assert.That(double.IsPositiveInfinity(posInf.Number)).IsTrue().ConfigureAwait(false);
}
```

______________________________________________________________________

## Data-Driven Testing

### Using `[Arguments]`

```csharp
[Test]
[AllLuaVersions]
[Arguments(0, 0)]
[Arguments(1.5, 1)]
[Arguments(-1.5, -2)]
public async Task MathFloorReturnsExpectedResults(
    LuaCompatibilityVersion version, double input, double expected)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
}
```

### Using `[MethodDataSource]`

```csharp
public static IEnumerable<(double input, double expected)> FloorTestData()
{
    yield return (0, 0);
    yield return (1.5, 1);
    yield return (-1.5, -2);

    // Generated cases
    for (int i = -100; i <= 100; i++)
    {
        double value = i * 0.1;
        yield return (value, Math.Floor(value));
    }
}

[Test]
[AllLuaVersions]
[MethodDataSource(nameof(FloorTestData))]
public async Task MathFloorMatchesDotNet(
    LuaCompatibilityVersion version,
    (double input, double expected) testCase)
{
    // ...
}
```

See [test-patterns](../code-samples/test-patterns.md) for more examples.

______________________________________________________________________

## Test Input Checklists

### Numeric Inputs

- [ ] Zero (0), Negative zero (-0.0)
- [ ] Positive/negative integers
- [ ] Positive/negative decimals
- [ ] Very small (double.Epsilon, 1e-308)
- [ ] Very large (1e308, double.MaxValue)
- [ ] Special values (Infinity, -Infinity, NaN)

### String Inputs

- [ ] Empty string ("")
- [ ] Single character ("a")
- [ ] Normal strings
- [ ] Very long strings (10,000+ chars)
- [ ] Unicode characters
- [ ] Control characters, escape sequences

### Table/Collection Inputs

- [ ] Empty table ({})
- [ ] Array-like, dictionary-like, mixed
- [ ] Nested tables, circular references
- [ ] Tables with nil holes, metatables

______________________________________________________________________

## Test Naming Conventions

| Pattern                              | Use Case                |
| ------------------------------------ | ----------------------- |
| `FeatureWorksCorrectly`              | Happy path              |
| `FeatureHandlesEdgeCase`             | Specific edge case      |
| `FeatureThrowsOnInvalidInput`        | Error handling          |
| `FeatureDoesNotExistInOlderVersions` | Negative version test   |
| `FeatureMatchesReferenceLuaBehavior` | Compliance verification |

______________________________________________________________________

## Test Completeness Checklist

### Coverage Categories

- [ ] Normal/happy path cases covered
- [ ] Edge cases at boundaries covered
- [ ] Error cases with invalid inputs covered
- [ ] Negative tests (what shouldn't work) covered
- [ ] Special values (infinity, NaN, empty, null) covered
- [ ] Version-specific behavior (5.1 through 5.5) covered

### Lua Compliance Verification

- [ ] **All expected values verified against reference Lua** (`lua5.X -e "..."`)
- [ ] Output format matches reference Lua **exactly**
- [ ] Error messages compared with reference Lua
- [ ] Created `.lua` fixture files runnable by reference Lua

### Quality Checks

- [ ] All tests pass locally
- [ ] Tests are deterministic (no flakiness)
- [ ] Test names clearly describe what's being tested
- [ ] Tests verify behavior, not implementation
