# Modern Testing & Coverage Plan

## Modernization Baseline (Nov 2025)
- ✅ Core runtime, debuggers, hardwire tooling, CLI shell, benchmarks, and automated tests now target **`netstandard2.1`** (runtime) and **`net8.0`** (tooling).
- ✅ Legacy `.NET 3.5/4.x`, Portable Class Library, Windows 8/Phone, Silverlight, Unity, Xamarin, and NuGet test harness projects removed from the tree.
- ✅ Solution simplified to `src/moonsharp.sln`; obsolete `moonsharp_*` variants deleted.
- ✅ Benchmark infrastructure rebuilt on BenchmarkDotNet with shared `PerformanceReportWriter` writing OS-specific results to `docs/Performance.md`; benchmarks remain local-only by design.
- ✅ Documentation refreshed: `docs/Performance.md`, `docs/Testing.md`, `docs/Modernization.md`, and README now describe the modern stack and link together.

## Testing Health Snapshot
- Runner: `src/TestRunners/DotNetCoreTestRunner` (`net8.0`) executes **627 tests** with **2 intentional skips** (`TestMore_308_io`, `TestMore_309_os`).
- Command: `dotnet run -c Release -- --ci` (mirrors CI behaviour). Produces `moonsharp_tests.log` for triage.
- Suite composition:
  - **Lua TAP fixtures** validating language semantics and standard library parity.
  - **End-to-end NUnit scenarios** covering userdata interop, debugger attach, coroutine pipelines, serialization, JSON.
  - **Unit tests** for low-level primitives (virtual machine stacks, binary dump/load, interop policies).
- CI (`.github/workflows/tests.yml`):
  - Restores and builds `src/moonsharp.sln` in Release.
  - Runs the dotnet runner.
  - Uploads `moonsharp_tests.log` as an artefact for every PR/push to `master`.
- Benchmarks (`MoonSharp.Benchmarks`, `PerformanceComparison`) compile but never run in CI; they must be invoked manually to update `docs/Performance.md`.

## Coverage Strengths
- Lua semantics, coroutine scheduling, binary dump/load, JSON, interop policy, and hardwire regeneration paths have broad regression protection.
- TAP fixtures catch regressions against upstream Lua behaviour.
- Benchmark baselines establish performance + allocation expectations for script loading and runtime scenarios.

## Gaps & Risks
- **Debugger automation**: No integration tests for VS Code or remote debugger protocols.
- **Tooling**: CLI shell and remaining DevTools lack regression coverage after modernization.
- **Cross-platform**: CI only runs on Linux; Windows/macOS coverage still manual.
- **Observability**: No branch/line coverage metrics; hard to quantify test depth.
- **Skip-list debt**: IO/OS TAP suites remain permanently skipped without compensating coverage.
- **Unity onboarding**: Legacy Unity samples removed; new guidance for consuming `netstandard2.1` packages is outstanding.

## Near-Term Priorities (ordered)
1. **Unity/Packaging Alignment**
   - Produce Unity quick-start docs leveraging the `netstandard2.1` runtime.
   - Ensure NuGet/packages expose the correct TFMs (`netstandard2.1` + optional `net8.0` tooling).
   - Audit residual docs (e.g., `release_readme.txt`, website copy) for legacy framework references.
2. **Debugger Smoke Tests**
   - Author lightweight harnesses to drive VS Code and remote debuggers (attach, breakpoint, variable inspection).
   - Gate behind opt-in flag while stabilising.
3. **Coverage Instrumentation**
   - Port the suite to `dotnet test` + `coverlet` or embed coverage into the existing runner.
   - Publish LCOV/HTML artefacts and evaluate Codecov integration.
4. **Cross-Platform CI**
   - Add Windows job (for .NET + Unity parity) and plan macOS lane after Unity docs ship.
   - Consider nightly runs enabling IO/OS TAP suites on trusted hardware.
5. **Benchmark Governance**
   - Document local execution workflow, thresholds, and reporting cadence.
   - Capture comparison runs (e.g., NLua parity) in release notes or `/docs`.

## Long-Horizon Ideas
- Property/fuzz testing for lexer/parser and VM instruction boundaries.
- Golden-file assertions for debugger protocol payloads and CLI output.
- Native AOT / trimming validation once runtime stack is fully nullable-clean.
- Automated regression harness for memory allocations using BenchmarkDotNet diagnosers.

Keep this plan in sync with `docs/Testing.md` and `docs/Modernization.md`. Update after each milestone so contributors know what's complete, in-flight, and still open.
