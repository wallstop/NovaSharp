# Skill: Exhaustive Test Coverage

**When to use**: Writing tests for any new feature or bug fix.

**Related Skills**: [tunit-test-writing](tunit-test-writing.md) (TUnit framework specifics), [lua-fixture-creation](lua-fixture-creation.md) (Lua test fixtures), [test-failure-investigation](test-failure-investigation.md) (debugging failures)

______________________________________________________________________

## 🔴 Philosophy: Test Everything, Trust Nothing

Every feature and bug fix requires **exhaustive testing**. Tests are not just verification — they are documentation, specification, and regression prevention. Skimp on tests, and you'll pay later with bugs.

### Test Coverage Goals

- **Normal cases** — The happy path works
- **Edge cases** — Boundaries, limits, and unusual inputs
- **Error cases** — Invalid inputs, exceptional conditions
- **Negative cases** — What SHOULDN'T work doesn't
- **Version-specific** — Behavior differences across Lua versions
- **"The Impossible"** — Scenarios that "can't happen" (they will)

______________________________________________________________________

## 🔴 Test Categories

### 1. Normal/Happy Path Tests

The expected use case works correctly.

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorReturnsCorrectValueForPositiveNumbers(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.floor(3.7)");
    await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
}
```

### 2. Edge Case Tests

Boundaries, limits, and unusual-but-valid inputs.

```csharp
[Test]
[AllLuaVersions]
[Arguments(0.0)]                    // Zero
[Arguments(-0.0)]                   // Negative zero
[Arguments(double.MaxValue)]        // Maximum value
[Arguments(double.MinValue)]        // Minimum value  
[Arguments(double.Epsilon)]         // Smallest positive value
[Arguments(1e308)]                  // Very large
[Arguments(1e-308)]                 // Very small
[Arguments(0.5)]                    // Exact midpoint
[Arguments(-0.5)]                   // Negative midpoint
[Arguments(0.99999999999999)]       // Near boundary
[Arguments(-0.00000000000001)]      // Near zero negative
public async Task MathFloorHandlesEdgeCases(LuaCompatibilityVersion version, double input)
{
    Script script = CreateScript(version);
    double expected = Math.Floor(input);
    DynValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
}
```

### 3. Error/Invalid Input Tests

Ensure proper behavior with invalid inputs.

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorThrowsOnNilArgument(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    ScriptRuntimeException exception = await Assert.ThrowsAsync<ScriptRuntimeException>(
        () => Task.FromResult(script.DoString("return math.floor(nil)"))
    ).ConfigureAwait(false);
    await Assert.That(exception.Message).Contains("number expected").ConfigureAwait(false);
}

[Test]
[AllLuaVersions]
[Arguments("'hello'")]      // String
[Arguments("{}")]           // Table
[Arguments("function() end")] // Function
[Arguments("true")]         // Boolean
public async Task MathFloorThrowsOnInvalidTypes(LuaCompatibilityVersion version, string invalidArg)
{
    Script script = CreateScript(version);
    await Assert.ThrowsAsync<ScriptRuntimeException>(
        () => Task.FromResult(script.DoString($"return math.floor({invalidArg})"))
    ).ConfigureAwait(false);
}
```

### 4. Negative Tests (Verify Absence)

Confirm that behavior that SHOULDN'T exist doesn't.

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

IEEE 754 special values and Lua-specific behaviors.

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorHandlesInfinity(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    
    DynValue posInf = script.DoString("return math.floor(math.huge)");
    await Assert.That(double.IsPositiveInfinity(posInf.Number)).IsTrue().ConfigureAwait(false);
    
    DynValue negInf = script.DoString("return math.floor(-math.huge)");
    await Assert.That(double.IsNegativeInfinity(negInf.Number)).IsTrue().ConfigureAwait(false);
}

