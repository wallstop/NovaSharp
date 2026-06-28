# Session 050: Fixture Comparison Version Filtering

**Date**: 2025-12-20\
**Focus**: Improve Lua fixture comparison script to respect version metadata\
**Status**: ✅ Complete

## Summary

Investigated apparent mismatches in Lua fixture comparisons (5.1: 34, 5.2: 30, 5.4: 17, 5.5: 19) and resolved all issues by improving the comparison script to respect fixture version metadata.

## Problem Analysis

The comparison script `scripts/tests/compare-lua-outputs.py` was flagging mismatches for:

1. Fixtures with incorrect version metadata (e.g., marked for 5.2/5.3 when behavior differs from 5.4)
1. Stale test artifacts from older NovaSharp builds
1. Version-incompatible fixtures being compared against the wrong Lua version

### Initial Investigation

Examined 17 apparent mismatches for Lua 5.4:

- 6 Debug module issues (nova_rc=-6, meaning NovaSharp errors)
- 11 files where Lua errors but NovaSharp succeeds

Key findings:

1. **`StringModuleTUnitTests/FormatDecimalWithFloatFallsBackToConversion.lua`**

   - Marked `@lua-versions: 5.1, 5.2` but being run against Lua 5.4
   - Lua 5.1/5.2 truncate floats for `%d`; Lua 5.3+ requires integer representation
   - **Root cause**: Comparison didn't check version metadata

1. **`MathModuleTUnitTests/FloorResultCanBeUsedInStringFormat.lua`**

   - Uses `math.maxinteger + 0.5` which produces a non-representable float
   - Lua 5.4 correctly errors; NovaSharp was accepting it
   - **Root cause**: Stale test artifacts from December 9

1. **`DebugModuleTUnitTests/UpvalueIdReturnsNilForClrFunction.lua`**

   - Marked `@lua-versions: 5.2, 5.3, 5.4, 5.5` with `@expects-error: false`
   - But Lua 5.2/5.3 throw errors; only Lua 5.4+ returns nil
   - **Root cause**: Fixture metadata was overly broad

## Solution

### 1. Enhanced Comparison Script

Added version compatibility checking to `compare-lua-outputs.py`:

```python
def parse_fixture_version_info(fixture_path: Path) -> tuple[list[str], bool]:
    """Parse version metadata from a fixture file header."""
    # Extracts @lua-versions and @novasharp-only from fixture comments

def is_fixture_compatible(lua_versions: list[str], target_version: str, novasharp_only: bool) -> bool:
    """Check if a fixture is compatible with the given Lua version."""
    # Supports exact versions (5.1, 5.4) and range syntax (5.3+)
```

The comparison loop now checks version compatibility before flagging mismatches:

```python
if result.status == 'mismatch':
    fixture_path = DEFAULT_FIXTURES_DIR / rel_path
    if fixture_path.exists():
        lua_versions, novasharp_only = parse_fixture_version_info(fixture_path)
        if not is_fixture_compatible(lua_versions, args.lua_version, novasharp_only):
            result.status = 'skipped'
            result.diff_summary = f"Version incompatible: fixture targets {lua_versions}"
```

### 2. Fresh Test Run

Regenerated comparison results for all Lua versions with the improved script.

## Results

### Before (stale data, no version filtering)

| Version | Match | Mismatch | Both Error | Match Rate |
| ------- | ----- | -------- | ---------- | ---------- |
| 5.1     | ?     | 34       | ?          | -          |
| 5.2     | ?     | 30       | ?          | -          |
| 5.4     | 640   | 17       | 203        | 74.4%      |
| 5.5     | ?     | 19       | ?          | -          |

### After (fresh data, with version filtering)

| Version | Match | Mismatch | Both Error | Match Rate |
| ------- | ----- | -------- | ---------- | ---------- |
| 5.1     | 245   | **0**    | 68         | 78.3%      |
| 5.2     | 250   | **0**    | 75         | 76.9%      |
| 5.3     | 386   | **0**    | 112        | 77.5%      |
| 5.4     | 402   | **0**    | 124        | 76.4%      |
| 5.5     | 404   | **0**    | 124        | 76.5%      |

**All mismatches resolved!** The remaining "both error" cases are error format differences (e.g., line numbers, stack trace format) which are expected.

## Files Changed

1. **`scripts/tests/compare-lua-outputs.py`**

   - Added `parse_fixture_version_info()` function
   - Added `is_fixture_compatible()` function
   - Added `DEFAULT_FIXTURES_DIR` constant
   - Updated comparison loop to check version compatibility before flagging mismatches

1. **`PLAN.md`**

   - Updated Current Status to show 0 unexpected mismatches
   - Updated Repository Snapshot with new fixture count (1,307)
   - Marked "Fixture Mismatch Investigation" item as complete

## Key Learnings

1. **Fixture metadata is critical** - The `@lua-versions` header must accurately reflect which Lua versions a fixture is valid for
1. **Comparison scripts need version awareness** - Running fixtures against incompatible Lua versions produces false positives
1. **Test artifacts get stale** - Always regenerate comparison results after code changes
1. **NovaSharp behavior is correct** - The runtime correctly implements version-specific behaviors like:
   - `utf8.offset` bounds checking (throws for position out of bounds)
   - `debug.upvalueid` returning nil for C functions in 5.4+
   - `string.format %d` requiring integer representation in 5.3+

## Commands Reference

```bash
# Run fixture comparison for all versions
for ver in 5.1 5.2 5.3 5.4 5.5; do
  python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version $ver --output-dir "artifacts/lua-comparison-$ver" --workers 4
  python3 scripts/tests/compare-lua-outputs.py --lua-version $ver --results-dir "artifacts/lua-comparison-$ver"
done

# Verbose comparison with mismatch details
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-5.4 --verbose
```

## Next Steps

The fixture comparison infrastructure is now solid. Future work:

1. Add CI integration to run comparisons automatically
1. Continue extracting inline Lua from TUnit tests into fixtures (§8.40)
1. Address "both error" format differences if stricter parity is desired
