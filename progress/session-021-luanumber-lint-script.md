# LuaNumber Usage Lint Script - Phase 5 Complete

**Date**: 2025-12-15
**PLAN.md Section**: §8.37 (Phase 5), §8.33
**Status**: Complete

## Summary

Completed Phase 5 of the Comprehensive LuaNumber Usage Audit (§8.37) by creating a lint script to detect potentially problematic patterns where raw C# numeric types are used instead of `LuaNumber` for Lua math operations. This also completes §8.33 (LuaNumber Compliance Sweep).

## What Was Done

### 1. Created Lint Script

Created `scripts/lint/check-luanumber-usage.py` that:

- Scans all C# files in `src/runtime/WallstopStudios.NovaSharp.Interpreter/`
- Detects potentially problematic patterns:
  - Direct `.Number` arithmetic (e.g., `x.Number + y.Number`)
  - Direct `.Number` comparisons (e.g., `x.Number < y.Number`)
  - Explicit casts like `(int)x.Number`, `(long)x.Number`, `(double)x.Number`
  - `Math.Floor(x.Number)` patterns that may lose precision
- Maintains a list of known-safe patterns that have been audited
- Reports issues with file path, line number, and description
- Supports `--detailed` and `--fail-on-issues` flags

### 2. Audited Existing Code

Verified all current `.Number` usages in the runtime are either:

1. **Safe patterns**: Argument count retrieval, type checks, DynValue constructors
1. **Audited files**: Files that use intentional patterns with documented rationale:
   - `Utf8Module.cs` - Bounds validated before casting
   - `LuaBase.cs` / `LuaPort` - Low-level interop
   - `StringRange.cs` - Documented Lua 5.1/5.2 truncation behavior
   - `TableIteratorsModule.cs` - ipairs uses sequential integer indices
   - `StandardEnumUserDataDescriptor.cs` - Enum values are bounded
   - `StringModule.cs` - String operations audited

### 3. Current Status

```
$ python3 scripts/lint/check-luanumber-usage.py
======================================================================
LuaNumber Usage Lint Report
======================================================================

✅ No issues found.
```

## Usage

```bash
# Basic check
python3 scripts/lint/check-luanumber-usage.py

# Detailed output with line-by-line issues
python3 scripts/lint/check-luanumber-usage.py --detailed

# Fail CI if issues found
python3 scripts/lint/check-luanumber-usage.py --fail-on-issues
```

## Files Changed

- Created: `scripts/lint/check-luanumber-usage.py`
- Updated: `scripts/lint/README.md` - Added documentation for new script
- Updated: `PLAN.md`:
  - §8.37 marked as ✅ COMPLETE
  - §8.33 marked as ✅ COMPLETE

## Problematic Patterns Detected

The script looks for these patterns that may indicate precision loss:

| Pattern                | Risk                                         |
| ---------------------- | -------------------------------------------- |
| `.Number + .Number`    | Integer subtype lost                         |
| `.Number < .Number`    | Precision comparison issues                  |
| `(int)x.Number`        | Truncation of large values                   |
| `(long)x.Number`       | Precision issues for large floats            |
| `Math.Floor(x.Number)` | Double input may already have lost precision |

## Safe Patterns (Whitelisted)

The script ignores these known-safe patterns:

- Argument count retrieval (always small values)
- Type checks (`IsNumber`, `AsNumber`)
- LuaNumber member access
- DynValue constructors
- Code using `ToLongWithValidation` helper
- Audited files with documented rationale

## Related

- PLAN.md §8.37: Comprehensive LuaNumber Usage Audit
- PLAN.md §8.33: LuaNumber Compliance Sweep
- `progress/2025-12-13-for-loop-luanumber-precision.md`
- `progress/2025-12-13-binary-operator-expression-luanumber-fix.md`
