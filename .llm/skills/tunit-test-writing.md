# Skill: Writing TUnit Tests for NovaSharp

**When to use**: Writing or modifying TUnit tests for the interpreter.

______________________________________________________________________

## Framework Basics

- **Framework**: TUnit only (`global::TUnit.Core.Test`)
- **Async assertions**: `await Assert.That(...).ConfigureAwait(false)`
- **Method names**: PascalCase, no underscores

______________________________________________________________________

## Required Isolation Attributes

Use these to prevent test interference:

```csharp
[UserDataIsolation]           // Isolates UserData registry
[ScriptGlobalOptionsIsolation] // Isolates global Script options
[PlatformDetectorIsolation]   // Isolates platform detection
```

______________________________________________________________________

## Cleanup Utilities

- `TempFileScope` — Auto-cleanup temporary files
- `SemaphoreSlimScope` — Auto-release semaphores
- `ConsoleTestUtilities` — Capture/restore console output

______________________________________________________________________

## 🔴 Multi-Version Testing (REQUIRED)

**All tests MUST run against all applicable Lua versions (5.1, 5.2, 5.3, 5.4, 5.5).**

### Version Data-Driving Helpers

| Helper                                              | Description              | Use Case                           |
| --------------------------------------------------- | ------------------------ | ---------------------------------- |
| `[AllLuaVersions]`                                  | Expands to Lua 5.1–5.5   | Universal coverage                 |
| `[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]`  | Versions from 5.3+       | Features introduced in 5.3         |
| `[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]` | Versions up to 5.2       | Features removed/changed after 5.2 |
| `[LuaVersionRange(Lua52, Lua54)]`                   | Inclusive version window | Focused compatibility spans        |
| `[LuaTestMatrix("input1", "input2")]`               | Versions × inputs        | Comprehensive edge-case testing    |

### Examples

```csharp
// Universal test - runs on all 5 versions
[Test]
[AllLuaVersions]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ...
}

// Matrix test - 5 versions × 2 inputs = 10 test cases
[Test]
[LuaTestMatrix("input1", "input2")]
public async Task FeatureWithInputs(LuaCompatibilityVersion version, string input)
{
    // ...
}
```

______________________________________________________________________

## Version-Specific Features: Test BOTH Scenarios

### POSITIVE: Feature works in supported versions

```csharp
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task MathTypeAvailableInLua53Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type(5)");
    await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
}
```

### NEGATIVE: Feature is absent in unsupported versions

```csharp
[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
public async Task MathTypeShouldBeNilInPreLua53(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type");
    await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
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

## Running Tests

```bash
# All tests
./scripts/test/quick.sh

# Filter by method name
./scripts/test/quick.sh Floor

# Filter by class name
./scripts/test/quick.sh -c MathModule

# Combined filter
./scripts/test/quick.sh -c Math -m Floor

# Skip build
./scripts/test/quick.sh --no-build Floor
```
