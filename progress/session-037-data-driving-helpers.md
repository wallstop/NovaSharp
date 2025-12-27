# Session 037: TUnit Data-Driving Helper Infrastructure

**Date**: 2025-12-19\
**Focus**: Test Data-Driving Helper Infrastructure (§8.42) — Phase 1 Implementation\
**Status**: Phase 1 Complete, Phase 1b (Build Validation) Blocked

## Summary

Implemented core TUnit data-driven helper attributes to reduce test boilerplate for multi-version Lua testing. All helper attributes are complete and integrated into the test infrastructure. Build validation is blocked by a devcontainer PATH issue.

## Problem Addressed

NovaSharp tests require explicit `[Arguments]` attributes for every Lua version (5.1, 5.2, 5.3, 5.4, 5.5). For tests with multiple inputs, this creates massive boilerplate:

- 5 versions × N inputs = 5N `[Arguments]` lines per test
- Error-prone manual enumeration
- Inconsistent version coverage across test files

## Implementation Completed

### Helper Attributes Created

All attributes located in `src/tests/TestInfrastructure/TUnit/`:

| Attribute                   | File                                    | Purpose                              |
| --------------------------- | --------------------------------------- | ------------------------------------ |
| `AllLuaVersionsAttribute`   | `LuaVersionDataSourceAttributes.cs:161` | All 5 versions (5.1-5.5)             |
| `LuaVersionsFromAttribute`  | `LuaVersionDataSourceAttributes.cs:169` | Minimum version (inclusive)          |
| `LuaVersionsUntilAttribute` | `LuaVersionDataSourceAttributes.cs:176` | Maximum version (inclusive)          |
| `LuaVersionRangeAttribute`  | `LuaVersionDataSourceAttributes.cs:183` | Bounded version range                |
| `LuaTestMatrixAttribute`    | `LuaTestMatrixAttribute.cs`             | Cartesian product: versions × inputs |

### Supporting Infrastructure

- `LuaVersionData` static class with `AllVersions`, `From()`, `Until()`, `Range()` methods
- All attributes inherit from TUnit's `UntypedDataSourceGeneratorAttribute`
- Validation throws `ArgumentException` for empty/invalid ranges
- `LuaTestMatrixAttribute` supports `MinimumVersion`/`MaximumVersion` properties

### Pilot Migration

`MathModuleTUnitTests.cs` converted as proof-of-concept:

- ~20 tests using `[AllLuaVersions]`
- Version-specific tests using `[LuaVersionsFrom]` and `[LuaVersionRange]`

## Blocker: Devcontainer PATH Issue

**Problem**: The bash shell in the devcontainer does not have `dotnet` on PATH.

```
$ ./scripts/build/quick.sh
dotnet: command not found
```

**Observations**:

- `dotnet --info` works in PowerShell
- `dotnet` works in zsh
- The bash shell specifically has PATH issues
- TUnit DLLs exist at `/home/vscode/.nuget/packages/tunit.core/1.5.70/`

**Investigation Needed**:

- Check `/etc/profile.d/` for dotnet PATH setup
- Verify `.bashrc` sources the correct profile
- Locate dotnet installation directory

## Documentation Updates

- Updated `.llm/context.md` with helper usage guidelines
- Updated PLAN.md §8.42 with implementation details and current status

## Next Steps

1. **Resolve bash PATH issue** — Investigate devcontainer configuration
1. **Build validation** — Run `./scripts/build/quick.sh --all` once PATH is fixed
1. **Test validation** — Run `./scripts/test/quick.sh -c MathModule` to verify helpers work
1. **Phase 2 migration** — Convert remaining ~424 Lua tests to use helpers
1. **Lint rule** — Add script to flag verbose `[Arguments]` patterns

## Files Modified

- `src/tests/TestInfrastructure/TUnit/LuaVersionDataSourceAttributes.cs` (new)
- `src/tests/TestInfrastructure/TUnit/LuaTestMatrixAttribute.cs` (new)
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs` (pilot migration)
- `.llm/context.md` (documentation)
- `PLAN.md` (status update)

## Related

- PLAN.md §8.42: Test Data-Driving Helper Infrastructure
- PLAN.md §8.39: TUnit Test Multi-Version Coverage Audit
- progress/session-025 through session-036: Prior TUnit version coverage work
