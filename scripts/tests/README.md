# tests scripts

Utilities that keep the interpreter test metadata in sync now that all fixtures run on
Microsoft.Testing.Platform/TUnit.

## update-fixture-catalog.ps1

- **Purpose:** Regenerates `src/tests/NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs` so analyzers always see explicit references to every `[TestFixture]`. The generated file now records “No NUnit fixtures remain,” but the script stays checked in so we can quickly resurrect NUnit coverage if a future project needs it.
- **Usage:** From the repo root run `pwsh ./scripts/tests/update-fixture-catalog.ps1`. The script accepts optional `-TestsRoot` and `-OutputPath` parameters when experimenting with other assemblies.
- **CI integration:** The interpreter NUnit project has been deleted, so the script no longer runs automatically. Execute it manually whenever you add/remove NUnit fixtures in any legacy branch to keep the generated file accurate.

## compare-test-runtimes.ps1

- **Purpose:** Captures timing deltas between two `dotnet test` invocations (historically NUnit vs. TUnit, now typically TUnit vs. TUnit or TUnit vs. archived NUnit logs) and stores the aggregate JSON under `artifacts/tunit-migration/<name>.json`.
- **Usage (current suites):** Provide the TUnit command via `-TUnitArguments` and, if the legacy NUnit project no longer exists, point `-BaselineArtefactPath` at a previously generated JSON file. The script replays the archived NUnit timing and only executes the TUnit run.
  ```powershell
  pwsh ./scripts/tests/compare-test-runtimes.ps1 `
      -Name remote-debugger-final `
      -BaselineArtefactPath artifacts/tunit-migration/remote-debugger-sample.json `
      -TUnitArguments @(
          "--project", "src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj",
          "-c", "Release"
      )
  ```
- **Usage (legacy migration):** When a runnable NUnit project still exists, omit `-BaselineArtefactPath` and pass `-NUnitArguments @(...)`. The script runs both commands, logs detailed output under `artifacts/tunit-migration/tmp/<label>/`, and emits a JSON artefact summarizing per-test durations plus the total delta.

## Retired assets

- `NovaSharp.Parallel.runsettings` was removed after the final TUnit migration (2025-12-01). Microsoft.Testing.Platform already maxes out concurrency, so there is no longer a bespoke runsettings profile to pass to `dotnet test`.
