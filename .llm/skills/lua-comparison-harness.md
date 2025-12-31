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
dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Cli -- LuaFixtures/TestClass/fixture_name.lua
```

### 4. Compare outputs side-by-side

```bash
diff -y \
    artifacts/lua-comparison-5.4/failed/fixture_name.lua.expected \
    artifacts/lua-comparison-5.4/failed/fixture_name.lua.actual
```

### 5. Cross-Platform Analysis (CI Failures)

When investigating CI failures across multiple platforms/versions:

```bash
# Check if failure is platform-specific
ls artifacts/lua-comparison-*/failed/fixture_name.*

# Compare same fixture across platforms
diff artifacts/lua-comparison-5.4-ubuntu-latest/failed/fixture.lua.expected \
     artifacts/lua-comparison-5.4-windows-latest/failed/fixture.lua.expected

# Check if it's a NaN formatting issue
grep -i nan artifacts/lua-comparison-*/failed/fixture_name.lua.*
```

**Key questions for cross-platform failures:**

1. Does the failure occur on all platforms or just some?
1. Does NovaSharp produce the same output on all platforms?
1. Does reference Lua produce different outputs on different platforms?
1. Is this a C library difference (strftime, NaN formatting, etc.)?

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

**🔴 INVESTIGATE THOROUGHLY** — Error message formatting may legitimately differ between interpreters, but this is **NOT an excuse to skip investigation**. You MUST verify:

1. Error **TYPE** is semantically identical (same Lua error class)
1. Error **LOCATION** (line/column) is correct when applicable
1. Error **CAUSE** is the same (e.g., indexing nil vs. calling nil vs. arithmetic on nil)
1. If NovaSharp produces a different error type than reference Lua, that is a **BUG**
1. Document any accepted format-only differences with justification

**⚠️ CRITICAL CLARIFICATION**: "Format differences" means ONLY:

- Filename/path representation in error messages
- Chunk name formatting (`stdin:1` vs `[string "test"]:1`)
- Whitespace around error text

**"Format differences" does NOT excuse:**

- Different error TYPE (type error vs arithmetic error is a **BUG**)
- Different error CAUSE (indexing nil vs calling nil is a **BUG**)
- Missing or extra diagnostic information
- Different line/column numbers (is a **BUG**)

**When in doubt: It's a NovaSharp bug until proven otherwise.**

### Floating-point precision

```diff
- 0.30000000000000004
+ 0.3
```

**🔴 NEVER ACCEPT DIFFERENCES** — Lua has specific formatting for floating-point output. If NovaSharp produces different output:

1. NovaSharp's `print()` and `tostring()` **MUST produce byte-identical output** to reference Lua for the same numeric value
1. This includes trailing zeros, scientific notation thresholds, and precision display
1. Test with `lua5.4 -e "print(0.1 + 0.2)"` — NovaSharp must match **character-for-character**
1. If Lua versions differ in output format, NovaSharp must match the **target version exactly**
1. Any display difference is a **BUG** in NovaSharp's number formatting — fix production code, not tests

### Version-specific behavior

```diff
- nil           # Lua 5.1 (no math.type)
+ integer       # Lua 5.3+ (has math.type)
```

**Fix**: Create version-specific fixtures or check `@lua-versions` metadata.

______________________________________________________________________

## 🔴 Platform-Specific C Library Differences

Comparison tests run across multiple platforms (macOS, Windows, Ubuntu). Reference Lua uses the platform's native C library, which causes legitimate differences that are **NOT NovaSharp bugs**.

### strftime() Platform Variations

The `os.date()` function calls C's `strftime()`, which behaves differently across platforms:

| Specifier | Linux (glibc)      | macOS (BSD libc)     | Windows (MSVCRT)  |
| --------- | ------------------ | -------------------- | ----------------- |
| `%C`      | Century (20)       | Century (20)         | **Not supported** |
| `%D`      | mm/dd/yy           | mm/dd/yy             | **Not supported** |
| `%F`      | YYYY-MM-DD         | YYYY-MM-DD           | **Not supported** |
| `%R`      | HH:MM              | HH:MM                | **Not supported** |
| `%T`      | HH:MM:SS           | HH:MM:SS             | **Not supported** |
| `%V`      | ISO week           | ISO week             | **Not supported** |
| `%u`      | ISO weekday        | ISO weekday          | **Not supported** |
| `%e`      | Day (space-padded) | Day (space-padded)   | **Not supported** |
| `%n`      | Newline            | Newline              | **Not supported** |
| `%t`      | Tab                | Tab                  | **Not supported** |
| `%O*`     | Locale-specific    | Alternative numerals | Varies            |
| `%E*`     | Locale-specific    | Era-based            | Varies            |

**Key insight**: NovaSharp's pure C# implementation is actually MORE correct (POSIX-compliant on all platforms) than reference Lua on Windows. Mark these tests as `@novasharp-only: true`.

### NaN String Formatting

Different C libraries format NaN differently:

| Platform | `tostring(0/0)` output |
| -------- | ---------------------- |
| Linux    | `nan`                  |
| macOS    | `nan`                  |
| Windows  | `-nan(ind)` or `-nan`  |

The comparison harness normalizes all NaN variants with this regex:

```python
# Case-insensitive, matches: nan, -nan, -nan(ind), NaN, etc.
result = re.sub(r'-?nan(\(ind\))?', 'nan', result, flags=re.IGNORECASE)
```

### Windows Lua Binary Build Configuration

CI Windows Lua binaries are often built WITHOUT `LUA_COMPAT_*` flags, causing missing functions:

| Missing in Windows CI Lua  | Affected Versions | NovaSharp Has It? |
| -------------------------- | ----------------- | ----------------- |
| `math.log10`               | 5.2+              | ✅ Yes            |
| `math.frexp`               | 5.3+              | ✅ Yes            |
| `math.ldexp`               | 5.3+              | ✅ Yes            |
| `math.pow`                 | 5.3+              | ✅ Yes            |
| `loadstring`               | 5.2+              | ✅ Yes            |
| `__le` metamethod fallback | 5.4               | ✅ Yes            |

**These are Lua build configuration issues, NOT NovaSharp bugs.** Mark these tests as `@novasharp-only: true` with `@compat-notes` explaining the platform difference.

### When to Use @novasharp-only for Platform Differences

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Windows strftime does not support %C specifier; NovaSharp is POSIX-compliant

print(os.date("%C"))  -- NovaSharp correctly outputs century on all platforms
```

