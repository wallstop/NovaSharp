# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-11-30 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release` (2025-11-30 09:20 UTC) finished with zero warnings while `<TreatWarningsAsErrors>true>` stays enforced across the solution.
- Tests: `scripts/coverage/coverage.ps1` now drives every suite via Microsoft.Testing.Platform. The latest run (2025-11-30 09:28 UTC) executed **2,136** interpreter TUnit tests (`src/tests/NovaSharp.Interpreter.Tests.TUnit`), **40** remote-debugger TUnit tests, and **881** NUnit tests (`src/tests/NovaSharp.Interpreter.Tests`) with zero skips/failures, keeping `FixtureCatalogGenerated.cs` synced at **75** fixtures.
- Coverage: `docs/coverage/latest/Summary.md` (2025-11-30 09:28 UTC) reports **86.6 % line / 86.5 % branch / 88.8 % method** overall after the combined Release run (total **3,057** tests).
  - NovaSharp.Interpreter: 96.13 % line / 93.83 % branch / 97.56 % method (still <95 % branch, so `COVERAGE_GATING_MODE` remains in monitor mode).
  - NovaSharp.Cli: 83.02 % line / 76.26 % branch.
  - NovaSharp.Hardwire: 55.72 % line / 46.58 % branch.
  - NovaSharp.RemoteDebugger: 76.73 % line / 66.25 % branch (headless debugger TUnit coverage is active again).
  - NovaSharp.VsCodeDebugger: 1.84 % line / 2.08 % branch (DAP automation still pending).
- Coverage collateral: rerun `./scripts/coverage/coverage.ps1` (Microsoft.Testing.Platform-aware) and refresh `docs/coverage/coverage-hotspots.md` whenever numbers move; last refresh completed 2025-11-30 09:28 UTC with the figures above.
- Audits: `documentation_audit.log`, `naming_audit.log`, and `spelling_audit.log` are green (0 missing XML docs, no naming/spelling findings). Re-run the trio whenever APIs or text-heavy docs change.
- Regions: `rg -n '#region'` only returns AGENTS.md/PLAN.md, so runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Keep the documentation audit checked in. Re-run `python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log` whenever public/internal APIs change.
- `tools/NamingAudit` and `tools/SpellingAudit` are wired into CI; refresh `naming_audit.log` and `spelling_audit.log` locally before pushing code that touches identifiers or docs.
- Namespace/script hub governance already gates CI. Any new helper must live under `scripts/<area>/` with a README update plus PR-template acknowledgement.
- Re-run `rg -n '#region'` whenever generators or imports are added; update offending generators to strip regions automatically.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned with the helpers in `scripts/` so contributors have a single source of truth.

## Active Initiatives

### CSharpier formatting backlog
- Current status: the entire repo (runtime/tooling/tests) has been reflowed with `dotnet csharpier`, and CI’s lint job now runs `dotnet csharpier check .` via `scripts/ci/check-csharpier.sh`. Treat any lingering `dotnet format` complaints as configuration bugs and update `.editorconfig`/workflow settings so they no longer contradict CSharpier output. **Next steps:** align the remaining formatter configuration (dotnet-format, IDE analyzers) with CSharpier and document the policy in `docs/Testing.md`/contributor guides once the tooling is in sync.

### Highest priority — Test framework modernization & parallelization (TUnit conversion)
- This initiative outranks every other workstream until the interpreter and debugger suites run natively on TUnit. Lead with TUnit conversion progress in every PLAN update and unblock contributors by documenting the migration playbook as we go.
- **High priority:** migrate all NovaSharp test projects from NUnit 2.x to modern test infrastructure so we can take advantage of fixture-level parallelism, `Assert.Throws`, and Microsoft.Testing.Platform. Remaining scope:
  1. Triage the Interpreter VM/core fixtures still on NUnit (Processor/Coroutine heavy hitters, Script loader helpers not yet ported).
  2. Keep the shared adapter (`src/tests/TestInfrastructure/*`) current so isolation helpers and TestUtilities stay reusable across NUnit/TUnit suites.
 3. Continue measuring NUnit vs. TUnit deltas with `scripts/tests/compare-test-runtimes.ps1` so performance trends remain visible.
