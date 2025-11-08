# MoonSharp Testing Guide

MoonSharp ships with a comprehensive test suite that blends historical Lua fixtures with .NET focused regression checks.

## Test Topology
- **Lua compatibility (TestMore)**: Lua TAP fixtures exercise language semantics, standard library coverage, and coroutine behaviour.
- **End-to-end suites**: C# driven NUnit scenarios cover userdata interop, debugger contracts, serialization, hardwire generation, and coroutine pipelines.
- **Units**: Focused checks for low-level structures (stacks, instruction decoding, binary dump/load).

The fixtures originate from the legacy `MoonSharp.Interpreter.Tests` tree and are compiled directly into the modern `.NET 8` runner.

## Running the Tests Locally
```bash
dotnet run -c Release -- --ci
# from: src/TestRunners/DotNetCoreTestRunner
```
- `--ci` suppresses interactive prompts and writes `moonsharp_tests.log` to the repository root.
- The GitHub Actions workflow mirrors this command on every push to `master` and on all pull requests.

## Pass/Fail Policy
- Two Lua TAP suites (`TestMore_308_io`, `TestMore_309_os`) remain skipped because they require raw filesystem/OS access. Enable them manually only on trusted machines.
- Failures write a detailed stack trace to `moonsharp_tests.log`; the CI pipeline publishes the log as a build artefact.

## Coverage Snapshot
- **Fixtures**: 39 `[TestFixture]` types, 627 active tests, 2 intentional skips.
- **Key areas covered**: Parser/lexer, binary dump/load paths, JSON subsystem, coroutine scheduling, interop binding policies, debugger attach/detach hooks.
- **Gaps**: Visual Studio Code/remote debugger integration still lacks automated smoke tests; CLI tooling and dev utilities remain manual.

## Expanding Coverage
1. Share fixtures between the runner and the archived `MoonSharp.Interpreter.Tests` tree to avoid drift.
2. Add coverlet-driven coverage reporting once the suite migrates to `dotnet test`.
3. Introduce debugger protocol integration tests (attach, breakpoint, variable inspection).
4. Restore the skipped OS/IO TAP fixtures through conditional execution in trusted environments.

Track active goals and gaps in `PLAN.md`, and update this document as new harnesses or policies ship.
