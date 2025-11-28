# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-11-27 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release -nologo` (2025-11-27) finished with zero warnings while `<TreatWarningsAsErrors>true>` stays enforced across the solution.
- Tests: `dotnet test --project src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings` (Microsoft.Testing.Platform runner) exercises 3,155 Release tests in ~10.5 seconds on a 12-core workstation (no skips/failures) and keeps the generated fixture catalog in sync. `scripts/coverage/coverage.ps1` now shells out to the same command (no VSTest legacy flags), so coverage runs stay aligned with the main runner.
- Remote-debugger TUnit pilot: `dotnet test --project src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj -c Release` drives the in-memory handshake/watch scenarios in ~0.7 seconds, giving us a baseline to compare against the legacy NUnit harness while we evaluate a broader migration.
- Coverage: `docs/coverage/latest/Summary.md` (2025-11-27 13:58 UTC) reports 87.6 % line / 87.5 % branch overall.
  - NovaSharp.Interpreter: 96.7 % line / 94.5 % branch (still <95 % branch, so `COVERAGE_GATING_MODE` remains in monitor mode until the next run holds ≥95 %).
  - NovaSharp.Cli: 82.8 % line / 76 % branch.
  - NovaSharp.Hardwire: 55.7 % line / 46.5 % branch.
  - NovaSharp.RemoteDebugger: 88.1 % line / 81.1 % branch.
  - NovaSharp.VsCodeDebugger: 1.8 % line / 2 % branch (still awaiting automated debugger smoke tests).
- Coverage collateral: rerun `./scripts/coverage/coverage.ps1` (now Microsoft.Testing.Platform-aware) and refresh `docs/coverage/coverage-hotspots.md` so the hotspot backlog reflects these numbers instead of the older 2025-11-24 snapshot.
- Audits: `documentation_audit.log`, `naming_audit.log`, and `spelling_audit.log` are green (0 missing XML docs, no naming/spelling findings). Re-run the trio whenever APIs or text-heavy docs change.
- Regions: `rg -n '#region'` only returns AGENTS.md/PLAN.md, so runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Keep the documentation audit checked in. Re-run `python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log` whenever public/internal APIs change.
- `tools/NamingAudit` and `tools/SpellingAudit` are wired into CI; refresh `naming_audit.log` and `spelling_audit.log` locally before pushing code that touches identifiers or docs.
- Namespace/script hub governance already gates CI. Any new helper must live under `scripts/<area>/` with a README update plus PR-template acknowledgement.
- Re-run `rg -n '#region'` whenever generators or imports are added; update offending generators to strip regions automatically.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned with the helpers in `scripts/` so contributors have a single source of truth.

## Active Initiatives

### Documentation audit backlog
- Current status: repository-wide XML-doc coverage is back to zero gaps (see `documentation_audit.log`). **Next steps:** rerun `python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log` (plus naming/spelling audits) whenever new APIs/docs land so the log stays clean.

### CSharpier formatting backlog
- Current status: the entire repo (runtime/tooling/tests) has been reflowed with `dotnet csharpier`, and CI’s lint job now runs `dotnet csharpier check .` via `scripts/ci/check-csharpier.sh`. Treat any lingering `dotnet format` complaints as configuration bugs and update `.editorconfig`/workflow settings so they no longer contradict CSharpier output. **Next steps:** align the remaining formatter configuration (dotnet-format, IDE analyzers) with CSharpier and document the policy in `docs/Testing.md`/contributor guides once the tooling is in sync.

### 1. Analyzer and warning debt
- Current status (2025-12-09): dotnet build src/NovaSharp.sln -c Release -nologo remains clean with <TreatWarningsAsErrors>true> enforced across the solution. **Next steps:** document any new suppressions in PLAN.md/docs/Testing.md, keep the PR template’s analyzer checklist up to date, and treat fresh CA hits as stop-ship items.
- Active follow-up: audit the remaining targeted suppressions (e.g., CA1051 field fixtures, IDE1006 Lua port intentional cases, module-level CA1515) and convert any that are no longer needed into real code fixes so the analyzer baseline remains suppression-light.
- CA1051 follow-up: the previously flagged test fixtures (`TestRunner` counters plus descriptor/userdata helpers) are clean, but keep auditing new test infrastructure so any future public-field regressions are either converted to properties or annotated with tightly scoped suppressions.
- CA1515 plan: fixture catalog automation (MSBuild, pre-commit, CI) now ensures every `[TestFixture]` has a `typeof(...)` reference in `FixtureCatalogGenerated.cs`. Next steps are (a) convert non-reflection fixtures/helpers to `internal` where possible and (b) scope the remaining CA1515 suppressions to the handful of helper types that must stay public for NUnit/BenchmarkDotNet.
- BenchmarkDotNet benchmark classes (`RuntimeBenchmarks`, `ScriptLoadingBenchmarks`, `LuaPerformanceBenchmarks`) must remain `public` and unsealed for discovery; keep the targeted CA1515 suppressions plus the AGENTS.md guidance so new benchmarks follow the same pattern.
- Policy reminder: AGENTS.md forbids nullable reference-type syntax (no `#nullable`, `string?`, `?.` targeting reference members, or `null!`). Keep running `artifacts/NrtScanner` (or a simple `rg`) before opening analyzer-heavy PRs so the ban stays enforced and CA1805 continues to pass without suppressions.

