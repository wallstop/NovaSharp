# Modern Testing & Coverage Plan

## Modernization Baseline (Nov 2025)
- ✅ Core runtime, debuggers, hardwire tooling, CLI shell, benchmarks, and automated tests now target **`netstandard2.1`** (runtime) and **`net8.0`** (tooling).
- ✅ Legacy `.NET 3.5/4.x`, Portable Class Library, Windows 8/Phone, Silverlight, Unity, Xamarin, and NuGet test harness projects removed from the tree.
- ✅ Solution simplified to `src/NovaSharp.sln`; obsolete `NovaSharp_*` variants deleted.
- ✅ Benchmark infrastructure rebuilt on BenchmarkDotNet with shared `PerformanceReportWriter` writing OS-specific results to `docs/Performance.md`; benchmarks remain local-only by design.
- ✅ Documentation refreshed: `docs/Performance.md`, `docs/Testing.md`, `docs/Modernization.md`, and README now describe the modern stack and link together.
- ✅ Consolidated interpreter tests under `src/tests/NovaSharp.Interpreter.Tests`; retired the bespoke DotNetCoreTestRunner harness.

- ## Testing Health Snapshot
- ✅ Runtime semantics target **Lua 5.4.8**; see `docs/LuaCompatibility.md` for tracked deltas and follow-ups.
- Runner: `src/tests/NovaSharp.Interpreter.Tests` (`net8.0`) executes **1095 tests** (Lua TAP suites `TestMore_308_io` and `TestMore_309_os` stay disabled unless explicitly enabled).
- Command: `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --logger "trx;LogFileName=NovaSharpTests.trx"` is the canonical entry point; the bespoke TAP harness has been removed.
- Coverage: `coverage.ps1` (coverlet.console + `dotnet test` + reportgenerator) records **74.9 % line / 75.5 % branch / 78.5 % method** against Release binaries (NovaSharp.Interpreter at 87.1 % line); raw artefacts + Markdown/JSON summaries live in `artifacts/coverage`, HTML in `docs/coverage/latest`.
- Suite composition:
  - **Lua TAP fixtures** validating language semantics and standard library parity.
  - **End-to-end NUnit scenarios** covering userdata interop, debugger attach, coroutine pipelines, serialization, JSON.
  - **Unit tests** for low-level primitives (virtual machine stacks, binary dump/load, interop policies).
- CI (`.github/workflows/tests.yml`):
  - Restores and builds `src/NovaSharp.sln` in Release.
  - Executes `dotnet test` against `NovaSharp.Interpreter.Tests` and uploads `artifacts/test-results` (TRX + attachments).
  - The `code-coverage` job runs `./coverage.ps1`, appends the Markdown summary to the job output, posts a PR comment (line/branch/method), and uploads both raw coverage data and a zipped HTML dashboard.
- Benchmarks (`NovaSharp.Benchmarks`, `NovaSharp.Comparison`) compile but never run in CI; they must be invoked manually to update `docs/Performance.md`.

## Coverage Strengths
- Lua semantics, coroutine scheduling, binary dump/load, JSON, interop policy, and hardwire regeneration paths have broad regression protection.
- TAP fixtures catch regressions against upstream Lua behaviour.
- Benchmark baselines establish performance + allocation expectations for script loading and runtime scenarios.

## Gaps & Risks
- **Debugger automation**: No integration tests for VS Code or remote debugger protocols.
- **Tooling**: CLI shell and remaining tooling projects lack regression coverage after modernization.
- **Cross-platform**: CI only runs on Linux; Windows/macOS coverage still manual.
- **Observability**: Coverage summaries now post automatically on PRs, but there is still no gating/threshold enforcement or historical trend tracking.
- **Skip-list debt**: IO/OS TAP suites remain permanently skipped without compensating coverage.
- **Unity onboarding**: Basic packaging workflow is documented in `docs/UnityIntegration.md`; still need automated packaging and refreshed Unity samples.
- **Enum allocation audit**: .NET `Enum.HasFlags` and default `ToString()` allocate; add follow-up to port the no-alloc helpers from DxMessaging/UnityHelpers (bitmask tests + generated name maps) so enum-heavy interpreter paths stay allocation-free while keeping user-friendly names.
- **Warning hygiene**: Fix all existing compiler/analyzer warnings and flip `TreatWarningsAsErrors` on across every project so Release builds (and coverage.ps1) stay green without manual suppressions.
- **Runtime security modes**: Design configurable Lua sandbox profiles that disable or stub risky primitives (file IO, env vars, OS commands, reflection hooks) and provide host-controlled policies for multi-tenant deployments.
- **Resource/QoS sandboxing**: Add configurable ceilings for time, memory, recursion depth, table growth, and coroutine counts so runaway mods can’t stall the game loop; expose watchdog hooks for graceful termination.
- **Deterministic execution profile**: Define and test a “deterministic mode” (stable PRNG seeding, locale-neutral formatting, deterministic iteration where Lua allows) to support lockstep networking and replays.
- **Mod isolation lifecycle**: Provide per-mod state containers, controlled export/import mechanics, and load/reload/unload events to prevent mods from trampling each other or leaking state across sessions.
- **Packaging & deployment pipeline**: Document and automate mod packaging (versioning, signatures, compression) and produce redistributable runtime bundles/NuGet packages plus sample integration stubs.
- **Observability & diagnostics**: Build structured logging, per-mod profiling (time/allocations), execution tracing, and crash report surfacing so hosts can debug and monitor mods in production.
- **Host API surface review**: Specify a stable C#/Lua interop contract (events, async bridging, error propagation), include analyzers/templates, and generate docs to keep exposed APIs safe and versionable.
- **Lua version parity audit**: Extend spec harness/tests to cover supported Lua versions (5.1–5.4) including bit32/utf8/goto/coroutine differences, and document any intentional deviations for modders.
- **RNG parity & quality**: Match Lua 5.4’s PCG32 sequence for `math.random`/`math.randomseed`, and expose optional higher-quality PRNGs (xoroshiro/xoshiro) without breaking deterministic mods.
- **Math & locale neutrality**: Audit `math`/`string` formatting vs Lua spec (handling of NaN, infinity, modulo, locale) and enforce invariant culture so mod behavior is stable across hosts.
- **Integer & bitwise semantics**: Validate 64-bit integer operations, overflow, and bit shifts against Lua’s reference behavior; align or document any differences.
- **Coroutine & metamethod conformance**: Expand spec tests for nested yields, pcall/xpcall interactions, and metamethod trigger order to mirror the Lua VM.
- **Table iteration determinism**: Confirm insertion/iteration behavior matches Lua’s expectations, avoiding .NET dictionary ordering surprises for modded state.
- **GC behavior parity**: Document .NET GC differences vs Lua’s incremental collector and evaluate exposing Lua-style pause/step knobs so mods relying on GC tuning remain predictable.
- **Debug hook fidelity**: Ensure `debug.sethook`, stack traces, and traceback formatting match Lua’s behavior so profiling tools and mod debuggers work transparently.
- **Bytecode handling policy**: Decide whether NovaSharp loads Lua bytecode; if supported, implement version checks and sandbox hardening, otherwise document the text-only stance.
- **Module searchers & paths**: Align `require`/`package.searchpath` resolution (case sensitivity, separators, LUA_PATH inheritance) with stock Lua for cross-platform mod portability.
- **Numeric mode consistency**: Audit integer/float handling (wraparound, `%` on negatives, `math.tointeger`) to guarantee Lua 5.3+/5.4 semantics.
- **UTF-8 library coverage**: Verify `utf8` API parity (surrogate handling, error returns) with Lua’s reference implementation.
- **OS/IO abstraction parity**: Normalize newline, path separator, and timezone behaviors across platforms so mods behave consistently.
- **Threading model guidance**: Define main-thread-only rules, provide safe queues or schedulers if background jobs are needed, and guard against multi-thread misuse.
- **Hot-reload resilience**: Add lifecycle hooks and cleanup strategies so mod reload/unload frees resources (events, timers, coroutines) without leaking or crashing.
- **Error surfacing parity**: Match Lua’s error objects and stack level reporting, ensuring `pcall`/`xpcall` return contracts remain intact despite C# exceptions.
- **Floating-point determinism**: Evaluate CPU/runtime variability; consider fixed-point or deterministic options for lockstep networking scenarios.
- **Sandbox escape audit**: Catalogue interop/reflection entry points that could bypass security modes and add mitigations or documentation.
- **Mod SDK & docs**: Produce modder-facing SDK artifacts (API docs, templates, analyzers) and automation for keeping documentation in sync with runtime releases.