[Test]
[AllLuaVersions]
public async Task MathFloorHandlesNaN(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.floor(0/0)");
    await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
}
```

### 6. "The Impossible" Tests

Scenarios that "can't happen" — but inevitably do.

```csharp
[Test]
[AllLuaVersions]
public async Task TableIterationSurvivesModificationDuringIteration(LuaCompatibilityVersion version)
{
    // "Users would never do this" — they will
    Script script = CreateScript(version);
    string code = @"
        local t = {a=1, b=2, c=3}
        local count = 0
        for k, v in pairs(t) do
            t['new_' .. k] = v * 2  -- Modify during iteration
            count = count + 1
            if count > 10 then break end  -- Prevent infinite loop
        end
        return count
    ";
    // Behavior varies by version; ensure no crash
    DynValue result = script.DoString(code);
    await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
}

[Test]
[AllLuaVersions]
public async Task DeeplyNestedFunctionCallsDoNotStackOverflow(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    script.Options.StackLimit = 1000;  // Reasonable limit
    string code = @"
        local function recurse(n)
            if n <= 0 then return 0 end
            return recurse(n - 1) + 1
        end
        return recurse(500)  -- Deep but within limit
    ";
    DynValue result = script.DoString(code);
    await Assert.That(result.Number).IsEqualTo(500).ConfigureAwait(false);
}
```

______________________________________________________________________

## 🔴 Data-Driven Testing

Use data-driven tests to maximize coverage with minimal code duplication.

### Using `[Arguments]` Attribute

```csharp
[Test]
[AllLuaVersions]
[Arguments(0, 0)]
[Arguments(1, 1)]
[Arguments(2, 2)]
[Arguments(3.5, 3)]
[Arguments(3.9999, 3)]
[Arguments(-0.1, -1)]
[Arguments(-1, -1)]
[Arguments(-1.5, -2)]
[Arguments(-1.9999, -2)]
public async Task MathFloorReturnsExpectedResults(
    LuaCompatibilityVersion version, 
    double input, 
    double expected)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
}
```

### Using `[MethodDataSource]` for Complex Data

```csharp
public static IEnumerable<(double input, double expected)> FloorTestData()
{
    // Normal cases
    yield return (0, 0);
    yield return (1.5, 1);
    yield return (-1.5, -2);
    
    // Edge cases
    yield return (double.MaxValue, double.MaxValue);
    yield return (double.MinValue, double.MinValue);
    yield return (double.Epsilon, 0);
    
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
public async Task MathFloorMatchesDotNetBehavior(
    LuaCompatibilityVersion version,
    (double input, double expected) testCase)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString($"return math.floor({testCase.input:R})");
    await Assert.That(result.Number).IsEqualTo(testCase.expected).ConfigureAwait(false);
}
```

### Using `[LuaTestMatrix]` for Version × Input Combinations

```csharp
// Automatically creates: 5 versions × 3 inputs = 15 test cases
[Test]
[LuaTestMatrix("hello", "world", "")]
public async Task StringLenWorksAcrossVersionsAndInputs(
    LuaCompatibilityVersion version,
    string input)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString($"return string.len('{input}')");
    await Assert.That(result.Number).IsEqualTo(input.Length).ConfigureAwait(false);
}
```

______________________________________________________________________

## 🔴 Test Organization

### Test Class Structure

```csharp
[UserDataIsolation]
[ScriptGlobalOptionsIsolation]
public sealed class MathModuleTUnitTests
{
    // Group 1: Normal/happy path tests
    [Test]
    [AllLuaVersions]
    public async Task FloorReturnsCorrectValueForPositiveNumbers(LuaCompatibilityVersion v) { }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorReturnsCorrectValueForNegativeNumbers(LuaCompatibilityVersion v) { }
    
    // Group 2: Edge cases
    [Test]
    [AllLuaVersions]
    [Arguments(/* edge case values */)]
    public async Task FloorHandlesEdgeCases(LuaCompatibilityVersion v, double input) { }
    
    // Group 3: Error cases
    [Test]
    [AllLuaVersions]
    public async Task FloorThrowsOnInvalidInput(LuaCompatibilityVersion v) { }
    
