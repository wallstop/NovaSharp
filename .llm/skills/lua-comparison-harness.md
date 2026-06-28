______________________________________________________________________

triggers:

- "comparison harness"
- "lua comparison"
- "cross-interpreter"
- "fixture verification"
- "reference Lua"
- "diff expected actual"
  category: lua
  related:
- lua-fixture-creation
- lua-spec-verification
- test-failure-investigation
  priority: recommended

______________________________________________________________________

# Skill: Lua Comparison Harness

**When to use**: Running fixtures against reference Lua interpreters to verify NovaSharp compliance.

**Related Skills**: [lua-fixture-creation](lua-fixture-creation.md) (creating fixtures), [lua-spec-verification](lua-spec-verification.md) (investigating differences)

______________________________________________________________________

## Overview

The comparison harness runs `.lua` fixture files against both reference Lua interpreters and NovaSharp, then compares outputs to find behavioral differences.

______________________________________________________________________

## Quick Commands

### Run fixtures against a Lua version

```bash
bash scripts/tests/run-lua-fixtures-fast.sh --lua-version 5.4
```

### Compare outputs

```bash
python3 scripts/tests/compare-lua-outputs.py \
    --lua-version 5.4 \
    --results-dir artifacts/lua-comparison-results
```

### Run against all versions

```bash
for v in 5.1 5.2 5.3 5.4 5.5; do
    bash scripts/tests/run-lua-fixtures-fast.sh --lua-version $v --output-dir artifacts/lua-comparison-results-$v
    python3 scripts/tests/compare-lua-outputs.py --lua-version $v --results-dir artifacts/lua-comparison-results-$v --enforce
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

Results are stored in `artifacts/lua-comparison-results/` by default:

```
artifacts/lua-comparison-results/
├── results.json                         # Runner summary
├── comparison.json                      # Comparator report
├── novasharp_summary.json               # Batch runner summary
├── <TestClass>/<TestMethod>.lua5.4.out  # Reference Lua stdout
├── <TestClass>/<TestMethod>.lua5.4.err  # Reference Lua stderr
├── <TestClass>/<TestMethod>.lua5.4.rc   # Reference Lua exit code
├── <TestClass>/<TestMethod>.nova.out    # NovaSharp stdout
├── <TestClass>/<TestMethod>.nova.err    # NovaSharp stderr
└── <TestClass>/<TestMethod>.nova.rc     # NovaSharp exit code
```

______________________________________________________________________

## Investigating Failures

1. **Check the report**: `python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-results --verbose`
1. **Run against reference Lua**: `lua5.4 path/to/fixture.lua`
1. **Run against NovaSharp**: `dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Cli -- path/to/fixture.lua`
1. **Compare side-by-side**: `diff -y artifacts/.../fixture.lua5.4.out artifacts/.../fixture.nova.out`

**For cross-platform failures**: Check if failure is platform-specific, if NovaSharp output is consistent, if reference Lua varies by platform, or if it's a C library difference.

______________________________________________________________________

## Common Failure Patterns

| Pattern                                   | Issue                | Action                                       |
| ----------------------------------------- | -------------------- | -------------------------------------------- |
| **Number format** (`3.0` vs `3`)          | Float/int display    | Fix NovaSharp number formatting              |
| **Error messages**                        | Different error text | Verify TYPE/CAUSE/LOCATION match Lua exactly |
| **Float precision** (`0.3` vs `0.30...4`) | Display formatting   | NovaSharp must be byte-identical to Lua      |
| **Version-specific**                      | Feature availability | Check `@lua-versions` metadata               |

**🔴 Error message "format differences" are ONLY acceptable for**: chunk name formatting (`stdin:1` vs `[string "test"]:1`), path representation, whitespace. Different error TYPE, CAUSE, or line numbers are BUGS.

______________________________________________________________________

## 🔴 Platform-Specific C Library Differences

Reference Lua uses the platform's C library, causing legitimate differences that are NOT NovaSharp bugs:

- **strftime()**: Windows MSVCRT doesn't support `%C`, `%D`, `%F`, `%R`, `%T`, `%V`, `%u`, `%e`, `%n`, `%t`. NovaSharp is POSIX-compliant on all platforms.
- **NaN formatting**: Windows outputs `-nan(ind)`, others output `nan`. Harness normalizes this.
- **Missing compat functions**: Windows Lua may lack `math.log10`, `math.frexp`, `math.ldexp`, `math.pow`, `loadstring` (NovaSharp has all of these).

**When to use `@novasharp-only: true`**: Only for NovaSharp extensions or documented platform C-library/spec implementation-defined behavior. Keep the explanation in the C# test, fixture name, or nearby docs because fixture metadata is limited to `@lua-versions`, `@novasharp-only`, and `@expects-error`.

______________________________________________________________________

## Regenerating Fixtures

After changing tests, regenerate the corpus:

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

______________________________________________________________________

## Output Normalization

The harness normalizes: NaN formats, version strings, memory addresses, line numbers in errors, and float precision. This handles cosmetic differences only.

**Still BUGS after normalization**: Different error types/causes, missing/extra output, semantically different values.

______________________________________________________________________

## Scripts & CI

| Script                                                | Purpose                            |
| ----------------------------------------------------- | ---------------------------------- |
| `scripts/tests/run-lua-fixtures-fast.sh`              | Run fixtures with batch NovaSharp  |
| `scripts/tests/run-lua-fixtures-parallel.py`          | Debug runner with per-file process |
| `scripts/tests/compare-lua-outputs.py`                | Compare outputs and generate diffs |
| `tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` | Extract fixtures from tests        |

CI runs the harness in a decoupled `lua-comparison` lane for Lua 5.1-5.5 across the supported OS matrix. `mismatch`, `lua_only`, and `nova_only` are hard failures under `--enforce`; `both_error` entries are checked against `docs/testing/lua-error-ratchet.json` so new or changed unclassified errors fail while reductions pass. Check the uploaded `lua-comparison-<version>-<os>` artifact for `comparison-<version>.json`, raw per-fixture output, and ratchet counts.

See [lua-fixture-creation](lua-fixture-creation.md) for creating new fixtures.
