# Session 039: TUnit Data-Driving Helpers — Phase 2 Migration Progress

**Date**: 2025-12-19
**Focus**: Test Data-Driving Helper Infrastructure (§8.42) — Phase 2 Migration
**Status**: 🟡 In Progress

## Summary

Continued Phase 2 migration work on the data-driving helpers initiative. The core helper infrastructure (Phase 1/1b) is complete and validated. Phase 2 focuses on migrating existing test files to use the new helpers, reducing boilerplate significantly.

## Current State

### Helper Infrastructure (Complete ✅)

All helper attributes are fully implemented and tested:

| Attribute                     | Purpose                                   | Location                            |
| ----------------------------- | ----------------------------------------- | ----------------------------------- |
| `[AllLuaVersions]`            | Test against all 5 Lua versions (5.1-5.5) | `LuaVersionDataSourceAttributes.cs` |
| `[LuaVersionsFrom(version)]`  | Test from specified version onwards       | `LuaVersionDataSourceAttributes.cs` |
| `[LuaVersionsUntil(version)]` | Test up to specified version              | `LuaVersionDataSourceAttributes.cs` |
| `[LuaVersionRange(min, max)]` | Test specific version range               | `LuaVersionDataSourceAttributes.cs` |
| `[LuaTestMatrix(inputs...)]`  | Cartesian product: versions × inputs      | `LuaTestMatrixAttribute.cs`         |

### Migration Progress

| File                         | Total Tests | Converted | Remaining | Status         |
| ---------------------------- | ----------- | --------- | --------- | -------------- |
| `MathModuleTUnitTests.cs`    | ~60         | ~60       | 0         | ✅ Complete    |
| `SimpleTUnitTests.cs`        | 83          | ~25       | ~58       | 🟡 Partial     |
| `ClosureTUnitTests.cs`       | TBD         | 0         | TBD       | 📋 Not started |
| `CoroutineTUnitTests.cs`     | TBD         | 0         | TBD       | 📋 Not started |
| `ErrorHandlingTUnitTests.cs` | TBD         | 0         | TBD       | 📋 Not started |
| `TableModuleTUnitTests.cs`   | TBD         | 0         | TBD       | 📋 Not started |
| Other files                  | ~400+       | TBD       | TBD       | 📋 Not started |

### SimpleTUnitTests.cs Progress

- **Total tests**: 83
- **Converted to `[AllLuaVersions]`**: ~25 tests (lines 21-449)
- **Pattern observed**: Tests with `new Script(version, ...)` already have version parameter
- **Remaining work**: Tests after line 449 need `[AllLuaVersions]` attribute added and version parameter added to method signature

## Next Steps

### Immediate (Next Session)

1. **Complete SimpleTUnitTests.cs migration**

   - Convert remaining ~58 tests to use `[AllLuaVersions]`
   - Each test needs:
     - Add `[AllLuaVersions]` attribute
     - Add `LuaCompatibilityVersion version` parameter
     - Update `new Script()` calls to `new Script(version, ...)`

1. **Migrate ClosureTUnitTests.cs**

   - Similar pattern to SimpleTUnitTests

1. **Migrate CoroutineTUnitTests.cs**

   - May have version-specific tests requiring `[LuaVersionsFrom]`

### Medium Term

4. **Create automated migration script**

   - Pattern-match `new Script()` without version parameter
   - Suggest or auto-apply `[AllLuaVersions]` + version parameter
   - Flag tests needing manual review (version-specific behavior)

1. **Handle version-specific tests**

   - Audit for features only available in certain Lua versions
   - Apply `[LuaVersionsFrom]`, `[LuaVersionsUntil]` as appropriate
   - Add negative tests for unavailable features

### Documentation

6. **Update docs/Testing.md**

   - Add section on using data-driving helpers
   - Include examples for each attribute type
   - Document when to use each helper

1. **Add lint rule**

   - Flag verbose `[Arguments]` patterns that could use helpers
   - Run in CI to prevent regression

## Files Modified This Session

None — documentation update only per user request.

## Related Sessions

- [Session 037](session-037-data-driving-helpers.md) — Initial helper design and prototype
- [Session 038](session-038-data-driving-helpers-phase1b.md) — Phase 1b build validation and fixes

## Related PLAN.md Sections

- §8.42 — Test Data-Driving Helper Infrastructure
- §8.39 — TUnit Multi-Version Coverage Audit (related initiative)