    // Group 4: Version-specific behavior
    [Test]
    [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
    public async Task FloorReturnsIntegerInLua53Plus(LuaCompatibilityVersion v) { }
    
    [Test]
    [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
    public async Task FloorReturnsFloatInLua52AndEarlier(LuaCompatibilityVersion v) { }
}
```

### Naming Conventions

| Pattern                              | Use Case                   |
| ------------------------------------ | -------------------------- |
| `FeatureWorksCorrectly`              | Happy path                 |
| `FeatureHandlesEdgeCase`             | Specific edge case         |
| `FeatureThrowsOnInvalidInput`        | Error handling             |
| `FeatureDoesNotExistInOlderVersions` | Negative version test      |
| `FeatureMatchesReferenceLuaBehavior` | Compliance verification    |
| `FeaturePerformsWithinLimits`        | Performance/resource tests |

______________________________________________________________________

## 🔴 Test Input Categories

### Numeric Input Checklist

- [ ] Zero (0)
- [ ] Positive integers (1, 2, 100, int.MaxValue)
- [ ] Negative integers (-1, -2, -100, int.MinValue)
- [ ] Positive decimals (0.5, 1.5, 99.99)
- [ ] Negative decimals (-0.5, -1.5, -99.99)
- [ ] Very small (double.Epsilon, 1e-308)
- [ ] Very large (1e308, double.MaxValue)
- [ ] Special values (Infinity, -Infinity, NaN)
- [ ] Negative zero (-0.0)
- [ ] Subnormal numbers

### String Input Checklist

- [ ] Empty string ("")
- [ ] Single character ("a")
- [ ] Normal string ("hello world")
- [ ] Very long string (10,000+ chars)
- [ ] Unicode characters ("こんにちは", "🎉")
- [ ] Control characters ("\\n", "\\t", "\\0")
- [ ] Escape sequences
- [ ] Binary data (invalid UTF-8)
- [ ] Null bytes embedded

### Table/Collection Input Checklist

- [ ] Empty table ({})
- [ ] Array-like ({1, 2, 3})
- [ ] Dictionary-like ({a=1, b=2})
- [ ] Mixed ({1, 2, a=3})
- [ ] Nested tables
- [ ] Circular references
- [ ] Very large tables (10,000+ entries)
- [ ] Tables with nil holes
- [ ] Tables with metatable

### Function Input Checklist

- [ ] Regular function
- [ ] Closure
- [ ] C# callback
- [ ] Coroutine
- [ ] Variadic function
- [ ] Function returning multiple values
- [ ] Recursive function
- [ ] Function with upvalues

______________________________________________________________________

## 🔴 Comprehensive Test Example

Here's a complete example demonstrating exhaustive testing:

```csharp
[UserDataIsolation]
[ScriptGlobalOptionsIsolation]
public sealed class MathFloorTUnitTests
{
    // ============================================================
    // NORMAL CASES
    // ============================================================
    
    [Test]
    [AllLuaVersions]
    [Arguments(0.0, 0.0)]
    [Arguments(1.0, 1.0)]
    [Arguments(1.5, 1.0)]
    [Arguments(1.9, 1.0)]
    [Arguments(2.0, 2.0)]
    [Arguments(99.99, 99.0)]
    public async Task FloorReturnsCorrectValueForPositive(
        LuaCompatibilityVersion version, double input, double expected)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString($"return math.floor({input:R})");
        await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    [Arguments(-0.1, -1.0)]
    [Arguments(-1.0, -1.0)]
    [Arguments(-1.5, -2.0)]
    [Arguments(-1.9, -2.0)]
    [Arguments(-2.0, -2.0)]
    [Arguments(-99.99, -100.0)]
    public async Task FloorReturnsCorrectValueForNegative(
        LuaCompatibilityVersion version, double input, double expected)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString($"return math.floor({input:R})");
        await Assert.That(result.Number).IsEqualTo(expected).ConfigureAwait(false);
    }
    
    // ============================================================
    // EDGE CASES
    // ============================================================
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesZero(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.floor(0)");
        await Assert.That(result.Number).IsEqualTo(0).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesNegativeZero(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.floor(-0.0)");
        // -0.0 should floor to -0.0, which equals 0 in comparison
        await Assert.That(result.Number).IsEqualTo(0).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesPositiveInfinity(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.floor(math.huge)");
        await Assert.That(double.IsPositiveInfinity(result.Number)).IsTrue().ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesNegativeInfinity(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.floor(-math.huge)");
        await Assert.That(double.IsNegativeInfinity(result.Number)).IsTrue().ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesNaN(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.floor(0/0)");
        await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesVerySmallPositive(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        // Smallest positive double
        DynValue result = script.DoString($"return math.floor({double.Epsilon:R})");
        await Assert.That(result.Number).IsEqualTo(0).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorHandlesVerySmallNegative(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString($"return math.floor({-double.Epsilon:R})");
        await Assert.That(result.Number).IsEqualTo(-1).ConfigureAwait(false);
    }
    
    // ============================================================
    // ERROR CASES
    // ============================================================
    
    [Test]
    [AllLuaVersions]
    public async Task FloorThrowsOnNil(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        await Assert.ThrowsAsync<ScriptRuntimeException>(
            () => Task.FromResult(script.DoString("return math.floor(nil)"))
        ).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    [Arguments("'string'")]
    [Arguments("{}")]
    [Arguments("true")]
    [Arguments("false")]
    [Arguments("function() end")]
    public async Task FloorThrowsOnInvalidTypes(LuaCompatibilityVersion version, string arg)
    {
        Script script = CreateScript(version);
        await Assert.ThrowsAsync<ScriptRuntimeException>(
            () => Task.FromResult(script.DoString($"return math.floor({arg})"))
        ).ConfigureAwait(false);
    }
    
    [Test]
    [AllLuaVersions]
    public async Task FloorThrowsOnNoArguments(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        await Assert.ThrowsAsync<ScriptRuntimeException>(
            () => Task.FromResult(script.DoString("return math.floor()"))
        ).ConfigureAwait(false);
    }
    
    // ============================================================
    // VERSION-SPECIFIC BEHAVIOR
    // ============================================================
    
    [Test]
    [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
    public async Task FloorReturnsIntegerTypeInLua53Plus(LuaCompatibilityVersion version)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString("return math.type(math.floor(3.5))");
        await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
    }
    
    // ============================================================
    // DATA-DRIVEN COMPREHENSIVE TEST
    // ============================================================
    
    public static IEnumerable<(double input, double expected)> ComprehensiveFloorData()
    {
        // Generate 1000 test cases covering the full range
        Random rng = new Random(42);  // Fixed seed for reproducibility
        for (int i = 0; i < 1000; i++)
        {
            double value = (rng.NextDouble() - 0.5) * 1e10;  // Wide range
            yield return (value, Math.Floor(value));
        }
    }
    
    [Test]
    [AllLuaVersions]
    [MethodDataSource(nameof(ComprehensiveFloorData))]
    public async Task FloorMatchesDotNetAcrossRange(
        LuaCompatibilityVersion version,
        (double input, double expected) testCase)
    {
        Script script = CreateScript(version);
        DynValue result = script.DoString($"return math.floor({testCase.input:R})");
        await Assert.That(result.Number).IsEqualTo(testCase.expected).ConfigureAwait(false);
    }
    
    // ============================================================
    // HELPER
    // ============================================================
    
    private static Script CreateScript(LuaCompatibilityVersion version)
    {
        return new Script(version);
    }
}
```

______________________________________________________________________

## 🔴 Test Completeness Checklist

Before submitting code, verify:

### Coverage Categories

- [ ] Normal/happy path cases covered
- [ ] Edge cases at boundaries covered
- [ ] Error cases with invalid inputs covered
- [ ] Negative tests (what shouldn't work) covered
- [ ] Special values (infinity, NaN, empty, null) covered
- [ ] Version-specific behavior (5.1 through 5.5) covered

### Data-Driven Tests

- [ ] Used `[Arguments]` or `[MethodDataSource]` where appropriate
- [ ] Covered full input range (not just a few examples)
- [ ] Included generated test data for comprehensive coverage

### Quality Checks

- [ ] All tests pass locally
- [ ] Tests are deterministic (no flakiness)
- [ ] Test names clearly describe what's being tested
- [ ] Tests verify behavior, not implementation

______________________________________________________________________

## Resources

- [tunit-test-writing](tunit-test-writing.md) — TUnit framework specifics
- [lua-fixture-creation](lua-fixture-creation.md) — Creating Lua test files
- [test-failure-investigation](test-failure-investigation.md) — Debugging failures
