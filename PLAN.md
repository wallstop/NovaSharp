# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-08 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **3,526** interpreter tests + **72** debugger tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads (initialize, threads, breakpoints, events, evaluate, scopes, stackTrace, variables).
- **Sandbox infrastructure**: `SandboxOptions` with instruction limits, recursion limits, module/function restrictions, **memory limits** (Table/Closure/Coroutine tracking), **coroutine count limits**, callbacks, and presets.
- **Benchmark CI**: `.github/workflows/benchmarks.yml` with BenchmarkDotNet, threshold-based regression alerting (115% = 15% regression), and historical tracking in `gh-pages`.
- **Namespace rebrand**: ✅ **Completed 2025-12-06** — Full rebrand to `WallstopStudios.NovaSharp.*` namespaces across all projects.
- **Packaging**: ✅ **Completed 2025-12-07** — NuGet publishing workflow (`.github/workflows/nuget-publish.yml`) + Unity UPM scripts (`scripts/packaging/`).

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
- ✅ **Memory tracking (2025-12-07)**: Per-allocation accounting implemented:
  - `AllocationTracker` class with thread-safe counters (current, peak, total allocated/freed)
  - `Script.AllocationTracker` property (created when `SandboxOptions.MaxMemoryBytes > 0`)
  - `Table` allocation tracking (base overhead + per-entry overhead)
  - `Closure` allocation tracking (base overhead + per-upvalue overhead)
  - `Coroutine` allocation tracking (base overhead)
  - VM instruction loop periodic memory limit enforcement (every 1024 instructions)
  - `OnMemoryLimitExceeded` callback support for graceful handling
  - 50 TUnit tests covering tracker and integration (36 unit + 14 integration)
- ✅ **Deterministic execution mode (2025-12-08)**: For lockstep multiplayer/replays:
  - `IRandomProvider` interface for random number abstraction (`NextInt`, `NextDouble`, `SetSeed`)
  - `SystemRandomProvider` - production implementation with cryptographic seeding
  - `DeterministicRandomProvider` - seeded implementation for reproducible sequences
  - `DeterministicTimeProvider` - controllable time for reproducible os.time/os.clock
  - `ScriptOptions.RandomProvider` property for per-script random source injection
  - `Script.RandomProvider` property exposing the configured provider
  - `MathModule.Random` and `MathModule.RandomSeed` updated to use provider
  - 27 TUnit tests covering provider infrastructure and Script integration
- **Remaining**:
  - Per-mod isolation containers with load/reload/unload hooks

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
- **Hyper-optimization roadmap**: See **Phase 4** in "Recommended Next Steps" section for comprehensive plan covering:
  - VM dispatch optimization (computed goto, opcode fusion)
  - Table redesign (hybrid array+hash like native Lua)
  - DynValue struct conversion (optional breaking change)
  - Span-based APIs throughout
  - Roslyn source generators for interop
  - Success metrics: <100 bytes/call allocation, match/beat native Lua performance

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

10. **Advanced sandbox features** (Initiative 4) — Remaining items:
   - ~~Memory tracking (per-allocation accounting)~~ ✅ **Completed 2025-12-07** (Phase 3 - Extended tracking)
   - ~~Coroutine count limits~~ ✅ **Completed 2025-12-07** (Phase 4)
   - ~~Deterministic execution mode for lockstep multiplayer/replays~~ ✅ **Completed 2025-12-08** (See Phase 5 below)
   - Per-mod isolation containers with load/reload/unload hooks

11. ~~**Packaging** (Initiative 5)~~ ✅ **Completed 2025-12-07** — NuGet and Unity packaging infrastructure:
    - ✅ NuGet package metadata (Authors, License, SourceLink, symbols)
    - ✅ GitHub workflow for automated NuGet publishing (`.github/workflows/nuget-publish.yml`)
    - ✅ Unity UPM package builder scripts (`scripts/packaging/build-unity-package.sh`, `.ps1`)
    - ✅ Package IDs updated to `WallstopStudios.NovaSharp.*` format
    - ✅ Version bumped to 3.0.0 (major version for namespace rebrand)
    - See `scripts/packaging/README.md` for usage documentation

12. **Tooling enhancements** (Initiative 6):
    - Roslyn source generators/analyzers for NovaSharp descriptors
    - DocFX (or similar) for API documentation
    - CLI output golden tests

