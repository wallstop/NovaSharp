---
name: exhaustive-test-coverage
description: "Design complete NovaSharp regression coverage across positive, negative, boundary, version, integration, and isolation paths. Use when testing any new feature or bug fix."
metadata:
  category: testing
  priority: core
  related: tunit-test-writing, lua-fixture-creation, test-failure-investigation
---
# Skill: Exhaustive Test Coverage

**Code Samples**: [test-patterns](../../code-samples/test-patterns.md)

**Related Skills**: [tunit-test-writing](../tunit-test-writing/SKILL.md), [lua-fixture-creation](../lua-fixture-creation/SKILL.md), [test-failure-investigation](../test-failure-investigation/SKILL.md)

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
    LuaValue result = script.DoString("return math.floor(3.7)");
    await Assert.That(result.AsNumber()).IsEqualTo(3).ConfigureAwait(false);
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
    LuaValue result = script.DoString($"return math.floor({input:R})");
    await Assert.That(result.AsNumber()).IsEqualTo(expected).ConfigureAwait(false);
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
    LuaValue result = script.DoString("return math.type");
    await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
}
```

### 5. Special Value Tests

```csharp
[Test]
[AllLuaVersions]
public async Task MathFloorHandlesInfinity(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    LuaValue posInf = script.DoString("return math.floor(math.huge)");
    await Assert.That(double.IsPositiveInfinity(posInf.AsNumber())).IsTrue().ConfigureAwait(false);
}
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Data-Driven Testing, Test Input Checklists, Test Naming Conventions, Test Completeness Checklist.
