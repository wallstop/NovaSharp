# NovaSharp Testing Guide

NovaSharp ships with a comprehensive test suite that blends historical Lua fixtures with .NET focused regression checks.

## Test Topology

- **Lua compatibility (TestMore)**: Lua TAP fixtures exercise language semantics, standard library coverage, and coroutine behaviour.
- **End-to-end suites**: C# driven TUnit scenarios cover userdata interop, debugger contracts, serialization, hardwire generation, and coroutine pipelines.
- **Units**: Focused checks for low-level structures (stacks, instruction decoding, binary dump/load).

## Running the Tests Locally

```bash
dotnet test --project src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj -c Release --logger "trx;LogFileName=NovaSharpInterpreterTUnit.trx"
```

- Produces a standards-based TRX file under `TestResults/` (or the supplied `--results-directory`) so failures can be inspected with the test explorer of your choice.
- Mirrors the execution that now powers CI, ensuring branch/line coverage is captured with the same runner configuration.

### Microsoft.Testing.Platform runner

- `global.json` now pins `test.runner` to `Microsoft.Testing.Platform`, so every `dotnet test` invocation must pass an explicit target via `--project`/`--solution`/`--test-modules`. The command above mirrors the CI lane (`--project` is required; the legacy `dotnet test <csproj>` syntax no longer works on .NET 10 SDKs).
- The interpreter TUnit suite (≈2.7 k Release tests) completes in roughly **7 s** on a 12-core workstation; expect ~1 s slower when a clean build is required because the Microsoft.Testing.Platform entry point is re-generated.
- Visual Studio and Rider inherit the same configuration because the runner is set in `global.json`; if a local command fails with “VSTest target is no longer supported”, re-run it with the explicit `--project` form shown above so the Microsoft.Testing.Platform mode is engaged.

### TUnit-first policy

- Interpreter and debugger suites now live entirely on TUnit. Use TUnit’s async assertions and data sources for every new test, and only introduce NUnit fixtures if a third-party dependency requires it (coordinate in `PLAN.md` before doing so).
- Shared Lua fixtures, TAP corpuses, and helper infrastructure remain under `src/tests/NovaSharp.Interpreter.Tests`. Link the files you need into the TUnit project instead of reviving the deleted NUnit host.
- The runtime/TAP blueprint in `docs/testing/tunit-migration-blueprint.md` is preserved for historical context. If you need to compare timing against the retired NUnit host, use `pwsh ./scripts/tests/compare-test-runtimes.ps1 -Name <scenario> -BaselineArguments @(...) -TUnitArguments @(...)` so the JSON artefact captures the delta.

### Build Helper Scripts

Use the helpers in `scripts/build` when you need the canonical build + interpreter-test pipeline in a single command (matching the CI lane).

```powershell
pwsh ./scripts/build/build.ps1
```

```bash
bash ./scripts/build/build.sh
```

- Both scripts restore local tools (unless `-SkipToolRestore`/`--skip-tool-restore` is supplied), build `src/NovaSharp.sln` in Release by default, and execute the interpreter tests with the TUnit command above, writing logs to `artifacts/test-results`.
- Pass `-SkipTests`/`--skip-tests` for build-only runs, or override `-Configuration`/`--configuration` to target Debug builds.

## Generating Coverage

```powershell
pwsh ./scripts/coverage/coverage.ps1
```

- If the host only has .NET 9 installed (common on new Ubuntu images), set `DOTNET_ROLL_FORWARD=Major` when invoking the script (PowerShell or Bash) so the .NET 9 runtime can execute the net8.0 testhost.

- Restores local tools, builds the solution in Release, and drives `dotnet test` through the `coverlet.console` wrapper so the TUnit suites execute exactly as they do in CI.

- Emits LCOV, Cobertura, and OpenCover artefacts under `artifacts/coverage`, with the TRX test logs in `artifacts/coverage/test-results`.

- Produces HTML + Markdown + JSON summaries in `docs/coverage/latest`; `SummaryGithub.md` and `Summary.json` are also copied to `artifacts/coverage` for automation and PR reporting.

- Pass `-SkipBuild` to reuse existing binaries and `-Configuration Debug` to collect non-Release stats.

- On macOS/Linux without PowerShell, run `bash ./scripts/coverage/coverage.sh` (identical flags/behaviour). Both scripts automatically set `DOTNET_ROLL_FORWARD=Major` when it isn’t already defined so .NET 9 runtimes can execute the net8.0 testhost; override the variable if you need different roll-forward behaviour.