13. ~~**Interpreter hot-path optimization - Phase 3**~~ (Initiative 5): ✅ **Completed 2025-12-07** — VM hot-path pooling:
    
    **Performance Gap Analysis (vs MoonSharp baseline):**
    | Scenario | NovaSharp | MoonSharp | Delta | Notes |
    |----------|-----------|-----------|-------|-------|
    | TableMutation | 5.234 µs | 4.205 µs | +24% | Table structure overhead |
    | UserDataInterop | 357 ns | 295 ns | +21% | Reflection/descriptor overhead |
    | Execute Large | 935 ms | 746 ms | +25% | Cumulative allocation pressure |
    | Execute Medium | 39.9 ms | 35.6 ms | +12% | Array allocations in VM loop |
    | NumericLoops | 178 ns | 195 ns | **-9%** | ✅ NovaSharp faster |
    | CoroutinePipeline | 259 ns | 247 ns | +5% | Near parity |
    
    **Root Causes Identified:**
    1. **VM Loop Array Allocations** - `ProcessorInstructionLoop.cs` allocates `new DynValue[]` frequently
    2. **`CallStackItem` Collections** - Every function call potentially allocates collections
    3. **Table Implementation** - `LinkedList<TablePair>` + three Dictionary indexes causes fragmented memory
    4. **Interop Overhead** - `StandardUserDataDescriptor` reflection discovery on type registration
    
    **Implementation Completed:**
    - [x] Use `DynValueArrayPool` in `ProcessorUtilityFunctions.StackTopToArray/StackTopToArrayReverse`
    - [x] Use `DynValueArrayPool` in `ProcessorInstructionLoop` for:
      - Error handler args (line 436)
      - `PerformMessageDecorationBeforeUnwind` args (line 484)
      - `__call` metamethod args (line 1246)
      - `PerformTco` temp array (line 1277)
    - [x] Add `CallStackItemPool` for reusing call frame objects
    - [x] Add `ObjectArrayPool` for reflection invocation arrays (existed)
    - [x] Add `CommunityToolkit.HighPerformance` 8.2.2 package
    - [x] Add `Microsoft.Extensions.ObjectPool` 8.0.0 package
    - [x] Implement `LuaStringPool` for interning variable names and table keys
    - [x] Added `StackTopToArrayPooled` and `StackTopToArrayReversePooled` variants returning `PooledResource<T>`
    - [x] All 3,459 tests passing
    
    **Arrays that cannot be pooled (stored/escape scope):**
    - Varargs in `ExecArgs` - stored in `DynValue.NewTuple()` and persist
    - `BlocksToClose` lists - persist across multiple frames for cleanup
    
    **Remaining (Future Phases):**
    - Span-based Table access methods (`Get(int)`, `Get(ReadOnlySpan<char>)`)
    - Buffer-populating Table methods (`FillPairs`, `FillKeys`)

