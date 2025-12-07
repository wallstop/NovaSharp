# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-06 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **3,287** interpreter tests + **72** debugger tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads (initialize, threads, breakpoints, events, evaluate, scopes, stackTrace, variables).
- **Sandbox infrastructure**: `SandboxOptions` with instruction limits, recursion limits, module/function restrictions, callbacks, and presets.
- **Benchmark CI**: `.github/workflows/benchmarks.yml` with BenchmarkDotNet, threshold-based regression alerting (115% = 15% regression), and historical tracking in `gh-pages`.
- **Namespace rebrand**: ✅ **Completed 2025-12-06** — Full rebrand to `WallstopStudios.NovaSharp.*` namespaces across all projects.

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage ceiling (informational)
Coverage has reached a practical ceiling. The remaining ~1.3% gap to 95% branch coverage is blocked by untestable code:
- **DebugModule** (~75 branches): REPL loop cannot be tested (VM state issue).
- **StreamFileUserDataBase** (~27 branches): Windows-specific CRLF paths cannot run on Linux CI.
- **TailCallData/YieldRequest** (~10 branches each): Internal processor paths not directly testable.
- **ScriptExecutionContext** (~30 branches): Internal processor callback/continuation paths.

No further coverage work planned unless these blockers are addressed.

### 2. Codebase organization & namespace hygiene
- ✅ **Completed (2025-12-06)**: Full namespace rebrand to `WallstopStudios.NovaSharp.*`.
- **Scope**: 648 C# files across all projects rebranded.
- **Changes made**:
  - All namespace declarations changed from `NovaSharp.*` to `WallstopStudios.NovaSharp.*`
  - All using statements updated
  - All project directories renamed (e.g., `NovaSharp.Interpreter` → `WallstopStudios.NovaSharp.Interpreter`)
  - All csproj files renamed
  - All project references updated in csproj files
  - Solution file updated with new project paths
  - AssemblyInfo files updated for version references
  - InternalsVisibleTo attributes updated
  - Package IDs changed to `com.wallstop-studios.novasharp.*` format
  - RootNamespace properties set in csproj files
  - Hardwire generator ManagedType strings updated
  - Test fixture paths updated
  - Scripts and documentation updated
  - **Fixed (2025-12-06)**: Embedded resource path in `DebugWebHost.cs` updated for namespace rebrand
  - **Fixed (2025-12-06)**: Test project relative paths in `RemoteDebugger.Tests.TUnit.csproj` updated
  - **Fixed (2025-12-06)**: Hardcoded assembly names in `RemoteDebuggerServiceTUnitTests.cs` and `TapRunnerTUnit.cs` updated
  - **Fixed (2025-12-06)**: `GoldenPayloadHelper.cs` fallback path updated for new project names
- **Projects renamed**:
  - `WallstopStudios.NovaSharp.Interpreter` (runtime)
  - `WallstopStudios.NovaSharp.Interpreter.Infrastructure` (runtime)
  - `WallstopStudios.NovaSharp.RemoteDebugger` (debugger)
  - `WallstopStudios.NovaSharp.VsCodeDebugger` (debugger)
  - `WallstopStudios.NovaSharp.Cli` (tooling)
  - `WallstopStudios.NovaSharp.Hardwire` (tooling)
  - `WallstopStudios.NovaSharp.Benchmarks` (tooling)
  - `WallstopStudios.NovaSharp.Comparison` (tooling)
  - `WallstopStudios.NovaSharp.LuaBatchRunner` (tooling)
  - `WallstopStudios.NovaSharp.Interpreter.Tests.TUnit` (tests)
  - `WallstopStudios.NovaSharp.Interpreter.Tests` (test fixtures)
  - `WallstopStudios.NovaSharp.RemoteDebugger.Tests.TUnit` (tests)
- **Remaining**:
  - Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
  - Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
  - Add guardrails so new code lands in correct folders with consistent namespaces

