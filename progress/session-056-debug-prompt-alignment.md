# Session 056: Debug Module & Version Parity Updates

**Date**: 2025-12-21
**Focus**: Complete §8.44 (Lua Output Format Alignment) and §9.9/§9.10 (Version Parity Items)

## Summary

This session completed multiple PLAN.md items:

1. ✅ Phase 4 of §8.44 - Debug prompt alignment
1. ✅ §9.9 - `debug.setcstacklimit` (N/A - C API only)
1. ✅ §9.9 - Multi-user-value support for debug.getuservalue/setuservalue
1. ✅ §9.10 - bit32 deprecation (already correct)

______________________________________________________________________

## Part 1: Debug Prompt Alignment (§8.44 Phase 4)

Changed `debug.debug()` prompt from `"> "` to `"lua_debug> "` to match reference Lua.

### Changes

- **DebugModule.cs**: Changed prompt to constant `"lua_debug> "`
- **DebugModuleTUnitTests.cs**: Added `DebugDebugUsesLuaDebugPrompt` test

### Test Results

```bash
./scripts/test/quick.sh DebugDebugUsesLuaDebugPrompt
✅ 5/5 passed (all Lua versions)
```

______________________________________________________________________

## Part 2: debug.setcstacklimit (§9.9)

**Status**: N/A - Does NOT exist as Lua function

### Finding

- `lua_setcstacklimit` is a **C API function only** (for host programs)
- It is NOT exposed to Lua scripts as `debug.setcstacklimit`
- The function is deprecated in Lua 5.5

No implementation needed.

______________________________________________________________________

## Part 3: Multi-User-Value Support (§9.9)

Implemented the `n` parameter for `debug.getuservalue` and `debug.setuservalue` per Lua 5.4 spec.

### Changes

**DebugModule.cs**:

- `debug.getuservalue(u, n)` - Added optional `n` parameter (1-based slot index)
  - Returns tuple `(value, hasValue)` in Lua 5.4+ (hasValue is boolean)
  - Returns single value in Lua 5.1-5.3 (backward compatible)
- `debug.setuservalue(u, value, n)` - Added optional `n` parameter
  - Returns nil on failure (invalid slot) in Lua 5.4+
  - Returns userdata on success

**Note**: NovaSharp only supports single user value (slot 1). Slot n≠1 returns (nil, false).

### Test Results

```bash
./scripts/test/quick.sh UserValue
✅ 45/45 passed
```

### New Tests Added

- `GetUserValueReturnsValueAndTrueForValidUserData54Plus`
- `GetUserValueReturnsNilAndFalseForInvalidSlot54Plus`
- `GetUserValueReturnsNilAndFalseForNonUserData54Plus`
- `SetUserValueWithNParam54Plus_Slot1Succeeds`
- `SetUserValueWithNParam54Plus_InvalidSlotReturnsNil`
- `SetUserValueDefaultsNTo1_54Plus`
- `GetUserValueReturnsOneValue_Pre54`

______________________________________________________________________

## Part 4: bit32 Deprecation (§9.10)

**Status**: Already Correct - No changes needed

### Finding

Reference Lua does NOT emit runtime deprecation warnings for `bit32`:

- **Lua 5.1**: bit32 did not exist
- **Lua 5.2**: Part of standard library
- **Lua 5.3**: Removed from standard library (native operators instead)
- **Lua 5.4+**: Completely removed

NovaSharp already handles this correctly - bit32 is only available in Lua 5.2 mode.

### New Tests Added

14 new tests verifying version-specific bit32 availability:

- `Bit32IsNilInLua51`
- `Bit32IsAvailableInLua52`
- `Bit32IsNilInLua53Plus`
- `NativeBitwiseOperatorsWorkInLua53Plus`

### Test Results

```bash
./scripts/test/quick.sh bit32
✅ 45/45 passed
```

______________________________________________________________________

## Files Modified

| File                       | Changes                                           |
| -------------------------- | ------------------------------------------------- |
| `DebugModule.cs`           | Debug prompt, getuservalue/setuservalue `n` param |
| `DebugModuleTUnitTests.cs` | New tests for debug prompt and user values        |
| `Bit32ModuleTUnitTests.cs` | New tests for version-specific availability       |
| `PLAN.md`                  | Marked items complete                             |

______________________________________________________________________

## PLAN.md Items Completed

- §8.44 Phase 4: Debug Prompt ✅
- §9.9 setcstacklimit: N/A (C API only) ✅
- §9.9 Multi-user-value: Complete ✅
- §9.10 bit32 deprecation: Already correct ✅

## Related Sessions

- Session 053: Address format alignment
- Session 054: Variable names in errors
- Session 055: Module search path in errors
