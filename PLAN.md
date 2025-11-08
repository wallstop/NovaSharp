# Modern Testing & Coverage Plan

## Modernization Baseline (Nov 2025)
- ✅ Core runtime, debuggers, hardwire tooling, CLI shell, benchmarks, and automated tests now target **`netstandard2.1`** (runtime) and **`net8.0`** (tooling).
- ✅ Legacy `.NET 3.5/4.x`, Portable Class Library, Windows 8/Phone, Silverlight, Unity, Xamarin, and NuGet test harness projects removed from the tree.
- ✅ Solution simplified to `src/MoonSharp.sln`; obsolete `moonsharp_*` variants deleted.
- ✅ Benchmark infrastructure rebuilt on BenchmarkDotNet with shared `PerformanceReportWriter` writing OS-specific results to `docs/Performance.md`; benchmarks remain local-only by design.
- ✅ Documentation refreshed: `docs/Performance.md`, `docs/Testing.md`, `docs/Modernization.md`, and README now describe the modern stack and link together.

## Testing Health Snapshot
- Runner: `src/tests/TestRunners/DotNetCoreTestRunner` (`net8.0`) executes **627 tests** with **2 intentional skips** (`TestMore_308_io`, `TestMore_309_os`).
- Command: `dotnet run -c Release -- --ci` (mirrors CI behaviour). Produces `moonsharp_tests.log` for triage.
- Coverage: `coverage.ps1` (coverlet + reportgenerator) records **62.2 % line / 61.4 % branch** against Release binaries; raw artefacts land in `artifacts/coverage`, HTML in `docs/coverage/latest`.
- Suite composition:
  - **Lua TAP fixtures** validating language semantics and standard library parity.
  - **End-to-end NUnit scenarios** covering userdata interop, debugger attach, coroutine pipelines, serialization, JSON.
  - **Unit tests** for low-level primitives (virtual machine stacks, binary dump/load, interop policies).
- CI (`.github/workflows/tests.yml`):
  - Restores and builds `src/MoonSharp.sln` in Release.
  - Runs the dotnet runner.
  - Uploads `moonsharp_tests.log` as an artefact for every PR/push to `master`.
- Benchmarks (`MoonSharp.Benchmarks`, `PerformanceComparison`) compile but never run in CI; they must be invoked manually to update `docs/Performance.md`.

## Coverage Strengths
- Lua semantics, coroutine scheduling, binary dump/load, JSON, interop policy, and hardwire regeneration paths have broad regression protection.
- TAP fixtures catch regressions against upstream Lua behaviour.
- Benchmark baselines establish performance + allocation expectations for script loading and runtime scenarios.

## Gaps & Risks
- **Debugger automation**: No integration tests for VS Code or remote debugger protocols.
- **Tooling**: CLI shell and remaining tooling projects lack regression coverage after modernization.
- **Cross-platform**: CI only runs on Linux; Windows/macOS coverage still manual.
- **Observability**: No branch/line coverage metrics; hard to quantify test depth.
- **Skip-list debt**: IO/OS TAP suites remain permanently skipped without compensating coverage.
- **Unity onboarding**: Legacy Unity samples removed; new guidance for consuming `netstandard2.1` packages is outstanding.

## Coverage Initiative (Target ≥ 90%)
- **Milestone 0 – Baseline Measurement (in-flight)**  
  Integrate Coverlet with `src/tests/TestRunners/DotNetCoreTestRunner`, emit LCOV + Cobertura outputs under `artifacts/coverage`, and publish HTML reports to `docs/coverage/latest`. Document the workflow in `docs/Testing.md` and ship a `coverage.ps1` helper for local execution. Acceptance: CI artefact with <current %> baseline and documented CLI workflow.
  - ✅ `coverage.ps1` restores local tools, runs coverlet against the runner, and generates HTML via reportgenerator (`artifacts/coverage`, `docs/coverage/latest`).
