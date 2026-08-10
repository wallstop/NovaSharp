# Writing TUnit Tests for NovaSharp Reference

## Version-Specific Features: Test BOTH Scenarios

### POSITIVE: Feature works in supported versions

```csharp
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task MathTypeAvailableInLua53Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    LuaValue result = script.DoString("return math.type(5)");
    await Assert.That(result.AsString()).IsEqualTo("integer").ConfigureAwait(false);
}
```

### NEGATIVE: Feature is absent in unsupported versions

```csharp
[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
public async Task MathTypeShouldBeNilInPreLua53(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    LuaValue result = script.DoString("return math.type");
    await Assert.That(result.IsNil).IsTrue().ConfigureAwait(false);
}
```

______________________________________________________________________

## Version Coverage Checklist

For every new test, ask:

1. **Universal feature?** → Test ALL 5 versions with `[AllLuaVersions]`
1. **Version-specific?** → Test BOTH:
   - ✅ Positive: Works in supported versions
   - ❌ Negative: Unavailable/nil/errors in unsupported versions
1. **Behavior differs?** → Create separate tests per behavior variant

______________________________________________________________________

## Test Naming Patterns

| Pattern                         | Use Case                         |
| ------------------------------- | -------------------------------- |
| `FeatureWorksAcrossAllVersions` | Universal behavior               |
| `FeatureAvailableInLua53Plus`   | Positive test for newer versions |
| `FeatureShouldBeNilInPreLua53`  | Negative test for older versions |
| `FeatureBehaviorDiffersInLua51` | Version-specific behavior        |

______________________________________________________________________

## Data-Driven Test Attributes

- `[Arguments(...)]` — Manual argument enumeration (legacy)
- `[MethodDataSource]` — Arguments from a method
- `[CombinedDataSources]` — Combine multiple sources

______________________________________________________________________

## Lint Guards (Run Before Push)

```bash
python scripts/lint/check-platform-testhooks.py
python scripts/lint/check-console-capture-semaphore.py
python scripts/lint/check-userdata-scope-usage.py
python scripts/lint/check-test-finally.py
python scripts/lint/check-temp-path-usage.py
```

______________________________________________________________________

## After Creating C# Tests

### 1. Create corresponding `.lua` fixture

Every C# test should have a matching `.lua` fixture for cross-interpreter verification. See [lua-fixture-creation](../../lua-fixture-creation/SKILL.md) for details.

### 2. Regenerate corpus (REQUIRED)

**Always run this after adding or modifying tests/fixtures:**

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

### 3. Run tests

```bash
./scripts/test/quick.sh TestMethodName
```

______________________________________________________________________

## Running Tests

See [context.md Quick Scripts](../../../context.md) for test commands (`./scripts/test/quick.sh`).

______________________________________________________________________

## Exhaustive Testing

For comprehensive testing guidelines including edge cases, error cases, negative tests, and data-driven approaches, see [exhaustive-test-coverage](../../exhaustive-test-coverage/SKILL.md).

**Key principle**: Every feature and bugfix needs tests covering:

- Normal/happy path cases
- Edge cases and boundaries
- Error cases with invalid inputs
- Negative tests (what shouldn't work)
- Special values (infinity, NaN, empty, null)
- "The impossible" scenarios
