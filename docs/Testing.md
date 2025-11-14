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

## Generating Coverage
```powershell
pwsh ./scripts/coverage/coverage.ps1
```
- If the host only has .NET 9 installed (common on new Ubuntu images), set `DOTNET_ROLL_FORWARD=Major` when invoking the script (PowerShell or Bash) so the .NET 9 runtime can execute the net8.0 testhost.
- Restores local tools, builds the solution in Release, and drives `dotnet test` through the `coverlet.console` wrapper so NUnit fixtures (including `[SetUp]/[TearDown]`) execute exactly as they do in CI.
- Emits LCOV, Cobertura, and OpenCover artefacts under `artifacts/coverage`, with the TRX test log in `artifacts/coverage/test-results`.
- Produces HTML + Markdown + JSON summaries in `docs/coverage/latest`; `SummaryGithub.md` and `Summary.json` are also copied to `artifacts/coverage` for automation and PR reporting.
- Pass `-SkipBuild` to reuse existing binaries and `-Configuration Debug` to collect non-Release stats.
- On macOS/Linux without PowerShell, run `bash ./scripts/coverage/coverage.sh` (identical flags/behaviour).
- When using the Bash variant on hosts without .NET 8, call it as `DOTNET_ROLL_FORWARD=Major bash ./scripts/coverage/coverage.sh …` so Coverlet can launch the net8.0 testhost via the installed .NET 9 runtime.

### Coverage in CI
- `.github/workflows/tests.yml` now includes a `code-coverage` job that runs `pwsh ./scripts/coverage/coverage.ps1` after the primary test job (falling back to the Bash variant on runners without PowerShell).
- The job appends the Markdown summary to the GitHub Action run, posts a PR comment with line/branch/method coverage, and uploads raw + HTML artefacts for inspection.
- Coverage deltas surface automatically on pull requests; the comment is updated in-place on retries to avoid noise.

## Pass/Fail Policy
- Two Lua TAP suites (`TestMore_308_io`, `TestMore_309_os`) remain skipped because they require raw filesystem/OS access. Enable them manually only on trusted machines.
- Failures are captured in the generated TRX; the CI pipeline publishes the `artifacts/test-results` directory for inspection.

- **Baseline (Release via `scripts/coverage/coverage.ps1`, 2025-11-11)**: 68.5 % line, 70.5 % branch, 73.0 % method coverage (interpreter module at 81.9 % line).
- **Fixtures**: 42 `[TestFixture]` types, 1095 active tests, 0 skips (the two TAP suites remain disabled unless explicitly enabled).
- **Key areas covered**: Parser/lexer, binary dump/load paths, JSON subsystem, coroutine scheduling, interop binding policies, debugger attach/detach hooks.
- **Gaps**: Visual Studio Code/remote debugger integration still lacks automated smoke tests; CLI tooling and dev utilities remain manual.

## Naming & Conventions
- NUnit test methods (`[Test]`, `[TestCase]`, etc.) must use PascalCase without underscores. The solution-wide `.editorconfig` enforces this as an error, so stray underscore names will fail analyzers and builds.

## Expanding Coverage
1. Deepen unit coverage across parser error paths, metatable resolution, and CLI tooling to raise the interpreter namespace above 70 % line coverage.
2. Introduce debugger protocol integration tests (attach, breakpoint, variable inspection) and capture golden transcripts for the CLI shell.
3. Keep Lua fixtures under version control in `tests/NovaSharp.Interpreter.Tests` to avoid drift and simplify regeneration.
4. Restore the skipped OS/IO TAP fixtures through conditional execution in trusted environments or provide managed equivalents.

Track active goals and gaps in `PLAN.md`, and update this document as new harnesses or policies ship.