## Coverage Burn-down Checkpoint (Latest)
• Progress
  - ✅ Added Lua 5.4 string-library spec coverage with manual (§6.4) citations (`string.byte` clamping, plain `string.find`, capture returns, `%q` formatting, zero-count `string.rep`), keeping interpreter semantics aligned with the canonical interpreter; Release `dotnet test` now passes 1 481 cases post-suite expansion.
  - ✅ Strengthened `MethodMemberDescriptor` unit coverage (default access fallback, AOT downgrade path, byref/out tuple returns, pointer/generic guardrails, visibility forcing) to push the class toward the ≥95 % line target while preserving green Release runs.
  - ⚠️ `./coverage.ps1` currently aborts because the Release build surfaces analyzer warnings (e.g., CA1834, CA1305) as errors; schedule a rerun after taming the warnings so coverage exports reflect the new tests.
  - Captured refreshed Release baseline via `./coverage.ps1` (2025-11-12 18:55 UTC), recording NovaSharp.Interpreter at **86.1 % line / 82.3 % branch** coverage; regenerated HTML/Markdown reports and refreshed both `docs/coverage/latest` and `docs/coverage/coverage-hotspots.md`.
  - ✅ Added focused NUnit suites for `FieldMemberDescriptor`, `StandardEnumUserDataDescriptor`, `ExprListExpression`, `ScriptExecutionContext`, `FastStack<T>`, `StringConversions`, and `RefIdObject`, clearing the former red-list and lifting each class above 95 % line coverage (with `FastStack<T>` now at 100 % after exercising explicit-interface members).
  - ✅ Synced this PLAN checkpoint with the latest coverage burn-down notes so contributors land on the 86 % baseline instead of the outdated 68 % snapshot.
  - ✅ Landed `SourceRefTests` to cover FormatLocation, range heuristics, and snippet extraction alongside `ExitCommandTests` that exercise help text and exit signaling; CLI ExitCommand line coverage now 100 % and SourceRef climbs to 81 %.
  - ✅ Added `LoadModuleTests` (require caching, load reader guards, safe environment paths), `SyntaxErrorExceptionTests`, `ParameterDescriptorTests`, `AutoDescribingUserDataDescriptorTests`, and expanded `EventMemberDescriptorTests` so LoadModule now sits at 71 % line coverage, parameter descriptors at 70.8 %, event descriptors at 53 %, and the Release suite reaches 1 081 tests with interpreter totals past 81 % line.
  - Restored stream-file userdata parity by fixing Lua-style close tuples (nil, message, -1) and normalising buffered seek behaviour; StreamFileUserDataBase NUnit regressions now pass.
  - ✅ Rebuilt `read('*n')` numeric parsing to track logical stream positions, honour leading `+/-`, decimals, and exponents without corrupting buffered reads; `read` line/block/all mode combos now succeed and keep subsequent reads aligned.
  - ✅ Hardened IO coverage with exponent/whitespace numeric reads, mixed newline handling, and EOF detection fixes that rely on a tracked logical position rather than StreamReader buffering.
  - ✅ Exercised `dynamic.eval`/`dynamic.prepare` with happy paths, invalid userdata, and syntax errors; fixed `Script.CreateDynamicExpression` so syntax failures now surface as Lua errors instead of `ArgumentOutOfRangeException`.
  - ✅ Added fast-path tests for `io.open`, `io.input`/`io.output`, and `io.tmpfile`, covering missing file tuples, invalid modes, default stream reassignment, and temporary file writes.
  - ✅ Brought `pcall`/`xpcall` coverage up with CLR callbacks, handler decoration, and guardrail assertions; observed that CLR yields currently return success, which needs deeper spec validation.
  - Added pcall-guarded coverage for stream flush/seek/setvbuf failures (including `io.flush` default routing) and wrapped the underlying exceptions in `ScriptRuntimeException`, exercising the new fallbacks.
  - Added BinaryEncoding edge-path coverage: verified null/invalid source buffers, undersized destinations, and negative max-count guards to enforce extreme-path behaviour.
  - Removed legacy tree; modernization docs/PLAN cleaned up.
  - Tightened coverage tooling (coverage.ps1 now quiet locally; opt-in via NOVASHARP_COVERAGE_SUMMARY).
  - Added coverage suites for DebugModule, OsSystemModule, and IO userdata; interpreter coverage ≈77%.
  - Updated DebugModule to exit on scripted return and added tests for the console loop.

• Preferences / Constraints
  - Keep local CLI output concise; use env vars for full summaries.
  - Follow PLAN.md milestones; focus on coverage burn-down.
  - Respect existing Lua-facing behavior and build on NUnit + Script.DoString.

• Next Work Items
  1. Run the Lua `<close>` TAP suites (or craft NUnit mirrors) to exercise error unwinds, `goto`/`break` exits, and nested closures; extend coverage where the new runtime paths still lack assertions.
  2. Design a versioned compatibility mode so scripts can opt into Lua 5.5, 5.4, 5.3, or 5.2 semantics while defaulting to latest; capture spec deltas and config surface.
  3. Audit naming consistency across the runtime (methods like `Emit_Op`, members such as `i_variable`, and other Hungarian-style locals), then propose a final alignment plan with tooling impacts documented.
  4. Tackle remaining hotspots listed in docs/coverage/coverage-hotspots.md (e.g., HardwiredDescriptors.DefaultValue,
     ReflectionSpecialName, ErrorHandlingModule, Io/Json/Load modules).
  5. Re-run ./coverage.ps1 after each batch and update PLAN/doc checkpoints accordingly; raise gate once suite sustain >90 %.
  6. Unity integration milestone: package NovaSharp as a Unity module (UPM and embedded), add example scenes/mod templates, document IL2CPP/AOT caveats, and verify the mod workflow in CI.
  7. Performance & allocation milestone: stand up BenchmarkDotNet regression runs, track allocations via dotnet-trace/dotMemory, enforce a perf budget in CI, and prototype Span<T>/pooling on hot paths.
  8. Roslyn code generation milestone: design the source generator/analyzer surface for NovaSharp descriptors and mod code, build a prototype, and document usage patterns.
  9. Documentation & samples milestone: set up DocFX (or equivalent), publish compatibility matrices + tutorials, refresh Unity/modding guides, and wire doc generation into CI.
  10. Compatibility corpus milestone: expand CI to run Lua TAP suites, real-world script corpora, and community mod packs across Windows/macOS/Linux + Unity editor builds.
  11. Style/quality automation milestone: enable analyzers/stylecop/formatter rules (naming, spacing, `_camelCase`), enforce them in CI, and close out the remaining naming debt.
  12. Research high-performance string/I/O libraries compatible with Unity (e.g., CySharp.ZString) and scope integration options to reduce allocations in parser, IO, and interop paths; document findings and prototype hotspots.
  13. Investigate `pcall` behaviour when CLR callbacks yield: confirm Lua parity, add repro harness, and patch runtime if NovaSharp should surface an error instead of returning success.
  14. Review CA1711 guidance for `SymbolRefAttributes` (rename vs. suppression) once `<close>` work stabilises, capturing the outcome in PLAN + analyzer settings.

## MoonSharp ➜ NovaSharp Finalization (New)
- ✅ Scope audit: `rg` confirmed zero `MoonSharp` identifiers remain in tracked source/docs; 612 residual artifacts still carry the legacy name (coverage HTML exports, benchmark logs, cached VS metadata). Directories flagged for rename: `src\.vs\moonsharp`, `src\legacy\moonsharp_netcore`. Acceptance: documented rename queue covering files, folders, and generated outputs.
- ✅ File & folder renames: Renamed lingering filesystem entries (`NovaSharp.Interpreter` attribute source files, VS Code debugger scaffolding, `_Projects` mirror, JetBrains `.DotSettings`, legacy `novasharp_netcore`, `.vs/novasharp` cache, Flash project metadata, `.gitignore` helpers) so tracked assets now ship with `NovaSharp` branding.
- ✅ Resource sync: Rebranded embedded assets (e.g., `Resources/NovaSharpdbg.png`) to unblock `NovaSharp.RemoteDebugger` and `NovaSharp.Cli` builds referencing the new resource names.
- ✅ Benchmark harness rename: `tooling/NovaSharp.Comparison` replaces `PerformanceComparison` with updated namespaces, net8.0 executable output, and auto-run BenchmarkDotNet wiring.
- ✅ Benchmark defaults: `NovaSharp.Benchmarks` and `NovaSharp.Comparison` now run all suites automatically when invoked (inject `--filter *`) while keeping shared `PerformanceReportWriter` reporting.
- ✅ Project hygiene: Audited `.csproj` metadata and purged references to missing/irrelevant assets (icons, logs, keypairs) so IDE surfacing stays clean.
- ✅ Build sanity check: `dotnet build src/runtime/NovaSharp.Interpreter/NovaSharp.Interpreter.csproj` passes after renames, confirming compiler parity.
- ✅ Artifact regeneration: Regenerated coverage via `./coverage.ps1` (67.8 % line / 69.7 % branch / 72.1 % method) and reran `NovaSharp.Benchmarks` + `NovaSharp.Comparison` to refresh BenchmarkDotNet outputs; stale MoonSharp-labelled artefacts are no longer present under `docs/coverage/latest` or `BenchmarkDotNet.Artifacts`.
- ✅ Automation guardrails: Added a GitHub Actions branding check in `.github/workflows/tests.yml` that fails the build if tracked files or filenames contain `MoonSharp`, preventing regressions.
- ✅ Baseline snapshot: Captured the current benchmark outputs under MoonSharp-labelled sections in `docs/Performance.md` so performance work can compare future NovaSharp runs against the pre-improvement baseline.
- ✅ OS metadata accuracy: Perf logging harness now reads the Windows edition + display version from `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`, so `docs/Performance.md` records entries like "Windows 11 Pro 25H2 (build 26200.6899, 10.0.26200)" instead of the kernel-only label.
- ✅ Unity compatibility: Disabled nullable reference types across remaining tooling/test projects and reinforced explicit-type editorconfig rules so Unity builds never see `var` or nullable metadata drift.

