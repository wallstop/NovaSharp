# Lua Comparison Harness

> **Status**: Implemented. CI runs multi-version comparison on every PR in a lane that is independent of the OS unit-test matrix.

## Overview

NovaSharp tests contain thousands of inline Lua snippets executed via `Script.DoString(...)`. To ensure semantic parity with canonical Lua (5.1–5.5), this harness:

1. Extracts all inline Lua snippets from C# test files with automatic version compatibility detection
1. Runs each snippet through both NovaSharp and reference Lua interpreters
1. Compares outputs with semantic normalization, flagging discrepancies

## Quick Start

```bash
# Run fixtures against Lua 5.4 and NovaSharp (fast batch mode)
bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.4

# Run fixtures against specific Lua version (slower, spawns processes per file)
bash scripts/tests/run-lua-fixtures.sh --lua-version 5.1

# Compare outputs between Lua and NovaSharp
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4

# View comparison summary
cat artifacts/lua-comparison-results/comparison.json | jq '.summary'
```

## Directory Structure

```
src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/
├── <TestClass>/
│   ├── <TestMethod>.lua      # Extracted fixture with metadata header
│   └── ...
├── manifest.json             # Mapping of fixtures to source locations
└── ...

artifacts/lua-comparison-results/
├── <TestClass>/<TestMethod>.lua5.4.out     # Lua stdout
├── <TestClass>/<TestMethod>.lua5.4.err     # Lua stderr
├── <TestClass>/<TestMethod>.lua5.4.rc      # Lua exit code
├── <TestClass>/<TestMethod>.nova.out       # NovaSharp stdout
├── <TestClass>/<TestMethod>.nova.err       # NovaSharp stderr
├── <TestClass>/<TestMethod>.nova.rc        # NovaSharp exit code
├── comparison.json                         # Full comparison report
└── novasharp_summary.json                  # Batch runner summary
```

## Fixture Metadata Headers

Each extracted `.lua` file includes a metadata header specifying compatibility. Author-controlled metadata is limited to `@lua-versions`, `@novasharp-only`, and `@expects-error`; the extractor may add generated provenance fields for traceability.

```lua
-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: path/to/TestClass.cs:123
-- @test: TestClass.TestMethod

-- Actual Lua code follows...
local x = 1 + 2
return x
```

### Author-Controlled Metadata

| Field             | Description                                                                     |
| ----------------- | ------------------------------------------------------------------------------- |
| `@lua-versions`   | Comma-separated list of compatible Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)       |
| `@novasharp-only` | `true` if fixture uses NovaSharp-specific features (CLR interop, `!=` operator) |
| `@expects-error`  | `true` if the test expects a runtime error                                      |

### Generated Provenance

| Field     | Description                                |
| --------- | ------------------------------------------ |
| `@source` | Original C# source file and line number    |
| `@test`   | Fully qualified test class and method name |

### Version Compatibility Detection

The extractor (`tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`) automatically detects version compatibility:

| Feature                                        | Incompatible Versions |
| ---------------------------------------------- | --------------------- |
| Goto labels (`::label::`)                      | 5.1                   |
| Bitwise operators (`&`, `\|`, `~`, `<<`, `>>`) | 5.1, 5.2              |
| Integer division (`//`)                        | 5.1, 5.2              |
| `<const>`, `<close>` attributes                | 5.1, 5.2, 5.3         |
| `utf8.` module                                 | 5.1, 5.2              |
| NovaSharp `!=` operator                        | All (NovaSharp-only)  |
| CLR interop (`clr.`, userdata)                 | All (NovaSharp-only)  |

## Test Authoring Patterns

### Pattern 1: Inline snippets (existing tests)

```csharp
[Test]
public async Task InlineSnippetExtracted()
{
    Script script = new();
    DynValue result = script.DoString(@"
        local x = 1 + 2
        return x
    ");
    await Assert.That(result.Number).IsEqualTo(3).ConfigureAwait(false);
}
```

The extractor automatically pulls inline `DoString(...)` snippets into `LuaFixtures/<TestClass>/<TestMethod>.lua`.

### Pattern 2: File-first approach (preferred for new tests)

```csharp
[Test]
public async Task TableMoveShiftsElements()
{
    Script script = new();
    string fixturePath = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "LuaFixtures/TableModuleTUnitTests/TableMoveShiftsElements.lua");
    DynValue result = script.DoFile(fixturePath);
    await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
}
```

```lua
-- src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/TableModuleTUnitTests/TableMoveShiftsElements.lua
-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false

local t = {1, 2, 3, 4, 5}
table.move(t, 2, 4, 3)
return t[5]  -- 42 (5th element after move)
```

### Skipping NovaSharp-Specific Tests

For tests using C# interop or NovaSharp extensions, add the skip comment:

```lua
-- novasharp: skip-comparison
-- @novasharp-only: true

-- Uses NovaSharp-specific userdata binding
local obj = clr.create("System.Text.StringBuilder")
obj:Append("hello")
return obj:ToString()
```

## Comparison Modes