### 3. Debugger DAP testing
- **Current**: **72 TUnit tests** in `NovaSharp.RemoteDebugger.Tests.TUnit/` covering handshake, breakpoints, stepping, watches, expressions, VS Code server lifecycle, **and golden payload validation**.
- **Completed (2025-12-06)**:
  - Added `GoldenPayloads/` directory with reference JSON files for initialize, threads, setBreakpoints, initialized event, evaluate, scopes, stackTrace, and variables responses.
  - Created `GoldenPayloadHelper.cs` with JSON comparison utilities supporting case-insensitive property matching, semantic normalization, and configurable ignored properties (sequence numbers).
  - Added `VsCodeDebugSessionGoldenTUnitTests.cs` with 20 tests validating DAP protocol responses against golden files:
    - Initialize response capabilities verification
    - Threads response structure validation
    - SetBreakpoints response with verified breakpoint entries
    - Initialized event emission
    - Evaluate response for number, nil, boolean, and function types
    - Multi-breakpoint verification
    - Scopes response with Locals and Self scope entries
    - StackTrace response structure (empty when not paused)
    - Variables response for Locals, Self, and invalid references
    - Scopes variablesReference constants (65536 Locals, 65537 Self)
  - All tests use actual DAP protocol fixtures to ensure wire-format correctness.
  - **Fixed (2025-12-06)**: JSON serialization bug where empty arrays serialized as `{}` instead of `[]`. Root cause was `JsonTableConverter.ObjectToJson` using `table.Length == 0` heuristic. Fixed by rewriting `ObjectToJson` to directly serialize CLR objects to JSON, preserving collection/array semantics correctly.
- **Remaining**: None for golden payload validation. Consider CLI output golden tests as future enhancement.

### 4. Runtime safety, sandboxing, and determinism
- ✅ **Completed (2025-12-06)**: Sandbox infrastructure implemented with:
  - `SandboxOptions` class with instruction limits, call stack depth limits, module/function restrictions
  - `SandboxViolationException` with typed `SandboxViolationType` enum
  - Integration with `ScriptOptions.Sandbox` property
  - Instruction counting in VM `ProcessingLoop` with callback support
  - Call stack depth checking in `InternalExecCall`
  - Function access checks for `load`, `loadfile`, `dofile`
  - Module access checks for `require`
  - Preset factories: `CreateRestrictive()` and `CreateModerate()`
  - 39 TUnit tests covering all sandbox features
- **Remaining**:
  - Memory tracking (per-allocation accounting)
  - Deterministic execution mode for lockstep multiplayer/replays
  - Per-mod isolation containers with load/reload/unload hooks
  - Coroutine count limits

### 5. Packaging and performance
- Unity UPM/embedded packaging with IL2CPP/AOT documentation.
- NuGet package pipeline with versioning/signatures.
- ✅ **Performance regression CI** (Completed 2025-12-06): BenchmarkDotNet runs in `.github/workflows/benchmarks.yml` with:
  - Automatic runs on master pushes and PRs touching runtime/benchmark code
  - Threshold-based alerting (115% = 15% regression by default)
  - PR comments when regressions detected
  - Historical tracking in `gh-pages` branch via `benchmark-action/github-action-benchmark`
  - Bash (`run-benchmarks.sh`) and PowerShell (`run-benchmarks.ps1`) helper scripts
- ✅ **Interpreter hot-path optimization - Phase 1** (Completed 2025-12-06): Zero-allocation strategies for DynValue:
  - Added `DynValue.FromBoolean(bool)` - returns cached `True`/`False` instances instead of allocating
  - Added `DynValue.FromNumber(double)` - returns cached instances for small integers (0-255)
  - Added small integer cache (256 readonly DynValue instances for array indices)
  - Updated VM hot paths to use cached values:
    - `ToBool` opcode uses `FromBoolean`
    - `ExecNot` and `ExecCNot` use `FromBoolean`
    - Binary comparison operators use `FromBoolean`
    - Unary `not` operator uses `FromBoolean`
    - `GetStoreValue` uses `DynValue.Nil` instead of allocating
    - `ExecArgs` uses `DynValue.Nil` instead of allocating
  - Updated CoreLib modules to use cached values:
    - `rawequal`, `math.ult`, `bit32.btest`, `json.isnull`, `coroutine.isyieldable`, `coroutine.running`, `pcall`/`xpcall` error handlers
  - All 3,287 tests passing; BenchmarkDotNet validates no regressions