## Coverage Initiative (Target ≥ 90%)
- **Milestone 0 – Baseline Measurement (in-flight)**  
Integrate Coverlet with `src/tests/NovaSharp.Interpreter.Tests`, emit LCOV + Cobertura outputs under `artifacts/coverage`, and publish HTML reports to `docs/coverage/latest`. Document the workflow in `docs/Testing.md` and ship a `coverage.ps1` helper for local execution. Acceptance: CI publishes raw + HTML artefacts, auto-generated Markdown summary, and a PR comment capturing the current baseline (67.8 % line / 69.7 % branch / 72.1 % method; interpreter 81.0 % line).
  - ✅ `coverage.ps1` now drives `dotnet test` through `coverlet.console`, copies Markdown/JSON summaries beside the raw reports, and refreshes `docs/coverage/latest`.
  - ✅ Tests workflow gained a dedicated `code-coverage` job that appends the Markdown summary to the run, updates a PR comment in place, and uploads both raw coverage data and a gzipped HTML dashboard.
- **Milestone 1 – Unit Depth Expansion (Weeks 1-3)**  
  Expand `src/tests/NovaSharp.Interpreter.Tests/Units` to cover parser error paths, metatable resolution, tail-call recursion limits, dynamic expression evaluation, and data-type marshaling edge cases (tables↔CLR types). Add regression tests for CLI utilities in `src/tooling` (command parsing, REPL, config). Target ≥ 70% line coverage for `NovaSharp.Interpreter` namespace before integration scenarios.
  - ✅ Added `ParserTests`, `MetatableTests`, `TailCallTests`, `DynamicExpressionTests`, and expanded `InteropTests` to lock in parser diagnostics, metatable `__index/__newindex` wiring, deep tail-call paths, dynamic expression environments, and CLR table marshaling.
  - ✅ Shipped `ReplInterpreterTests` + `JsonModuleTests` to cover REPL prompts/dynamic evaluation and JSON encode/decode paths; interpreter namespace line coverage still 67.7 %, so JSON branch is unblocked but IO/OS modules remain uncovered.
  - ✅ Added `IoModuleVirtualizationTests` to back the IO/OS pathways with an in-memory platform accessor and keep `os.remove`/`io.output` behaviour under regression; interpreter namespace line coverage still 67.7 %.
  - ✅ Captured CLI command parsing transcripts with `ShellCommandManagerTests` and extended the virtualised IO suite to assert `os.tmpname`/`os.rename` plus stdout/stderr separation; interpreter namespace coverage gain pending analysis.
  - ✅ Migrated the legacy NUnit suite off `Assert.That`/`Assert.AreEqual` APIs to NUnit 4 constraint syntax, removed the `EMBEDTEST` shim, sealed helper fixtures flagged by CA1852, and restored a green Release build on net8.0.
  - ✅ Added CLI failure-path tests (`ShellCommandManagerTests`) covering null/empty commands and tightened regression coverage.
  - ✅ Exercised `Program.CheckArgs` and `HardWireCommand` interactive flows with unit tests, enabling InternalsVisibleTo for NovaSharp.Cli so tests hit real code paths without reflection.
  - ✅ Added `RunCommandTests`, `RegisterCommandTests`, and `CompileCommandTests` (including loader failure propagation) to cover CLI script execution wiring, syntax help, type registration output, binary dump generation, and error paths; Release suite (679 tests) stays green while CLI namespace coverage rises.
  - ✅ Refactored `DebugCommand` behind injectable factory/launcher bridges and added `DebugCommandTests` to assert debugger attach + browser launch behaviour without spawning external processes; Release suite now passes 681 tests.
  - ✅ Stubbed the Lua dump loader so `HardWireCommand` can be exercised deterministically; new tests cover C#/VB generation paths, visibility warnings, and ensure code artifacts are emitted (Release suite at 684 tests).
   - ✅ Added `HelpCommandTests` to validate command listings, detailed help, and error paths, keeping CLI interactive surfaces covered (Release suite now 687 tests).
