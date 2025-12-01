# NovaSharp Interpreter TUnit Migration Blueprint *(historical reference)*

> Snapshot: 2025-11-27 — owners and dates reflect the original migration plan. The interpreter suite now runs entirely on TUnit; keep this file for architectural context, adapter patterns, and timing history, but use `docs/Testing.md` for the current expectations.

## Objectives

- Finish the test framework modernization initiative by moving the interpreter suites from NUnit to TUnit while staying on the Microsoft.Testing.Platform runner.
- Preserve the existing isolation helpers (`UserDataIsolation`, `ScriptGlobalOptionsIsolation`, `PlatformDetectorIsolation`) by rehoming them into a shared adapter that both NUnit and TUnit projects consume during the overlap period.
- Improve parallel fan-out and async ergonomics so the interpreter suites run at or below the 9‑second target once the debugger fixtures stop depending on TCP timeouts.

## Deliverables

1. **`NovaSharp.Interpreter.Tests.TUnit` host project** — mirrors the references that previously lived in the legacy NUnit host, links the shared `TestUtilities/*` helpers, and sets `UseMicrosoftTestingPlatform=true` so the new suite plugs into CI without special casing.
1. **Shared adapter layer** — move cross-runner helpers (custom isolation attributes, fixture metadata, Lua TAP harness, REPL/CLI helpers) into `src/tests/TestInfrastructure/` so both NUnit and TUnit projects compile against the same source.
1. **Fixture conversion batches** — convert fixtures in clearly defined groups (VM core, stdlib, interop, CLI/tooling, debugger/platform, Lua spec/TAP) with owners and dates so progress is measurable.
1. **Parity + gating** — dual-run converted fixtures (NUnit + TUnit) until their measurements match within ±5 % runtime and the coverage artefacts stay within ±0.1 % line/branch deltas. Once all batches are green, retire the NUnit project.

## Host Project Specification (`NovaSharp.Interpreter.Tests.TUnit`)

- Location: `src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj`.
- Target framework: `net8.0`, `LangVersion=latest`, `<IsTestProject>true>`, `<UseMicrosoftTestingPlatform>true>`.
- Package references: `TUnit` (runtime), `TUnit.Assertions`, `Microsoft.Testing.Platform.MSBuild`, `Microsoft.Testing.Extensions.VSTestBridge`. Match versions pinned in the NUnit csproj to avoid drift.
- Project references: identical to the NUnit project (`runtime/NovaSharp.Interpreter`, `tooling/NovaSharp.Cli`, `tooling/NovaSharp.Hardwire`, `debuggers/NovaSharp.RemoteDebugger`, `debuggers/NovaSharp.VsCodeDebugger`).
- Shared sources: link everything under `src/tests/NovaSharp.Interpreter.Tests/TestUtilities` plus the new adapter folder via `<Compile Include="..\NovaSharp.Interpreter.Tests\TestUtilities\**\*.cs" Link="TestUtilities\%(RecursiveDir)%(Filename)%(Extension)" />`.
- Runsettings: Microsoft.Testing.Platform already honors the machine’s logical cores, so the legacy `scripts/tests/NovaSharp.Parallel.runsettings` profile was removed once the interpreter suites went TUnit-only (2025-12-01). No custom runsettings are required now.
- Custom attributes: recreate `UserDataIsolation`, `ScriptGlobalOptionsIsolation`, and `PlatformDetectorIsolation` as TUnit fixtures by implementing `global::TUnit.Core.Lifecycle.ITestLifecycle` in the shared adapter. The TUnit versions must be API-compatible so fixtures only swap namespace aliases during conversion.

## Migration Phases

1. **Phase 0 — Foundation (Nov 27 – Dec 3)**

   - Create the TUnit csproj, shared adapter folder, and Microsoft.Testing.Platform integration.
   - Port isolation attributes + fixture catalog equivalents (if analyzers still require explicit references).
   - Add sample fixtures (reuse a subset of `ProcessorTests`) to validate the build/test loop and capture baseline runtimes.
   - ✅ `src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj` now mirrors the runtime/tooling project references, links shared TestInfrastructure sources, and houses the initial `ScriptSmokeTests` fixture so the TUnit runner is exercised end-to-end (`dotnet test --project src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj -c Release`).
   - ✅ (Historical) `src/tests/TestInfrastructure/NUnit` briefly centralized the NUnit-only isolation attributes (`UserDataIsolation`, `ScriptGlobalOptionsIsolation`, `PlatformDetectorIsolation`) until the migration finished. The folder has since been deleted now that every fixture runs on the shared TUnit adapters.
   - ✅ Added the first `TestInfrastructure/TUnit/*IsolationAttribute.cs` implementations (built on the TUnit event receiver interfaces) so migrated fixtures can keep the same `[UserDataIsolation]`, `[ScriptGlobalOptionsIsolation]`, and `[PlatformDetectorIsolation]` annotations.

