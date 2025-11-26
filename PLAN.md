# Modern Testing & Coverage Plan

-## Repository Snapshot — 2025-11-26 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release -nologo` is warning-free; keep the zero-warning bar enforced by rerunning the build after every analyzer touchpoint.
- Tests: `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build` now executes 3,010 Release tests in ~48 seconds (TAP IO/OS fixtures remain skipped).
- Coverage: `docs/coverage/latest/Summary.md` (2025-11-26 12:35 UTC) reports 87.5 % line / 87.3 % branch / 89.7 % method overall.
  - NovaSharp.Interpreter: 96.69 % line / 94.35 % branch / 98.44 % method (branch coverage still <95 %, so `COVERAGE_GATING_MODE` stays in monitor mode).
  - NovaSharp.Cli: 83.4 % line / 80.5 % branch.
  - NovaSharp.Hardwire: 52.7 % line / 40.7 % branch.
  - NovaSharp.RemoteDebugger: 88.2 % line / 81.2 % branch.
  - NovaSharp.VsCodeDebugger: 1.8 % line / 2.1 % branch (no automated debugger tests yet).
- Coverage collateral: `docs/coverage/coverage-hotspots.md` now reflects the 2025-11-24 run (2,779 tests) and highlights the remaining interpreter branch debt.
- Audits: `documentation_audit.log` now lists 85 missing XML-doc entries (mostly `CliMessages`, interpreter instruction metadata, debugger protocol DTOs, and lexer tokens); prioritize filling those gaps before shipping any new public APIs. `spelling_audit.log` remains clean, and `naming_audit.log` mirrors the latest repo-wide sweep. CI runs all three scripts.
- Regions: `rg -n '#region'` only finds references inside contributor docs (AGENTS.md and this file), so runtime/tooling/tests stay region-free.

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
- Current status (2025-12-09): dotnet build src/NovaSharp.sln -c Release -nologo remains clean with <TreatWarningsAsErrors>true> enforced across the solution. Keep running that command after every analyzer/formatting sweep so the zero-warning bar never regresses. **Next steps:** document any new suppressions in PLAN.md/docs/Testing.md, keep the PR template’s analyzer checklist up to date, and treat fresh CA hits as stop-ship items.

### 2. Coverage and test depth
- Refresh artefacts: rerun ./scripts/coverage/coverage.ps1 (Release, gate = enforce) so docs/coverage/latest/* and docs/coverage/coverage-hotspots.md describe the latest test suite.
- Interpreter: add debugger/coroutine regression tests that drive pause-during-refresh, queued actions that drain after a pause, forced resume, and message decoration paths so branch coverage climbs from ~93 % to ≥95 %.
- Tooling: extend NovaSharp.Cli tests beyond current command-unit coverage (record REPL transcripts and golden outputs) and build Hardwire generator tests that validate descriptor generation/error handling, targeting ≥80 % line coverage for each project.
- Debuggers: add headless VS Code + Remote Debugger smoke tests (attach/resume/breakpoint/watch evaluation) to push NovaSharp.VsCodeDebugger line coverage past 50 % and NovaSharp.RemoteDebugger branch coverage above 85 %.
- Replace skipped IO/OS TAP suites with NUnit fixtures so Release runs exercise those semantics without Lua harnesses.
- Observability: enhance the GitHub coverage job to compare the new Summary.json against the last successful run and fail on ≥3 percentage point regressions; archive history under rtifacts/coverage/history.
- Remaining interpreter branch debt (updated 2025-11-26 21:45 UTC): Coroutine (~83.3 %), UnityAssetsScriptLoader (~86.8 %), PlatformAutoDetector (~87.5 %), Script (~83.8 %), UnaryOperatorExpression (~85 %), and any lingering Script/repl helpers not yet converted to guard-tested code paths. Prioritize these guard paths so branch coverage can cross ≥95 % and we can re-enable gating.
- Next steps: Close out the remaining hotspots (Coroutine, UnityAssetsScriptLoader, PlatformAutoDetector, Script, UnaryOperatorExpression, and the outstanding Script/REPL helpers) by adding guard-path unit tests so interpreter branch coverage can cross the ≥95 % enforcement bar.
- Coverage refresh (2025-11-26 12:35 UTC): Re-ran `pwsh ./scripts/coverage/coverage.ps1 -SkipBuild` after the OsTime/Coroutine sweeps (3,010 tests; NovaSharp.Interpreter now reports 96.69 % line / 94.35 % branch / 98.44 % method). Branch coverage is still shy of the ≥95 % gate, so the coverage job remains in monitor mode.

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
