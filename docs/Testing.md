# NovaSharp Testing Guide

NovaSharp ships with a comprehensive test suite that blends historical Lua fixtures with .NET focused regression checks.

## Test Topology

- **Lua compatibility (TestMore)**: Lua TAP fixtures exercise language semantics, standard library coverage, and coroutine behaviour.
- **End-to-end suites**: C# driven NUnit scenarios cover userdata interop, debugger contracts, serialization, hardwire generation, and coroutine pipelines.
- **Units**: Focused checks for low-level structures (stacks, instruction decoding, binary dump/load).

## Running the Tests Locally

```bash
dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --logger "trx;LogFileName=NovaSharpTests.trx"
```

- Produces a standards-based TRX file under `TestResults/` (or the supplied `--results-directory`) so failures can be inspected with the test explorer of your choice.
- Mirrors the execution that now powers CI, ensuring branch/line coverage is captured with the same runner configuration.

### Build Helper Scripts

Use the helpers in `scripts/build` when you need the canonical build + interpreter-test pipeline in a single command (matching the CI lane).

```powershell
pwsh ./scripts/build/build.ps1
```

```bash
bash ./scripts/build/build.sh
```

- Both scripts restore local tools (unless `-SkipToolRestore`/`--skip-tool-restore` is supplied), build `src/NovaSharp.sln` in Release by default, and execute the interpreter tests with `dotnet test --no-build --logger "trx;LogFileName=NovaSharpTests.trx"` writing logs to `artifacts/test-results`.
- Pass `-SkipTests`/`--skip-tests` for build-only runs, or override `-Configuration`/`--configuration` to target Debug builds.

## Generating Coverage

```powershell
pwsh ./scripts/coverage/coverage.ps1
```

- If the host only has .NET 9 installed (common on new Ubuntu images), set `DOTNET_ROLL_FORWARD=Major` when invoking the script (PowerShell or Bash) so the .NET 9 runtime can execute the net8.0 testhost.

- Restores local tools, builds the solution in Release, and drives `dotnet test` through the `coverlet.console` wrapper so NUnit fixtures (including `[SetUp]/[TearDown]`) execute exactly as they do in CI.

- Emits LCOV, Cobertura, and OpenCover artefacts under `artifacts/coverage`, with the TRX test log in `artifacts/coverage/test-results`.

- Produces HTML + Markdown + JSON summaries in `docs/coverage/latest`; `SummaryGithub.md` and `Summary.json` are also copied to `artifacts/coverage` for automation and PR reporting.

- Pass `-SkipBuild` to reuse existing binaries and `-Configuration Debug` to collect non-Release stats.

- On macOS/Linux without PowerShell, run `bash ./scripts/coverage/coverage.sh` (identical flags/behaviour). Both scripts automatically set `DOTNET_ROLL_FORWARD=Major` when it isn’t already defined so .NET 9 runtimes can execute the net8.0 testhost; override the variable if you need different roll-forward behaviour.

- Both coverage helpers honour gating settings: set `COVERAGE_GATING_MODE` to `monitor` (warn) or `enforce` (fail), and override the per-metric targets via `COVERAGE_GATING_TARGET_LINE`, `COVERAGE_GATING_TARGET_BRANCH`, and `COVERAGE_GATING_TARGET_METHOD`. CI currently runs with `COVERAGE_GATING_MODE=monitor` at **95 %** line/branch/method so it warns without failing while interpreter branch coverage sits just below the goal. To rehearse the enforced experience (or flip CI once ≥95 % holds), export the stricter settings locally:

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

- **Current baseline (Release via `scripts/coverage/coverage.ps1 -SkipBuild`, 2025-11-20 04:49 UTC)**: 96.66 % line / 94.51 % branch / 98.29 % method for `NovaSharp.Interpreter` across 2 486 Release tests (overall repository line coverage 87.2 %).

- **Fixtures**: ~45 `[TestFixture]` types, 2 486 active tests, 0 skips (the two TAP suites remain disabled unless explicitly enabled).

- **Key areas covered**: Parser/lexer, binary dump/load paths, JSON subsystem, coroutine scheduling, interop binding policies, debugger attach/detach hooks.

- **Gaps**: Visual Studio Code/remote debugger integration still lacks automated smoke tests; CLI tooling and dev utilities remain manual.

## Naming & Conventions

- NUnit test methods (`[Test]`, `[TestCase]`, etc.) must use PascalCase without underscores. The solution-wide `.editorconfig` enforces this as an error, so stray underscore names will fail analyzers and builds.

## Expanding Coverage

1. Deepen unit coverage across parser error paths, metatable resolution, and CLI tooling to raise the interpreter namespace above 70 % line coverage.
1. Introduce debugger protocol integration tests (attach, breakpoint, variable inspection) and capture golden transcripts for the CLI shell.
1. Keep Lua fixtures under version control in `tests/NovaSharp.Interpreter.Tests` to avoid drift and simplify regeneration.
1. Restore the skipped OS/IO TAP fixtures through conditional execution in trusted environments or provide managed equivalents.

Track active goals and gaps in `PLAN.md`, and update this document as new harnesses or policies ship.
