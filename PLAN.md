# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-11-25 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release -nologo` now emits ~1,285 warnings. NovaSharp.Hardwire is clean for CA1062/CA1305/CA1854/CA1822/CA1031/CA1725 after the latest guard/invariant/static sweep, so most remaining hits live in NovaSharp.VsCodeDebugger (CA1063, CA1051, CA1716, CA1012, CA1805, CA1865) and the NUnit projects (CA1812/CA1859 helpers). Runtime code itself only reports CA1024 on `Closure.GetUpvaluesCount/GetUpvalue*`.
- Tests: `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build` passes 2,779 tests in ~38 seconds; TAP IO/OS fixtures remain skipped.
- Coverage: `docs/coverage/latest/Summary.md` (2025-11-23 20:56 UTC) reports 87.8% line / 87.2% branch overall.
  - NovaSharp.Interpreter: 96.0% line / 92.9% branch (still below the ≥95% branch target needed before turning `COVERAGE_GATING_MODE` back to `enforce`).
  - NovaSharp.Cli: 83.5% line / 80.4% branch.
  - NovaSharp.Hardwire: 57.8% line / 51.2% branch.
  - NovaSharp.RemoteDebugger: 90.9% line / 79.8% branch.
  - NovaSharp.VsCodeDebugger: 1.9% line / 2.2% branch (no automated debugger tests yet).
- Coverage collateral: `docs/coverage/coverage-hotspots.md` still describes the 2025-11-22 run (2,753 tests) and no longer matches the latest artefacts.
- Audits: `documentation_audit.log`, `naming_audit.log`, and `spelling_audit.log` all report zero outstanding issues; CI runs the corresponding scripts.
- Regions: `rg -n '#region'` only finds references inside contributor docs (AGENTS.md and this file), so runtime/tooling/tests stay region-free.

## Baseline Controls (must stay green)
- Keep the documentation audit checked in. Re-run `python tools/DocumentationAudit/documentation_audit.py --write-log documentation_audit.log` whenever public/internal APIs change.
- `tools/NamingAudit` and `tools/SpellingAudit` are wired into CI; refresh `naming_audit.log` and `spelling_audit.log` locally before pushing code that touches identifiers or docs.
- Namespace/script hub governance already gates CI. Any new helper must live under `scripts/<area>/` with a README update plus PR-template acknowledgement.
- Re-run `rg -n '#region'` whenever generators or imports are added; update offending generators to strip regions automatically.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned with the helpers in `scripts/` so contributors have a single source of truth.

## Active Initiatives

