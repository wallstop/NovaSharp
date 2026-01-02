# Test Patterns

Reusable patterns for TUnit tests and Lua fixtures in NovaSharp.

______________________________________________________________________

## Multi-Version Test Attributes

### All versions (future-proof)

```csharp
[Test]
[AllLuaVersions]
public async Task FeatureWorksAcrossAllVersions(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ...
}
```

### Version range (recommended over explicit lists)

```csharp
// 5.3 and above (auto-includes future versions like 5.6)
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task FeatureAvailableInLua53Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    // ...
}

// Up to 5.2 only
[Test]
[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
public async Task FeatureShouldBeNilInPreLua53(LuaCompatibilityVersion version)
{
    // ...
}

// Specific range
[Test]
[LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua54)]
public async Task FeatureInVersionRange(LuaCompatibilityVersion version)
{
    // ...
}
```

### Matrix test (versions x inputs)

```csharp
// 5 versions x 2 inputs = 10 test cases
[Test]
[LuaTestMatrix("input1", "input2")]
public async Task FeatureWithInputs(LuaCompatibilityVersion version, string input)
{
    // ...
}
```

______________________________________________________________________

## Assertion Patterns

```csharp
// TUnit async assertions
await Assert.That(result.String).IsEqualTo("expected").ConfigureAwait(false);
await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
await Assert.That(result.Number).IsEqualTo(42.0).ConfigureAwait(false);
```

______________________________________________________________________

## Isolation Attributes

```csharp
[UserDataIsolation]            // Isolates UserData registry
[ScriptGlobalOptionsIsolation] // Isolates global Script options
[PlatformDetectorIsolation]    // Isolates platform detection
```

______________________________________________________________________

## Lua Fixture Template

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/ExampleTests.cs:42
-- @test: ExampleTests.TestMethod

-- Test: Brief description
local result = some_function()
assert(result == expected, "Expected X, got " .. tostring(result))
print("PASS")
```

### Version-specific fixture

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/.../MathModuleTUnitTests.cs:150
-- @test: MathModuleTUnitTests.IntegerDivisionBasic

-- Test: Floor division returns integer result
local result = 7 // 3
assert(result == 2, "Expected 2, got " .. tostring(result))
print("PASS")
```

### Error-expecting fixture

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/.../MathEdgeCasesTUnitTests.cs:99
-- @test: MathEdgeCasesTUnitTests.IntegerDivisionByZeroErrors

-- Test: Integer division by zero throws error
return 1 // 0
```

______________________________________________________________________

## Positive and Negative Test Pairs

For version-specific features, test BOTH scenarios:

```csharp
// POSITIVE: Feature works in supported versions
[Test]
[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
public async Task MathTypeAvailableInLua53Plus(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    DynValue result = script.DoString("return math.type(5)");
    await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
}

// NEGATIVE: Feature is absent in unsupported versions
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

## Data-Driven Tests

```csharp
[Test]
[MethodDataSource(nameof(GetTestCases))]
public async Task DataDrivenTest(string input, int expected)
{
    // ...
}

public static IEnumerable<(string, int)> GetTestCases()
{
    yield return ("input1", 1);
    yield return ("input2", 2);
    yield return ("edge_case", 0);
}
```

______________________________________________________________________

## Workflow

```bash
# 1. Create C# test
# 2. Create .lua fixture with metadata
# 3. Verify against reference Lua
lua5.4 path/to/fixture.lua

# 4. Regenerate corpus
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py

# 5. Run tests
./scripts/test/quick.sh TestMethodName
```
