# Session 160: B1 Generator Golden Tests

Date: 2026-07-05

## Summary

- Added the first `LuaInteropSourceGenerator` incremental generator to the interop generator package.
- Generated deterministic companion partial output for valid top-level `[LuaObject]` class, struct, record, and record struct inputs:
  - Private `ReadOnlySpan<LuaValue>` string-switch dispatch placeholder.
  - Private manifest string containing the Lua object name, CLR type, Lua-visible member names, and referenced enum types.
  - `[LuaIgnore]` filtering so ignored exposed members are omitted from generated output.
- Added golden-source generator tests that run the generator with `CSharpGeneratorDriver`, compile the generated source, and compare the generated companion partial against a checked-in golden file.
- Added a negative generator test for non-partial `[LuaObject]` input so analyzer-owned invalid shapes do not produce companion partials.
- Updated the generator package description now that it contains both analyzer and source-generator entry points.
- Addressed Copilot review feedback by allowing record declarations and emitting `partial record`/`partial record struct` companion types instead of silently skipping records or generating mismatched partial type keywords.
- Hardened the naming and documentation audit type-declaration parsers for `record struct` declarations after pre-commit exposed the naming audit's false positive on the new record-struct fixture.

## Rationale

- B1 already had analyzer diagnostics, but generator output was still absent.
- A deterministic generated shape and golden test harness give future runtime binding, enum table, async adapter, and stub-output work a stable review surface.
- The slice intentionally avoids public runtime registration or callback APIs because those are still unsettled in the facade; the generated dispatch method is private and inert until the concrete binding surface lands.

## Validation

- `dotnet build src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj --no-restore` passed.
- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` passed: 5 tests, 0 failures.
- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` passed: 30 tests, 0 failures.
- `./scripts/build/quick.sh` passed.
- `dotnet pack src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj -c Release --no-restore` passed.
- `git diff --check` passed.
- `python3 tools/NamingAudit/naming_audit.py --write-log docs/audits/naming_audit.log` passed.
- `python3 tools/DocumentationAudit/documentation_audit.py --write-log docs/audits/documentation_audit.log` passed with the existing 74 undocumented declarations report.
- `./scripts/test/quick.sh` passed: 14,881 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` completed successfully with existing LLM skill metadata warnings.