- **Milestone 1 – Unit Depth Expansion (Weeks 1-3)**  
  Expand `src/tests/MoonSharp.Interpreter.Tests.Legacy/Units` to cover parser error paths, metatable resolution, tail-call recursion limits, dynamic expression evaluation, and data-type marshaling edge cases (tables↔CLR types). Add regression tests for CLI utilities in `src/tooling` (command parsing, REPL, config). Target ≥ 70% line coverage for `MoonSharp.Interpreter` namespace before integration scenarios.
  - ✅ Added `ParserTests`, `MetatableTests`, `TailCallTests`, `DynamicExpressionTests`, and expanded `InteropTests` to lock in parser diagnostics, metatable `__index/__newindex` wiring, deep tail-call paths, dynamic expression environments, and CLR table marshaling.
  - ✅ Shipped `ReplInterpreterTests` + `JsonModuleTests` to cover REPL prompts/dynamic evaluation and JSON encode/decode paths; interpreter namespace line coverage still 67.7 %, so JSON branch is unblocked but IO/OS modules remain uncovered.
  - ✅ Added `IoModuleVirtualizationTests` to back the IO/OS pathways with an in-memory platform accessor and keep `os.remove`/`io.output` behaviour under regression; interpreter namespace line coverage still 67.7 %.
  - ✅ Captured CLI command parsing transcripts with `ShellCommandManagerTests` and extended the virtualised IO suite to assert `os.tmpname`/`os.rename` plus stdout/stderr separation; interpreter namespace coverage gain pending analysis.
  - ⏳ Next: quantify the new coverage delta, plug remaining IO/CLI edge cases (e.g., failure paths, config persistence), and line up the pull request that nudges the interpreter past the 70 % gate.
- **Milestone 2 – Integration & Debugger Coverage (Weeks 2-4)**  
  Build scripted VS Code + remote debugger sessions using `Microsoft.VisualStudio.Shared.VstestHost` (or equivalent) to validate attach/resume/breakpoint-insert/watch evaluation flows. Add Lua fixture-backed smoke tests for `MoonSharp.RemoteDebugger` and CLI shell transcripts; capture transcripts as golden files in `src/tests/TestRunners/TestData`. Ensure TAP IO/OS suites run in trusted CI lane or supply equivalent managed tests hitting filesystem/environment abstractions.
- **Milestone 3 – Coverage Gates & Reporting (Week 4)**  
  Wire coverage upload to Codecov (or Azure DevOps equivalent), failing PRs under 85% line coverage and raising the gate to 90% once Milestones 1-2 land. Add a dashboard to `docs/Testing.md` summarizing module-by-module coverage and bake badge URLs into `README`. Ensure nightly CI refreshes artefacts and warns on ≥3% regressions via GitHub checks.
- **Milestone 4 – Sustained Quality (Ongoing)**  
  Author contributor guidance (PR template checklist, test matrix updates) requiring new features to include both unit and integration coverage. Automate review lint that rejects files in `src/MoonSharp.Interpreter` without accompanying tests unless tagged `[NoCoverageJustification]` with an engineering sign-off. Track skip-list debt in an issue epic and burn it down as platform lanes stabilize.

## Project Structure & Code Style Alignment
- **Milestone A – Solution & Directory Audit (High Priority)**  
  Inventory every `.sln`/`.csproj` in `src`, classify runtime vs tooling vs samples, and propose a consolidated folder hierarchy (runtime/interpreter, debuggers, tooling, samples, tests). Produce a migration blueprint documenting path moves, namespace impacts, and packaging implications. Acceptance: reviewed architecture doc + updated `docs/Modernization.md` appendix.
  - ✅ `docs/ProjectStructureBlueprint.md` captures the current vs. proposed layout, legacy inventory, and phased migration plan.
- **Milestone B – Refactor Execution (High Priority)**  
  Reshuffle projects/solutions per blueprint (rename folders, update `.sln`, `.csproj`, `Directory.Build.props`), add `Directory.Packages.props` if needed, and ensure build/test scripts, CI workflows, and documentation follow new layout. Include regression checklist covering NuGet outputs, debugger packaging, and tooling discovery.
  - ✅ Rehomed runtime, debugger, tooling, samples, tests, docs, and legacy assets under the new directory taxonomy; updated `MoonSharp.sln`, helper scripts, and top-level docs to match.
  - ✅ Collapsed the interpreter `_Projects` mirror into a multi-targeted `MoonSharp.Interpreter.csproj` (netstandard2.1 + net8.0), updating dependent projects, the solution, and automation scripts.
  - ✅ Folded the VS Code debugger `_Projects` mirror into the primary `MoonSharp.VsCodeDebugger.csproj` (netstandard2.1 + net8.0) and refreshed downstream project references.
  - ✅ Renamed the CLI shell to `tooling/MoonSharp.Cli/MoonSharp.Cli.csproj` and adjusted tests/solution/docs to point at the new path.
  - ⏳ Audit packaging (NuGet metadata, release notes, scripts) for the CLI rename and update any hard-coded paths.
  - ✅ Replaced CLI `packages/*` binaries with NuGet-managed references; ensure remaining legacy tooling cleans up any straggler DLL drops.