1. **Phase 1 — Batch conversions (Dec 4 – Dec 18)**

   - Convert fixtures group-by-group (see table below).
   - For each batch: add TUnit copies of the fixtures, keep the NUnit versions compiling, and dual-run the batch locally/CI with the measurement harness (step 2 of the PLAN next actions).
   - File PLAN checkpoints per batch with runtime deltas, coverage diffs, and any analyzer suppressions needed for TUnit.
   - ✅ ScriptLoad suite migrated into `ScriptLoadTUnitTests` (33 tests covering load/dump/coroutine helpers) with measurement artefact `artifacts/tunit-migration/script-load.json` (current sample: NUnit 2.15 s vs. TUnit 1.83 s).
   - ✅ ScriptRun + ScriptCall suites now have TUnit counterparts (`ScriptRunTUnitTests`, `ScriptCallTUnitTests`), and their compare artefacts (`script-run.json`, `script-call.json`) track the incremental performance deltas (e.g., ScriptCall currently 1.29 s NUnit vs. 1.28 s TUnit for 27/37 tests).
   - ✅ ScriptExecutionContext scenarios (22 NUnit tests / 22 TUnit tests) are live via `ScriptExecutionContextTUnitTests`, with measurements recorded in `script-execution-context.json` (currently ~1.01 s NUnit vs. 1.10 s TUnit).
   - ✅ ScriptLoaderBase coverage (11 NUnit tests) now runs in `ScriptLoaderBaseTUnitTests`; artefact `script-loader-base.json` tracks the current delta (≈1.20 s NUnit vs. 1.09 s TUnit).

1. **Phase 2 — Cutover (Dec 19 – Dec 23)**

   - Once every batch reports ±5 % runtime parity and the coverage gate stays green, stop compiling the NUnit csproj, remove leftover `[Parallelizable]` attributes, and update scripts/CI to point at TUnit-only suites.
   - Archive the final NUnit runtime numbers in `docs/testing/tunit-migration-blueprint.md` and update `docs/Testing.md` to reflect the new default.

## Measurement harness

- Use `pwsh ./scripts/tests/compare-test-runtimes.ps1` to record “NUnit vs. TUnit” timings. The script injects `--output Detailed`, parses the Microsoft.Testing.Platform console output for per-test durations, and writes a JSON artefact under `artifacts/tunit-migration/<name>.json` plus raw logs beneath `artifacts/tunit-migration/tmp/<label>/<label>.log`.

