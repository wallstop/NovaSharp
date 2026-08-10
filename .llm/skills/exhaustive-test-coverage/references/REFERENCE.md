# Exhaustive Test Coverage Reference

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
    LuaValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.AsNumber()).IsEqualTo(expected).ConfigureAwait(false);
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

See [test-patterns](../../../code-samples/test-patterns.md) for more examples.

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
