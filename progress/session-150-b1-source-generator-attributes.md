# Session 150: B1 Source Generator Attributes

Date: 2026-07-05

## Summary

- Started Phase B1 by adding the public source-generator attribute contract to the root `NovaSharp` API.
- Added `LuaObjectAttribute`, `LuaMemberAttribute`, `LuaMetamethodAttribute`, `LuaMetamethodKind`, and `LuaIgnoreAttribute`.
- Updated the public API baseline so the facade surface remains explicitly reviewed.
- Added smoke coverage for attribute metadata, target scopes, multiple metamethod annotations, and invalid empty/custom-name construction.
- Kept the slice intentionally limited to public metadata. Generator output, analyzer diagnostics, enum table exposure, and stub emission remain open B1 work.

## Adversarial Review

- A sub-agent flagged positional assertions over reflection attribute order as fragile. The test now sorts metamethod names before asserting.
- The same review noted that member lookup failures would be less diagnostic. The test now asserts reflected members and attributes are present before checking their values.
- The review called out the nullable-looking `Name` contract. Null remains intentional for the default-name convention because the repo forbids nullable reference syntax; XML docs state that null means the generator should use the CLR name.

## Validation

- `dotnet tool run csharpier format src/runtime/WallstopStudios.NovaSharp.Interpreter/Api/LuaInteropAttributes.cs src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Smoke/NovaSharpFacadeSmokeTUnitTests.cs` completed.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 45 tests, 0 failures.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14,830 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` completed successfully; it reported existing documentation and skill metadata warnings and refreshed the naming audit namespace-count baseline.

## Notes

- No standalone Lua fixture was added because this is compile-time CLR metadata for the future source generator, not Lua runtime behavior.
- The broader B1 source generator implementation remains unstarted.