- Example (remote debugger pilot):

  ```powershell
  $nunit = @(
      "--project", "path/to/<legacy-suite>.csproj",
      "-c", "Release",
      "--no-build",
      "--filter", "FullyQualifiedName~RemoteDebuggerTests"
  )
  $tunit = @(
      "--project", "src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj",
      "-c", "Release",
      "--no-build"
  )
  pwsh ./scripts/tests/compare-test-runtimes.ps1 `
      -Name remote-debugger-handshake `
      -NUnitArguments $nunit `
      -TUnitArguments $tunit
  ```

- Each run emits a JSON payload containing:

  - The exact `dotnet test` commands (with injected `--output Detailed`/`--results-directory` flags).
  - Total runtime `(totalSeconds)` per suite plus the summarized Microsoft.Testing.Platform counts (`total`, `failed`, `succeeded`, `skipped`).
  - A `tests` array listing every test name, raw duration text, and computed seconds so regression reports can sort by slowest cases.

- Store the generated JSON in source control when it represents a milestone measurement (one file per batch under `artifacts/tunit-migration/<batch>.json`) and reference it from PLAN checkpoints.

## Fixture Conversion Batches

| Batch                           | Scope (folders / exemplar fixtures)                                                                                                                                                       | Owner(s)                                         | Target date | Notes                                                                                                                                     |
| ------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ | ----------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| VM Core & Coroutine Engine      | `Units/Processor*`, `Units/Coroutine*`, `Units/Script*`, `Units/ByteCode*`, `Units/UnaryOperatorExpressionTests.cs`, `Units/ScriptLoadTests.cs`, plus the REPL harness in `EndToEnd/Repl` | Interpreter Core maintainers (#interpreter-core) | 2025-12-05  | Highest churn area; start here to validate adapter patterns for `[SetUp]/[TearDown]`, async helpers, and the shared isolation attributes. |
| Standard Library & Modules      | `Units/String*`, `Units/Table*`, `Units/Math*`, `Units/Bit32*`, `Units/CoroutineModule*`, `Units/IoModule*`, `Units/Os*`, `Units/Utf8*`                                                   | Standard Library maintainers (#stdlib)           | 2025-12-09  | Requires Lua fixture parity plus TAP helpers. Coordinate with the Lua TAP owners before moving shared `.lua` assets into the adapter.     |
| Interop & UserData              | `Units/UserData*`, descriptor suites (`FieldMemberDescriptorTests`, `DispatchingUserDataDescriptorTests`, etc.), `Units/ProxyUserData*`, `Units/CustomConverterRegistryTests.cs`          | Interop/UserData maintainers (#userdata)         | 2025-12-11  | Depends on the TUnit versions of `UserDataIsolation` and `ScriptGlobalOptionsIsolation`. Blocked until Phase 0 finishes.                  |
| Tooling & CLI                   | CLI/Hardwire fixtures (`Units/CliIntegrationTests.cs`, `Units/Hardwire*`, `Units/ProgramTests.cs`, `Units/CompileCommandTests.cs`, `Units/ShellCommandManagerTests.cs`)                   | Tooling + CLI maintainers (#tooling-cli)         | 2025-12-13  | Needs deterministic console IO helpers inside the adapter plus updated assertions for file-system transcripts.                            |
| Debugger & Platform Integration | `Units/RemoteDebugger*`, `Units/Debug*`, `Units/PlatformAutoDetectorTests.cs`, `Units/PlatformAccessor*`, plus the existing `RemoteDebugger` TUnit pilot                                  | Debugger maintainers (#debuggers)                | 2025-12-16  | Reuse the in-memory transport harness from the current TUnit pilot; convert NUnit fixtures after capturing NUnit vs. TUnit profiles.      |
| Lua Spec & TAP Harness          | `TestMore/*`, `Spec/*`, `Lua/*`, TAP bridge fixtures, long-running compatibility suites                                                                                                   | Lua Spec maintainers (#lua-spec)                 | 2025-12-20  | Requires TUnit-friendly TAP reader plus cancellation guards. Keep NUnit copies until TAP reliability reaches parity.                      |

## Definition of Done per Batch

1. Fixtures compile + run under both NUnit and TUnit (temporary duplication is acceptable).
1. Measurement harness records (<batch> runtime, ±5 % threshold) and stores the JSON artefact under `artifacts/tunit-migration/<batch>.json`.
1. Coverage deltas for converted files are ±0.1 % line/branch/method in `docs/coverage/latest/Summary.json`.
1. PLAN.md updated with a checkpoint summarizing runtime delta, coverage delta, and any follow-up issues.

## Risks & Mitigations

- **Isolation gaps:** Until all isolation attributes have TUnit equivalents, interop fixtures cannot safely run in parallel. Mitigation: finish adapter work before converting the `UserData*` suites; document any remaining thread-static state in PLAN.md.
- **Analyzer noise:** TUnit introduces async wrappers that may trigger new CA warnings (e.g., CA2007 `ConfigureAwait`). Mitigation: update `.editorconfig` only if absolutely required and document every suppression.
- **Fixture drift:** Running NUnit and TUnit copies side-by-side risks divergent assertions. Mitigation: convert fixtures by moving shared assertions into helper methods inside the adapter so both versions call the same code.

## Tracking & Reporting

- Use PLAN.md for milestone checkpoints (date, batch, runtime delta, coverage delta, blockers).
- Record measurement harness outputs under `artifacts/tunit-migration/` and link them from PLAN.md.
- Update this blueprint whenever owners/dates shift or new batches are added.
