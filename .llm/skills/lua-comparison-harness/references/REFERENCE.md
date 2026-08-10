# Lua Comparison Harness Reference

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

See [lua-fixture-creation](../../lua-fixture-creation/SKILL.md) for creating new fixtures.