14. **Interpreter hyper-optimization - Phase 4** (Initiative 5): 🔮 **PLANNED** — Zero-allocation runtime goal
    
    **Target:** Match or exceed native Lua performance; achieve <100 bytes/call allocation overhead.
    
    #### 4.1 VM Core Optimizations
    
    **Instruction dispatch:**
    - [ ] Replace `switch` dispatch with computed goto via function pointer table (IL emit or `Unsafe.Add`)
    - [ ] Inline hot opcodes (ADD, MUL, MOVE, GETTABLE, SETTABLE) to eliminate call overhead
    - [ ] Add opcode fusion: detect patterns like `GETTABLE+CALL` and execute as single fused op
    - [ ] Consider `ref struct` locals for instruction decoding to avoid copying
    
    **Stack operations:**
    - [ ] Replace `FastStack<DynValue>` with `Span<DynValue>` window over pre-allocated array
    - [ ] Add stack frame regions with compile-time slot offsets (eliminates dictionary lookups)
    - [ ] Implement register-based VM variant for tight numeric loops (optional path)
    
    **Call frame management:**
    - [ ] Integrate `CallStackItemPool` into actual `ExecutionStack.Push`/`Pop` operations
    - [ ] Pre-allocate `BlocksToClose` list capacity based on function analysis
    - [ ] Use `ArrayPool<T>` for `LocalScope` arrays instead of fresh allocations
    
    #### 4.2 Table Implementation Redesign
    
    **Hybrid array+hash structure (like native Lua):**
    - [ ] Contiguous array part for integer keys 1..n (cache-friendly sequential access)
    - [ ] Hash part only for string keys and sparse integers
    - [ ] Dynamic boundary tracking to minimize hash lookups
    - [ ] `Span<DynValue>` access for array part iteration
    
    **Memory layout:**
    - [ ] Flatten `LinkedList<TablePair>` into single contiguous buffer
    - [ ] Remove redundant `_arrayMap`, `_stringMap`, `_objectMap` dictionaries
    - [ ] Use open addressing with robin hood hashing for hash part
    - [ ] Consider struct-of-arrays layout for better cache utilization
    
    **Span-based APIs:**
    - [ ] `ReadOnlySpan<DynValue> GetArrayPart()` - zero-copy array access
    - [ ] `bool TryGet(ReadOnlySpan<char> key, out DynValue value)` - avoid string allocation for lookups
    - [ ] `void FillKeys(Span<DynValue> buffer)` / `FillValues` - buffer-populating enumeration
    - [ ] `ref DynValue GetRef(int index)` - direct reference for mutation without boxing
    
    #### 4.3 String Handling
    
    **Interning and pooling:**
    - [ ] Integrate `LuaStringPool` into Lexer for all identifiers and string literals
    - [ ] Use `StringPool` from `CommunityToolkit.HighPerformance` for runtime string creation
    - [ ] Implement rope-based string concatenation for `..` operator (defer allocation)
    - [ ] Add `ReadOnlyMemory<char>` path through string operations where possible
    
    **ZString integration:**
    - [ ] Replace all `string.Format` in hot paths with `ZString.Format`
    - [ ] Use `Utf8ValueStringBuilder` for error message construction
    - [ ] Add `ZString.Concat` for metamethod `__tostring` results
    
    #### 4.4 DynValue Architecture
    
    **Struct-based DynValue (breaking change, optional):**
    - [ ] Convert `DynValue` from class to `readonly struct` (eliminates heap allocation per value)
    - [ ] Use discriminated union pattern: `DataType` enum + overlapped value storage
    - [ ] Store small strings inline (≤16 chars) using `InlineArray` or fixed buffer
    - [ ] Reference types stored as indices into per-Script object tables
    
    **Non-breaking improvements:**
    - [ ] Expand small integer cache to 0-1023 (covers most Lua array indices)
    - [ ] Add `DynValue.Void` / `DynValue.True` / `DynValue.False` static caches (done in Phase 1)
    - [ ] Pool `DynValue[]` tuples of common sizes (1, 2, 3, 4 elements)
    - [ ] Add `DynValue.TryGetNumber(out double)` to avoid boxing in type checks
    
    #### 4.5 Interop Performance
    
    **Descriptor caching:**
    - [ ] Pre-generate descriptors at compile time via Roslyn source generator
    - [ ] Cache method dispatch delegates instead of `MethodInfo.Invoke`
    - [ ] Use `Delegate.CreateDelegate` for direct invocation without reflection
    - [ ] Add `Expression.Compile` path for complex parameter binding
    
    **Parameter marshaling:**
    - [ ] Use `Span<object>` for reflection parameter arrays (with `ObjectArrayPool`)
    - [ ] Add fast-path for common signatures: `(DynValue)`, `(DynValue, DynValue)`, `(Script)`
    - [ ] Generate specialized invokers for registered types via IL emit
    
    #### 4.6 Memory and GC Optimization
    
    **Allocation tracking:**
    - [ ] Wire `AllocationTracker` into `Script` (per-script memory accounting)
    - [ ] Add allocation recording to `Table`, `Closure`, `DynValue.NewString`
    - [ ] Periodic GC pressure check in VM loop with configurable threshold
    
    **Object lifetime:**
    - [ ] Implement weak reference table (`__mode = "kv"`) with `ConditionalWeakTable`
    - [ ] Add explicit disposal for `Script` to return all pooled resources
    - [ ] Consider `IMemoryOwner<T>` pattern for large temporary buffers
    
    **Native memory (advanced):**
    - [ ] `NativeMemory.Alloc` for large Table backing stores (bypass GC)
    - [ ] `MemoryMarshal.Cast` for binary data manipulation
    - [ ] Pinned object heap for interop buffers
    
    #### 4.7 Modern .NET Features (conditional compilation)
    
    **When targeting .NET 8+:**
    - [ ] Use `FrozenDictionary<K,V>` for static lookup tables (opcodes, metamethods)
    - [ ] Use `SearchValues<T>` for character class matching in Lexer
    - [ ] Use `CompositeFormat` for pre-parsed format strings
    - [ ] Enable `[SkipLocalsInit]` on hot methods to avoid zero-initialization
    
    **When targeting .NET 9+:**
    - [ ] Use `System.Threading.Lock` for cleaner lock semantics
    - [ ] Use `Tensor<T>` for numeric array operations (if applicable)
    
    #### 4.8 Third-Party Libraries (Unity-compatible, MIT)
    
    Already integrated:
    - `CommunityToolkit.HighPerformance` 8.2.2 - `StringPool`, `MemoryOwner`, `SpanOwner`, `Ref<T>`
    - `Microsoft.Extensions.ObjectPool` 8.0.0 - Complex object pooling
    - `Cysharp.ZString` 2.6.0 - Zero-allocation string building
    
    Candidates for integration:
    - [ ] `RecyclableMemoryStream` - Microsoft's pooled MemoryStream for I/O operations
    - [ ] `NetFabric.Hyperlinq` - Zero-allocation LINQ-like operations (enumeration without allocation)
    - [ ] `Utf8StringInterpolation` - UTF-8 interpolated string handler
    
    #### 4.9 Benchmarking Infrastructure
    
    - [ ] Add micro-benchmarks for each optimization (before/after)
    - [ ] Add allocation regression tests using `dotnet-counters` or `BenchmarkDotNet` memory diagnoser
    - [ ] Create "allocation budget" tests that fail if a scenario exceeds N bytes
    - [ ] Add flame graph generation for profiling instruction-level hotspots
    
    #### 4.10 Success Metrics
    
    | Metric | Current | Target | Notes |
    |--------|---------|--------|-------|
    | NumericLoops | 178 ns | 150 ns | Already faster than MoonSharp |
    | TableMutation | 5.2 µs | 3.5 µs | Match or beat MoonSharp |
    | UserDataInterop | 357 ns | 250 ns | Delegate caching |
    | Execute Large | 935 ms | 700 ms | Cumulative gains |
    | Alloc/call | ~500 B | <100 B | Zero-alloc hot paths |
    | GC Gen0/sec | ~50 | <10 | Reduced GC pressure |