- ✅ Quantified the latest coverage run: overall line coverage at **56.5 %** (↑0.1), with `CommandManager` now 100 % covered. Remaining hotspots are `NovaSharp.Program` (0 %), `HardWireCommand` (4.5 %), and other CLI commands <60 %.
- ✅ Replaced the placeholder `SerializationExtensionsTests` with comprehensive coverage for prime tables, nested tables, identifier validation, tuple flattening, and string escaping, correcting `SerializationExtensions` to emit Lua-compliant braces/newlines instead of the `${` scaffolding.
  - ✅ Standardise test naming: migrated all NUnit test methods to PascalCase (no underscores), elevated the `.editorconfig` rule to `error`, refreshed `docs/Testing.md`, and re-ran the Release suite (671 tests passed).
  - ✅ Closed remaining CLI gaps: added `ProgramTests.CheckArgsHardwireFlagGeneratesDescriptors` for the `-W` flow and `ScriptDefaultOptionsTests` to lock in default loader persistence; Release suite now passes 690 tests with NovaSharp.Cli coverage trending upward.
  - ✅ Expanded interpreter dynamic-expression coverage with constant/equality/symbol-resolution scenarios (`DynamicExpressionTests`), lifting the Release suite to 693 tests and covering `CreateConstantDynamicExpression`, `DynamicExpression.FindSymbol`, and equality semantics.
  - ✅ Exercised colon-operator configuration flags with new `ColonOperatorBehaviourTests`, validating `TreatAsDot`, `TreatAsColon`, and `TreatAsDotOnUserData` paths (Release suite now 696 tests).
  - ✅ Added `SymbolRefTests` covering binary write/read + environment restoration for global/local/upvalue symbols; Release suite up to 699 tests with serialization helpers now guarded.
  - ✅ Brought `ScriptExecutionContextTests` online to validate local symbol resolution, global env access, and metatable inspection, pushing the Release suite to 702 tests and exercising execution-context helpers.
  - ✅ Extended execution-context coverage to tail-call helpers and message decoration via new tests, lifting the Release suite to 704 tests and guarding `GetMetamethodTailCall` plus `PerformMessageDecorationBeforeUnwind`.
  - ✅ Added `ProcessorStackTraceTests` to validate coroutine stack traces and interpreter exception call stacks, bringing the Release suite to 706 tests and exercising `Coroutine.GetStackTrace` along with debugger call-stack capture.
  - ✅ Exercised debugger refresh plumbing via `DebuggerRefreshTests`, capturing call stack, locals, watch expressions, value stack, and thread snapshots (Release suite now 707 tests).
  - ✅ Hardened coroutine lifecycle coverage (`CoroutineLifecycleTests`) to ensure resume error paths, recycling flows, and auto-yield force suspends are guarded; Release suite now sits at 712 tests.
  - ✅ Added unit coverage for `coroutine.running/status`, `coroutine.wrap`, and `coroutine.resume` behaviours plus forced-yield handling in `pcall`/`xpcall` (Release suite now 722 tests).
  - ✅ Extended `CoroutineModuleTests` to cover nested tuple flattening, argument forwarding through `resume`/`wrap`, the `coroutine.create` guardrail, and direct `coroutine.yield` payloads; Release suite now passes 727 tests.
  - ✅ Ported the outstanding coroutine TAP scenarios into managed coverage (`IsYieldableReturnsFalseOnMainCoroutine`, `IsYieldableReturnsTrueInsideCoroutine`, `WrapPropagatesErrorsToCaller`, `WrapPropagatesErrorsAfterYield`), wiring the runtime `coroutine.isyieldable` hook and ensuring wrap propagates exceptions; Release suite now passes 735 tests.
  - ✅ Exercised CLR callback yieldability and `wrap` + `pcall` interop (`IsYieldableReturnsFalseInsideClrCallback`, `WrapWithPcallCapturesErrors`, `WrapWithPcallReturnsYieldedValues`) while fixing `coroutine.wrap` to retain its coroutine handle under protected calls; Release suite now passes 738 tests.
  - ✅ Mirrored the nested coroutine TAP assertions (`IsYieldableInsidePcallWithinCoroutine`, `WrapWithPcallHandlesTailCalls`) so yieldability and tail-call continuations stay faithful when protected by `pcall`; Release suite now passes 740 tests.
  - ✅ Closed the remaining TAP parity gaps (`IsYieldableInsideXpcallErrorHandlerWithinCoroutine`, `CoroutineStatusRemainsAccurateAfterNestedResumes`), confirming error-handler yieldability and status drift behaviour; Release suite now passes 742 tests.
  - ⏳ Next: Raise interpreter coverage gates above 70 % and wire CI enforcement now that the coroutine TAP matrix is mirrored in managed tests.
  - ✅ Extended debugger + CLI coverage with `SourceRefTests` (FormatLocation, distance heuristics, snippet extraction) and `ExitCommandTests` (name/help/execution), boosting `SourceRef` to 81 % line coverage and `ExitCommand` to 100 % while the Release suite grows to 1095 tests.
  - ✅ (2025-11-13) Patched CLI `Program` coverage regression by switching the decorated-error unit to `ScriptRuntimeException`, restoring Release builds after the missing `InterpreterException` symbol failure and keeping the decorated message branch measurable ahead of the next `./coverage.ps1` snapshot.
  - ✅ (2025-11-13) Added `ScriptLoaderBaseTests` covering LUA_PATH overrides, ignore flags, string-path unpacking, and environment fallbacks so `ScriptLoaderBase` exits the red list once the next coverage refresh lands.
  - 🔄 (2025-11-13) Landed `CharPtrTests` to target pointer arithmetic, comparisons, and string projections; rerun coverage to confirm `LuaStateInterop.CharPtr` climbs out of the low-coverage bucket.
  - 🔄 (2025-11-13) Added `DescriptorHelpersTests` asserting visibility attributes, member access classification, identifier conversions, and naming utilities; patched `ToUpperUnderscore`/`NormalizeUppercaseRuns` (aligned with Wallstop Unity helpers) to stop inserting stray digit separators and preserve PascalCase initials ahead of the next coverage snapshot.
  - 🔄 (2025-11-13) Expanded `EventMemberDescriptorTests` with wide-arity coverage (1–16 parameters) so `EventMemberDescriptor.CreateDelegate` switch cases are exercised before the next burn-down review.
  - 🔄 (2025-11-13) Extended `LuaStateInteropToolsTests` to cover octal alternate padding, pointer formatting, `%n` substitution, and the positive space flag, lifting `LuaStateInterop.Tools` beyond the low-coverage threshold.
  - 🔄 (2025-11-13) Augmented `FastStackTests` with zero-count, overflow, slot-clearing, and full reset scenarios so `FastStack.RemoveLast`/`Clear` guard paths are now measured.
  - 🔄 (2025-11-13) Added `DotNetCorePlatformAccessorTests` covering file mode parsing, environment I/O, filesystem operations, and the NotSupported execute path to raise `DotNetCorePlatformAccessor` coverage.
  - 🔄 (2025-11-13) Broadened `JsonModuleTests` (invalid parse/serialize paths, `isnull`, `null`) to burn down the remaining gaps in `JsonModule`.
  - 🔄 (2025-11-13) Introduced `ClosureContextTests` validating symbol name copying and value storage so `ClosureContext` no longer drags down scope coverage.
  - 🔄 (2025-11-13) Added `TablePairTests` to cover constructor/`Nil` behavior and document the current value setter semantics for `TablePair`.
  - 🔄 (2025-11-13) Added `PropertyTableAssignerTests` (happy-path, subassigners, fuzzy matching, expected-missing) to drive `PropertyTableAssigner<T>` + core assigner coverage.
  - 🔄 (2025-11-13) Added `SliceTests` covering indexing, enumeration, conversions, and unsupported mutators to lift `Slice<T>` coverage.
  - 🔄 (2025-11-13) Added `InteropRegistrationPolicyTests` to assert policy factory behavior (default/automatic/explicit) and verify the obsolete marker on `Explicit`.
  - 🔄 (2025-11-13) Disabled nullable reference types solution-wide (`Directory.Build.props`) and elevated CS8632/CS8669 to errors in `.editorconfig` so nullable annotations cannot reappear.

  - ✅ (2025-11-13) Closed the immediate `ScriptRuntimeException` factory gap by adding `ScriptRuntimeExceptionTests` (table index errors, `ConvertObjectFailed` overloads, coroutine guard rails, access-member helpers, `Rethrow` guard) under `src/tests/NovaSharp.Interpreter.Tests/Units`, exercising every static branch that previously sat redlined.
  - 🔁 After closing the exception gap, rerun `dotnet test` and `./coverage.ps1`, then log the refreshed interpreter metrics back into this PLAN and `docs/coverage/coverage-hotspots.md`. (2025-11-13) `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release` ✅; `./coverage.ps1` ✅ (line: 74.9 % total / 87.1 % interpreter, branch: 75.5 % total / 82.8 % interpreter). Snapshot + notes pushed to `docs/coverage/coverage-hotspots.md`.
  - 🔜 Next burn-down target: pick up the remaining low-cover interpreter helpers (current suspects: coroutine scheduling utilities and debugger resumptions) and repeat the add-tests → coverage cycle.
    - 🔄 (2025-11-13) Added `CallbackFunctionTests` covering colon-operator behaviour, default access mode validation, delegate conversions, and signature heuristics; line coverage now **91.3 %** for `CallbackFunction`.
    - 🔄 (2025-11-13) Expanded `ClosureTests` (upvalue classification, delegate helpers, call overloads, metadata assertions) lifting `Closure` to **86.6 %** line / **83.3 %** method coverage; remaining gap stems from the non-executable zero-upvalue branch (Lua `_ENV` always captured).
    - 🔄 (2025-11-13) Rounded out `ParameterDescriptorTests` with restriction-constructor, by-ref wiring, original-type fallbacks, and `ToString` coverage; `ParameterDescriptor` now reports **100 %** line coverage.
    - 🔄 (2025-11-13) Hardened `LoadModuleTests` (reader fragment concatenation, safe-environment failure, `loadfile` syntax errors, `dofile` success/error flows), boosting `LoadModule` to **91.3 %** line / **86.9 %** branch coverage.
    - 🔄 (2025-11-13) Expanded `IoModuleTests` (default stream setters, close/flush, iterator API, binary encoding guardrails, `tmpfile`, exception fallbacks), raising `IoModule` to **93.2 %** line / **88.0 %** branch coverage and clearing it from the red list.
    - 🔄 (2025-11-13) Added `StringModuleTests` and `StringRangeTests` exercising char/byte/unicode ranges, formatter helpers, metatable wiring, positive/negative index coercion, and the internal `AdjustIndex` path; reran `./coverage.ps1` (Release) capturing `StringModule` at **99.0 %** line / **100 %** branch and `StringRange` at **100 %** line with the interpreter suite now at **87.3 %** line / **83.1 %** branch (1 430 tests).
    - 🔜 Pivot the coverage sweep toward coroutine scheduling utilities and debugger resume helpers before returning to remote-debugger automation.
  - ✅ Restored CLI interpreter decorated-error coverage by swapping `ProgramTests.InterpreterLoopPrintsInterpreterExceptionDecoratedMessage` to throw `ScriptRuntimeException`, unblocking Release builds and keeping the `Program` decorated-message branch under test (coverage refresh pending next `./coverage.ps1` run).
  - ✅ Added `LoadModuleTests`, `ParameterDescriptorTests`, `AutoDescribingUserDataDescriptorTests`, `StandardEnumUserDataDescriptorTests`, `UnityAssetsScriptLoaderTests`, `WatchItemTests`, `ValueTypeDefaultCtorMemberDescriptorTests`, and parser exception coverage (`SyntaxErrorExceptionTests`, dynamic expression constructors), pushing interop descriptors above 70 % coverage, closing debugger/watch gaps, and keeping interpreter totals on an upward trend.
- **Milestone 2 – Integration & Debugger Coverage (Weeks 2-4)**  
  Build scripted VS Code + remote debugger sessions using `Microsoft.VisualStudio.Shared.VstestHost` (or equivalent) to validate attach/resume/breakpoint-insert/watch evaluation flows. Add Lua fixture-backed smoke tests for `NovaSharp.RemoteDebugger` and CLI shell transcripts; capture transcripts as golden files in `src/tests/TestRunners/TestData`. Ensure TAP IO/OS suites run in trusted CI lane or supply equivalent managed tests hitting filesystem/environment abstractions.
- **Milestone 3 – Coverage Gates & Reporting (Week 4)**  
  Wire coverage upload to Codecov (or Azure DevOps equivalent), failing PRs under 85% line coverage and raising the gate to 90% once Milestones 1-2 land. Add a dashboard to `docs/Testing.md` summarizing module-by-module coverage and bake badge URLs into `README`. Ensure nightly CI refreshes artefacts and warns on ≥3% regressions via GitHub checks.
- **Milestone 4 – Sustained Quality (Ongoing)**  
  Author contributor guidance (PR template checklist, test matrix updates) requiring new features to include both unit and integration coverage. Automate review lint that rejects files in `src/NovaSharp.Interpreter` without accompanying tests unless tagged `[NoCoverageJustification]` with an engineering sign-off. Track skip-list debt in an issue epic and burn it down as platform lanes stabilize.

## Project Structure & Code Style Alignment
- **Milestone A – Solution & Directory Audit (High Priority)**  
  Inventory every `.sln`/`.csproj` in `src`, classify runtime vs tooling vs samples, and propose a consolidated folder hierarchy (runtime/interpreter, debuggers, tooling, samples, tests). Produce a migration blueprint documenting path moves, namespace impacts, and packaging implications. Acceptance: reviewed architecture doc + updated `docs/Modernization.md` appendix.
  - ✅ `docs/ProjectStructureBlueprint.md` captures the current vs. proposed layout, legacy inventory, and phased migration plan.