- Both coverage helpers honour gating settings: set `COVERAGE_GATING_MODE` to `monitor` (warn) or `enforce` (fail), and override the per-metric targets via `COVERAGE_GATING_TARGET_LINE`, `COVERAGE_GATING_TARGET_BRANCH`, and `COVERAGE_GATING_TARGET_METHOD`. CI now exports `COVERAGE_GATING_MODE=enforce` with **95 %** line/branch/method thresholds so coverage dips fail fast; set the mode to `monitor` locally if you need a warning-only rehearsal. To mirror the enforced gate (the default in CI), export the stricter settings before rerunning the script:

  ```powershell
  $env:COVERAGE_GATING_MODE = "enforce"
  $env:COVERAGE_GATING_TARGET_LINE = "95"
  $env:COVERAGE_GATING_TARGET_BRANCH = "95"
  $env:COVERAGE_GATING_TARGET_METHOD = "95"
  pwsh ./scripts/coverage/coverage.ps1 -SkipBuild
  ```

  ```bash
  COVERAGE_GATING_MODE=enforce \
  COVERAGE_GATING_TARGET_LINE=95 \
  COVERAGE_GATING_TARGET_BRANCH=95 \
  COVERAGE_GATING_TARGET_METHOD=95 \
  bash ./scripts/coverage/coverage.sh --skip-build
  ```

### Coverage in CI

- `.github/workflows/tests.yml` now includes a `code-coverage` job that runs `pwsh ./scripts/coverage/coverage.ps1` after the primary test job (falling back to the Bash variant on runners without PowerShell).
- The job now exports `COVERAGE_GATING_MODE=enforce` together with 95 % line/branch/method targets so coverage dips fail fast. The PowerShell coverage helper enforces the same gate, and the workflow’s `Evaluate coverage threshold` step double-checks all three metrics before publishing artefacts.
- Coverage deltas surface automatically on pull requests; the comment is updated in-place on retries to avoid noise. When the gate passes, the Action log includes a “Coverage Gate” summary showing both the current percentages and thresholds.

## Pass/Fail Policy

- Two Lua TAP suites (`TestMore_308_io`, `TestMore_309_os`) remain skipped because they require raw filesystem/OS access. Enable them manually only on trusted machines.

- Failures are captured in the generated TRX; the CI pipeline publishes the `artifacts/test-results` directory for inspection.

- **Current baseline (Release via `scripts/coverage/coverage.ps1 -SkipBuild`, 2025-11-20 10:23 UTC)**: 96.98 % line / 95.13 % branch / 98.57 % method for `NovaSharp.Interpreter` across 2 547 Release tests (overall repository line coverage 87.5 %).

- **Fixtures**: ~45 `[TestFixture]` types, 2 547 active tests, 0 skips (the two TAP suites remain disabled unless explicitly enabled).

- **Key areas covered**: Parser/lexer, binary dump/load paths, JSON subsystem, coroutine scheduling, interop binding policies, debugger attach/detach hooks.

- **Gaps**: Visual Studio Code/remote debugger integration still lacks automated smoke tests; CLI tooling and dev utilities remain manual.

## Naming & Conventions

- NUnit test methods (`[Test]`, `[TestCase]`, etc.) must use PascalCase without underscores. The solution-wide `.editorconfig` enforces this as an error, so stray underscore names will fail analyzers and builds.
- Author all new failure expectations with `Assert.Throws<TException>(...)`/`Assert.That(async () => ..., Throws.TypeOf<TException>())` rather than `[ExpectedException]`. The NUnit 3 runner no longer honors the legacy attribute, and the explicit assertion keeps error paths local to the test body.
- When a test fixture touches shared static registries (e.g., `UserData.RegisterType`, global caches, or other mutable singletons), decorate the class with `[UserDataIsolation]` so the registry is sandboxed per test and keep the fixture `[Parallelizable(ParallelScope.Self)]`. Suites that tweak `Script.GlobalOptions` should also open a scope in `[SetUp]` via `Script.BeginGlobalOptionsScope()` (and dispose it in `[TearDown]`) so converters/platform overrides don’t leak across tests. Only drop back to `[NonParallelizable]` when a given suite still depends on mutable globals you can’t isolate yet.
- Multi-word Lua concepts keep their canonical casing when surfaced through C# APIs. In particular, treat “upvalue” as `UpValue`/`UpValues` so helpers such as `GetUpValue`, `UpValuesType`, and `SymbolRef.UpValue` remain consistent with the runtime surface. Do **not** collapse these identifiers to `Upvalue` or `Upvalues`, and document any additional Lua-specific casing decisions in `PLAN.md` before introducing new APIs.

## Expanding Coverage

1. Deepen unit coverage across parser error paths, metatable resolution, and CLI tooling to raise the interpreter namespace above 70 % line coverage.
1. Introduce debugger protocol integration tests (attach, breakpoint, variable inspection) and capture golden transcripts for the CLI shell.
1. Keep Lua fixtures under version control in `tests/NovaSharp.Interpreter.Tests` to avoid drift and simplify regeneration.
1. Restore the skipped OS/IO TAP fixtures through conditional execution in trusted environments or provide managed equivalents.

Track active goals and gaps in `PLAN.md`, and update this document as new harnesses or policies ship.

## Analyzer & Warning Policy

