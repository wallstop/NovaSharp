# tests scripts

Utilities that keep the NUnit test suite metadata in sync live here.

## update-fixture-catalog.ps1

- **Purpose:** Regenerates `src/tests/NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs` so every `[TestFixture]` type is referenced via `typeof(...)`. This keeps analyzers (CA1515/CA1812) satisfied once fixtures become `internal`.
- **Usage:** From the repo root run `pwsh ./scripts/tests/update-fixture-catalog.ps1`. The script accepts optional `-TestsRoot` and `-OutputPath` parameters when experimenting with other assemblies.
- **CI integration:** `NovaSharp.Interpreter.Tests.csproj` runs this script automatically before compilation, but you should still run it manually whenever you add, rename, or delete a fixture to avoid stale diffs.

## NovaSharp.Parallel.runsettings

- **Purpose:** Centralizes the test-runner configuration so `dotnet test` always executes with `RunConfiguration.MaxCpuCount=0` and `NUnit.NumberOfTestWorkers=0`, enabling machine-wide parallelism once fixtures opt into `[Parallelizable]`.
- **Usage:** Append `--settings scripts/tests/NovaSharp.Parallel.runsettings` to every `dotnet test` invocation (the build helpers already do this) so local runs match CI.
