______________________________________________________________________

triggers:

- "coverage"
- "code coverage"
- "coverage report"
- "test coverage"
- "untested code"
  category: testing
  related:
- tunit-test-writing
- lua-fixture-creation
  priority: reference

______________________________________________________________________

# Skill: Coverage Analysis

**When to use**: Running code coverage, interpreting reports, finding coverage gaps.

**Related Skills**: [tunit-test-writing](tunit-test-writing.md) (adding tests for gaps), [lua-fixture-creation](lua-fixture-creation.md) (creating .lua fixtures)

______________________________________________________________________

## Running Coverage

### Quick coverage run

```bash
# Full coverage analysis
bash ./scripts/coverage/coverage.sh
```

### PowerShell (Windows/Linux with pwsh)

```powershell
DOTNET_ROLL_FORWARD=Major pwsh ./scripts/coverage/coverage.ps1
```

______________________________________________________________________

## Output Locations

| Path                              | Contents                |
| --------------------------------- | ----------------------- |
| `artifacts/coverage/`             | Raw coverage data       |
| `docs/coverage/latest/`           | HTML reports            |
| `docs/coverage/latest/index.html` | Main report entry point |

______________________________________________________________________

## Interpreting Reports

### Coverage metrics

| Metric          | Description         | Target |
| --------------- | ------------------- | ------ |
| Line Coverage   | % of lines executed | >= 80% |
| Branch Coverage | % of branches taken | >= 80% |
| Method Coverage | % of methods called | >= 80% |

### Understanding the HTML report

1. **Summary page** — Overall project coverage
1. **Assembly view** — Coverage per assembly
1. **Class view** — Coverage per class
1. **Source view** — Line-by-line highlighting

### Color coding

- 🟢 **Green** — Covered lines
- 🔴 **Red** — Uncovered lines
- 🟡 **Yellow** — Partially covered (some branches)

______________________________________________________________________

## Finding Coverage Gaps

### 1. Check summary for low-coverage assemblies

```bash
# Open the report
open docs/coverage/latest/index.html
# or
xdg-open docs/coverage/latest/index.html
```

### 2. Drill into low-coverage classes

Look for classes with < 70% line coverage.

### 3. Examine uncovered branches

Yellow highlighting indicates partially covered code — some branches not taken.

### 4. Identify untested code paths

Common gaps:

- Error handling paths
- Edge cases (null, empty, boundary values)
- Version-specific code paths
- Platform-specific code

______________________________________________________________________

## Adding Tests for Gaps

### Example: Uncovered error path

```csharp
// Coverage shows this catch block is never hit
try
{
    // ...
}
catch (SomeException ex)  // 🔴 RED - never executed
{
    // ...
}
```

Add a test that triggers the exception:

```csharp
[Test]
[AllLuaVersions]
public async Task HandlesErrorGracefully(LuaCompatibilityVersion version)
{
    Script script = CreateScript(version);
    
    // Trigger the error condition
    await Assert.ThrowsAsync<ScriptRuntimeException>(async () =>
    {
        script.DoString("code_that_triggers_error()");
    }).ConfigureAwait(false);
}
```

### Example: Uncovered version-specific code

```csharp
// Coverage shows Lua 5.1 path never taken
if (version == LuaCompatibilityVersion.Lua51)  // 🔴 RED
{
    // 5.1 specific handling
}
```

Ensure multi-version tests cover all paths:

```csharp
[Test]
[AllLuaVersions]  // Runs 5.1, 5.2, 5.3, 5.4, 5.5
public async Task WorksAcrossVersions(LuaCompatibilityVersion version)
{
    // Each version takes different code paths
}
```

______________________________________________________________________

## Coverage Exclusions

Some code is intentionally excluded from coverage:

### Attributes for exclusion

```csharp
[ExcludeFromCodeCoverage]
public void DebugOnlyMethod() { }
```

### Common exclusions

- Debug-only code
- Platform-specific code that can't run in test environment
- Generated code
- Trivial property accessors

______________________________________________________________________

## CI Integration

Coverage runs automatically in CI after `lint`, independent of the OS unit-test matrix. Failed coverage gates block merges.

### Checking CI coverage results

1. Go to the PR's CI checks
1. Find the coverage step
1. Download coverage artifacts if needed

______________________________________________________________________

## Improving Coverage Incrementally

### Strategy

1. Run coverage report
1. Identify lowest-coverage modules
1. Add tests for critical paths first
1. Repeat

### Priority order

1. **Core execution** — VM, opcodes, stack operations
1. **Standard library** — math, string, table, io
1. **Parser/Lexer** — Syntax handling
1. **Interop** — C#/Lua bridge
1. **Utilities** — Helpers, converters