### 1. Analyzer and warning debt
- Triaged Release build count: ~1,285 warnings (VS Code debugger and NUnit fixtures dominate). Runtime no longer raises CA1024 after converting the closure upvalue helpers to properties; remaining warnings live in RemoteDebugger + NUnit projects.
- Hardwire (2025-11-24): completed a context/generator sweep that adds explicit null-argument guards, invariant formatting, `TryGetValue` usage, and `static` helper conversions while deleting the legacy CodeDom region directives. NovaSharp.Hardwire no longer reports CA1062/CA1305/CA1854/CA1822/CA1031/CA1725 in Release builds, and generator discovery now skips placeholders (e.g., `NullGenerator`) so HardWireCommand + registry tests wire up the built-ins again. Follow-on work should tackle the remaining CA1859 spots (list/dictionary abstractions) and add analyzer coverage for any new generators. The project now builds with `<TreatWarningsAsErrors>true>` so keep it warning-free.
- VS Code debugger: `NovaSharpVsCodeDebugServer` now follows the full dispose pattern, exposes the `CurrentDebugger` property (replacing the obsolete `GetDebugger()` helper), and guards public entry points (clearing CA1063/CA1816/CA2213/CA1805/CA1062/CA1024). `ProtocolServer` and `DebugSession` now use protected constructors, eliminating the CA1012 warnings while keeping the public API surface abstract-only. CA1002 (List<T> exposure) is resolved by accepting `IEnumerable<T>` in the SDK response bodies, CA1310/CA1865/CA1866 are now clear after swapping everything to ordinal comparisons/char overloads (`Utilities`, `ProtocolServer`, `NovaSharpDebugSession`), the AsyncDebugger map lookups now use `TryGetValue` (no CA1854), socket/session catches target concrete exception types, and public entry points validate arguments (CA1031/CA1062 cleared). The CA1822/CA1805/CA1825 warnings are gone after the static sweep + field cleanup, and CA1716 is resolved via the `ProtocolEvent` rename plus the `ContinueExecution`/`StepOver` API updates. `TreatWarningsAsErrors` is now enabled inside `NovaSharp.VsCodeDebugger.csproj`, so the latest `dotnet build src/debuggers/NovaSharp.VsCode.Debugger/NovaSharp.VsCode.Debugger.csproj -c Release -nologo` run reports zero project-local warnings. Next steps: keep the zero-warning bar enforced and document the debugger-specific analyzer policies in `docs/Testing.md`.
- Remote debugger (2025-11-24): `RemoteDebuggerService`, `DebugServer`, `HttpServer`, `Utf8TcpServer`, `DebugWebHost`, and the shared network helpers now follow the full dispose pattern, validate external inputs, and run every formatter through `CultureInfo.InvariantCulture`. Static helper sweeps (including the VS-style resource loader) plus the `WatchElementNames` dictionary fix cleared the CA1063/CA1816/CA1305/CA1822/CA1859 backlog and dropped the project-specific warning count from 60 → 24. The latest pass eliminates the remaining analyzer hits (CA1031/CA1008/CA1056/CA1711/CA1815/CA1819) by renaming the blocking channel, converting debugger URLs to `Uri`, exposing HTTP payloads via `ReadOnlyMemory<byte>`, tightening catch blocks, and adding proper value semantics to `RemoteDebuggerOptions`. Release builds now emit zero warnings and `NovaSharp.RemoteDebugger.csproj` ships with `<TreatWarningsAsErrors>true>`, so the next steps pivot to adding targeted tests (watch/channel/equality regression coverage) and documenting the stricter analyzer contract in `docs/Testing.md`.
- Tests: convert nested fixtures to `static`/records, mark helper methods `static`, and adjust collection types to eliminate the CA1812 and CA1859 noise inside `NovaSharp.Interpreter.Tests`.
- Runtime: decide whether to expose `Closure.Context` as a property or suppress CA1024 with rationale.
- Naming consistency audit: sweep the runtime/debugger/tooling code for mixed-case identifiers (e.g., `Upvalues` vs. `UpValues`) and apply consistent PascalCase naming, updating public APIs/tests together and documenting notable renames.
- Foreach allocation audit: sweep runtime/tooling/tests to ensure every `foreach` uses value-based enumeration (e.g., `Span<T>`, pooled struct enumerators, or `List<T>.Enumerator`) whenever the source type supports it, so we avoid iterator allocations on hot paths; document guidance in `docs/Modernization.md`.
- Foreach allocation audit: sweep runtime/tooling/tests to ensure every `foreach` uses value-based enumeration (e.g., `Span<T>`, pooled struct enumerators, or `List<T>.Enumerator`) whenever the source type supports it, so we avoid iterator allocations on hot paths; document guidance in `docs/Modernization.md`.
- After each sweep rerun `dotnet build src/NovaSharp.sln -c Release -nologo | Select-String "warning"` and log counts per CA rule; once zeroed, flip `TreatWarningsAsErrors` in `Directory.Build.props` and document the policy in `docs/Testing.md`.

### 2. Coverage and test depth
- Refresh artefacts: rerun `./scripts/coverage/coverage.ps1` (Release, gate = enforce) so `docs/coverage/latest/*` and `docs/coverage/coverage-hotspots.md` describe the 2,779-test suite.
- Interpreter: add debugger/coroutine regression tests that drive pause-during-refresh, queued actions that drain after a pause, forced resume, and message decoration paths so branch coverage climbs from 92.9% to ≥95%.
- Tooling: extend NovaSharp.Cli tests beyond current command-unit coverage (record REPL transcripts and golden outputs) and build Hardwire generator tests that validate descriptor generation/error handling, targeting ≥80% line coverage for each project.
- Debuggers: add headless VS Code + Remote Debugger smoke tests (attach/resume/breakpoint/watch evaluation) to push NovaSharp.VsCodeDebugger line coverage past 50% and NovaSharp.RemoteDebugger branch coverage above 85%.
- Replace skipped IO/OS TAP suites with NUnit fixtures so Release runs exercise those semantics without Lua harnesses.
- Observability: enhance the GitHub coverage job to compare the new `Summary.json` against the last successful run and fail on ≥3 percentage point regressions; archive history under `artifacts/coverage/history`.

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