- **Baseline captured (2025-12-06)**: Container baseline saved to `docs/performance-history/container-baseline-2025-12-06/`:
  - ScriptLoadingBenchmarks: Tiny Execute = 148 ns (696 B), Large Execute = 987 ms (3.5 GB)
  - RuntimeBenchmarks: NumericLoops = 195 ns, TableMutation = 5.24 µs, CoroutinePipeline = 278 ns, UserDataInterop = 374 ns
  - Environment: Intel Core Ultra 9 285K, .NET 8.0.22, ShortRun job (10 iterations)
- **Remaining optimization opportunities**:
  - Object pooling for Closure, Table instances
  - String interning for frequently used strings
  - Span-based parsing to reduce string allocations

### 6. Tooling, docs, and contributor experience
- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.

### 7. Concurrency improvements (optional)
- Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics.
- Split debugger locks for reduced contention.
- Add timeout to `BlockingChannel`.

See `docs/modernization/concurrency-inventory.md` for the full synchronization audit.

## Lua Specification Parity

### Reference Lua comparison harness
- **Status**: Fully implemented. CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4.
- **Gating**: ✅ Now in `enforce` mode (2025-12-06). Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory. See `StringModuleFixtureBasedTUnitTests.cs` for examples.

Key infrastructure:
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/` – 855 Lua fixtures with metadata headers
- `src/tests/TestInfrastructure/LuaFixtures/LuaFixtureHelper.cs` – Test helper for loading fixtures
- `src/tooling/NovaSharp.LuaBatchRunner/` – Batch execution tool (32s for 830 files)
- `scripts/tests/run-lua-fixtures-fast.sh` – Multi-version fixture runner
- `scripts/tests/compare-lua-outputs.py` – Diff engine with semantic normalization and divergence allowlist
- `docs/testing/lua-comparison-harness.md` – Contributor guide
- `docs/testing/lua-divergences.md` – Known divergence catalog

### Full Lua specification audit
- **Tracking**: `docs/testing/spec-audit.md` contains detailed tracking table with status per feature.
- **Progress**: Most core features verified against Lua 5.4 manual; `string.pack`/`unpack` extended options remain unimplemented.

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- ~~Golden-file assertions for debugger payloads~~ (completed 2025-12-06) and CLI output.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

## Recommended Next Steps (Priority Order)

### Completed Items

1. ~~**DAP golden payload tests**~~ (Initiative 3): ✅ **Completed 2025-12-06** — 20 golden payload tests validating initialize, threads, breakpoints, events, evaluate, scopes, stackTrace, and variables responses.

2. ~~**Runtime sandboxing profiles**~~ (Initiative 4): ✅ **Completed 2025-12-06** — Comprehensive sandbox infrastructure with instruction/recursion limits, module/function restrictions, presets, and 39 tests.

3. ~~**Lua comparison gating**~~: ✅ **Completed 2025-12-06** — CI now enforces Lua comparison with 23 documented divergences.

4. ~~**Namespace restructuring - Full rebrand**~~ (Initiative 2): ✅ **Completed 2025-12-06** — Full `WallstopStudios.NovaSharp.*` namespace rebrand:
   - All 648 C# files updated
   - All projects renamed and references updated
   - Solution file updated
   - Scripts and documentation updated
   - All 3,287 tests passing
   - **Improved (2025-12-06)**: Replaced hardcoded namespace strings with `typeof().FullName` in Hardwire generators for compile-time safety

5. ~~**Performance regression CI**~~ (Initiative 5): ✅ **Completed 2025-12-06** — BenchmarkDotNet workflow with threshold-based alerting and historical tracking.

6. ~~**Interpreter hot-path optimization - Phase 1**~~ (Initiative 5): ✅ **Completed 2025-12-06** — Zero-allocation DynValue caching:
   - Added `FromBoolean` and `FromNumber` static helpers returning cached instances
   - Small integer cache for 0-255 (common Lua array indices)
   - Updated VM opcodes and CoreLib modules to use cached values
   - Baseline captured to `docs/performance-history/container-baseline-2025-12-06/`
   - All 3,287 tests passing

7. ~~**Interpreter hot-path optimization - Phase 2**~~ (Initiative 5): ✅ **Completed 2025-12-06** — Allocation reduction infrastructure:
   - Added `DynValueArrayPool` for pooling DynValue[] arrays (common in function calls)
   - Added `StringBuilderPool` for pooling StringBuilder instances (used heavily in lexer)
   - Removed LINQ `.Last()` calls in `BuildTimeScope` (replaced with cached `CurrentFrame` property)
   - Removed LINQ `.Select()` + `string.Join()` in `DynValue.ToPrintString/ToString` (manual loop with pooled StringBuilder)
   - Removed LINQ `.Skip().ToArray()` in `Coroutine.GetStackTrace` (manual array copy)
   - Updated Lexer to use `StringBuilderPool` for all token building (ReadLongString, ReadNumberToken, ReadHashBang, ReadComment, ReadSimpleStringToken, ReadNameToken)
   - Created comprehensive optimization opportunities document: `docs/performance/optimization-opportunities.md`
   - All 3,278 tests passing

8. ~~**Interpreter hot-path optimization - Phase 2.5**~~ (Initiative 5): ✅ **Completed 2025-12-06** — ZString integration and PooledResource pattern:
   - Added Cysharp ZString 2.6.0 NuGet package for zero-allocation string operations
   - Created `ZStringBuilder` wrapper utilities (`Create`, `CreateNested`, `CreateUtf8`, `Concat`, `Format`, `Join`)
   - Created `PooledResource<T>` struct following Unity Helper's IDisposable pattern for automatic pool return
   - Updated `DynValueArrayPool` with `Get(int, out T[])` method returning `PooledResource<T>`
   - Updated `DynValue.JoinTupleStrings` to use ZString's `Utf16ValueStringBuilder`
   - All 3,278 tests passing

9. ~~**Interpreter hot-path optimization - Phase 2.6**~~ (Initiative 5): ✅ **Completed 2025-12-07** — Pooled collections and string optimizations:
   - Added `GenericPool<T>` thread-safe pool following Unity Helpers pattern
   - Added `ListPool<T>`, `HashSetPool<T>`, `DictionaryPool<TK,TV>`, `StackPool<T>`, `QueuePool<T>`
   - Updated `FileUserDataBase.Read()` to use `ListPool<DynValue>` with `ToExactArray()` helper
   - Updated `DynValue.ToString()` to use `ZString.Concat()` for string quoting
   - Updated `SerializationExtensions.EscapeString()` with fast-path check and single-pass ZString builder
   - Updated `Instruction.PurifyFromNewLines()` with short-circuit check and ZString builder
   - Added `TrimLineEnding()` helper with short-circuit to avoid TrimEnd allocations
   - Created comprehensive audit document: `docs/performance/pooling-and-allocation-audit-2025-12.md`
   - All 3,325 tests passing

### Active/Upcoming Items

10. **Advanced sandbox features** (Initiative 4):
   - Memory tracking (per-allocation accounting)
   - Deterministic execution mode for lockstep multiplayer/replays
   - Per-mod isolation containers with load/reload/unload hooks
   - Coroutine count limits

11. **Packaging** (Initiative 5):
    - Unity UPM/embedded packaging with IL2CPP/AOT documentation
    - NuGet package pipeline with versioning/signatures

12. **Tooling enhancements** (Initiative 6):
    - Roslyn source generators/analyzers for NovaSharp descriptors
    - DocFX (or similar) for API documentation
    - CLI output golden tests

13. **Interpreter hot-path optimization - Phase 3** (Initiative 5):
    - Use `DynValueArrayPool` in `ProcessorUtilityFunctions` for `StackTopToArray` methods
    - Use `DynValueArrayPool` in `ProcessorInstructionLoop` for function call arguments
    - Pool `CallStackItem` collections (`BlocksToClose`, `ToBeClosedIndices`)
    - Pool `BuildTimeScopeBlock` collections during parsing
    - Expand ZString usage to Lexer (complex control flow with exceptions needs careful handling)
    - Object pooling for Closure, Table instances
    - String interning for frequently used strings

14. **Concurrency improvements** (Initiative 7, optional):
    - Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics
    - Split debugger locks for reduced contention
    - Add timeout to `BlockingChannel`

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
