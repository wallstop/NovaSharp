# Real-World Lua Script Corpus

NovaSharp executes a curated set of third-party Lua scripts to spot regressions that unit coverage alone might miss. Every fixture is redistributed under a permissive license with upstream attribution preserved next to the source (`LICENSE` files sit beside the fixtures).

## Current Corpus

| Script                 | Version / Commit                                    | License | Local Path                                                                | Source                                |
| ---------------------- | --------------------------------------------------- | ------- | ------------------------------------------------------------------------- | ------------------------------------- |
| `json.lua` (rxi)       | v0.1.2 (`d1e3b0f5d0f3d3493c7dadd0bb54135507fcebd7`) | MIT     | `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/rxi-json`       | https://github.com/rxi/json.lua       |
| `inspect.lua` (kikito) | v3.1.0 (`2cc61aa5a98ea852e48fd28d39de7b335ac983c6`) | MIT     | `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/kikito-inspect` | https://github.com/kikito/inspect.lua |

## Execution Harness

- `RealWorldScriptTests` (`src/tests/NovaSharp.Interpreter.Tests/EndToEnd/RealWorldScriptTests.cs`) loads each script with `Script.DoString` under `CoreModules.PresetComplete` and asserts behaviour that mirrors real-world usage:
  - `json.lua`: round-trips nested tables via `encode`/`decode`, keeping booleans, numeric fields, and array elements intact.
  - `inspect.lua`: formats nested tables, including a metatable-backed value, and verifies the readable output.
- `NovaSharp.Interpreter.Tests.csproj` copies `Fixtures/RealWorld/**` into the test output so the harness never relies on runtime network access.

## Maintenance Checklist

- Add new scripts under `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/<vendor-script>` with the upstream `LICENSE`.
- Extend `RealWorldScriptTests` with a descriptive `TestCaseData` entry that exercises a behaviour NovaSharp must support.
- Update the table above (and `docs/ThirdPartyLicenses.md`) with the script name, tag/commit, license, and source URL.
- Re-run `dotnet test --project src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings --filter RealWorld` after every corpus change.

## Completed Setup

- [x] Curated the initial corpus (rxi/json.lua, kikito/inspect.lua) with MIT licenses preserved.
- [x] Stored fixtures under `Fixtures/RealWorld` with accompanying `LICENSE` files.
- [x] Documented provenance, integration details, and maintenance guidance for contributors.