15. **Concurrency improvements** (Initiative 7, optional):
    - Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics
    - Split debugger locks for reduced contention
    - Add timeout to `BlockingChannel`

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.

## Checkpoint — 2025-12-07 (Memory Limit Infrastructure)

### What changed

Added memory-tracking infrastructure to the sandbox system:

1. **`SandboxOptions` enhancements:**
   - Added `MaxMemoryBytes` property (0 = unlimited)
   - Added `HasMemoryLimit` computed property
   - Added `OnMemoryLimitExceeded` callback (signature: `Func<Script, long, bool>`)
   - Copy constructor preserves new settings

2. **`SandboxViolationType` enum:**
   - Added `MemoryLimitExceeded` value

3. **New `SandboxViolationDetails` struct:**
   - Provides a richer, type-safe API surface for programmatic exception handling
   - Factory methods: `InstructionLimit()`, `RecursionLimit()`, `MemoryLimit()`, `ModuleAccessDenied()`, `FunctionAccessDenied()`
   - `IsLimitViolation` / `IsAccessDenial` discriminator properties
   - `Kind`, `LimitValue`, `ActualValue`, `AccessName` structured data properties
   - `FormatMessage()` for human-readable output
   - Full `IEquatable<T>` implementation with equality operators
   - Serializable for cross-AppDomain/remoting scenarios

4. **`SandboxViolationException` updates:**
   - New `Details` property exposing structured `SandboxViolationDetails`
   - New constructor accepting `SandboxViolationDetails` directly
   - Existing properties (`ViolationType`, `ConfiguredLimit`, `ActualValue`, `DeniedAccessName`) now delegate to `Details` for backward compatibility
   - Updated `FormatViolationMessage` to handle `MemoryLimitExceeded` with descriptive message

5. **New `AllocationTracker` class** (`Sandboxing/AllocationTracker.cs`):
   - Thread-safe allocation counter using `Interlocked` operations
   - Tracks current bytes, peak bytes, total allocated, total freed
   - `RecordAllocation(long bytes)` / `RecordDeallocation(long bytes)` methods
   - `ExceedsLimit(long maxBytes)` / `ExceedsLimit(SandboxOptions)` helpers
   - `Reset()` to clear all counters
   - `CreateSnapshot()` returns an immutable `AllocationSnapshot` struct

6. **New `AllocationSnapshot` struct:**
   - Immutable record of tracker state at a point in time
   - Implements `IEquatable<AllocationSnapshot>` with equality operators

7. **New tests** (`Sandbox/SandboxMemoryLimitTUnitTests.cs`):
   - 36 TUnit tests covering all `AllocationTracker` methods, `SandboxOptions` memory properties, `SandboxViolationDetails` factory methods/equality/formatting, and `SandboxViolationException` structured data access