The comparison script (`scripts/tests/compare-lua-outputs.py`) supports two modes:

| Mode       | Flag       | Description                                                                                     |
| ---------- | ---------- | ----------------------------------------------------------------------------------------------- |
| `semantic` | (default)  | Normalized comparison: floating-point precision, memory addresses, line numbers, platform paths |
| `strict`   | `--strict` | Exact byte-for-byte output match                                                                |

### Semantic Normalization

The semantic mode applies these normalizations:

- **NovaSharp CLI**: Removes `[compatibility]` info lines
- **Floating-point**: Rounds to 10 decimal places, normalizes `-0` to `0`
- **Memory addresses**: Replaces `0x7f...` with `<addr>`
- **Table/function addresses**: Normalizes `table: 0x...` to `table: <addr>`
- **Error line numbers**: Normalizes `.lua:123:` to `.lua:<line>:`
- **Stack traces**: Normalizes chunk names and line references

## CI Integration

The `lua-comparison` job in `.github/workflows/tests.yml` runs after `lint`, not after the full `dotnet-tests` OS matrix. That keeps specification-comparison signal visible even if one platform's unit-test job fails. The job still runs a **matrix** of Lua versions across all supported platforms:

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
    lua-version: ['5.1', '5.2', '5.3', '5.4', '5.5']
```

Each matrix job:

1. Installs the specific Lua version using platform-appropriate methods
1. Builds NovaSharp CLI
1. Runs all compatible fixtures through both interpreters
1. Compares outputs with semantic normalization
1. Checks `both_error` signatures against `docs/testing/lua-error-ratchet.json`
1. Uploads version and platform-specific artifacts (e.g., `lua-comparison-5.4-ubuntu-latest`)

### Platform-Specific Lua Installation

| Platform | Method                                                                       |
| -------- | ---------------------------------------------------------------------------- |
| Linux    | `apt-get install lua5.x` or source build cached under `.lua-cache`           |
| macOS    | Homebrew (`brew install lua` or `lua@5.x`) or source build cached locally    |
| Windows  | Official `lua.org` source build with MSVC, cached under `.lua-cache/Windows` |

### CI Gating

CI runs `compare-lua-outputs.py --enforce`. `mismatch`, `lua_only`, and `nova_only` are hard failures. `both_error` entries are allowed only when their current normalized signatures match `docs/testing/lua-error-ratchet.json`; new or changed unclassified entries fail, while reductions pass.

## Performance Optimization

### Fast Batch Runner

For local development, use the optimized batch runner:

```bash
bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.4
```

This uses `src/tooling/WallstopStudios.NovaSharp.LuaBatchRunner/`, which:

- Processes all fixtures in a **single .NET process** (no per-file spawn overhead)
- Runtime and fixture counts vary with the current fixture manifest and runner; use `artifacts/lua-comparison-results/results.json` for the observed count, elapsed time, and worker count.
- Includes 5-second per-script timeout for infinite loops
- Intercepts `os.exit()` to prevent process termination

### Slow Reference Runner

For CI or when you need per-file isolation:

```bash
bash scripts/tests/run-lua-fixtures.sh --lua-version 5.4
```

## Extraction Statistics (baseline varies by fixture catalog)

| Metric                          | Source                           |
| ------------------------------- | -------------------------------- |
| Total fixtures compared         | Latest comparison `results.json` |
| Per-version comparable fixtures | `artifacts/.../results.json`     |
| Match/mismatch/error counts     | `artifacts/.../comparison.json`  |

## Re-extracting Fixtures

To re-extract fixtures after test changes:

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py \
    --source-dir src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit \
    --output-dir src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures \
    --verbose
```

The extractor:

- Scans all `*.cs` files for `DoString(...)` calls
- Extracts literal string arguments
- Detects version compatibility via regex patterns
- Generates metadata headers and `manifest.json`

## Troubleshooting

### Fixture not running

Check the `@lua-versions` header—the fixture may be incompatible with the selected version.

### False positive mismatch

If outputs differ only in formatting (addresses, line numbers), the semantic normalizer should handle it. File an issue if a legitimate normalization is missing.

### Timeout during batch run

The batch runner has a 5-second timeout per script. Scripts waiting for stdin or in infinite loops will timeout. Check `novasharp_summary.json` for timeout details.

## Implementation Status

- [x] Phase 1: Lua snippet extraction infrastructure (`lua_corpus_extractor_v2.py`)
- [x] Phase 2: Multi-version Lua execution harness (`run-lua-fixtures.sh`, `compare-lua-outputs.py`)
- [x] Phase 3: CI integration (decoupled matrix job for 5.1–5.5)
- [x] Phase 4: Performance optimization (`WallstopStudios.NovaSharp.LuaBatchRunner`)
- [x] Phase 5: Multi-platform CI (ubuntu, windows, macos)
- [x] Phase 6: `both_error` ratchet for unclassified error parity gaps
- [ ] Phase 7: File-first test authoring pattern (migrate existing tests)

See `PLAN.md` → "Reference Lua comparison harness" for the implementation timeline.