- **Milestone B – Refactor Execution (High Priority)**  
  Reshuffle projects/solutions per blueprint (rename folders, update `.sln`, `.csproj`, `Directory.Build.props`), add `Directory.Packages.props` if needed, and ensure build/test scripts, CI workflows, and documentation follow new layout. Include regression checklist covering NuGet outputs, debugger packaging, and tooling discovery.
  - ✅ Rehomed runtime, debugger, tooling, samples, tests, docs, and legacy assets under the new directory taxonomy; updated `NovaSharp.sln`, helper scripts, and top-level docs to match.
  - ✅ Collapsed the interpreter `_Projects` mirror into a multi-targeted `NovaSharp.Interpreter.csproj` (netstandard2.1 + net8.0), updating dependent projects, the solution, and automation scripts.
  - ✅ Folded the VS Code debugger `_Projects` mirror into the primary `NovaSharp.VsCodeDebugger.csproj` (netstandard2.1 + net8.0) and refreshed downstream project references.
  - ✅ Renamed the CLI shell to `tooling/NovaSharp.Cli/NovaSharp.Cli.csproj` and adjusted tests/solution/docs to point at the new path.
  - ✅ Audit packaging (NuGet metadata, release notes, scripts) for the CLI rename and update any hard-coded paths. `src/release_readme.txt` now documents the `cli/` drop (formerly `repl`) and `docs/ProjectStructureBlueprint.md` reflects the new naming.
  - ✅ Replaced CLI `packages/*` binaries with NuGet-managed references; ensure remaining legacy tooling cleans up any straggler DLL drops.
- **Milestone C – Namespace & Using Enforcement**  
  Introduce Roslyn analyzers or custom scripts to ensure namespaces mirror the physical path + project root (`NovaSharp.Interpreter.Debugging` style), and require `using` directives to live inside namespaces. Provide migration scripts to batch-update existing files, codify exceptions for generated/bundled code, and document rules in `docs/Contributing.md`.
  - ✅ Added `tools/NamespaceAudit/namespace_audit.py` to surface directory/namespace mismatches (current hot spots: CLI/Hardwire/NovaSharp.Comparison).
- **Milestone D – EditorConfig Adoption + Lua Exceptions**  
  Import `.editorconfig` from `D:/Code/DxMessaging-Unity/Packages/com.wallstop-studios.unity-helpers`, strip BOM (`charset = utf-8`), and align with repo conventions (CRLF is acceptable). Add sub-directory `.editorconfig` under Lua fixture folders to keep Lua-specific indentation/whitespace expectations. Document formatting commands and exception rationale in `docs/Testing.md` + PR template.
- **Milestone E – Solution Organization & Naming (Current Sprint)**  
  Harden the Visual Studio solution layout and align naming with PascalCase conventions for first-party assets. Scope includes renaming the solution artifact, ensuring nested folders mirror `runtime/tooling/tests/debuggers`, and cleaning up lingering `.netcore` suffixes in project surfaces.
  - ✅ Renamed `src/NovaSharp.sln` to `src/NovaSharp.sln`, updated build/test docs, and nested Benchmarks under the Tooling solution folder to prevent empty roots.
  - ✅ Rebranded the VS Code debugger project (`NovaSharp.VsCodeDebugger.csproj`) and refreshed dependent project references to respect PascalCase naming.
  - ✅ Updated VS Code debugger package metadata (dropped `.netcore` suffix) and documented the new single-source layout.

## Near-Term Priorities (ordered)
1. **MoonSharp ➜ NovaSharp finalization**
   - ✅ Cleared the filesystem rename queue (`NovaSharp.Interpreter` attribute files, VS Code debugger scaffolding, `_Projects` mirror, JetBrains `.DotSettings`, legacy cache folders).
   - ✅ Rebranded the comparison benchmark harness (`tooling/NovaSharp.Comparison`) with default BenchmarkDotNet config and VS run target output.
   - ✅ Scrubbed `.csproj` entries referencing missing/irrelevant assets (icons, logs, keypairs) to keep IDE surfaces clean.
   - ✅ Refreshed generated artefacts (coverage, benchmarks) under the NovaSharp name and cleaned stale MoonSharp results.
   - ✅ Add guardrail automation (CI grep or analyzer) blocking `MoonSharp` regressions across code, docs, and assets. CI now runs `scripts/ensure-novasharp-branding.sh`, which fails on new `MoonSharp` identifiers outside the curated allowlist (perf baseline + benchmark writer shim).
- ✅ Completed an initial vestigial inventory (`docs/modernization/vestigial-inventory.md`) highlighting keep/remove candidates (e.g., legacy REPL history) and modernization notes for performance counters.
- ✅ Audit outstanding MoonSharp issues that still apply to NovaSharp, classify real defects vs legacy gaps, and schedule fixes or deprecations accordingly. See `docs/modernization/moonsharp-issue-audit.md` for the owner/status matrix feeding the modernization tracker.
2. **Vestigial Runtime Review (Pre-Coverage)**
   - ✅ Inventory instrumentation and ancillary runtime helpers (e.g., `PerformanceStopwatch`, `GlobalPerformanceStopwatch`, `DummyPerformanceStopwatch`) — catalog captured in `docs/modernization/vestigial-inventory.md` (2025-11-10 snapshot).
   - ✅ Replaced remaining reflection-based access with friend-assembly visibility: `InfrastructureTests` now sets `PerformanceResult` properties directly and instantiates `ExecutionState` without reflection (Release suite 990 tests, 2025-11-12).
   - ✅ Produced keep/modernize/remove recommendations and blocked coverage expansion until the inventory landed; decisions tracked in `docs/modernization/vestigial-inventory.md`.
   - ✅ Logged the diagnostics instrumentation review in `docs/modernization/vestigial-inventory.md`; current guidance is to keep the stopwatch counters and cover them with tests (completed).