### Example: Programming against the exception

```csharp
try
{
    script.DoString("while true do end");
}
catch (SandboxViolationException ex)
{
    // Type-safe access via Details
    if (ex.Details.IsLimitViolation)
    {
        Console.WriteLine($"Limit exceeded: {ex.Details.Kind}");
        Console.WriteLine($"  Configured: {ex.Details.LimitValue}");
        Console.WriteLine($"  Actual:     {ex.Details.ActualValue}");
    }
    else if (ex.Details.IsAccessDenial)
    {
        Console.WriteLine($"Access denied to: {ex.Details.AccessName}");
    }
}
```

### Files modified/added (Phase 1 - Infrastructure)

- `src/runtime/.../Sandboxing/SandboxOptions.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationType.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationException.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationDetails.cs` (new)
- `src/runtime/.../Sandboxing/AllocationTracker.cs` (new)
- `src/tests/.../Sandbox/SandboxMemoryLimitTUnitTests.cs` (new)

---

## Checkpoint — 2025-12-07 (Memory Tracking Integration — Phase 2)

### What changed

Wired `AllocationTracker` into `Script` and added VM enforcement:

1. **`Script` class updates:**
   - Added `_allocationTracker` private readonly field
   - Added `AllocationTracker` public property (returns `null` if memory tracking disabled)
   - Constructor creates tracker when `Options.Sandbox.HasMemoryLimit` is true

2. **`Table` class updates:**
   - Added `BaseTableOverhead` constant (256 bytes estimated per empty table)
   - Added `PerEntryOverhead` constant (64 bytes estimated per table entry)
   - Added `_trackedEntryCount` field for tracking
   - Constructor calls `tracker.RecordAllocation(BaseTableOverhead)` if tracker exists
   - `PerformTableSet<T>` tracks entry additions/removals

3. **VM instruction loop (`ProcessorInstructionLoop.cs`):**
   - Added `hasSandboxMemoryLimit`, `sandboxMaxMemoryBytes`, `allocationTracker` locals
   - Added `MemoryCheckInterval` constant (1024 instructions between checks)
   - Added periodic memory limit check using bitwise AND for efficiency
   - Invokes `OnMemoryLimitExceeded` callback if set; throws `SandboxViolationException` otherwise

4. **New integration tests:**
   - `ScriptWithMemoryLimitHasAllocationTracker` — verifies tracker creation
   - `ScriptWithoutMemoryLimitHasNoAllocationTracker` — verifies no tracker when disabled
   - `ScriptAllocationTrackerRecordsTableCreation` — verifies table creation tracking
   - `ScriptAllocationTrackerRecordsTableEntries` — verifies per-entry tracking
   - `ScriptMemoryLimitThrowsWhenExceeded` — verifies enforcement throws exception
   - `ScriptMemoryLimitCallbackCanAllowContinuation` — verifies callback can allow continuation
   - `ScriptAllocationTrackerPeakBytesTracksHighWaterMark` — verifies peak tracking
   - `ScriptAllocationTrackerSnapshotCapturesState` — verifies snapshot API

### Files modified/added (Phase 2 - Integration)

- `src/runtime/.../Script.cs` (modified — added AllocationTracker field/property, constructor init)
- `src/runtime/.../DataTypes/Table.cs` (modified — added allocation tracking in constructor and PerformTableSet)
- `src/runtime/.../Execution/VM/Processor/ProcessorInstructionLoop.cs` (modified — added memory limit enforcement)
- `src/tests/.../Sandbox/SandboxMemoryLimitTUnitTests.cs` (modified — added 8 integration tests)

### Test results

All **3,467** tests pass (Release configuration) — 8 new integration tests added.

### Example usage

```csharp
// Create a script with a 1 MB memory limit
var options = new ScriptOptions
{
    Sandbox = new SandboxOptions
    {
        MaxMemoryBytes = 1024 * 1024,
        OnMemoryLimitExceeded = (script, currentMemory) =>
        {
            Console.WriteLine($"Memory limit exceeded: {currentMemory} bytes");
            // Return true to allow continuation, false to throw
            return false;
        }
    }
};
var script = new Script(options);

// Check current memory usage
Console.WriteLine($"Current: {script.AllocationTracker.CurrentBytes} bytes");
Console.WriteLine($"Peak: {script.AllocationTracker.PeakBytes} bytes");

// Get a snapshot of memory state
var snapshot = script.AllocationTracker.CreateSnapshot();
```

### Remaining memory tracking work