- **Milestone C – Namespace & Using Enforcement**  
  Introduce Roslyn analyzers or custom scripts to ensure namespaces mirror the physical path + project root (`MoonSharp.Interpreter.Debugging` style), and require `using` directives to live inside namespaces. Provide migration scripts to batch-update existing files, codify exceptions for generated/bundled code, and document rules in `docs/Contributing.md`.
- **Milestone D – EditorConfig Adoption + Lua Exceptions**  
  Import `.editorconfig` from `D:/Code/DxMessaging-Unity/Packages/com.wallstop-studios.unity-helpers`, strip BOM (`charset = utf-8`), and align with repo conventions (CRLF is acceptable). Add sub-directory `.editorconfig` under Lua fixture folders to keep Lua-specific indentation/whitespace expectations. Document formatting commands and exception rationale in `docs/Testing.md` + PR template.
- **Milestone E – Solution Organization & Naming (Current Sprint)**  
  Harden the Visual Studio solution layout and align naming with PascalCase conventions for first-party assets. Scope includes renaming the solution artifact, ensuring nested folders mirror `runtime/tooling/tests/debuggers`, and cleaning up lingering `.netcore` suffixes in project surfaces.
  - ✅ Renamed `src/moonsharp.sln` to `src/MoonSharp.sln`, updated build/test docs, and nested Benchmarks under the Tooling solution folder to prevent empty roots.
  - ✅ Rebranded the VS Code debugger project (`MoonSharp.VsCodeDebugger.csproj`) and refreshed dependent project references to respect PascalCase naming.
  - ✅ Updated VS Code debugger package metadata (dropped `.netcore` suffix) and documented the new single-source layout.

## Near-Term Priorities (ordered)
1. **Coverage Push (>90%)**
   - Complete Milestone 0 baseline capture and land coverage helper scripts.
   - Begin Milestone 1 by targeting parser + metatable hot spots and porting CLI shell tests.
   - Draft GitHub workflow updates with conditional coverage gating (warning-only until ≥85% reached).
2. **Project Structure Refactor (High Priority)**
   - ✅ Solution + debugger project rename landed (PascalCase `MoonSharp.sln`, `MoonSharp.VsCodeDebugger.csproj`).
   - ✅ VS Code debugger `_Projects` mirror removed; project now multi-targets `netstandard2.1;net8.0`.
   - ✅ CLI rename complete (`tooling/MoonSharp.Cli`).
   - ⏳ Audit packaging/scripts for the new CLI name (NuGet spec, release docs, tooling installers).
   - ✅ CLI now restores solely via NuGet (no checked-in packages).
   - ⏳ Audit the `src/legacy` tree for lingering native DLL drops (e.g., `lua52.dll`) and decide whether to vendor via packages or formally quarantine.
   - Update automation/scripts (`rsync_projects.sh`, CI workflows) and documentation with new directory paths (partial).
   - Coordinate with owners of legacy assets under `src/legacy` to confirm deletion/archive strategy.
3. **Namespace & Formatting Enforcement**
   - Draft analyzer configuration enforcing path-aligned namespaces + `using` inside namespace.
   - Stage `.editorconfig` import (Milestone D) and prepare Lua fixture overrides.
   - Identify legacy/generated files needing suppression tags or sub-config.
4. **Unity/Packaging Alignment**
   - Produce Unity quick-start docs leveraging the `netstandard2.1` runtime.
   - Ensure NuGet/packages expose the correct TFMs (`netstandard2.1` + optional `net8.0` tooling).
   - Audit residual docs (e.g., `release_readme.txt`, website copy) for legacy framework references.
5. **Debugger Smoke Tests**
   - Author lightweight harnesses to drive VS Code and remote debuggers (attach, breakpoint, variable inspection).
   - Gate behind opt-in flag while stabilising.
6. **Cross-Platform CI**
   - Add Windows job (for .NET + Unity parity) and plan macOS lane after Unity docs ship.
   - Consider nightly runs enabling IO/OS TAP suites on trusted hardware.
7. **Benchmark Governance**
   - Document local execution workflow, thresholds, and reporting cadence.
   - Capture comparison runs (e.g., NLua parity) in release notes or `/docs`.

## Long-Horizon Ideas
- Property/fuzz testing for lexer/parser and VM instruction boundaries.
- Golden-file assertions for debugger protocol payloads and CLI output.
- Native AOT / trimming validation once runtime stack is fully nullable-clean.
- Automated regression harness for memory allocations using BenchmarkDotNet diagnosers.

Keep this plan in sync with `docs/Testing.md` and `docs/Modernization.md`. Update after each milestone so contributors know what's complete, in-flight, and still open.
