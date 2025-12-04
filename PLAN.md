# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-04 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release` (2025-12-04 06:00 UTC) finishes with zero warnings while `<TreatWarningsAsErrors>true>` remains enforced across the solution.
- Tests: The latest Release coverage sweep (`pwsh ./scripts/coverage/coverage.ps1`, 2025-12-04 06:03 UTC) executed **2,790** interpreter tests via Microsoft.Testing.Platform. Two pre-existing failures remain unrelated to interpreter logic (bit32 rotate test, os.getenv TAP test).
- Coverage: Interpreter coverage sits at **95.0 % line / 91.93 % branch / 96.88 % method**. Branch coverage remains below the 95% target for enabling `COVERAGE_GATING_MODE=enforce`.
  - NovaSharp.Interpreter: 95.0 % line / 91.93 % branch / 96.88 % method.
  - NovaSharp.Cli: 78.82 % line / 71.69 % branch / 83.72 % method.
  - NovaSharp.Hardwire: 56.12 % line / 46.58 % branch / 67.70 % method.
  - NovaSharp.RemoteDebugger: 0.19 % line / 0 % branch / 1.42 % method (tests not running in current sweep).
  - NovaSharp.VsCodeDebugger: 0 % line / 0 % branch / 0 % method.
- Coverage scripts: Fixed `scripts/coverage/coverage.ps1` and `scripts/coverage/coverage.sh` to use `dotnet run` instead of `dotnet test --project` for Microsoft.Testing.Platform compatibility (global.json sets `"test.runner": "Microsoft.Testing.Platform"`). Also made scripts tolerate test failures when collecting coverage data.
- Audits: `documentation_audit.log`, `naming_audit.log`, and `spelling_audit.log` are green (0 missing XML docs, no naming/spelling findings). Re-run the trio whenever APIs or text-heavy docs change.
- Regions: `rg -n '#region'` only returns AGENTS.md/PLAN.md, so runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Keep the documentation audit checked in. Re-run `python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log` whenever public/internal APIs change.
- `tools/NamingAudit` and `tools/SpellingAudit` are wired into CI; refresh `naming_audit.log` and `spelling_audit.log` locally before pushing code that touches identifiers or docs.
- Run the lint guards (`python scripts/lint/check-platform-testhooks.py` / `scripts/ci/check-platform-testhooks.sh` and `python scripts/lint/check-console-capture-semaphore.py` / `scripts/ci/check-console-capture-semaphore.sh`) before pushing; the CI lint job invokes the same scripts and will fail if detector/semaphore scopes are bypassed.
- Namespace/script hub governance already gates CI. Any new helper must live under `scripts/<area>/` with a README update plus PR-template acknowledgement.
- Re-run `rg -n '#region'` whenever generators or imports are added; update offending generators to strip regions automatically.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned with the helpers in `scripts/` so contributors have a single source of truth.

## Active Initiatives

### CSharpier formatting backlog
- Current status: the entire repo (runtime/tooling/tests) has been reflowed with `dotnet csharpier`, and CI’s lint job now runs `dotnet csharpier check .` via `scripts/ci/check-csharpier.sh`. Treat any lingering `dotnet format` complaints as configuration bugs and update `.editorconfig`/workflow settings so they no longer contradict CSharpier output. **Next steps:** align the remaining formatter configuration (dotnet-format, IDE analyzers) with CSharpier and document the policy in `docs/Testing.md`/contributor guides once the tooling is in sync.

### Test cleanup helpers & using-pattern enforcement (complete)
- Status: **Complete.** All test cleanup helpers are in place under `TestInfrastructure/Scopes/` and lint guards enforce their usage.
- Implemented scopes: `TempFileScope`, `TempDirectoryScope`, `UserDataRegistrationScope`, `UserDataIsolationScope`, `PlatformDetectorScope`, `ScriptGlobalOptionsScope`, `ScriptDefaultOptionsScope`, `ScriptCustomConvertersScope`, `EnvironmentVariableScope`, `SemaphoreSlimScope`, `ConsoleCaptureCoordinator`, and others.
- Lint enforcement: `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`, `check-console-capture-semaphore.py` — all pass.
- Maintenance guidelines:
  1. New tests should use the existing scope helpers rather than manual `try`/`finally` blocks.
  2. The lint scripts will flag violations automatically in CI.
  3. When adding new cleanup patterns, create a corresponding scope helper in `TestInfrastructure/Scopes/`.

### High priority — Codebase organization & namespace hygiene
- Current state: runtime/tooling/test projects largely mirror the historical MoonSharp layout, leaving most interpreter tests under monolithic buckets such as `Units/`, `TestMore/`, or `TUnit/VM/` with little discoverability. Production code follows the same pattern—`NovaSharp.Interpreter` is a single assembly containing interpreter, modules, platforms, IO helpers, and tooling adapters.
- Problem: Contributors struggle to locate feature-specific code/tests, and the wide namespaces make it hard to reason about ownership or layering (e.g., Lua VM vs. tooling vs. debugger). PLAN.md now tracks the reorganization as a high-priority initiative so we treat it as a first-class modernization step alongside TUnit.
- Objectives:
  1. Propose a refined solution layout that splits runtime/tooling/test assets into feature-scoped projects (e.g., `NovaSharp.Interpreter.Core`, `NovaSharp.Interpreter.IO`, `NovaSharp.Tooling.Cli`, `NovaSharp.Tests.Interpreter.Runtime`, `NovaSharp.Tests.Interpreter.Modules`, etc.) while keeping build scripts and packaging intact.
  2. Restructure the interpreter test tree by domain rather than framework (e.g., `Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`, `Debugger/Remote`, `Integration/EndToEnd`). Update namespaces to match the new folder structure and keep fixture catalog generation aligned.
  3. Document the new layout in PLAN.md + `docs/Testing.md`, and add guardrails (analyzers or CI checks) so new code/tests land in the correct folders with consistent namespaces.
  4. Ensure the reorganization is incremental but tracked: migrate one subsystem at a time, update project references, and verify coverage after each move so we do not destabilize the main branch.