1. ~~**Additional allocation hooks**~~ ✅ **Completed in Phase 3**:
   - ✅ `Closure` creation tracking
   - ✅ `Coroutine` creation tracking
   - `DynValue.NewString` / string interning tracking (deferred — strings are immutable and lack Script context)
2. **Memory deallocation** (complex — requires finalizers or weak references)
3. **More accurate size estimation** (currently uses fixed estimates)

---

## Checkpoint — 2025-12-07 (Extended Allocation Tracking — Phase 3)

### What changed

Extended allocation tracking to include `Closure` and `Coroutine` creation:

1. **`Closure` class updates:**
   - Added `BaseClosureOverhead` constant (128 bytes estimated)
   - Added `PerUpValueOverhead` constant (16 bytes per captured upvalue)
   - Added `TrackAllocation(Script, int upValueCount)` helper method
   - All three Closure constructors now call `TrackAllocation` after initialization

2. **`Coroutine` class updates:**
   - Added `BaseCoroutineOverhead` constant (512 bytes estimated — includes Processor with stacks)
   - Added `TrackAllocation(Script)` helper method
   - `Coroutine(Processor)` constructor now calls `TrackAllocation` after initialization
   - Note: `Coroutine(CallbackFunction)` has `OwnerScript = null` so cannot track (CLR callback coroutines)

3. **New integration tests** (6 tests added):
   - `ScriptAllocationTrackerRecordsClosureCreation` — verifies basic closure tracking
   - `ScriptAllocationTrackerRecordsClosureWithUpValues` — verifies upvalue overhead tracking
   - `ScriptAllocationTrackerRecordsMultipleClosures` — verifies multiple closure tracking
   - `ScriptAllocationTrackerRecordsCoroutineCreation` — verifies coroutine tracking
   - `ScriptAllocationTrackerRecordsMultipleCoroutines` — verifies multiple coroutine tracking
   - `ScriptAllocationTrackerCombinedTableClosureCoroutine` — verifies combined tracking of all types

### Files modified

- `src/runtime/.../DataTypes/Closure.cs` (modified — added allocation tracking)
- `src/runtime/.../DataTypes/Coroutine.cs` (modified — added allocation tracking)
- `src/tests/.../Sandbox/SandboxMemoryLimitTUnitTests.cs` (modified — added 6 integration tests)

### Test results

All **3,473** tests pass (Release configuration) — 6 new integration tests added.

### Allocation tracking coverage

| Type | Tracked | Estimated Size | Notes |
|------|---------|----------------|-------|
| `Table` | ✅ | 256 base + 64/entry | Per-entry tracking on set operations |
| `Closure` | ✅ | 128 base + 16/upvalue | Tracks captured variable overhead |
| `Coroutine` | ✅ | 512 base | Includes Processor with stacks |
| `String` | ❌ | N/A | No Script context in `DynValue.NewString` |

### Example: Monitoring closure/coroutine allocations

```csharp
var options = new ScriptOptions
{
    Sandbox = new SandboxOptions { MaxMemoryBytes = 1024 * 1024 }
};
var script = new Script(options);

long before = script.AllocationTracker.CurrentBytes;

// Create closures with upvalues
script.DoString(@"
    local x, y, z = 1, 2, 3
    function closure() return x + y + z end
    
    function producer()
        for i = 1, 10 do coroutine.yield(i) end
    end
    co = coroutine.create(producer)
");

long after = script.AllocationTracker.CurrentBytes;
Console.WriteLine($"Allocated: {after - before} bytes");
// Expected: ~128 (closure) + 48 (3 upvalues × 16) + 512 (coroutine) = ~688 bytes minimum
```

### Remaining work

1. **String tracking**: Would require passing Script context through `DynValue.NewString` or tracking at table/closure storage points
2. **Memory deallocation**: Requires finalizers, weak references, or explicit disposal patterns
3. **Size accuracy**: Current estimates are conservative; could use reflection or manual measurement for tighter bounds

---

## Checkpoint — 2025-12-07 (Packaging Infrastructure)

### What changed

Implemented comprehensive NuGet and Unity packaging infrastructure:

1. **`Directory.Build.props` enhancements:**
   - Added shared NuGet metadata: Authors, Company, Product, License (MIT)
   - Added repository URLs (GitHub)
   - Added package tags for discoverability
   - Enabled SourceLink (`Microsoft.SourceLink.GitHub`) for debuggable packages
   - Enabled symbol packages (`.snupkg`)
   - Enabled deterministic builds for reproducibility
   - Set default version to 3.0.0 (major bump for namespace rebrand)

