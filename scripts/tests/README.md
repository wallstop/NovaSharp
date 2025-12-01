# tests scripts

Utilities that keep the interpreter test metadata in sync now that all fixtures run on
Microsoft.Testing.Platform/TUnit.

## update-fixture-catalog.ps1

- **Purpose:** Regenerates `src/tests/NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs` so analyzers always see explicit references to every `[TestFixture]`. The generated file now records “No NUnit fixtures remain,” but the script stays checked in so we can quickly resurrect NUnit coverage if a future project needs it.
- **Usage:** From the repo root run `pwsh ./scripts/tests/update-fixture-catalog.ps1`. The script accepts optional `-TestsRoot` and `-OutputPath` parameters when experimenting with other assemblies.
- **CI integration:** The interpreter NUnit project has been deleted, so the script no longer runs automatically. Execute it manually whenever you add/remove NUnit fixtures in any legacy branch to keep the generated file accurate.

## Retired assets

- `NovaSharp.Parallel.runsettings` was removed after the final TUnit migration (2025-12-01). Microsoft.Testing.Platform already maxes out concurrency, so there is no longer a bespoke runsettings profile to pass to `dotnet test`.
