# Modern Testing & Coverage Plan

-## Repository Snapshot — 2025-11-26 (UTC)
- Build: `dotnet build src/NovaSharp.sln -c Release -nologo` is warning-free; keep the zero-warning bar enforced by rerunning the build after every analyzer touchpoint.
- Tests: `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build` now executes 3,010 Release tests in ~48 seconds (TAP IO/OS fixtures remain skipped).
- Coverage: `docs/coverage/latest/Summary.md` (2025-11-27 11:26 UTC) reports 87.79 % line / 87.75 % branch / 89.79 % method overall.
  - NovaSharp.Interpreter: 96.99 % line / 94.76 % branch / 98.4 % method (branch coverage still <95 %, so `COVERAGE_GATING_MODE` stays in monitor mode but the new tests moved the needle).
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
- Current status (2025-12-09): dotnet build src/NovaSharp.sln -c Release -nologo remains clean with <TreatWarningsAsErrors>true> enforced across the solution. **Next steps:** document any new suppressions in PLAN.md/docs/Testing.md, keep the PR template’s analyzer checklist up to date, and treat fresh CA hits as stop-ship items.
- Active follow-up: audit the remaining targeted suppressions (e.g., CA1051 field fixtures, IDE1006 Lua port intentional cases, module-level CA1515) and convert any that are no longer needed into real code fixes so the analyzer baseline remains suppression-light.
- 2025-11-27 checkpoint: eliminated the CA1024 suppression in `VtUserDataPropertiesTests` by exposing the write-only property value through a dedicated read-only property (`WoIntProp2Value`) so tests no longer need helper methods that trip analyzer rules. Favor property-based helpers for future fixtures to avoid reintroducing the warning.
- 2025-11-27 checkpoint: retired the unused `EnumOverloadsTestClass.Get/GetF` helpers in `UserDataEnumsTests`, replaced them with read-only properties, and purged the corresponding entries from `_Hardwired.cs` so the CA1024 suppressions could be removed without affecting hardwire coverage.
- CA1051 follow-up: the previously flagged test fixtures (`TestRunner` counters plus descriptor/userdata helpers) are clean, but keep auditing new test infrastructure so any future public-field regressions are either converted to properties or annotated with tightly scoped suppressions.
- 2025-11-28 checkpoint: removed the CA1051 suppression in `FieldMemberDescriptorTests` by narrowing the `SampleFields` helpers to `internal` instance fields and updating their metadata binding flags so analyzer guidance is satisfied without sacrificing coverage.
- 2025-11-28 checkpoint: removed the remaining CA1308 suppressions in `UserDataMethodsTests` and `VtUserDataMethodsTests` by routing their lowercase conversions through `InvariantString.ToLowerInvariantIfNeeded`, keeping globalization analyzers green without sacrificing the Lua interop scenarios.
- 2025-11-28 checkpoint: retired the CA1852 suppressions in `DescriptorHelpersTests` by converting the `MemberVisibilityFixtures`/`PropertyFixtures` helpers to abstract classes (so their protected members stay valid) and dropping the unused instantiations.
- CA1515 plan: fixture catalog automation (MSBuild, pre-commit, CI) now ensures every `[TestFixture]` has a `typeof(...)` reference in `FixtureCatalogGenerated.cs`. Next steps are (a) convert non-reflection fixtures/helpers to `internal` where possible and (b) scope the remaining CA1515 suppressions to the handful of helper types that must stay public for NUnit/BenchmarkDotNet.
- BenchmarkDotNet benchmark classes (`RuntimeBenchmarks`, `ScriptLoadingBenchmarks`, `LuaPerformanceBenchmarks`) must remain `public` and unsealed for discovery; keep the targeted CA1515 suppressions plus the AGENTS.md guidance so new benchmarks follow the same pattern.