- **Formatter gate**: Run `dotnet csharpier .` (or `dotnet csharpier --check .` if you only need verification) before every push. CI executes the same command via `scripts/ci/check-csharpier.sh` during the lint job, so any mismatch will fail PRs immediately. If `dotnet format` or another tool disagrees with CSharpier output, treat that as a tooling bug—update `.editorconfig`/workflow settings instead of reformatting away from CSharpier style.

- **Solution baseline (2025-12-07)**: `Directory.Build.props` now sets `<TreatWarningsAsErrors>true>`, so `dotnet build src/NovaSharp.sln -c Release -nologo` fails on any compiler or analyzer warning. Run that command before every push and note it in your PR (the template now calls out analyzer commands explicitly). If a warning is unavoidable, add a targeted suppression plus a `PLAN.md` entry before merging.

- `src/debuggers/NovaSharp.VsCodeDebugger/NovaSharp.VsCodeDebugger.csproj` now builds with `<TreatWarningsAsErrors>true>`. Any new warning in the VS Code debugger project fails the build locally and in CI, so always run:

  ```bash
  dotnet build src/debuggers/NovaSharp.VsCodeDebugger/NovaSharp.VsCodeDebugger.csproj -c Release -nologo
  ```

  before pushing debugger changes. Keep the analyzer configuration warning-free; suppressions should be avoided unless they are documented in `PLAN.md`.

- `src/tooling/NovaSharp.Hardwire/NovaSharp.Hardwire.csproj` now also treats warnings as errors. Run `dotnet build src/tooling/NovaSharp.Hardwire/NovaSharp.Hardwire.csproj -c Release -nologo` before committing tooling changes, and keep analyzer suppressions documented.

- `src/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj` now builds with `<TreatWarningsAsErrors>true>` (2025‑11‑24). Before pushing debugger/network changes, run `dotnet build src/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj -c Release -nologo` (or the full solution build) to keep the analyzer set clean. Remote-debugger tests now live in `src/tests/NovaSharp.RemoteDebugger.Tests.TUnit`; add coverage there when touching RemoteDebugger code.

- Record every analyzer command you run when filling out `.github/pull_request_template.md`. Reviewers expect to see the solution build plus any scoped project builds/tests for the areas you touched.

- Because the solution-wide warning gate is now on, suppressions must remain surgical. Any new `[SuppressMessage]` or ruleset tweak requires a `PLAN.md` entry (rule, justification, follow-up owner) before merging.

### Debugger Analyzer Guardrails

Both debugger stacks now rely on analyzers to catch regressions; any new warning fails the Release build, so keep the following guardrails in mind whenever you touch debugger code.

- **Disposal & ownership (CA1063/CA2213/CA2000)**: `NovaSharpVsCodeDebugServer`, `ProtocolServer`, `DebugSession`, `RemoteDebuggerService`, `HttpServer`, `Utf8TcpServer`, and every socket/listener/`HttpClient` wrapper must implement the full dispose pattern and wrap transient streams/sockets/readers in `using` statements. Tests should assert deterministic disposal by creating helpers such as the blocking channel/queue fixtures already in the suite.
- **Argument validation & targeted catches (CA1031/CA1062)**: Guard every public/protected entry point (commands, protocol handlers, HTTP endpoints) against `null` or invalid arguments and only catch specific exception types (IO/security/format) so analyzers keep `Program`, `RunCommand`, `HardwireCommand`, and debugger transports free of blanket `catch (Exception)` blocks.
- **Culture/compare invariance (CA1305/CA1310/CA1865/CA1866)**: Always format/parse using `CultureInfo.InvariantCulture` and specify `StringComparison.Ordinal` (or the char overloads) for protocol routing, manifest parsing, and CLI messaging. Remote debugger HTTP payload builders should also stay culture-invariant when emitting JSON or diagnostics.
- **Collections & API surfaces (CA1002/CA1012/CA1716/CA1822/CA1854/CA1859)**: Expose protocol lists as `IEnumerable<T>`, keep debugger constructors `protected` to enforce abstract entry points, rename identifiers that collide with reserved keywords, and favor `static` helpers when no instance state is touched. Access dictionaries via `TryGetValue` to avoid double lookups, and store concrete list/dictionary instances instead of their interfaces when state mutation is required.
- **Binary payload & immutability rules (CA1008/CA1056/CA1815/CA1819)**: Remote debugger URIs must use `System.Uri`, byte payloads should flow through `ReadOnlyMemory<byte>`/`ReadOnlySpan<byte>`, and value types such as `RemoteDebuggerOptions` need explicit equality members so analyzers see deterministic semantics.

Validation checklist:

```powershell
dotnet build src/debuggers/NovaSharp.VsCodeDebugger/NovaSharp.VsCodeDebugger.csproj -c Release -nologo
dotnet build src/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj -c Release -nologo
dotnet test --project src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj -c Release --filter "FullyQualifiedName~RemoteDebugger"
```

Document any new suppressions or analyzer exclusions in `PLAN.md` (with the CA rule, justification, and follow-up owner) before merging.