3. **Coverage Push (>90%)**
   - ✅ Completed Milestone 0 baseline capture by rerunning `./coverage.ps1` (2025-11-11), recording 67.8 % line / 69.7 % branch / 72.1 % method coverage (NovaSharp.Interpreter 81.0 % line), and updating `docs/Testing.md` + coverage exports to document the helper workflow.
   - ✅ Begin Milestone 1 by targeting parser + metatable hot spots and porting CLI shell tests.
   - ✅ Drafted GitHub workflow updates with conditional coverage gating: `code-coverage` job now runs an `Evaluate coverage threshold` step that parses `Summary.json`, warns when line coverage falls below 85 % in monitor mode, and supports flipping to `COVERAGE_GATING_MODE=enforce` once ≥85 % is sustained.
   - ✅ Expanded Hardwire coverage scaffolding: interpreter test suite now references `NovaSharp.Hardwire`, exposes internals via `InternalsVisibleTo`, and adds `HardwireGeneratorRegistryTests`/`HardwireCodeGenerationContextTests` to lock in registry fallbacks, assembly discovery, logging, and visibility gating behaviours ahead of deeper generator coverage pushes.
   - ✅ Added `HardwireParameterDescriptorTests` plus dispatch-focused `HardwireCodeGenerationContextTests` coverage, ensuring ref/out validation, default-parameter metadata, registry error paths, and initialization wiring are exercised so NovaSharp.Hardwire line coverage begins moving off the 22 % floor.
   - ✅ Hardened `coverage.ps1` to surface failing builds: logs the full `dotnet build` output, explicitly builds the interpreter test project, and reports actionable messages (including log locations) when the runner DLL is missing or build steps fail.
   - ✅ Backfilled NovaSharp.Hardwire generator coverage by introducing `HardwireGeneratorTests` which verify BuildCodeModel registrations and `AllowInternals` propagation, while adding deterministic registry stubs; interpreter suite now drives Hardwire line coverage to ~29 %.
   - ✅ Extended `HardwireGeneratorTests` to cover ref/out default dispatch, property setter special-names, and static indexer warnings—verifying both generated source and emitted diagnostics so MethodMemberDescriptorGenerator edge paths stay under regression.
   - ✅ Hardened `HardwireParameterDescriptorTests` to assert default-value placeholders emit `DefaultValue` instances, keeping `HardwiredDescriptors.DefaultValue` under coverage.
   - ✅ Added `StandardUserDataDescriptorGeneratorTests` to verify member/meta-member wiring so `StandardUserDataDescriptorGenerator` emits `AddMember`/`AddMetaMember` calls with the expected descriptors.
   - ✅ Added Lua `io.read("*n")` edge-case coverage (exponent-only tokens, hex literals, exponent overflow) with NUnit scenarios now skipped pending parity; documented the gap in `docs/LuaCompatibility.md` to highlight the remaining divergence from Lua 5.4.8 parsing rules.
   - Resolve uncovered behaviours by shipping production fixes when necessary; correctness improvements take priority over keeping the existing implementation untouched.
   - ✅ Stand up a Red/Green tracking doc (`docs/coverage/coverage-hotspots.md`) that enumerates interpreter hotspots still below 90 % with provisional owners.
   - 🔝 Drive `NovaSharp.Interpreter` coverage to ≥95 % line / ≥95 % branch / ≥95 % method by iterating on the red-list in `docs/coverage/coverage-hotspots.md`; treat any class below that threshold as a release blocker.
   - 🔄 Added dedicated unit suites for `UnaryOperatorExpression` and `BinaryOperatorExpression`, pushing their coverage to **100 %** and **87 %** line respectively and clearing previously untested Eval branches.
   - Expand NUnit suites (parser, metatables, IO/OS TAP replacements) until nightly coverage reports consistently exceed 90 % line/branch for `NovaSharp.Interpreter`, `NovaSharp.Cli`, and `NovaSharp.Hardwire`.
   - ✅ Added `ParserTests` coverage for hex float literals, Unicode escape decoding, and decimal-escape guardrails so `LexerUtils` error surfaces stay exercised and decorated messages reference the offending token.
   - ✅ Expanded `MetatableTests` to cover callable tables, custom `__pairs` iterators, and protected metatable failures under `CoreModules.PresetComplete`, ensuring metamethod regressions are caught without relying on TAP fixtures.
   - ✅ Raised the coverage gate to 90 % and flipped `.github/workflows/tests.yml` to enforce failures when Summary.json reports line coverage below the threshold (still overridable via `COVERAGE_GATING_MODE` repo vars).
   - ✅ Ported CLI shell REPL loop tests (command dispatch, evaluation output, interpreter/general exception handling) so interactive pathways run under NUnit with redirected console input.
  - ✅ Hardened CLI coverage across `Program`, `CommandManager`, `Run`, `Register`, `Compile`, `Debug`, `Help`, and `HardWire` commands; Release suite at 687 tests after deterministic hardwire + help harnesses.
  - ✅ Added Lua-level regression tests for `os.time`, `os.difftime`, and `os.date` (string + table forms), covering missing-field errors and placeholder week specifiers; Release suite climbs to 701 tests.
  - ✅ Added math module regression tests hitting logarithms (default/custom base), power, modf, min/max aggregators, ldexp, deterministic random sequences, and NaN/overflow behaviors.
  - ✅ Replaced `LinqHelpers` LINQ usage with allocation-free iterators and backed the helpers with unit tests to unblock future analyzer enforcement around `System.Linq` usage in hot paths.
  - ✅ Added `UnityAssetsScriptLoader` unit tests using an in-memory resource map, covering path trimming, not-found errors, existence checks, and loaded-script enumeration without relying on Unity runtime types.
  - ✅ Implemented root-scope `<close>` handling by seeding to-be-closed tracking in `Processor.ExecBeginFn` and rerunning the Release interpreter suite (1336 pass / 2 skip) to validate the change.
  - ✅ Mirrored Lua TAP `io.read` number cases by extending `FileUserDataBase.ReadNumber` (hex floats, huge exponents) and activating NUnit coverage in `IoModuleTests`.
  - ✅ Introduced `LuaCompatibilityVersion` plumbing (global + per-script options) so callers can pin interpreter behaviour to Lua 5.5/5.4/5.3/5.2 or stay on the rolling `Latest` target.
  - ✅ Captured a naming audit (`docs/style/naming-audit.md`) covering `Emit_*` helpers and `i_` prefixed members to guide the upcoming renaming pass.
  - ✅ Normalized enum declarations: every enum now starts with an obsolete `None`/`Unknown` at value 0, all subsequent values are explicit (flags use `1 << n`), and call sites favor defaults over obsolete sentinels to avoid new warnings.
  - ✅ **Harvest real-world Lua suites.**
    - Added `json.lua` (rxi, v0.1.2) and `inspect.lua` (kikito, v3.1.0) under `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld`, preserving upstream MIT licenses.
    - Introduced `RealWorldScriptTests` to load the fixtures via `Script.DoString`, exercising JSON encode/decode and table inspection scenarios to guard against regressions.
    - Refreshed `docs/testing/real-world-scripts.md` and `docs/ThirdPartyLicenses.md` with provenance metadata, paths, and maintenance guidance for future corpus growth.
  - ✅ **Feature coverage checklist.**
    - Added a sectioned Lua 5.4 parity matrix to `docs/LuaCompatibility.md`, covering syntax, standard libraries, metatables/GC, and coroutine semantics with status, coverage links, and owners.
    - Highlighted gaps for `<close>` variables, weak/ephemeron tables, string pack/unpack, IO hex parsing, and GC modes so they flow into runtime modernization follow-ups.
    - Documented observability actions (enabling disabled TAP suites, filing issues for ❌ entries) to keep parity efforts actionable.
    - **Golden rule:** when a regression test fails, assume the production implementation is wrong until proven otherwise. Review the official Lua 5.4 reference manual (`https://www.lua.org/manual/5.4/`) to confirm expected behavior, and prioritize fixes that bring NovaSharp back to Lua 5.4 compliance rather than weakening or deleting tests.
  - ✅ Verified `NovaSharpHideMemberAttribute` hides methods/properties (including inherited members) by registering sample userdata and asserting scripts only see the intended surface.
  - ✅ Added coverage for `FastStackDynamic<T>` operations, REPL module path resolution (environment + `LUA_PATH` overrides), `CoreLib.IO.BinaryEncoding` round-trips, and hardwired descriptor metadata checks to prep Roslyn parity.
  - ✅ Added guardrail tests around `InvalidScriptLoader` failure paths to keep runtime diagnostics stable when custom loaders plug in.
   - ✅ Exercised hardwired member/method descriptors via targeted hardwired userdata tests (read/write gating, optional-argument marshalling, static access guards), removing the zero-coverage gap from the excluded auto-generated harness.
   - ✅ Expanded `ReflectionSpecialName` unit coverage to map every operator/event/indexer branch (true/false, unary/binary symbols, namespaced members), ensuring the helper enum conversion stays under regression.
   - 🔄 New: bake "extreme path" coverage into every milestone—add tests for undersized buffers, null inputs, unexpected/malformed data, and other non-happy paths (e.g., BinaryEncoding invalid arrays). Track gaps per hotspot and refuse closure until happy + edge paths are both covered.
     - ✅ Extended `BinaryEncodingTests` with offset/zero-count/destination-null cases so every guard path in `ValidateDestination`/`ValidateBufferRange` now executes under NUnit.
   - ✅ Expanded `DebugModule` coverage to include CLR function upvalue handling, invalid `upvaluejoin` indices, non-string traceback messages, and coroutine/thread traces; edge-path assertions now guard previously untested branches.
   - ✅ Covered `pcall`/`xpcall` CLR edge paths (tail-call continuations, yield requests, handler-before-unwind decoration) so `ErrorHandlingModule` red-list gaps now assert both happy and unhappy flows.
   - ✅ Exercised `pcall`/`xpcall` tail-call forwarding, argument propagation, and CLR handler success paths; rerun `coverage.ps1` to capture the >90 % lift for `ErrorHandlingModule`.
   - ✅ Exercised `HardwiredMemberDescriptor` default getter/setter implementations so the base-path exceptions are now under coverage; only the by-ref conversion scenarios remain red-listed.
   - ✅ Added parameterised coverage for `ReflectionSpecialName` arithmetic/relational operator mappings; refreshed coverage now reports 95.8 % line / 100 % branch so the class exits the red list.
   - ✅ Covered `DynValueMemberDescriptor` getters, CLR function execution flags, setter guard, and wiring fallbacks (primitive/table/userdata/unsupported) to lift the descriptor from 17 % to double-digit green coverage.
   - ✅ Stream file coverage now includes signed exponent parsing, standalone-sign rewinds, numeric byte-count EOF, and invalid option guards; `StreamFileUserDataBase` sits at 83 % line / 82 % branch after the latest `StreamFileUserDataBaseTests`.
   - ✅ `Slice<T>` gained focused coverage (enumeration, reversed views, copy semantics, read-only guards) pulling it out of the low-coverage bucket while validating exception surfaces.
    - ✅ Exercised `debug.debug` interactive edge paths (return values, CLR exception surfaces, whitespace/no-op input, null provider) to chip away at the remaining `DebugModule` debt; rerun `coverage.ps1` to capture the new lines and confirm the 91 % / 78 % snapshot.
    - ✅ Hardened `OsTimeModule` against edge inputs (missing month/year fields, pre-epoch timestamps, invalid specifiers, escape sequences) and verified `os.clock` monotonic behaviour; Release suite holds at 990 tests (2025-11-12).
    - ✅ Covered `DebuggerAction` constructor/timestamp, aging behaviour, defensive line storage, and breakpoint formatting so debugger actions exit the red list; interpreter Release suite at 1001 tests (2025-11-12).
    - ✅ Composite user-data aggregation now covered end-to-end (index, set, meta, metadata) via `CompositeUserDataDescriptorTests`, lifting coverage above 92 % and pushing the Release suite to 1008 tests (2025-11-12).
    - ✅ Added `UndisposableStreamTests` to assert dispose/close suppression, synchronous/asynchronous forwarding, timeout propagation, and metadata passthrough; interpreter suite now totals 1019 tests with `UndisposableStream` at 94 % line coverage (2025-11-12).
    - ✅ `LuaStateInteropToolsTests` cover numeric detection, rounding, meta-character decoding, and representative `sprintf` flag combinations, pushing `LuaStateInterop.Tools` beyond 90 % line coverage (2025-11-12).
    - ✅ `PlatformAccessorBaseTests` toggle detector flags to verify mono/unity suffixes, portable/AOT annotations, and default input bridging; Release suite now reports 1023 tests (2025-11-12).
    - ✅ Unary operator suite now exercises runtime error paths (unexpected operator evaluation) in addition to happy-path negation/length/logical cases; bytecode guardrails remain green with compile-time invalid operator assertions.
    - ✅ Event facade + descriptor coverage now exercises static events, duplicate-removal guardrails, and unknown-handler removals; `TestHelpers.CreateExecutionContext` now calls `Script.CreateDynamicExecutionContext()` so the unit suite avoids reflection. Interpreter suite climbs to 1 038 tests after the new cases (2025-11-13).
    - ✅ Scheduled the repo-wide reflection/`nameof` audit: captured current usages in `docs/modernization/reflection-audit.md` and linked the policy in `AGENTS.md` so future changes document additions before implementation.
   - ✅ Plan the test-suite reorg: drafted `docs/testing/test-suite-reorg-proposal.md` outlining the target folder hierarchy, namespace conventions, migration steps, and open questions for maintainer review.
   - ✅ Added `HardwiredDescriptorTests` covering member read/write conversions and method argument marshalling/defaults, pairing with existing `PermanentRegistrationPolicy` tests to clear the interop red-list backlog; rerun `coverage.ps1` to confirm instrumentation reflects the new lines.
   - ✅ Extended `OsSystemModule` edge-path coverage (non-zero exits, missing files, move/delete failures, locale placeholder) capturing 98 % line coverage; runtime red list now narrowed to `DebugModule` interactive paths.
   - ✅ Added injectable console hooks + regression tests for `debug.debug`, enabling automated coverage of command execution, error reporting, and queued returns.
 - ✅ Drove `FileUserDataBase`/`StreamFileUserDataBase` coverage by stubbing stream failures (seek/flush/setvbuf) and verifying IO module fallbacks.
  - 🔝 **Raise NovaSharp.Interpreter coverage to ≥95 % line / branch / method.**  
    - Identify remaining gap areas (e.g., coroutine corner cases, Lua 5.4 features, IO/OS modules) via the latest `coverage.ps1` HTML dashboards.
    - Prioritize unit/integration tests that close the gaps without brittle assertions; update TAP fixtures where feasible.
    - Treat this as blocking before moving on to broader allocation/interop work; track progress weekly in `docs/coverage/coverage-hotspots.md`.
  - ⚖️ Guardrail: treat any failing test as a spec investigation. Cross-check behaviour against the official Lua manuals for every supported version (baseline: 5.4.8), capture the cited section/link in the diff, and only adjust production/tests once the canonical interpreter behaviour is understood.
  - ⏳ Establish a multi-version Lua spec harness: enumerate the manuals/sections for each supported version, expand spec suites (string/math/table/core/error paths) with exhaustive edge/error cases that cite the manual, and sync findings into `docs/LuaCompatibility.md`.
  - ✅ Expanded EventMemberDescriptor, CharPtr, StringConversions, DescriptorHelpers, RefIdObject, ExprListExpression, FieldMemberDescriptor, StandardEnumUserDataDescriptor, ScriptExecutionContext, and `FastStack<T>` unit suites (null-pointer guards, tuple compilation, SafeGetTypes failure path, string conversion errors, ref-id formatting, field read/write guardrails, enum conversion/validation, call/AdditionalData edge paths, explicit-interface stack members) and reran `./coverage.ps1` (2025-11-12 18:55 UTC); NovaSharp.Interpreter now reports **86.1 % line / 82.3 % branch** coverage with the former red-listed classes clearing 95 %+ (`FastStack<T>` now 100 %).