### 1. Analyzer and warning debt
- Current status: `dotnet build src/NovaSharp.sln -c Release` remains clean with `<TreatWarningsAsErrors>true>` enforced across the solution. **Next steps:** document any new suppressions in PLAN.md/docs/Testing.md, keep the PR template's analyzer checklist up to date, and treat fresh CA hits as stop-ship items.
- Active follow-up: audit the remaining targeted suppressions (e.g., CA1051 field fixtures, IDE1006 Lua port intentional cases, module-level CA1515) and convert any that are no longer needed into real code fixes so the analyzer baseline remains suppression-light.
- CA1051 follow-up: the previously flagged test fixtures (`TestRunner` counters plus descriptor/userdata helpers) are clean, but keep auditing new test infrastructure so any future public-field regressions are either converted to properties or annotated with tightly scoped suppressions.
- CA1515 plan: fixture catalog automation (MSBuild, pre-commit, CI) now ensures every `[TestFixture]` has a `typeof(...)` reference in `FixtureCatalogGenerated.cs`. Next steps are (a) convert non-reflection fixtures/helpers to `internal` where possible and (b) scope the remaining CA1515 suppressions to the handful of helper types that must stay public for NUnit/BenchmarkDotNet.
- BenchmarkDotNet benchmark classes (`RuntimeBenchmarks`, `ScriptLoadingBenchmarks`, `LuaPerformanceBenchmarks`) must remain `public` and unsealed for discovery; keep the targeted CA1515 suppressions plus the AGENTS.md guidance so new benchmarks follow the same pattern.
- Policy reminder: AGENTS.md forbids nullable reference-type syntax (no `#nullable`, `string?`, `?.` targeting reference members, or `null!`). Keep running `artifacts/NrtScanner` (or a simple `rg`) before opening analyzer-heavy PRs so the ban stays enforced and CA1805 continues to pass without suppressions.

### 2. Coverage and test depth
- Current coverage (2025-12-03): Interpreter at **95.22 % line / 92.21 % branch**, RemoteDebugger at **76.73 % line / 66.25 % branch**, VsCodeDebugger at **40.57 % line / 38.54 % branch**.
- Priority targets:
  1. **Interpreter branch >= 95 %**: Add tests for remaining hotspots (Coroutine, UnityAssetsScriptLoader, PlatformAutoDetector, Script, UnaryOperatorExpression) to enable `COVERAGE_GATING_MODE=enforce`.
  2. **VsCodeDebugger >= 50 % line**: Build headless DAP smoke tests (launch, attach, breakpoints, watches) without requiring VS Code.
  3. **RemoteDebugger >= 85 % branch**: Add smoke tests for HTTP attach, TCP streaming, queue draining, and error signaling.
  4. **CLI/Hardwire >= 80 % line**: Extend command-unit coverage with REPL transcripts and golden outputs.
- Observability: enhance the GitHub coverage job to compare Summary.json against the last successful run and fail on >= 3 percentage point regressions.

### Coverage orchestration simplification
- Problem: coverage helpers currently emit per-suite artefacts (interpreter vs. remote debugger vs. CLI) and require reviewers to mentally merge multiple `Summary.*` outputs. We need a single coverage report that aggregates every Release test run so dashboards, docs, and gating logic all consume the same unified result.
- Objectives:
  1. Inventory every command that produces coverage today (`scripts/coverage/coverage.ps1`, remote-debugger smoke jobs, CLI automation) and document which coverlet/files ReportGenerator merges (or fails to merge).
  2. Update the coverage script(s) to drive all suites in one invocation, merge their `.json`/`.xml` payloads (via `coverlet merge` or ReportGenerator multi-input), and emit a single `docs/coverage/latest` snapshot that includes per-assembly breakdowns plus an overall summary.
  3. Update `docs/Testing.md`, CI workflows, and gating logic so only the aggregated artefact is referenced; fail builds when the unified summary regresses past agreed thresholds.
- Status: design + scripting pending. Capture candidate approaches (coverlet merge vs. ReportGenerator multi-input) and validate that the combined pipeline still surfaces individual-module deltas even though we publish one top-level coverage summary.

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
- Contributor ignore lists (`.claudeignore`, `.codexignore`) are in place; keep them updated when new artefact directories are introduced.

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
- Consolidate `AGENTS.md`, `CLAUDE.md`, and `.github/copilot-instructions.md` into a unified agent instruction surface once the team agrees on the canonical format; until then, keep all three files in sync when policies change.

Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`, and update it whenever coverage artefacts, warning counts, or milestone statuses change.
