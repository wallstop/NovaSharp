# Progress: LuaVersionGuard Implementation

**Date**: 2025-12-11
**Task**: Create `LuaVersionGuard` helper class for Lua version compatibility
**Status**: ✅ Complete
**Related Section**: PLAN.md §8.41 (Comprehensive Lua Version Compatibility Audit)

---

## Summary

Implemented the `LuaVersionGuard` helper class to provide version-aware guards for Lua standard library functions that were added, deprecated, or removed across different Lua versions (5.1, 5.2, 5.3, 5.4, 5.5).

## Files Created

### Production Code

**`src/runtime/WallstopStudios.NovaSharp.Interpreter/Compatibility/LuaVersionGuard.cs`**

A static helper class providing:

| Method | Purpose |
|--------|---------|
| `ThrowIfUnavailable(version, minVersion, funcName)` | Throws if a function isn't available in the current Lua version (for functions added in later versions) |
| `ThrowIfRemoved(version, maxVersion, funcName)` | Throws if a function was removed in the current Lua version (for deprecated functions) |
| `ThrowIfOutsideRange(version, minVersion, maxVersion, funcName)` | Throws if a function is outside its supported version range |
| `IsAvailable(version, minVersion)` | Non-throwing check for function availability |
| `IsRemoved(version, maxVersion)` | Non-throwing check for function removal |
| `IsAvailableInRange(version, minVersion, maxVersion?)` | Non-throwing check for version range |
| `GetVersionDisplayName(version)` | Human-readable version string (e.g., "Lua 5.4") |
| `GetNextVersionDisplayName(version)` | Next version string for "removed in X" messages |

### Test Code

**`src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/LuaVersionGuardTUnitTests.cs`**

Comprehensive test suite with 30+ data-driven tests covering:
- `ThrowIfUnavailable` throwing and non-throwing scenarios
- `ThrowIfRemoved` throwing and non-throwing scenarios
- `IsAvailable` correctness across all version combinations
- `IsRemoved` correctness across all version combinations
- `IsAvailableInRange` with and without maximum version
- `ThrowIfOutsideRange` throwing and non-throwing scenarios
- `GetVersionDisplayName` and `GetNextVersionDisplayName` for all versions
- Error message format validation

## Key Findings

### Existing Version Guard Infrastructure

During implementation, discovered that NovaSharp already has a robust version guard system via `LuaCompatibilityAttribute`:

1. **`[LuaCompatibility(minVersion, maxVersion)]`** attribute on module methods
2. **`IsMemberCompatible()`** in `ModuleRegister.cs` filters functions at registration time
3. Functions not available in the current Lua version are simply not registered

This means:
- `coroutine.close()` already has `[LuaCompatibility(Lua54)]` — not registered in Lua 5.1-5.3 mode
- `warn()` already has `[LuaCompatibility(Lua54)]` — not registered in older modes
- Calling unavailable functions yields standard Lua error: "attempt to call a nil value"

### When to Use LuaVersionGuard

The new `LuaVersionGuard` class is useful for:

1. **Custom error messages** when the standard "nil value" error isn't descriptive enough
2. **Runtime checks** when the function exists but has version-specific behavior
3. **Guard conditions** in complex logic that depends on version availability
4. **Documentation** of version requirements in code

## Test Results

```
Passed! - Failed: 0, Passed: 4701, Skipped: 0, Total: 4701, Duration: 24s
```

All 4,701 tests pass, including the new 30+ tests for `LuaVersionGuard`.

## PLAN.md Updates

Updated the following sections:

1. **§8.38 (Lua Spec Compliance)**: Marked `LuaVersionGuard` creation as complete
2. **§8.41 (Lua Version Compatibility Audit)**: 
   - Marked steps 1-3 as complete
   - Documented that `LuaCompatibilityAttribute` handles most cases
   - Updated status to reflect progress

## Next Steps

Per PLAN.md, the remaining work in §8.41 is:

1. **Version-specific random providers** (lower priority):
   - POSIX LCG for 5.1/5.2/5.3
   - xoshiro256** for 5.4
   - `math.randomseed()` return value change in 5.4

## Architecture Decision

The `LuaVersionGuard` class complements rather than replaces `LuaCompatibilityAttribute`:

| Mechanism | Use Case | Error Type |
|-----------|----------|------------|
| `LuaCompatibilityAttribute` | Prevent function registration | "attempt to call a nil value" (standard Lua) |
| `LuaVersionGuard.ThrowIfUnavailable()` | Runtime check with custom message | Descriptive ScriptRuntimeException |
| `LuaVersionGuard.IsAvailable()` | Conditional logic | No exception |

This layered approach ensures Lua spec compliance while allowing informative diagnostics when needed.
