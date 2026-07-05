# Session 154: B1 Analyzer Diagnostics

Date: 2026-07-05

## Summary

- Added the `WallstopStudios.NovaSharp.Interop.Generator` analyzer package under `src/interop/`.
- Implemented B1 source-generator contract diagnostics:
  - `NS0001`: `[LuaObject]` type is not partial.
  - `NS0002`: exposed member uses an unsupported type.
  - `NS0003`: exposed signature uses ref/out/in, pointer, or open generic shapes.
  - `NS0004`: duplicate Lua-visible member names.
  - `NS0005`: async return type requires the future adapter package.
  - `NS0006`: Lua interop attribute appears outside a `[LuaObject]` type.
- Added focused Roslyn/TUnit analyzer coverage for the valid contract and each diagnostic.
- Added the analyzer project to `src/NovaSharp.sln` and locked its Roslyn package graph.
- Addressed Copilot review feedback by clarifying `NS0006` wording for all Lua interop attributes, narrowing the package description to the analyzer currently shipped, making trusted-platform-assembly discovery fail with a direct diagnostic in analyzer tests, and rejecting analyzer test fixtures that do not compile cleanly.

## Rationale

- B1 had the public attribute contract but no analyzer behavior.
- The analyzer package catches contract violations before generator output exists, keeping future generator work bounded to known-valid input shapes.
- The package is tested directly instead of attached to runtime projects as a live analyzer, avoiding unrelated warning churn before generated bindings are consumed.

## Validation

- `dotnet build src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj --no-restore` passed.
- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` passed: 9 tests, 0 failures.
- `dotnet pack src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj -c Release --no-restore` passed.
- `unzip -l src/interop/WallstopStudios.NovaSharp.Interop.Generator/bin/Release/WallstopStudios.NovaSharp.Interop.Generator.3.0.0.nupkg` confirmed `analyzers/dotnet/cs/WallstopStudios.NovaSharp.Interop.Generator.dll` and `README.md` are present.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh --full` passed: 14,840 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` completed successfully with existing documentation audit and skill metadata warnings.
