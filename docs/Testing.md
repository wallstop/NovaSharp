# NovaSharp Testing Guide

NovaSharp ships with a comprehensive test suite that blends historical Lua fixtures with .NET focused regression checks.

## Test Topology
- **Lua compatibility (TestMore)**: Lua TAP fixtures exercise language semantics, standard library coverage, and coroutine behaviour.
- **End-to-end suites**: C# driven NUnit scenarios cover userdata interop, debugger contracts, serialization, hardwire generation, and coroutine pipelines.
- **Units**: Focused checks for low-level structures (stacks, instruction decoding, binary dump/load).

The fixtures originate from the legacy `tests/NovaSharp.Interpreter.Tests.Legacy` tree and are compiled directly into the modern `.NET 8` runner.

## Running the Tests Locally
```bash
dotnet run -c Release -- --ci
# from: src/tests/TestRunners/DotNetCoreTestRunner
```
- `--ci` suppresses interactive prompts and writes `NovaSharp_tests.log` to the repository root.
- The GitHub Actions workflow mirrors this command on every push to `master` and on all pull requests.

## Generating Coverage
```powershell
.\coverage.ps1
```
- Restores local tools, builds the solution in Release, and drives the DotNetCoreTestRunner via coverlet.
- Emits LCOV, Cobertura, and OpenCover artefacts under `artifacts/coverage` (ignored by Git).
- Produces an HTML dashboard in `docs/coverage/latest`; open `index.html` for the full drill-down.
- Pass `-SkipBuild` to reuse existing binaries and `-Configuration Debug` to collect non-Release stats.

## Pass/Fail Policy
- Two Lua TAP suites (`TestMore_308_io`, `TestMore_309_os`) remain skipped because they require raw filesystem/OS access. Enable them manually only on trusted machines.
- Failures write a detailed stack trace to `NovaSharp_tests.log`; the CI pipeline publishes the log as a build artefact.

## Coverage Snapshot
- **Baseline (Release via `coverage.ps1`)**: 62.2 % line, 61.4 % branch, 62.7 % method coverage.
- **Fixtures**: 39 `[TestFixture]` types, 627 active tests, 2 intentional skips.
- **Key areas covered**: Parser/lexer, binary dump/load paths, JSON subsystem, coroutine scheduling, interop binding policies, debugger attach/detach hooks.
- **Gaps**: Visual Studio Code/remote debugger integration still lacks automated smoke tests; CLI tooling and dev utilities remain manual.

## Expanding Coverage
1. Deepen unit coverage across parser error paths, metatable resolution, and CLI tooling to raise the interpreter namespace above 70 % line coverage.
2. Introduce debugger protocol integration tests (attach, breakpoint, variable inspection) and capture golden transcripts for the CLI shell.
3. Share fixtures between the runner and archived `tests/NovaSharp.Interpreter.Tests.Legacy` tree to avoid drift and simplify regeneration.
4. Restore the skipped OS/IO TAP fixtures through conditional execution in trusted environments or provide managed equivalents.

Track active goals and gaps in `PLAN.md`, and update this document as new harnesses or policies ship.
