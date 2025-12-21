# Skill: Lua Comparison Harness

**When to use**: Running fixtures against reference Lua interpreters to verify NovaSharp compliance.

______________________________________________________________________

## Overview

The comparison harness runs `.lua` fixture files against both reference Lua interpreters and NovaSharp, then compares outputs to find behavioral differences.

______________________________________________________________________

## Quick Commands

### Run fixtures against a Lua version

```bash
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4
```

### Compare outputs

```bash
python3 scripts/tests/compare-lua-outputs.py \
    --lua-version 5.4 \
    --results-dir artifacts/lua-comparison-5.4
```

### Run against all versions

```bash
for v in 5.1 5.2 5.3 5.4 5.5; do
    python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version $v
done
```

______________________________________________________________________

## Fixture Discovery

The harness finds fixtures in:

- `LuaFixtures/` — Primary fixture directory
- `src/tests/**/LuaFixtures/` — Test-adjacent fixtures

### Fixture requirements

1. Must have harness-compatible metadata header
1. Must be runnable with `lua5.X filename.lua`
1. Must produce deterministic output

______________________________________________________________________

## Metadata Filtering

The harness uses fixture metadata to determine which files to run:

```lua
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
```

| Metadata                  | Effect                          |
| ------------------------- | ------------------------------- |
| `@lua-versions: 5.3, 5.4` | Only run on specified versions  |
| `@novasharp-only: true`   | Skip when running reference Lua |
| `@expects-error: true`    | Expect non-zero exit code       |

______________________________________________________________________

## Output Artifacts

Results are stored in `artifacts/lua-comparison-{version}/`:

```
artifacts/lua-comparison-5.4/
├── results.json          # Machine-readable results
├── summary.txt           # Human-readable summary
├── passed/               # Fixtures that matched
├── failed/               # Fixtures with differences
│   ├── fixture_name.lua.expected   # Reference Lua output
│   ├── fixture_name.lua.actual     # NovaSharp output
│   └── fixture_name.lua.diff       # Diff between outputs
└── errors/               # Fixtures that crashed
```

______________________________________________________________________

## Investigating Failures

### 1. Check the diff

```bash
cat artifacts/lua-comparison-5.4/failed/fixture_name.lua.diff
```

### 2. Run manually against reference Lua

```bash
lua5.4 LuaFixtures/TestClass/fixture_name.lua
```

### 3. Run manually against NovaSharp

```bash
dotnet run --project src/tooling/NovaSharp.Cli -- LuaFixtures/TestClass/fixture_name.lua
```

### 4. Compare outputs side-by-side

```bash
diff -y \
    artifacts/lua-comparison-5.4/failed/fixture_name.lua.expected \
    artifacts/lua-comparison-5.4/failed/fixture_name.lua.actual
```

______________________________________________________________________

## Common Failure Patterns

### Output format differences

```diff
- 3.0          # Reference Lua (float formatting)
+ 3            # NovaSharp (integer formatting)
```

**Fix**: Check number formatting in output functions.

### Error message differences

```diff
- stdin:1: attempt to index a nil value
+ [string "test"]:1: attempt to index nil value
```

**Usually OK**: Error message formatting often differs. Check the error type matches.

### Floating-point precision

```diff
- 0.30000000000000004
+ 0.3
```

**Check**: Is this a display issue or calculation difference?

### Version-specific behavior

```diff
- nil           # Lua 5.1 (no math.type)
+ integer       # Lua 5.3+ (has math.type)
```

**Fix**: Create version-specific fixtures or check `@lua-versions` metadata.

______________________________________________________________________

## Regenerating Fixtures

After changing tests, regenerate the corpus:

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

______________________________________________________________________

## Harness Scripts Reference

| Script                                                | Purpose                            |
| ----------------------------------------------------- | ---------------------------------- |
| `scripts/tests/run-lua-fixtures-parallel.py`          | Run fixtures in parallel           |
| `scripts/tests/compare-lua-outputs.py`                | Compare outputs and generate diffs |
| `tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` | Extract fixtures from tests        |

______________________________________________________________________

## CI Integration

The comparison harness runs in CI for all Lua versions. Failures block merges.

### Viewing CI results

1. Check the CI workflow logs
1. Download artifacts from the comparison step
1. Review `artifacts/lua-comparison-*/failed/` for differences

______________________________________________________________________

## Adding New Fixtures

1. Create `.lua` file with proper metadata header
1. Verify it runs with reference Lua: `lua5.4 your_fixture.lua`
1. Run the harness: `python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4`
1. Check results in `artifacts/lua-comparison-5.4/`