### 2. Coverage and test depth
- Refresh artefacts: rerun ./scripts/coverage/coverage.ps1 (Release, gate = enforce) so docs/coverage/latest/* and docs/coverage/coverage-hotspots.md describe the latest test suite.
- Interpreter: add debugger/coroutine regression tests that drive pause-during-refresh, queued actions that drain after a pause, forced resume, and message decoration paths so branch coverage climbs from ~93 % to ≥95 %.
- Tooling: extend NovaSharp.Cli tests beyond current command-unit coverage (record REPL transcripts and golden outputs) and build Hardwire generator tests that validate descriptor generation/error handling, targeting ≥80 % line coverage for each project.
- Debuggers: add headless VS Code + Remote Debugger smoke tests (attach/resume/breakpoint/watch evaluation) to push NovaSharp.VsCodeDebugger line coverage past 50 % and NovaSharp.RemoteDebugger branch coverage above 85 %.
- Replace skipped IO/OS TAP suites with NUnit fixtures so Release runs exercise those semantics without Lua harnesses.
- Observability: enhance the GitHub coverage job to compare the new Summary.json against the last successful run and fail on ≥3 percentage point regressions; archive history under rtifacts/coverage/history.
- 2025-11-27 checkpoint: added a UnityAssetsScriptLoader default-constructor regression test so the NovaSharp/Scripts fallback path executes under test, nudging Unity loader branch coverage toward the ≥95 % goal. Re-run coverage after the next interpreter suite to quantify the lift.
- 2025-11-27 checkpoint: added `UnityAssetsScriptLoaderTests.ReflectionConstructorUnwrapsTargetInvocationExceptions` so the loader’s reflection path now hits the inner-exception branch (e.g., `SecurityException` wrapped in `TargetInvocationException`), covering another UnityAssetsScriptLoader hotspot.
- 2025-11-27 checkpoint: expanded `ProcessorDebuggerTests` with a CLR-throwing watch expression so `RefreshDebuggerWatch` now covers the non-interpreter exception path (the `InvalidOperationException` guard). This chips away at the remaining `Execution.VM.Processor` branch debt highlighted in `docs/coverage/coverage-hotspots.md`.
- 2025-11-27 checkpoint: added dynamic-expression regression tests (`CreateDynamicExpressionRegistersSourceAndEvaluates` / `…RemovesSourceOnFailure`) so the `Script.CreateDynamicExpression` success and failure branches (source registration plus rollback) are now covered, addressing part of the Script-class branch gap called out in the hotspot report.
- 2025-11-27 checkpoint: extended `CoroutineTests` with cross-script guard coverage (`ResumeWithContextFromDifferentScriptThrows`, `ResumeWithArgumentsFromDifferentScriptThrows`), the `MarkClrCallbackAsDead` invalid-state guard, and a suspended-stack trace probe so `Coroutine` branch coverage moves off the ~83 % plateau.
- 2025-11-27 checkpoint: added a cached-detection regression to `PlatformAutoDetectorTests` so the `AutoDetectionsDone` early-return branch is exercised (unity flags stay true when cached), covering part of the PlatformAutoDetector gap.
- 2025-11-27 checkpoint: added a `TargetInvocationException` harness to `UnityAssetsScriptLoaderTests` so the reflection path’s inner-exception handling is covered (`ReflectionConstructorUnwrapsTargetInvocationExceptions`), addressing one of the remaining UnityAssetsScriptLoader branches.
- 2025-11-27 checkpoint: added `UnaryOperatorExpressionTests.EvalLengthThrowsWhenOperandHasNoLength` so the Lua `#` operator’s error branch now runs under NUnit, shrinking the UnaryOperatorExpression gap that kept branch coverage near 85 %.
- 2025-11-27 checkpoint: expanded `ScriptCallTests` with additional object-call/coroutine coverage (`CallObjectOverloadInvokesClosureAndConvertsArguments`, `CallObjectOverloadInvokesDelegateCallback`, the non-callable/null/foreign-script guards, `CreateCoroutineObjectOverloadUsesClosure`, `CreateCoroutineObjectOverloadSupportsDelegates`, and the corresponding cross-script rejection tests) so the `Script.Call(object …)`/`CreateCoroutine(object …)` wrappers are now exercised across Lua closures, CLR delegates, and failure paths, further reducing the Script-class branch debt.
- 2025-11-27 checkpoint: extended `CoroutineLifecycleTests` with `CloseSuspendedCoroutineReturnsTrue` / `CloseSuspendedCoroutinePropagatesErrors` and the new not-started/dead follow-ups so `Coroutine.Close()` now has coverage for the success, failure, and dead-state tuple paths that unwind pending `<close>` variables.
- 2025-11-27 checkpoint: restored all reverted interpreter guard-path tests (Script.Call/CreateCoroutine overloads, dynamic expressions, coroutine cross-script checks/close tuples, debugger watch error handling, UnityAssetsScriptLoader reflection unwrap, PlatformAuto Unity probe, UserData CA1024 fixes, unary `#` failure path) and re-ran `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --no-build` (3,125 tests) to verify the suite is green again.
- 2025-11-27 checkpoint: Re-ran `./scripts/coverage/coverage.ps1` so `docs/coverage/latest/*` now captures the restored suite (overall 87.79 % line / 87.75 % branch / 89.79 % method; NovaSharp.Interpreter 96.99 % / 94.76 % / 98.4 %).
- 2025-11-27 checkpoint: Pinned the Unity auto-detection fixtures to an explicit NUnit order so the “no Unity assemblies” regression executes before the Unity probe tests; this keeps the new PlatformAutoDetector coverage without cross-test contamination.
- 2025-11-28 checkpoint: added an explicit null-stream guard to `Script.LoadStream` plus a regression test (`ScriptLoadTests.LoadStreamThrowsWhenStreamNull`) so the load path fails fast with `ArgumentNullException` and the guard branch stays covered.
- 2025-11-28 checkpoint: added a matching null-code guard to `Script.LoadString` (with `ScriptLoadTests.LoadStringThrowsWhenCodeNull`) to tighten the string-based loaders and cover the remaining Script-class guard path.
- 2025-11-28 checkpoint: added a null-filename guard to `Script.LoadFile` (covered by `ScriptLoadTests.LoadFileThrowsWhenFilenameNull`) so file-based loads fail fast before hitting the script loader and keep the guard branch under test.
- 2025-11-28 checkpoint: hardened `PlatformAutoDetector` so auto-detection always marks `AutoDetectionsDone` and added `PlatformAutoDetectorTests.AutoDetectionDoesNothingWhenAlreadyInitialized` to cover the short-circuit branch that previously went untested.
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


