---
name: coverage-analysis
description: "Run NovaSharp code coverage, interpret reports, and turn uncovered behavior into meaningful tests. Use for coverage reports, coverage gaps, or untested code analysis."
metadata:
  category: testing
  priority: reference
  related: tunit-test-writing, lua-fixture-creation
---
# Skill: Coverage Analysis

**Related Skills**: [tunit-test-writing](../tunit-test-writing/SKILL.md) (adding tests for gaps), [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (creating .lua fixtures)

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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Adding Tests for Gaps, Coverage Exclusions, CI Integration, Improving Coverage Incrementally.