### 2. Coverage and test depth
- Refresh artefacts: rerun ./scripts/coverage/coverage.ps1 (Release, gate = enforce) so docs/coverage/latest/* and docs/coverage/coverage-hotspots.md describe the latest test suite.
- Interpreter: add debugger/coroutine regression tests that drive pause-during-refresh, queued actions that drain after a pause, forced resume, and message decoration paths so branch coverage climbs from ~93 % to ≥95 %.
- Tooling: extend NovaSharp.Cli tests beyond current command-unit coverage (record REPL transcripts and golden outputs) and build Hardwire generator tests that validate descriptor generation/error handling, targeting ≥80 % line coverage for each project.
- Debuggers: add headless VS Code + Remote Debugger smoke tests (attach/resume/breakpoint/watch evaluation) to push NovaSharp.VsCodeDebugger line coverage past 50 % and NovaSharp.RemoteDebugger branch coverage above 85 %.
- Replace skipped IO/OS TAP suites with NUnit fixtures so Release runs exercise those semantics without Lua harnesses.
- Observability: enhance the GitHub coverage job to compare the new Summary.json against the last successful run and fail on ≥3 percentage point regressions; archive history under rtifacts/coverage/history.
- Remaining interpreter branch debt (updated 2025-11-26 21:45 UTC): Coroutine (~83.3 %), UnityAssetsScriptLoader (~86.8 %), PlatformAutoDetector (~87.5 %), Script (~83.8 %), UnaryOperatorExpression (~85 %), and any lingering Script/repl/helpers not yet converted to guard-tested code paths. Prioritize these guard paths so interpreter branch coverage can cross ≥95 % and we can re-enable gating.
- Next steps: Close out the remaining hotspots (Coroutine, UnityAssetsScriptLoader, PlatformAutoDetector, Script, UnaryOperatorExpression, and the outstanding Script/REPL helpers) by adding guard-path unit tests so interpreter branch coverage can cross the ≥95 % enforcement bar.

### 3. Test framework modernization & parallelization
- **High priority:** migrate all NovaSharp test projects from NUnit 2.6 to the latest NUnit 3 release so we can take advantage of fixture-level parallelism, `Assert.Throws`, and modern runner integrations. This requires:
  1. Updating package references / test SDKs and replacing deprecated attributes (`[ExpectedException]`, custom ExpectedExceptionAttribute, etc.) with NUnit 3 equivalents.
  2. Regenerating the reflection-based fixture catalog to ensure NUnit 3 discovery still sees every test (or replacing it with NUnit 3’s discovery hooks).
  3. Updating Ci pipelines (`dotnet test`, scripts/tests/update-fixture-catalog.ps1) to run under NUnit 3.
  4. Once NUnit 3 is in place, enable `[Parallelizable]` on fixtures that don’t mutate shared state, and configure `dotnet test` with `--parallel` (or appropriate settings) so the suite runs in parallel where safe.
- Document the migration plan in `docs/Testing.md` and add a PLAN.md checkpoint per major milestone (packages upgraded, attributes migrated, CI updated, parallelism enabled).
- 2025-11-27: Removed the legacy `[ExpectedException]` attribute across the interpreter suite, deleted the custom NUnit shim, updated `TestRunner` accordingly, and verified `dotnet test --project src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings` stays green (3,152 tests). Contributor docs (`AGENTS.md`, `docs/Testing.md`) now point to the `Assert.Throws` guidance—next step is to scope fixture-level parallelization and retire any other NUnit 2-era shims (e.g., ExpectedException references in historical docs/blog posts).
- 2025-11-27: Classified every `UserData*`/`VtUserData*` fixture as shared-state heavy and annotated them `[NonParallelizable]` so they continue to run serially while the rest of the suite parallelizes. Documentation now calls out the rule of thumb (immutable/local fixtures ⇒ `[Parallelizable]`, shared state ⇒ `[NonParallelizable]`). Next up: audit the remaining fixtures for shared-state usage and start isolating the `UserData` registries so more suites can safely opt into parallel scope.
- 2025-11-27: Added scoped `UserData` registry isolation (new `UserData.BeginIsolationScope()` API plus the `[UserDataIsolation]` NUnit attribute) and converted every `UserData*`, `VtUserData*`, and collections/proxy fixtures back to `[Parallelizable(ParallelScope.Self)]`. Release tests remain green and docs now describe the attribute. Next steps: extend the isolation helpers to any remaining shared-state suites and measure the parallel speedup in CI.
- 2025-11-27: Introduced `Script.BeginGlobalOptionsScope()` and wrapped the converter-heavy fixtures (`UserDataMethodsTests`, `VtUserDataMethodsTests`) in SetUp/TearDown scopes so `Script.GlobalOptions` tweaks stay local even while the suites run in parallel. OsSystemModule tests now rely on `[ScriptGlobalOptionsIsolation]` + `[Parallelizable(ParallelScope.Self)]`; next step is to ensure every CLI/REPL consumer follows the same pattern so cross-fixture leaks remain impossible.
- 2025-11-27: Added `scripts/tests/NovaSharp.Parallel.runsettings` (`RunConfiguration.MaxCpuCount=0`, `NUnit.NumberOfTestWorkers=0`) and wired it into `scripts/build/build.ps1`, `scripts/build/build.sh`, and the contributor docs so every `dotnet test` invocation fans out across all cores by default. Verified the full interpreter suite via `dotnet test --project src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings` (3,155 tests). Next step: capture the CI runtime delta once the runsettings roll-out lands.
- 2025-11-27: Replaced the TCP-dependent `RemoteDebuggerTests` harness with an in-memory transport + harness factory so the suite no longer blocks on 2 s socket timeouts. Remote-debugger coverage now runs in ~1 s (`dotnet test ... --filter FullyQualifiedName~RemoteDebuggerTests`), and the full interpreter suite drops from 48 s to 9 s (`dotnet test ... --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings`). Next step: pilot a TUnit variant of the debugger fixture to evaluate additional async/test-runner wins before considering a broader migration.
- 2025-11-27: Profiling the new runsettings shows the remaining 48 s runtime is dominated by `RemoteDebuggerTests` (top cases such as `DeleteWatchCommandQueuesHardRefresh`, `AddWatchDuringActiveGetActionDoesNotSendHostBusy`, etc. each consume 2‑4 s thanks to real TCP sockets and 2 s timeouts). **Action:** (a) refactor the RemoteDebugger harness to use in-memory transports or deterministic fakes so the per-test timeout drops below 250 ms, and (b) prototype the same fixture under a TUnit proof-of-concept to measure whether TUnit’s async-friendly scheduling reduces latency compared to NUnit. Document the findings (pros/cons, perf deltas, migration effort) before considering a repo-wide move.
- 2025-11-27: Locked `dotnet test` to the Microsoft.Testing.Platform runner with `global.json` (`"test":{"runner":"Microsoft.Testing.Platform"}`) and updated the interpreter tests to set `<EnableNUnitRunner>true</EnableNUnitRunner>` + `<UseMicrosoftTestingPlatform>true</UseMicrosoftTestingPlatform>` (plus `Microsoft.Testing.Extensions.VSTestBridge`/`Microsoft.Testing.Platform.MSBuild`). Docs (`docs/Testing.md`, `docs/Contributing.md`, `docs/testing/real-world-scripts.md`, `docs/modernization/namespace-rebrand-plan.md`) now instruct contributors to run `dotnet test --project ... --settings ...`, and the Release suite clocks in at ~10.5 s on a 12-core workstation. Keep this bullet updated with the latest timing so we can spot any regression that pushes the suite back toward the 48 s baseline.
- 2025-11-27: Extended the TUnit pilot (`src/tests/NovaSharp.RemoteDebugger.Tests.TUnit`) beyond the handshake test by importing the shared harness helpers, adding `AddWatchQueuesHardRefreshAndCreatesDynamicExpression` + `WatchesEvaluateExpressionsAgainstScriptState`, and wiring CA1515 suppressions so the public harness types compile cleanly. `dotnet test --project src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj -c Release` now executes three scenarios in ~0.7 s (vs. ~1.7 s for the NUnit `RemoteDebuggerTests`). **Action:** keep migrating the slowest debugger behaviours (host-busy draining, pause/resume) into the TUnit suite, record the NUnit vs. TUnit timing deltas in PLAN.md, and document any friction (assertion ergonomics, analyzer noise) before proposing a repo-wide switch.
- **Full TUnit migration mandate:** with the Microsoft.Testing.Platform runner now required everywhere and the TUnit pilot demonstrating ~60 % runtime savings plus better async/parallel ergonomics, the long-term plan is to move every NovaSharp test project from NUnit to TUnit so we can lean on TUnit’s first-class async scheduling, fluent assertions, scoped fixtures, per-test cancellation, and deterministic diagnostics. This includes (1) finalizing the remote-debugger pilot findings, (2) adding a TUnit adapter project for interpreter/unit suites, (3) codifying assertion/safety patterns (timeouts, cancellation tokens, `ParallelGroup` annotations), and (4) updating docs/tooling/build scripts to treat TUnit as the canonical runner. Document each milestone here as we convert suites so contributors know when to expect the repo-wide cutover.
- **Next steps (2025-11-27 update):**
  1. Capture a fresh coverage snapshot using the Microsoft.Testing.Platform-aware script and update `docs/coverage/latest` + `docs/coverage/coverage-hotspots.md` so the 0 % placeholder numbers are replaced with the real 87 %+ baseline.
  2. Expand the TUnit suite with two more remote-debugger behaviours (pause/resume, host-busy draining) and log the timing delta for each scenario vs. NUnit.
  3. Sketch the interpreter-wide TUnit migration (adapter project, assertion patterns, gating) and add those milestones plus owners in a dedicated PLAN subsection.
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