4. **Project Structure Refactor (High Priority)**
   - ✅ Solution + debugger project rename landed (PascalCase `NovaSharp.sln`, `NovaSharp.VsCodeDebugger.csproj`).
   - ✅ VS Code debugger `_Projects` mirror removed; project now multi-targets `netstandard2.1;net8.0`.
   - ✅ CLI rename complete (`tooling/NovaSharp.Cli`).
   - ✅ Audit packaging/scripts for the new CLI name (NuGet spec, release docs, tooling installers). Packaging notes now consistently reference `cli`/`NovaSharp.Cli` for the REPL bundle and no longer mention the removed `repl` layout.
   - ✅ CLI now restores solely via NuGet (no checked-in packages).
   - ✅ Audited legacy assets: `src/legacy/Tools/lua52.dll` quarantined with README; exclude from modern builds unless a native packaging strategy is required.
   - ✅ Update automation/scripts (branding + build workflows) to reflect the new directory paths; legacy `rsync_projects.sh` has been removed and the shared guard/coverage workflows already reference the current tooling layout.
  - ✅ Retired the entire `src/legacy` tree (Flash client, NovaSharpPreGen console, Lua52 binaries, empty `novasharp_netcore`) after confirming no build/test dependencies; modernization docs now capture the removal for historical context.
  - 🔜 Plan the namespace/package rebrand: migrate code to `WallstopStudios.*` namespaces and align package IDs to `com.wallstop-studios.*` while keeping the NovaSharp product name. Produce a staged rollout plan (analyzers, namespace audit, package metadata) before executing.
5. **Namespace & Formatting Enforcement**
   - ✅ Drafted analyzer configuration: root `Directory.Build.props` enables analyzers during build; `.editorconfig` keeps IDE0130 at warning until the ~700 existing namespace mismatches are triaged under the follow-on suppression/cleanup tasks.
   - ✅ Normalized `Execution/Scopes` namespaces and updated consumers so `ClosureContext`, `RuntimeScope*`, and loop helpers live under `NovaSharp.Interpreter.Execution.Scopes` without breaking the build.
   - ✅ Migrated `DataTypes/*` to `NovaSharp.Interpreter.DataTypes` and added a scoped global using so dependent code compiles while we continue tightening folder-aligned namespaces.
   - ✅ Shifted `DataStructs/*` to `NovaSharp.Interpreter.DataStructs` and introduced shared global using coverage for their extensions.
   - ✅ Moved core execution helpers (`ScriptExecutionContext`, `DynamicExpression`) under `NovaSharp.Interpreter.Execution` and rehomed interop attributes + runtime errors beneath folder-aligned namespaces.
   - 🔄 Work-in-progress: reapply namespace/import fixes with Unity-compatible constructs (no global usings), updating runtime/interop consumers one file at a time to keep source drops usable inside Unity.
     - ✅ Closed the remaining `DataStructs` loopholes by moving `Extension_Methods` under `NovaSharp.Interpreter.DataStructs` and swapping every consumer over to explicit `NovaSharp.Interpreter.DataStructs` imports; `NovaSharp.Interpreter` builds clean aside from pre-existing IDE0130 warnings.
     - ✅ Completed the `DataTypes/*` namespace migration with explicit `NovaSharp.Interpreter.DataTypes` imports woven through runtime/interop call sites; build now succeeds (warnings only) without relying on global-usings.
     - ✅ Realigned `Errors/*` to `NovaSharp.Interpreter.Errors` and propagated the new namespace through compiler/runtime call sites, eliminating the remaining IDE0130 warnings for that folder without introducing globals.
     - ✅ Rehomed `Execution/*.cs` (`DynamicExpression`, `ScriptExecutionContext`, `InstructionFieldUsage`) under `NovaSharp.Interpreter.Execution` and updated all consumers plus performance wrappers, keeping the build warning-free outside of the remaining legacy namespace backlog.
     - ✅ Lifted all interop attribute types into `NovaSharp.Interpreter.Interop.Attributes` and retargeted every consumer (`CoreLib`, `Interop`, `DataTypes/UserData`) to the new namespace, clearing the attribute naming inconsistencies without relying on global usings.
     - ✅ Converted the remaining runtime underscore artifacts to PascalCase (`Tree/FastInterface`, `ExtensionMethods`, `KopiLuaStringLib`, processor partials, `LuaBaseCLib`, `NamespaceDoc` split) and adjusted imports; the Options and Lexer namespaces now match their folders while consumers import `NovaSharp.Interpreter.Options` / `NovaSharp.Interpreter.Tree.Lexer`.
     - ✅ Swapped the CoreLib module surfaces back to instantiable classes (with targeted CA1052 suppressions) so generic registration accepts them, and renamed the platform accessor API (`OpenFile`, `GetStandardStream`, `GetTempFileName`, `ExitFast`, etc.) to PascalCase; updated runtime/tests to honor the new signatures while preserving Lua behaviour.
     - ✅ Rewired hardwire generators, benchmarks, and debugger shims to the new descriptor namespaces and `DynamicExpression.ExpressionCode` member, and updated CLI/test infrastructure to the PascalCase API surface so `dotnet build -c Release` returns cleanly (tests now consume shared NovaSharp usings via the project-level settings).
     - ✅ Confirmed `dotnet build src/NovaSharp.sln -c Release` and `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release` succeed; remaining analyzer warnings are pre-existing CA/IDE suggestions and tracked separately.
     - ✅ Brought `Execution/Scopes` files in line with their folder namespace and threaded `Execution.Scopes` imports through the compiler/bytecode pipeline (chunk/loop statements, loader, debug module, VM call stack) so IDE0130 no longer fires for that area; scoped `ClosureContext` consumers now reference the new namespace without relying on globals.
     - ✅ Propagated the PascalCase namespace updates into debugger, CLI, tests, and hardwire tooling; replaced legacy `Interpreter` imports with explicit `NovaSharp.Interpreter.*`/`NovaSharp.Interpreter.DataTypes` references and retargeted hardwire descriptors to `StandardDescriptors.*`, restoring a clean Release build across the solution.
     - ✅ Reworked VS Code protocol DTOs to expose read-only lists and init-only capability flags, eliminating the CA1819/CA1051 warnings triggered by public arrays and fields in `DebugSession`.
     - ✅ Renamed interop constants and Unity loader defaults to PascalCase and updated loader/member surfaces to return `IReadOnlyList<T>`/`Array.Empty<T>()`, clearing the residual CA1707/CA1805/CA1819 diagnostics in those areas.
     - ✅ Staged the `.editorconfig` import with Lua fixture overrides by adding `src/tests/NovaSharp.Interpreter.Tests/TestMore/.editorconfig` so TAP suites retain two-space indentation.
     - Catalogue remaining underscore-based files, folders, and identifiers and convert them to PascalCase wherever Lua compatibility does not mandate the legacy casing; document any unavoidable Lua-bound exceptions and the alternative mechanisms evaluated.
       - ✅ Renamed `XmlWriter_Extensions.cs` → `XmlWriterExtensions.cs` and `release_readme.txt` → `ReleaseReadme.txt`, leaving `_Hardwired.cs` and Lua assets as intentional holdouts.
     - Identify legacy/generated files needing suppression tags or sub-config.