2. **Project file updates:**
   - `WallstopStudios.NovaSharp.Interpreter.csproj`:
     - Updated PackageId to `WallstopStudios.NovaSharp.Interpreter`
     - Added comprehensive description
     - Updated copyright to Wallstop Studios
     - Added README.md to package
   - `WallstopStudios.NovaSharp.Interpreter.Infrastructure.csproj`:
     - Updated PackageId and metadata
   - `WallstopStudios.NovaSharp.VsCodeDebugger.csproj`:
     - Updated PackageId and metadata
   - `WallstopStudios.NovaSharp.RemoteDebugger.csproj`:
     - Updated PackageId and metadata

3. **GitHub workflow (`.github/workflows/nuget-publish.yml`):**
   - Automatic publishing on GitHub Release
   - Manual workflow dispatch with version input and dry-run option
   - Builds and tests before packing
   - Packs all four publishable projects
   - Pushes to NuGet.org (with `NUGET_API_KEY` secret)
   - Pushes to GitHub Packages
   - Builds Unity package as separate artifact

4. **Unity packaging scripts:**
   - `scripts/packaging/build-unity-package.sh` (Bash)
   - `scripts/packaging/build-unity-package.ps1` (PowerShell)
   - Generates UPM-compatible package structure:
     - `package.json` with proper Unity metadata
     - Assembly definitions (`.asmdef`) for Runtime and Debuggers
     - Sample code (`Samples~/BasicUsage/`)
     - Documentation (`Documentation~/`)
     - CHANGELOG.md with version history

5. **Documentation:**
   - `scripts/packaging/README.md` with usage instructions
   - Updated `scripts/README.md` with packaging folder reference

### Files added/modified

- `Directory.Build.props` (modified)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj` (modified)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter.Infrastructure/WallstopStudios.NovaSharp.Interpreter.Infrastructure.csproj` (modified)
- `src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/WallstopStudios.NovaSharp.VsCodeDebugger.csproj` (modified)
- `src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/WallstopStudios.NovaSharp.RemoteDebugger.csproj` (modified)
- `.github/workflows/nuget-publish.yml` (new)
- `scripts/packaging/build-unity-package.sh` (new)
- `scripts/packaging/build-unity-package.ps1` (new)
- `scripts/packaging/README.md` (new)
- `scripts/README.md` (modified)

### Test results

All **3,473** tests pass (Release configuration).

### Package output

**NuGet packages:**
```
artifacts/packages/
├── WallstopStudios.NovaSharp.Interpreter.3.0.0.nupkg
└── WallstopStudios.NovaSharp.Interpreter.3.0.0.snupkg
```

**Unity package:**
```
artifacts/unity/com.wallstop-studios.novasharp/
├── package.json
├── LICENSE.md
├── CHANGELOG.md
├── Runtime/
│   ├── WallstopStudios.NovaSharp.Interpreter.dll
│   ├── WallstopStudios.NovaSharp.Runtime.asmdef
│   └── Debuggers/
│       ├── WallstopStudios.NovaSharp.RemoteDebugger.dll
│       ├── WallstopStudios.NovaSharp.VsCodeDebugger.dll
│       └── WallstopStudios.NovaSharp.Debuggers.asmdef
├── Documentation~/
│   ├── README.md
│   ├── UnityIntegration.md
│   └── ThirdPartyLicenses.md
└── Samples~/
    └── BasicUsage/BasicUsage.cs
```

### Usage

**NuGet (after publishing):**
```bash
dotnet add package WallstopStudios.NovaSharp.Interpreter
```

**Unity Package Manager:**
1. Open Package Manager (Window > Package Manager)
2. Click '+' > Add package from disk...
3. Select `artifacts/unity/com.wallstop-studios.novasharp/package.json`

Or add to `manifest.json`:
```json
{
  "dependencies": {
    "com.wallstop-studios.novasharp": "file:../path/to/package"
  }
}
```

### Remaining packaging work

1. **XML documentation**: Fix malformed XML comments to enable `GenerateDocumentationFile`
2. **Package icon**: Add NovaSharp icon for NuGet gallery
3. **Release notes automation**: Generate CHANGELOG from git history
4. **Unity OpenUPM**: Register package with OpenUPM registry

---

## Checkpoint — 2025-12-07 (Coroutine Count Limits — Phase 4)

### What changed

Implemented coroutine count limits for sandbox enforcement:

1. **`SandboxOptions` enhancements:**
   - Added `MaxCoroutines` property (0 = unlimited)
   - Added `HasCoroutineLimit` computed property
   - Added `OnCoroutineLimitExceeded` callback (signature: `Func<Script, int, bool>`)
   - Copy constructor preserves new settings

2. **`SandboxViolationType` enum:**
   - Added `CoroutineLimitExceeded` value

3. **`SandboxViolationDetails` struct:**
   - Added `CoroutineLimit(int limit, int count)` factory method
   - Updated `IsLimitViolation` to include coroutine limits
   - Updated `FormatMessage()` for coroutine limit violations

4. **`SandboxViolationException` updates:**
   - `CreateLimitDetails` now handles `CoroutineLimitExceeded`
   - `FormatViolationMessage` now handles `CoroutineLimitExceeded`

5. **`AllocationTracker` enhancements:**
   - Added `_currentCoroutines`, `_peakCoroutines`, `_totalCoroutinesCreated` counters
   - Added `CurrentCoroutines`, `PeakCoroutines`, `TotalCoroutinesCreated` properties
   - Added `RecordCoroutineCreated()` and `RecordCoroutineDisposed()` methods
   - Added `ExceedsCoroutineLimit(int)` and `ExceedsCoroutineLimit(SandboxOptions)` helpers
   - Updated `Reset()` to clear coroutine counters
   - Updated `CreateSnapshot()` to include coroutine data

6. **`AllocationSnapshot` struct:**
   - Added `CurrentCoroutines`, `PeakCoroutines`, `TotalCoroutinesCreated` properties
   - Updated constructor, `ToString()`, `Equals()`, and `GetHashCode()`

7. **`Script` class updates:**
   - `_allocationTracker` now created when `HasMemoryLimit || HasCoroutineLimit`
   - Added `CheckCoroutineLimit()` private method
   - `CreateCoroutine()` now calls `CheckCoroutineLimit()` before creation

8. **`Coroutine` class updates:**
   - `TrackAllocation()` now skips the main processor's pseudo-coroutine (checks `CoroutineState.Main`)
   - Calls `RecordCoroutineCreated()` for user-created coroutines

9. **New tests** (25 tests added to `SandboxMemoryLimitTUnitTests.cs`):
   - AllocationTracker coroutine counting tests (9 tests)
   - SandboxOptions coroutine limit tests (4 tests)
   - SandboxViolationDetails/Exception coroutine limit tests (4 tests)
   - Script integration tests for coroutine limits (8 tests)

### Files modified/added

- `src/runtime/.../Sandboxing/SandboxOptions.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationType.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationDetails.cs` (modified)
- `src/runtime/.../Sandboxing/SandboxViolationException.cs` (modified)
- `src/runtime/.../Sandboxing/AllocationTracker.cs` (modified)
- `src/runtime/.../Script.cs` (modified)
- `src/runtime/.../DataTypes/Coroutine.cs` (modified)
- `src/tests/.../Sandbox/SandboxMemoryLimitTUnitTests.cs` (modified — added 25 tests)

### Test results

All **3,498** interpreter tests + **72** debugger tests pass (Release configuration).

### Example usage

```csharp
// Create a script with a coroutine limit
var options = new ScriptOptions
{
    Sandbox = new SandboxOptions
    {
        MaxCoroutines = 10,
        OnCoroutineLimitExceeded = (script, currentCount) =>
        {
            Console.WriteLine($"Coroutine limit reached: {currentCount}");
            // Return true to allow continuation, false to throw
            return false;
        }
    }
};
var script = new Script(options);

// Check current coroutine count
Console.WriteLine($"Current: {script.AllocationTracker.CurrentCoroutines}");
Console.WriteLine($"Peak: {script.AllocationTracker.PeakCoroutines}");

// Get a snapshot of tracker state
var snapshot = script.AllocationTracker.CreateSnapshot();
```

### Remaining advanced sandbox work

1. ~~**Deterministic execution mode**~~ — ✅ **Completed 2025-12-08**. For lockstep multiplayer/replays:
   - `IRandomProvider` interface (`NextInt`, `NextDouble`, `SetSeed`)
   - `SystemRandomProvider` (cryptographic seeding) and `DeterministicRandomProvider` (reproducible)
   - `DeterministicTimeProvider` for controllable `os.time`/`os.clock`
   - `ScriptOptions.RandomProvider` and `Script.RandomProvider` for per-script injection
   - 27 TUnit tests in `DeterministicExecutionTUnitTests.cs`
2. **Per-mod isolation containers** — Load/reload/unload hooks for script modules

Checkpoint recorded on branch `dev/wallstop/initial-work-3`.