- TUnit data-driven audit: once the interpreter NUnit project is fully retired, sweep the TUnit suites for manual loops or duplicated assertions that could use `[Arguments]`, `[MethodDataSource]`, `[ClassDataSource]`, `[CombinedDataSources]`, or matrix sources (see https://tunit.dev/llms.txt). Capture gaps + helper ideas here and update AGENTS.md so future tests default to these mechanics.
- **Progress snapshot (2025-11-30):** The interpreter TUnit suite now runs **2,136** tests in ~7 s (latest coverage run) with `FixtureCatalogGenerated.cs` tracking **75** fixtures. The Hardwire descriptor stack (code generation context, descriptor/member/method/userdata fixtures), DebugModule, LoadModule, LuaBase, and HardwireParameter suites all live on TUnit, and `rg -l "[TestFixture]" src/tests/NovaSharp.Interpreter.Tests | Measure-Object` reports **74** NUnit fixtures left to migrate.
- **2025-11-30 09:28 UTC update:** Linked the shared Hardwire/TestUtilities helpers into the TUnit project, added `HardwireParameterDescriptorTUnitTests`, fixed the analyzer violations (StringComparison, CA1806, CoreModules.None usage) in the newly ported suites, deleted the redundant NUnit fixtures, regenerated `FixtureCatalogGenerated.cs`, and reran `dotnet test --project src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj -c Release --no-build` to validate the expanded suite (total TUnit tests: **2,136**).
  - **Outstanding interpreter fixtures:** Focus next on the VM-heavy NUnit suites that still exist (`ProcessorTests`, `CoroutineApiTests`, `CoroutineCloseTests`, lingering Script/Debugger helpers, and the remaining module/tooling adapters). Clearing those high-risk files will collapse the remaining **74** NUnit fixtures quickly so we can retire the interpreter NUnit project.
  - **Full TUnit migration mandate:** with the Microsoft.Testing.Platform runner now required everywhere and the TUnit pilot demonstrating ~60 % runtime savings plus better async/parallel ergonomics, the long-term plan is to move every NovaSharp test project from NUnit to TUnit so we can lean on TUnit’s first-class async scheduling, fluent assertions, scoped fixtures, per-test cancellation, and deterministic diagnostics. This includes (1) finalizing the remote-debugger pilot findings, (2) adding a TUnit adapter project for interpreter/unit suites, (3) codifying assertion/safety patterns (timeouts, cancellation tokens, `ParallelGroup` annotations), and (4) updating docs/tooling/build scripts to treat TUnit as the canonical runner. Document each milestone here as we convert suites so contributors know when to expect the repo-wide cutover.
  - **Next steps (2025-11-30 update):**
    1. Finish migrating the remaining NUnit fixtures (Processor/coroutine/Script helpers plus the last CLI/Debugger adapters) and regenerate the fixture catalog after each drop so we can remove the interpreter NUnit project entirely.
    2. Once the interpreter fixtures are green on TUnit, delete the duplicated NUnit suites, update docs/tooling/scripts to treat TUnit as the canonical runner, and capture a final `scripts/tests/compare-test-runtimes.ps1` report for posterity.
    3. Keep expanding the remote-debugger and VS Code debugger TUnit coverage (attach/pause/watch transport errors, DAP smoke tests) so their NUnit counterparts can also be removed and coverage for those assemblies clears the ≥80 % targets.
    4. After both interpreter and debugger suites run exclusively on TUnit, review every TUnit fixture for modernization opportunities (data attributes, `[CombinedDataSources]`, `[MatrixDataSource]`, helper scopes) and codify the refreshed patterns in AGENTS.md/docs so future tests default to the modern style.
    5. After the TUnit cutover is complete, begin the repository re-organization effort (see “Codebase organization & namespace hygiene”) so the folder layout mirrors the cleaner test split and namespace guidance.
    6. Continue eliminating ad-hoc `try`/`finally` cleanup blocks by introducing the shared disposable scopes called out below so the entire suite relies on consistent helper primitives.
    7. Keep the `[UserDataIsolation]` semaphore temporary—once Microsoft.Testing.Platform exposes the per-worker lifecycle hook we need, replace the gate with a proper test-start/end receiver so user-data fixtures can regain full parallelism without the serial bottleneck.

### High priority — Test cleanup helpers & using-pattern enforcement
- Problem: interpreter/tooling/CLI fixtures still rely on ad-hoc `try`/`finally` blocks (platform overrides, Unity harness toggles, temp directories, console capture) which leads to missed tear-down paths when new assertions short-circuit the test. We need a repo-wide sweep to replace the manual cleanup logic with `IDisposable` helpers that are consumed via `using` statements (or the C# 8 `using var` pattern) so resource lifetimes are explicit and analyzer-friendly.
- Plan of record:
  1. Inventory every `try`/`finally` pair under `src/tests` (NUnit + TUnit) and categorize the cleanup semantics (temp files, platform overrides, Unity harness toggles, user-data registrations, `Script.GlobalOptions` resets, environment variables, IO streams, etc.).
  2. For each category, design or extend a reusable `IDisposable` helper (e.g., `PlatformOverrideScope`, `TempDirectoryScope`, `UserDataIsolationScope`, `GlobalOptionsScope`, `ConsoleCaptureScope`). Prefer colocating these in `TestInfrastructure/Scopes` so both NUnit and TUnit projects can share them.
  3. Codemod the identified tests to depend on the new helpers via `using` statements rather than manual `try`/`finally` blocks. Favor data-driven helpers (e.g., `using var scope = PlatformOverrideScope.UnityDesktop();`) so the intent is obvious in each test.
  4. Add an analyzer or Roslyn-based lint (even a temporary `rg`/CI script) that flags new `try`/`finally` usage in tests when the `finally` block simply tears down disposable state, keeping the suite aligned with the helper abstractions long term.
  5. Update `docs/Testing.md` + AGENTS.md so contributors know to reach for the helper scopes before writing manual cleanup logic, and highlight this initiative in PR templates until the sweep is finished.
- Success criteria: zero ad-hoc `try`/`finally` cleanup blocks remain in interpreter/tooling/debugger tests (outside of assertions that legitimately require them), new helpers are documented and shared, and CI enforces the pattern for future contributions.

### High priority — Codebase organization & namespace hygiene
- Current state: runtime/tooling/test projects largely mirror the historical MoonSharp layout, leaving most interpreter tests under monolithic buckets such as `Units/`, `TestMore/`, or `TUnit/VM/` with little discoverability. Production code follows the same pattern—`NovaSharp.Interpreter` is a single assembly containing interpreter, modules, platforms, IO helpers, and tooling adapters.
- Problem: Contributors struggle to locate feature-specific code/tests, and the wide namespaces make it hard to reason about ownership or layering (e.g., Lua VM vs. tooling vs. debugger). PLAN.md now tracks the reorganization as a high-priority initiative so we treat it as a first-class modernization step alongside TUnit.
- Objectives:
  1. Propose a refined solution layout that splits runtime/tooling/test assets into feature-scoped projects (e.g., `NovaSharp.Interpreter.Core`, `NovaSharp.Interpreter.IO`, `NovaSharp.Tooling.Cli`, `NovaSharp.Tests.Interpreter.Runtime`, `NovaSharp.Tests.Interpreter.Modules`, etc.) while keeping build scripts and packaging intact.
  2. Restructure the interpreter test tree by domain rather than framework (e.g., `Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`, `Debugger/Remote`, `Integration/EndToEnd`). Update namespaces to match the new folder structure and keep fixture catalog generation aligned.
  3. Document the new layout in PLAN.md + `docs/Testing.md`, and add guardrails (analyzers or CI checks) so new code/tests land in the correct folders with consistent namespaces.
  4. Ensure the reorganization is incremental but tracked: migrate one subsystem at a time, update project references, and verify coverage after each move so we do not destabilize the main branch.

### 1. Analyzer and warning debt
- Current status (2025-12-09): dotnet build src/NovaSharp.sln -c Release -nologo remains clean with <TreatWarningsAsErrors>true> enforced across the solution. **Next steps:** document any new suppressions in PLAN.md/docs/Testing.md, keep the PR template’s analyzer checklist up to date, and treat fresh CA hits as stop-ship items.
- Active follow-up: audit the remaining targeted suppressions (e.g., CA1051 field fixtures, IDE1006 Lua port intentional cases, module-level CA1515) and convert any that are no longer needed into real code fixes so the analyzer baseline remains suppression-light.
- CA1051 follow-up: the previously flagged test fixtures (`TestRunner` counters plus descriptor/userdata helpers) are clean, but keep auditing new test infrastructure so any future public-field regressions are either converted to properties or annotated with tightly scoped suppressions.
- CA1515 plan: fixture catalog automation (MSBuild, pre-commit, CI) now ensures every `[TestFixture]` has a `typeof(...)` reference in `FixtureCatalogGenerated.cs`. Next steps are (a) convert non-reflection fixtures/helpers to `internal` where possible and (b) scope the remaining CA1515 suppressions to the handful of helper types that must stay public for NUnit/BenchmarkDotNet.
- BenchmarkDotNet benchmark classes (`RuntimeBenchmarks`, `ScriptLoadingBenchmarks`, `LuaPerformanceBenchmarks`) must remain `public` and unsealed for discovery; keep the targeted CA1515 suppressions plus the AGENTS.md guidance so new benchmarks follow the same pattern.
- Policy reminder: AGENTS.md forbids nullable reference-type syntax (no `#nullable`, `string?`, `?.` targeting reference members, or `null!`). Keep running `artifacts/NrtScanner` (or a simple `rg`) before opening analyzer-heavy PRs so the ban stays enforced and CA1805 continues to pass without suppressions.

### 2. Coverage and test depth
- Refresh artefacts: rerun ./scripts/coverage/coverage.ps1 (Release, gate = enforce) so docs/coverage/latest/* and docs/coverage/coverage-hotspots.md describe the latest test suite.
- 2025-11-30 09:28 UTC: Reran `./scripts/coverage/coverage.ps1` (Release suites: **2,136** interpreter TUnit + **40** remote-debugger TUnit + **881** NUnit) after wiring up the Hardwire/Module conversions. The run finished with **0** failures, refreshed `docs/coverage/latest/*` + `docs/coverage/coverage-hotspots.md`, and reports NovaSharp.Interpreter at **96.13 % line / 93.83 % branch / 97.56 % method**; `COVERAGE_GATING_MODE` stays in monitor mode until branch coverage clears the ≥95 % bar.
- Remote-debugger coverage is back to **76.73 % line / 66.25 % branch**, but NovaSharp.VsCodeDebugger still sits at **1.84 % line / 2.08 % branch**, so the DAP smoke tests remain a top priority.
- Interpreter: add debugger/coroutine regression tests that drive pause-during-refresh, queued actions that drain after a pause, forced resume, and message decoration paths so branch coverage climbs from ~93 % to ≥95 %.
- Tooling: extend NovaSharp.Cli tests beyond current command-unit coverage (record REPL transcripts and golden outputs) and build Hardwire generator tests that validate descriptor generation/error handling, targeting ≥80 % line coverage for each project.
- Debuggers: add headless VS Code + Remote Debugger smoke tests (attach/resume/breakpoint/watch evaluation) to push NovaSharp.VsCodeDebugger line coverage past 50 % and NovaSharp.RemoteDebugger branch coverage above 85 %.
- Replace skipped IO/OS TAP suites with NUnit fixtures so Release runs exercise those semantics without Lua harnesses.
- Observability: enhance the GitHub coverage job to compare the new Summary.json against the last successful run and fail on ≥3 percentage point regressions; archive history under rtifacts/coverage/history.
- Remaining interpreter branch debt (updated 2025-11-26 21:45 UTC): Coroutine (~83.3 %), UnityAssetsScriptLoader (~86.8 %), PlatformAutoDetector (~87.5 %), Script (~83.8 %), UnaryOperatorExpression (~85 %), and any lingering Script/repl/helpers not yet converted to guard-tested code paths. Prioritize these guard paths so interpreter branch coverage can cross ≥95 % and we can re-enable gating.
- Next steps: Close out the remaining hotspots (Coroutine, UnityAssetsScriptLoader, PlatformAutoDetector, Script, UnaryOperatorExpression, and the outstanding Script/REPL helpers) by adding guard-path unit tests so interpreter branch coverage can cross the ≥95 % enforcement bar.

### 3. Debugger and tooling automation
- Build a DAP test harness that drives NovaSharp.VsCodeDebugger end-to-end (launch, attach, breakpoints, watches) without requiring VS Code, and feed its transcripts into NUnit.
- Add CLI integration tests that execute scripted sessions (stdin/stdout golden files) covering success/failure paths for `run`, `register`, `debug`, `compile`, `hardwire`, and `help` commands.
- Stand up remote-debugger smoke tests that exercise HTTP attach, TCP streaming, queue draining, and error signaling; add golden payload assertions so regressions show up in diffs.
- Expand CI beyond Linux so debugger + CLI automation also run on Windows and macOS, matching the platforms we claim to support.

### 4. Runtime safety, sandboxing, and determinism
- Design Lua sandbox profiles that toggle risky primitives (file IO, environment variables, OS commands, reflection hooks) and expose host-driven policies via `ScriptOptions`; document the behaviour in `docs/LuaCompatibility.md`.
- Add configurable ceilings for time, memory, recursion depth, coroutine counts, and table growth along with watchdog callbacks so runaway mods cannot stall hosts.
- Introduce a deterministic execution mode (stable PRNG seeding, invariant formatting, deterministic iteration where Lua allows) for lockstep multiplayer/replays.
- Provide per-mod isolation containers plus load/reload/unload hooks so mods do not leak state across sessions.

### 5. Packaging, performance, and runtime ergonomics
- Unity onboarding: automate UPM/embedded packaging, refresh the sample scenes, and capture IL2CPP/AOT caveats in `docs/UnityIntegration.md`; wire the packaging script into CI.
- Packaging pipeline: publish redistributable runtime bundles/NuGet packages with versioning/signatures and document the workflow in release notes.
- Enum allocation audit: port the allocation-free flag helpers/name maps to remove `Enum.HasFlags`/`ToString()` allocations on hot paths; add benchmarks and NUnit coverage.
- Custom collections: audit `LinkedListIndex`, `FastStack`, `MultiDictionary`, etc. for BCL parity, replace `ContainsKey`+indexer patterns with `TryGetValue`, and document preferred usage.
- Performance regression harness: run BenchmarkDotNet (runtime + comparison suites) in CI/nightly, capture allocation deltas, and require `docs/Performance.md` updates when numbers move.
- Investigate high-performance string/IO libraries (e.g., ZString) and prototype them in parser/IO hotspots without harming readability or Unity compatibility.
- String operations audit: inventory the runtime/tooling string formatting, concatenation, and builder-heavy paths, evaluate whether ZString or compiler-generated interpolation can replace ad-hoc `StringBuilder` usage, and document any trade-offs (Unity/IL2CPP safety, culture invariance) before landing changes.
- Interpreter hot-path optimization: profile the Lua VM (instruction loop, stack ops, call/return pipelines) to identify heap allocations and branch mispredictions, then prototype zero-allocation strategies (array/object pools, bit-packed instruction metadata, custom data structures, ZLINQ/ZString integration) and document measured wins before rolling them out broadly.
- I/O throughput audit: benchmark every runtime/tooling I/O surface (script loaders, REPL streams, debugger transports, file/tcp helpers), minimize allocations via pooling/buffering, and consider high-performance libraries (e.g., pipelines, Span-based readers) while keeping Unity/IL2CPP compatibility intact.
- Whole-runtime optimization pass: schedule iterative sweeps (excluding test code) that profile each subsystem, track allocations/instructions, and apply aggressive optimizations (custom data structures, pooling, bit packing, low-level intrinsics) where they deliver measurable wins without breaking API/back-compat.

### 6. Tooling, docs, and contributor experience
- Roslyn code generation milestone: design and prototype source generators/analyzers for NovaSharp descriptors/mod code, then document how to consume them.
- Documentation & samples: adopt DocFX (or similar), publish compatibility matrices/tutorials, refresh Unity/modding guides, and automate doc generation in CI.
- Compatibility corpus: expand CI to run Lua TAP suites, community mod packs, and script corpora across Windows, macOS, Linux, and Unity editor builds; track the matrix in `docs/Testing.md`.
- Style/quality automation: extend lint to reject runtime changes that lack matching tests (unless `[NoCoverageJustification]` is present), enforce `_camelCase` fields, and ensure new scripts/docs update the relevant indexes.
- 2025-11-26 23:59 UTC: Added contributor-facing ignore lists (`.claudeignore`, `.codexignore`) so both assistant profiles skip generated artefacts (bin/obj, coverage HTML, logs, IDE folders). **Next steps:** document the policy in `AGENTS.md` and keep the ignore lists updated when new artefact directories are introduced.

### 7. Outstanding investigations
- Confirm `pcall`/`xpcall` semantics when CLR callbacks yield; add regression tests or update runtime behaviour to match Lua 5.4 if needed.
- Decide whether `SymbolRefAttributes` should be renamed to satisfy CA1711 or if a documented suppression is acceptable; capture the outcome in this plan and analyzer settings.

### 8. Concurrency and synchronization audit
- Inventory every `lock`, `Monitor`, and ad-hoc concurrency helper across runtime, tooling, and debugger code. Document each critical section’s purpose, contention risk, and whether a `ReaderWriterLockSlim`, `SemaphoreSlim`, or lock-free primitive would improve scalability without hurting determinism.
- Identify shared collections that still use `List<T>`/`Dictionary<T>` under concurrent access (e.g., debugger server lists, HTTP caches, tooling registries). Where appropriate, switch to `ConcurrentDictionary<T>`/`ImmutableArray<T>` or add guarding APIs instead of external locks.
- Validate that dispose paths, async callbacks, and network events cannot deadlock (double-lock patterns, nested locks across types). Capture any required lock-ordering guidance in `docs/Modernization.md`.
- Benchmark contention-sensitive paths (RemoteDebugger queues, interpreter hook dispatch, CLI registries) before swapping primitives; keep measurements in `docs/Performance.md` so future changes have baselines.
- Produce a checked-in concurrency inventory (e.g., `docs/modernization/concurrency-inventory.md`) that lists every `lock`/`Monitor`/`SemaphoreSlim` usage, captures contention risk, and calls out candidates for `ReaderWriterLockSlim`/lock-free swaps so future reviewers can reason about synchronization at a glance.

## Lua specification parity
- Keep the Lua 5.4 parity matrix in `docs/LuaCompatibility.md` up to date; cite manual sections for every behaviour we touch.
- Extend the compatibility-mode surface so hosts can opt into Lua 5.1, 5.2, 5.3, or 5.4 semantics (chunk loading rules, standard library variants, coroutine differences) via `ScriptOptions`/`LuaCompatibilityProfile`, and back each mode with targeted NUnit + spec-harness coverage plus docs describing supported toggles.
- Extend the spec harness beyond the existing string suite (math, table, utf8, coroutine, debug, IO) and store fixtures beside the NUnit TAP corpus.
- Integrate the spec harness into CI so spec regressions fail builds; document the workflow in `docs/testing/spec-coverage.md`.
- Golden rule: when a regression test fails, assume production is wrong until the Lua 5.4 manual proves otherwise; prefer fixing runtime behaviour over weakening tests.

## Long-horizon ideas
- Property and fuzz testing for the lexer, parser, and VM instruction boundaries.
- Golden-file assertions for debugger protocol payloads and CLI output.
- Native AOT/trimming validation once the runtime stack is fully nullable-clean.
- Automated allocation regression harnesses using BenchmarkDotNet diagnosers or `dotnet-trace`.

Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`, and update it whenever coverage artefacts, warning counts, or milestone statuses change.


- Note: need to port ProcessorTests