6. **Unity/Packaging Alignment**
   - ✅ Updated `docs/UnityIntegration.md` with a netstandard2.1-focused quick-start walkthrough (publish → copy → wire in Unity) plus sample bootstrap code.
   - ✅ Audited tooling TFMs: runtime/debugger libraries stay on `netstandard2.1` while the CLI now targets `net8.0` so it can run as a dotnet tool/hosted executable.
   - ✅ Completed doc audit; active guides now reference `netstandard2.1`/`net8.0` (see `docs/Modernization.md`), with the archived Sandcastle project noted as historical .NET Framework content.
   - 🔜 Automate Unity packaging via CI/CD: build `.unitypackage`/UPM tarballs containing all NovaSharp binaries (and dependencies) and publish artifacts on every tagged release.
   - 🔜 Ship full Unity integration scripts: provide tooling that copies DLLs into `Plugins` (hash-aware), configures import settings, and enables analyzers—use Wallstop Studios’ DxMessaging pipeline as inspiration.
   - 🔜 Produce comprehensive Unity samples under `Samples~` demonstrating interpreter bootstrapping, modding hooks, CLI usage, and recommended project structure.
7. **Debugger Smoke Tests**
   - Author lightweight harnesses to drive VS Code and remote debuggers (attach, breakpoint, variable inspection).
   - Gate behind opt-in flag while stabilising.
8. **Cross-Platform CI**
   - ✅ Added `dotnet-tests-windows` lane in `tests.yml` to mirror the Linux run and upload TRX artifacts; macOS lane still pending once Unity docs ship.
   - Consider nightly runs enabling IO/OS TAP suites on trusted hardware.
9. **Benchmark Governance**
   - ✅ Documented the local execution workflow, thresholds, and reporting cadence in `docs/Performance.md#Benchmark Governance`, and seeded the archival process with `docs/performance-history/README.md`.
   - ✅ Captured the November 10 benchmark sweep (runtime, script loading, NLua comparison), archived it at `docs/performance-history/2025-11-10.md`, and refreshed `docs/Performance.md` with the latest deltas and environment metadata.
10. **Roslyn Hardwire Replacement**
   - Design and implement a Roslyn source generator/analyzer that emits the hardwired userdata descriptors at compile time (NetStandard-compatible).
   - Integrate generator into the interpreter build and Unity pipeline; ensure output matches or exceeds existing hardwire coverage via new unit tests.
   - After generator parity is verified, delete the legacy `NovaSharp.Hardwire` tooling project and remove `_Hardwired.cs` artefacts.
   - 🔄 Drafted `docs/proposals/roslyn-hardwire-generator.md` outlining generator goals, architecture, diagnostics, integration phases, and risks to guide implementation.
   - ✅ Augmented the proposal with a reflection-free interop scaffolding study: `docs/proposals/roslyn-hardwire-generator.md` now outlines hotspots and a phased plan for generator-emitted delegates that replace the existing reflection helpers.
11. **Performance Optimization Campaign**
   - Instrument allocation and timing baselines for runtime, CLI, and debugger pipelines using BenchmarkDotNet + EventCounters; publish the before/after data alongside coverage dashboards.
   - Replace high-traffic LINQ/query expressions in production code with allocation-free loops once equivalent tests guard behaviour (focus areas: `Execution`, `DataTypes`, `Interop`).
     - ✅ Replaced `Table.Pairs/Keys/Values` LINQ projections with custom iterators to eliminate per-enumeration allocations in hot table traversal paths; `TableTests` remain green.
     - ✅ Dropped LINQ usage from `StandardUserDataDescriptor` member discovery by replacing `OfType/Select/Any` with manual loops to reduce reflection-time allocations for userdata registration.
   - Profile per-request allocations in critical interpreters (metatables, dynamic expressions, coroutine resumption) and eliminate hot allocations via pooling or struct spans while keeping APIs intact.
   - ✅ Add allocation guardrails: schedule an investigation into object pooling / buffering strategies inspired by UnityHelpers’ `Buffers` (and similar libraries) to ensure new allocations only occur when absolutely necessary. Scope includes:
     - Evaluate struct-based API variants (e.g., `TryGetX(out ValueListBuilder<T> builder)` or list-populating overloads) alongside existing `IEnumerable` returns so callers can reuse buffers.
     - Prototype value-based `IDisposable` helpers that avoid boxing while remaining assignment-compatible (`IDisposable result = ...`).
     - Catalogue candidate hotspots (table enumeration, descriptor generation, coroutine stacks) and capture findings/tests before rollout.
     - ✅ Captured the initial study in `docs/performance/object-pooling-study.md`, listing hotspots, proposed buffer-based APIs, and next steps for instrumentation.
   - Investigate introducing generic variants for heavily-used helpers to eliminate boxing of interfaces/value types (e.g., `IEnumerator`, `IUserDataType` call-sites); capture candidates, prototype generic alternatives, and benchmark/alloc-diff results before rollout.
   - Add regression tests/benchmarks that fail when allocations or median timings exceed established thresholds so the gains remain enforced in CI.
   - Evaluate value-type substitutions: audit frequently allocated helper classes/struct-like containers and prototype replacing them with `struct` equivalents (when safe) to reduce GC pressure; include benchmarks + safety tests (copy semantics, mutation) before adoption.
12. **Debugger Orchestration Library (Low Priority)**
   - Evaluate extracting common remote-debugger session management into a shared `NovaSharp.Debugger.Core` library consumed by CLI, VS Code, Cursor, Visual Studio (2022/2026), and Rider front ends.
   - Inventory protocol overlaps (transport, attach semantics, UI-hooks) across existing adapters and spike shared abstractions for launch, attach, and breakpoint workflows.
   - Track incremental adoption roadmap in docs, ensuring each host keeps the current UX while reusing the shared core.
13. **Shared Core Modules Feasibility (Low Priority)**
    - Explore factoring shared runtime tooling (benchmark writers, CLI helpers, debugger utilities) into reusable libraries instead of relying on `InternalsVisibleTo`.
    - Audit existing cross-project dependencies to identify candidates for a `NovaSharp.Core.Tooling` / `NovaSharp.Core.Debug` module boundary.
    - Prototype a small migration (e.g., benchmark utilities) to validate packaging, versioning, and breaking-change surface.

14. **Lua 5.4 Compatibility Expansion**
15. **Lua Spec Conformance Harness**
    - ✅ Inventory the upstream Lua 5.4 features we still miss (e.g., `goto` semantics, new library APIs, weak tables, to-be-closed variables) and track them in a parity matrix (`docs/LuaCompatibility.md`).
    - Investigate implementing weak tables/ephemeron behaviour in `Table` + GC to match Lua 5.4; validate via new regression suites and memory stress tests.
    - Evaluate the impact of Lua 5.4 VM/runtime changes (metamethod tweaks, math library updates, `debug` additions) and scope incremental delivery plans with benchmarks to ensure no regressions.
    - Catalog the Lua versions we plan to support (5.2–5.5 plus “Latest”) and map every manual section (core language, standard libraries, appendix) to spec test coverage targets tracked alongside the parity matrix.
    - ✅ Captured an initial coverage ledger in `docs/testing/spec-coverage.md`, outlining version targets, manual sections, and current status indicators.
    - 🔄 Added the first Lua 5.4 spec harness suite (`Lua54StringSpecTests`) covering canonical string library examples (byte/char ranges, pattern-free helpers); expand to additional libraries next.
    - Stand up a conformance harness that executes official manual examples, edge cases, and error scenarios, storing fixtures beside the NUnit TAP corpus for reuse across interpreter builds.
    - For each module (`StringModule`, `MathModule`, coroutine APIs, etc.), enumerate edge/error behaviours from the spec and add exhaustive NUnit coverage so we rely on canonical Lua semantics rather than legacy MoonSharp behaviour.
    - Integrate the harness into CI, fail builds on spec regressions, and capture progress/remaining work in `docs/testing/spec-coverage.md` with links back to manual sections.

## Long-Horizon Ideas
- Property/fuzz testing for lexer/parser and VM instruction boundaries.
- Golden-file assertions for debugger protocol payloads and CLI output.
- Native AOT / trimming validation once runtime stack is fully nullable-clean.
- Automated regression harness for memory allocations using BenchmarkDotNet diagnosers.

Keep this plan in sync with `docs/Testing.md` and `docs/Modernization.md`. Update after each milestone so contributors know what's complete, in-flight, and still open.