| Scenario                                 | Action                                       |
| ---------------------------------------- | -------------------------------------------- |
| NovaSharp more correct than platform Lua | `@novasharp-only: true` with `@compat-notes` |
| Platform C library quirk                 | `@novasharp-only: true` with `@compat-notes` |
| Lua build missing compat flags           | `@novasharp-only: true` with `@compat-notes` |
| Actual NovaSharp bug                     | **Fix production code**                      |

______________________________________________________________________

## Regenerating Fixtures

After changing tests, regenerate the corpus:

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

______________________________________________________________________

## Output Normalization

The comparison harness normalizes outputs before comparison to handle expected variations. Understanding these normalizations helps diagnose failures.

### Normalizations Applied

```python
# 1. NaN normalization (platform-specific)
result = re.sub(r'-?nan(\(ind\))?', 'nan', result, flags=re.IGNORECASE)

# 2. Version string normalization
result = re.sub(r'Lua 5\.\d+', '<lua-version>', result)
result = re.sub(r'NovaSharp \d+\.\d+\.\d+\.\d+', '<lua-version>', result)

# 3. Compatibility header stripping
result = re.sub(r'^\[compatibility\].*$\n?', '', result, flags=re.MULTILINE)

# 4. Memory address normalization
result = re.sub(r'0x[0-9a-fA-F]+', '<addr>', result)
result = re.sub(r'(table|function|userdata|thread): <addr>', r'\1: <addr>', result)

# 5. Line number normalization in errors
result = re.sub(r'(\.lua):(\d+):', r'\1:<line>:', result)
result = re.sub(r'\[C\]:\s*-?\d+:', '[C]:<line>:', result)
result = re.sub(r'\[string "[^"]*"\]:\d+:', '[string "<chunk>"]:<line>:', result)

# 6. Float precision normalization (rounds to 10 decimal places)
def normalize_float(match):
    num = float(match.group(0))
    if abs(num) < 1e-10:
        return "0"
    rounded = round(num, 10)
    if rounded == int(rounded):
        return str(int(rounded))
    return str(rounded).rstrip('0').rstrip('.')
result = re.sub(r'-?\d+\.\d+(?:e[+-]?\d+)?', normalize_float, result)
```

### What Normalization Does NOT Fix

Normalization handles cosmetic differences only. These are still **BUGS**:

- Different error TYPES (type error vs arithmetic error)
- Different error CAUSES (indexing nil vs calling nil)
- Missing or extra output lines
- Semantically different values
- Different function behavior

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

See [lua-fixture-creation](lua-fixture-creation.md) for complete fixture creation guidelines including required metadata headers, version-specific naming conventions, and validation checklist.

**Remember**: Every new fixture requires:

1. A corresponding C# TUnit test (see [tunit-test-writing](tunit-test-writing.md))
1. Corpus regeneration: `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`
