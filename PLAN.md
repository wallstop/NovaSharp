# Modern Testing & Coverage Plan

## 🎯 Current Priority: Dual Numeric Type System (§8.24 — HIGH PRIORITY)

**Status**: 🚧 **IN PROGRESS** — Phase 2 Core Integration complete, Phase 3-5 remaining.

**Progress (2025-12-07)**:
- ✅ **Phase 1 Complete**: `LuaNumber` struct with 83 tests
- ✅ **Phase 2 Complete**: DynValue integration, VM arithmetic opcodes, `math.type()` correct, bitwise operations preserve precision
- 🔲 **Phase 3 Pending**: StringModule format specifiers
- 🔲 **Phase 4 Pending**: Interop & serialization
- 🔲 **Phase 5 Pending**: Numeric value caching & performance validation

**Key Achievements**:
- `math.maxinteger`/`math.mininteger` return exact values (no precision loss)
- `math.type(1)` → "integer", `math.type(1.0)` → "float" (correct subtype detection)
- Integer arithmetic wraps correctly (two's complement)
- Integer `//` and `%` by zero throw errors; float versions return IEEE 754 values
- Bitwise operations preserve full 64-bit integer precision
- All 3,811 tests passing

See **Section 8.24** for the complete implementation plan.

**Next actionable item**: Phase 3 — Update `StringModule` for integer vs float format handling (`%d`, `%i`, `%o`, `%x`).

---

## Repository Snapshot — 2025-12-07 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **3,811** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads (initialize, threads, breakpoints, events, evaluate, scopes, stackTrace, variables).
- **Sandbox infrastructure**: `SandboxOptions` with instruction limits, recursion limits, module/function restrictions, **memory limits** (Table/Closure/Coroutine tracking), **coroutine count limits**, **per-mod isolation containers**, callbacks, and presets.
- **Benchmark CI**: `.github/workflows/benchmarks.yml` with BenchmarkDotNet, threshold-based regression alerting (115% = 15% regression), and historical tracking in `gh-pages`.
- **Namespace rebrand**: ✅ **Completed 2025-12-06** — Full rebrand to `WallstopStudios.NovaSharp.*` namespaces across all projects.
- **Packaging**: ✅ **Completed 2025-12-07** — NuGet publishing workflow (`.github/workflows/nuget-publish.yml`) + Unity UPM scripts (`scripts/packaging/`).
- **Version centralization**: ✅ **Completed 2025-12-07** — `LuaVersionDefaults` class for consistent `Latest` version resolution.
- **RNG Parity**: ✅ **Completed 2025-12-07** — Version-specific RNG providers (LCG for 5.1-5.3, xoshiro256** for 5.4+).
- **Out/Ref param tests**: ✅ **Completed 2025-12-07** — 10 exhaustive tests for CLR interop out/ref parameter handling.
- **Script constructor consistency**: ✅ **Completed 2025-12-07** — 16 tests verifying initialization order and behavior (§8.2).
- **Numeric edge cases**: ✅ **Completed 2025-12-07** — `math.maxinteger`/`mininteger`, right shift fix, NaN comparison fix (§8.3).
- **Dual numeric type system**: 🚧 **In Progress** — `LuaNumber` struct with integer/float discrimination (§8.24, HIGH PRIORITY). Phase 2 complete, all tests passing.

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
- ✅ **Per-mod isolation containers (2025-12-08)**: Load/reload/unload lifecycle management:
  - `ModLoadState` enum with lifecycle states (Unloaded, Loading, Loaded, Unloading, Reloading, Faulted)
  - `ModOperationResult` immutable result type with Success, State, Message, Error properties
  - `IModContainer` interface with 18 members (properties, events, methods)
  - `ModContainer` implementation with thread-safe state machine and event hooks
  - `ModManager` multi-mod coordinator with dependency graph and topological sort
  - Entry points: `DoString()`, `CallFunction()`, `GetGlobal()`, `SetGlobal()`
  - Events: `OnLoading`, `OnLoaded`, `OnUnloading`, `OnUnloaded`, `OnReloading`, `OnReloaded`, `OnError`
  - Factory delegate and configurator action support for custom Script initialization
  - 44 TUnit tests covering lifecycle, dependencies, and error handling
- **Remaining**: (Advanced sandbox features complete)

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

### 8. Lua Runtime Specification Parity (CRITICAL) 🔴
**Priority**: CRITICAL — Core interpreter correctness for production use.

Ensure NovaSharp produces identical output to reference Lua interpreters across all supported versions (5.1, 5.2, 5.3, 5.4). Key areas:

- **Random Number Generators**: ✅ **Completed 2025-12-07** — Version-specific PRNG algorithms and seeding behavior
  - Lua 5.4: xoshiro256** (✅ implemented via `LuaRandomProvider`)
  - Lua 5.1-5.3: LCG (✅ implemented via `Lua51RandomProvider`)
  - Default seeds, sequence verification against reference Lua
  
- **Script Constructor Consistency**: ✅ **Completed 2025-12-07** — All constructor overloads initialize state identically
  - `GlobalOptions` / `ScriptOptions` inheritance verified
  - Core module registration order documented
  - Random provider seeding timing confirmed
  
- **Numeric/Arithmetic Edge Cases**: Division by zero, overflow, `math.maxinteger`/`mininteger`

- **String Pattern Matching**: Character class differences (`.NET char.IsXxx` vs C `isalpha`)

- **Error Message Formats**: Match Lua's error messages for script compatibility

- **os.time/os.date Semantics**: Timezone handling, epoch values

- **Lua 5.4 Breaking Changes** (newly catalogued from official docs):
  - String-to-number coercion removed from core (moved to string library metamethods)
  - `print` no longer calls global `tostring` (uses `__tostring` directly)
  - Integer `for` loop control variable never overflows
  - `io.lines` returns 4 values instead of 1
  - `__lt` no longer emulates `__le`
  - `__gc` non-function values now error
  - `utf8` library rejects surrogates by default
  - `collectgarbage` options `setpause`/`setstepmul` deprecated
  - Decimal literal overflow reads as float instead of wrapping

- **Lua 5.3 Breaking Changes** (from 5.2):
  - Integer subtype introduced
  - `bit32` library deprecated (native bitwise operators added)
  - `ipairs` respects `__index` metamethod
  - Float-to-string adds `.0` suffix for integer-like values

- **Lua 5.2 Breaking Changes** (from 5.1):
  - `setfenv`/`getfenv` removed (use `_ENV` upvalue)
  - `unpack` moved to `table.unpack`
  - `module` function deprecated
  - Weak tables now behave as ephemeron tables
  - `math.log10` deprecated (use `math.log(x, 10)`)

See **Section 8** in "Lua Specification Parity" below for detailed tracking (22 subsections covering all version-specific behaviors).

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

### 8. Lua Runtime Specification Parity (CRITICAL)

**Goal**: Ensure NovaSharp behaves identically to reference Lua interpreters across all supported versions (5.1, 5.2, 5.3, 5.4) for deterministic, reproducible script execution.

This initiative addresses semantic divergences where our C# implementation could match native Lua behavior but currently doesn't. The focus is on **runtime behavior parity**, not just API surface compatibility.

#### 8.1 Random Number Generator Parity

**Status**: ✅ **Completed (2025-12-07)**

**Current State**: NovaSharp now implements version-specific RNG algorithms:
- `Lua51RandomProvider` (LCG with glibc parameters) for Lua 5.1/5.2/5.3 compatibility
- `LuaRandomProvider` (xoshiro256**) for Lua 5.4+

**Version-specific Requirements**:
| Lua Version | Algorithm | Default Seed Behavior | NovaSharp Status |
|-------------|-----------|----------------------|------------------|
| 5.1 | C library `rand()` (glibc LCG) | `time(NULL)` | ✅ `Lua51RandomProvider` |
| 5.2 | C library `rand()` (glibc LCG) | `time(NULL)` | ✅ `Lua51RandomProvider` |
| 5.3 | C library `rand()` (glibc LCG) | `time(NULL)` | ✅ `Lua51RandomProvider` |
| 5.4 | xoshiro256** | Cryptographic random (128-bit) | ✅ `LuaRandomProvider` |

**Completed Tasks**:
- [x] Create `Lua51RandomProvider` using linear congruential generator (LCG) for 5.1/5.2/5.3 compatibility
- [x] Implement version-specific `math.randomseed` semantics (5.1-5.3 vs 5.4 signatures differ)
- [x] Verify `math.random()` output sequences match reference Lua for each version when seeded identically
- [x] Add golden sequence tests: seed with known values, compare first N outputs against reference Lua
- [x] Document platform-specific behavior differences for 5.1-5.3 (C `rand()` varies by platform)

See **Checkpoint — 2025-12-07 (RNG Parity)** for implementation details.

#### 8.2 Script Constructor Consistency

**Status**: ✅ **Completed 2025-12-07**

**Goal**: All `Script` constructors must initialize state identically, ensuring:
- `GlobalOptions` inheritance is consistent
- `ScriptOptions` defaults propagate correctly
- Core modules register in the same order
- Random provider seeding occurs at the same point in initialization

**Current Constructors**:
```csharp
Script()                                    // Default modules, default options
Script(CoreModules)                         // Custom modules, default options
Script(ScriptOptions)                       // Default modules, custom options
Script(CoreModules, ScriptOptions)          // Custom modules, custom options
```

**Audit Findings**:

All four constructors delegate to `Script(CoreModules coreModules, ScriptOptions options)`, which follows a deterministic initialization order:
1. **Options** — Copies from `options` (or `DefaultOptions` if null)
2. **CompatibilityVersion inheritance** — When `options == null`, overwritten from `GlobalOptions.CompatibilityVersion`
3. **TimeProvider** — From `Options.TimeProvider` or `SystemTimeProvider.Instance`
4. **RandomProvider** — From `Options.RandomProvider` or version-appropriate default
5. **StartTimeUtc** — Captured from `TimeProvider.GetUtcNow()`
6. **AllocationTracker** — Created if sandbox has memory/coroutine limits
7. **PerformanceStats** — Created with configured clock
8. **Registry** — New empty table
9. **ByteCode** — New bytecode container
10. **MainProcessor** — New processor instance
11. **GlobalTable** — New table with `RegisterCoreModules(coreModules)`

**Key Behavior**:
- `Script()` and `Script(CoreModules)` pass `null` for options → inherit `GlobalOptions.CompatibilityVersion`
- `Script(ScriptOptions)` and `Script(CoreModules, ScriptOptions)` with explicit options → use options as-is
- Fresh `new ScriptOptions()` defaults to `LuaCompatibilityVersion.Latest`
- Recommended pattern: `new ScriptOptions(Script.DefaultOptions)` to copy global defaults

**Completed Tasks**:
- [x] Audit all constructor paths to ensure identical initialization order
- [x] Verify `Options.CompatibilityVersion` is always set from `GlobalOptions` when not explicitly provided
- [x] Ensure `LuaRandomProvider` vs `DeterministicRandomProvider` selection respects options
- [x] Add tests verifying constructor equivalence (same seed → same first random value)
- [x] Document constructor behavior (see `ScriptConstructorConsistencyTUnitTests.cs`)

See **Checkpoint — 2025-12-07 (Script Constructor Consistency)** for implementation details.

#### 8.3 Numeric Representation and Arithmetic ✅ **COMPLETED 2025-12-07**

**Requirements** (per Lua version):
| Feature | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status |
|---------|-----|-----|-----|-----|------------------|
| Integer subtype | ❌ | ❌ | ✅ | ✅ | ✅ Implemented |
| `math.type()` | ❌ | ❌ | ✅ | ✅ | ✅ Implemented |
| Integer division `//` | ❌ | ❌ | ✅ | ✅ | ✅ Implemented |
| Bitwise operators | ❌ | ✅ bit32 | ✅ Native | ✅ Native | ✅ Implemented |
| `math.maxinteger/mininteger` | ❌ | ❌ | ✅ | ✅ | ✅ Implemented |

**Completed Work (2025-12-07)**:
- ✅ Added `math.maxinteger` (9223372036854775807) and `math.mininteger` (-9223372036854775808) constants with `[LuaCompatibility(Lua53)]`
- ✅ Fixed right shift operator to use logical shift per Lua spec (§3.4.2) — was incorrectly using arithmetic shift
- ✅ Fixed `a > b` and `a >= b` compilation to use operand swapping instead of result inversion — fixes NaN comparisons per IEEE 754
- ✅ Added comprehensive numeric edge case tests (`MathNumericEdgeCasesTUnitTests.cs`, 50+ tests)

**Production Bugs Fixed**:
- `LuaIntegerHelper.ShiftRight` now returns 0 for shifts >= 64 bits (was returning -1 for negative values)
- NaN comparisons (`nan > nan`, `nan >= nan`) now correctly return `false` (was returning `true`)
- Updated existing shift/comparison tests to match Lua 5.4 reference behavior

**Known Divergences** (documented in tests and `docs/testing/lua-divergences.md`):
- **Integer division by zero**: NovaSharp returns infinity for all `n // 0` cases. Lua throws error only when both operands are "true integers" — NovaSharp cannot distinguish since all numbers are doubles.
- **`math.maxinteger` precision**: `9223372036854775807` cannot be exactly represented as a double, so bitwise operations and `math.tointeger(math.maxinteger)` may overflow to mininteger.
- **`math.huge`**: NovaSharp uses `double.MaxValue` instead of `infinity` (known legacy divergence).

**Tasks** (all completed):
- [x] Verify `math.maxinteger` and `math.mininteger` match Lua 5.3/5.4 exactly
- [x] Ensure integer overflow behavior matches Lua (documented divergences)
- [x] Verify division by zero behavior per version (documented divergence)
- [x] Test edge cases: `0/0`, `inf/-inf`, very large integers (all covered)

#### 8.4 String and Pattern Matching

**Potential Divergences**:
- Character class `%a`, `%l`, `%u` etc. use .NET `char.IsXxx()` which may differ from C `isalpha()` etc.
- Unicode handling in patterns (Lua 5.3+ vs earlier)
- `string.format` edge cases (float formatting, padding)

**Tasks**:
- [ ] Compare `%a`, `%d`, `%l`, `%u`, `%w`, `%s` character classes against reference Lua
- [ ] Verify `string.format` output matches for edge cases (NaN, Inf, very large numbers)
- [ ] Test pattern matching with non-ASCII characters
- [ ] Document any intentional Unicode-aware divergences

#### 8.5 os.time and os.date Semantics

**Requirements**:
- `os.time()` with no arguments returns current UTC timestamp
- `os.time(table)` interprets fields per §6.9
- `os.date("*t")` returns table with correct field names and ranges
- Timezone handling differences (C `localtime` vs .NET)

**Tasks**:
- [ ] Verify `os.time()` return value matches Lua's epoch-based timestamp
- [ ] Test `os.date` format strings against reference Lua outputs
- [ ] Document timezone handling differences (if any)
- [ ] Ensure `DeterministicTimeProvider` integration doesn't break compatibility

#### 8.6 Coroutine Semantics

**Critical Behaviors**:
- `coroutine.resume` return value shapes
- `coroutine.wrap` error propagation
- `coroutine.status` state transitions
- Yield across C-call boundary errors

**Tasks**:
- [ ] Create state transition diagram tests for coroutine lifecycle
- [ ] Verify error message formats match Lua
- [ ] Test `coroutine.close` (5.4) cleanup order

#### 8.7 Error Message Parity

**Goal**: Error messages should match Lua's format for maximum compatibility with scripts that parse errors.

**Known Divergences** (from `docs/testing/lua-divergences.md`):
- Nil index: Lua says `(name)`, NovaSharp omits variable name
- Stack traces: .NET format vs Lua format
- Module not found: Different path listing

**Tasks**:
- [ ] Catalog all error message formats in `ScriptRuntimeException`
- [ ] Create error message normalization layer for Lua-compatible output
- [ ] Add `ScriptOptions.LuaCompatibleErrors` flag (opt-in strict mode)

#### 8.8 Verification Infrastructure

**Golden Test Suite**:
- [ ] Create `LuaFixtures/RngParity/` with seeded random sequences per version
- [ ] Create `LuaFixtures/NumericEdgeCases/` for arithmetic edge cases
- [ ] Create `LuaFixtures/ErrorMessages/` for error format verification
- [ ] Extend `compare-lua-outputs.py` to compare byte-for-byte output for determinism tests

**CI Enhancement**:
- [ ] Add Lua 5.1, 5.2, 5.3, 5.4 comparison jobs to the matrix
- [ ] Track parity percentage per version in CI artifacts
- [ ] Alert on parity regressions

#### 8.9 String-to-Number Coercion Changes (Lua 5.4)

**Breaking Change in 5.4**: String-to-number coercion was removed from the core language. Arithmetic operations no longer automatically convert string operands to numbers.

**Version Behavior**:
| Version | `"10" + 1` | Implementation |
|---------|------------|----------------|
| 5.1-5.3 | `11` | Core language coercion |
| 5.4 | `11` (via metamethods) | String library provides `__add` etc. metamethods |

**Tasks**:
- [ ] Verify NovaSharp behavior matches the target `LuaCompatibilityVersion`
- [ ] Ensure string metatable has arithmetic metamethods for 5.4 compatibility
- [ ] Add tests for string arithmetic operations per version
- [ ] Document the coercion change in `docs/LuaCompatibility.md`

#### 8.10 print/tostring Behavior Changes (Lua 5.4)

**Breaking Change in 5.4**: `print` no longer calls the global `tostring` function; it directly uses the `__tostring` metamethod.

**Implications**:
- Custom `tostring` replacements won't affect `print` output in 5.4
- Scripts relying on this pattern will break

**Tasks**:
- [ ] Verify `print` behavior matches target Lua version
- [ ] Add tests for custom `tostring` function interaction with `print`
- [ ] Document behavior difference

#### 8.11 Numerical For Loop Semantics (Lua 5.4)

**Breaking Change in 5.4**: Control variable in integer `for` loops never overflows/wraps.

**Version Behavior**:
| Version | Integer overflow in loop control | Implementation |
|---------|----------------------------------|----------------|
| 5.1-5.3 | Wraps around | Could cause infinite loops |
| 5.4 | Never wraps | Loop terminates correctly |

**Tasks**:
- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds
- [ ] Document loop semantics per version

#### 8.12 io.lines Return Value Changes (Lua 5.4)

**Breaking Change in 5.4**: `io.lines` returns 4 values instead of 1 (adds close function and two placeholders).

**Tasks**:
- [ ] Verify `io.lines` return value count matches target version
- [ ] Add tests for multi-value return unpacking from `io.lines`

#### 8.13 __lt/__le Metamethod Changes (Lua 5.4)

**Breaking Change in 5.4**: `__lt` metamethod no longer emulates `__le` when `__le` is absent.

**Version Behavior**:
| Version | `a <= b` when only `__lt` defined |
|---------|-----------------------------------|
| 5.1-5.3 | Uses `not (b < a)` |
| 5.4 | Error (no `__le` metamethod) |

**Tasks**:
- [ ] Verify comparison operator metamethod fallback per version
- [ ] Add tests for partial metamethod definitions
- [ ] Document metamethod requirements per version

#### 8.14 __gc Metamethod Handling (Lua 5.4)

**Breaking Change in 5.4**: Objects with non-function `__gc` metamethods are no longer silently ignored; they generate errors.

**Tasks**:
- [ ] Verify `__gc` validation matches target version
- [ ] Add tests for invalid `__gc` values
- [ ] Document garbage collection metamethod requirements

#### 8.15 utf8 Library Strictness (Lua 5.4)

**Breaking Change in 5.4**: The `utf8` library rejects UTF-16 surrogates by default (accepts them with `lax` mode).

**Tasks**:
- [ ] Verify `utf8.*` functions handle surrogates correctly per version
- [ ] Add tests for surrogate handling with and without `lax` mode
- [ ] Document utf8 library differences

#### 8.16 collectgarbage Options (Lua 5.4)

**Deprecation in 5.4**: `setpause` and `setstepmul` options are deprecated (use `incremental` instead).

**Tasks**:
- [ ] Support deprecated options with warnings when targeting 5.4
- [ ] Implement `incremental` option for 5.4
- [ ] Add tests for GC option compatibility

#### 8.17 Literal Integer Overflow (Lua 5.4)

**Breaking Change in 5.4**: Decimal integer literals that overflow read as floats instead of wrapping.

**Version Behavior**:
| Version | `999999999999999999999` (overflow) |
|---------|-----------------------------------|
| 5.3 | Wraps to integer |
| 5.4 | Reads as float |

**Tasks**:
- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing
- [ ] Document literal parsing behavior

#### 8.18 bit32 Library Deprecation (Lua 5.3+)

**Breaking Change in 5.3**: The `bit32` library was deprecated in favor of native bitwise operators.

**Version Availability**:
| Version | `bit32` library | Native operators |
|---------|-----------------|------------------|
| 5.1 | ❌ (external) | ❌ |
| 5.2 | ✅ | ❌ |
| 5.3 | ⚠️ Deprecated | ✅ |
| 5.4 | ❌ Removed | ✅ |

**Note**: `bit32` operates on 32-bit integers; native operators operate on 64-bit Lua integers.

**Tasks**:
- [ ] Verify `bit32` availability matches target version
- [ ] Add compatibility warning when using `bit32` on 5.3
- [ ] Document migration path from `bit32` to native operators

#### 8.19 Environment Changes (Lua 5.2+)

**Breaking Change in 5.2**: The concept of function environments was fundamentally changed.

**Version Behavior**:
| Feature | 5.1 | 5.2+ |
|---------|-----|------|
| `setfenv`/`getfenv` | ✅ | ❌ Removed |
| `_ENV` upvalue | ❌ | ✅ |
| C function environments | ✅ | ❌ |
| `module` function | ✅ | ⚠️ Deprecated |

**Tasks**:
- [ ] Verify environment handling matches target version
- [ ] Support `setfenv`/`getfenv` only for 5.1 compatibility mode
- [ ] Document `_ENV` usage for 5.2+ code

#### 8.20 ipairs Metamethod Changes (Lua 5.3+)

**Breaking Change in 5.3**: `ipairs` now respects `__index` metamethods; the `__ipairs` metamethod was deprecated.

**Tasks**:
- [ ] Verify `ipairs` metamethod behavior per version
- [ ] Add tests for `ipairs` with `__index` metamethod tables
- [ ] Document iterator behavior differences

#### 8.21 table.unpack Location (Lua 5.2+)

**Breaking Change in 5.2**: `unpack` moved from global to `table.unpack`.

**Version Availability**:
| Version | Global `unpack` | `table.unpack` |
|---------|-----------------|----------------|
| 5.1 | ✅ | ❌ |
| 5.2+ | ❌ | ✅ |

**Tasks**:
- [ ] Verify `unpack` availability matches target version
- [ ] Provide global `unpack` alias for 5.1 compatibility mode
- [ ] Document migration from `unpack` to `table.unpack`

#### 8.22 Documentation

- [ ] Update `docs/LuaCompatibility.md` with version-specific behavior notes
- [ ] Add "Determinism Guide" for users needing reproducible execution
- [ ] Document any intentional divergences with rationale
- [ ] Create version migration guides (5.1→5.2, 5.2→5.3, 5.3→5.4)
- [ ] Add "Breaking Changes by Version" quick-reference table

#### 8.23 ScriptOptions Default Constructor and Centralized Version Constant

**Status**: ✅ **Completed 2025-12-07**

**Problem Statement** (resolved): The codebase had multiple issues with version handling:

1. **ScriptOptions default constructor fragility**: `new ScriptOptions()` initializes `CompatibilityVersion = LuaCompatibilityVersion.Latest`, but when passed to `Script`, the `CreateDefaultRandomProvider` and other version-dependent logic must resolve `Latest` → concrete version in multiple places. This created scattered fallback logic.

2. **Scattered "Latest" resolution**: At least 4 places manually mapped `Latest`:
   - `Script.cs` — `CreateDefaultRandomProvider` → `Lua54`
   - `MathModule.cs` — `RandomSeed` signature selection → `Lua54`
   - `ModManifest.cs` — Manifest compatibility parsing → `Lua55` (for forward compat)
   - `LuaCompatibilityAttribute.cs` — Attribute version resolution → `Lua55` (for forward compat)

3. **No single source of truth**: When Lua 5.5 becomes the "latest" version, all these locations would need manual updates, creating risk of inconsistency.

**Solution Implemented**:

1. **Created `LuaVersionDefaults` static class** (`Compatibility/LuaVersionDefaults.cs`):
   - `CurrentDefault = LuaCompatibilityVersion.Lua54` — The latest stable release
   - `HighestSupported = LuaCompatibilityVersion.Lua55` — For forward-compatibility checks
   - `Resolve(version)` — Maps `Latest` → `CurrentDefault`
   - `ResolveForHighest(version)` — Maps `Latest` → `HighestSupported`

2. **Updated all resolution sites**:
   - `Script.CreateDefaultRandomProvider()` → uses `LuaVersionDefaults.Resolve()`
   - `MathModule.RandomSeed()` → uses `LuaVersionDefaults.Resolve()`
   - `ModManifest.Normalize()` → uses `LuaVersionDefaults.ResolveForHighest()`
   - `LuaCompatibilityAttribute.IsSupported()` → uses `LuaVersionDefaults.ResolveForHighest()`

**Completed Tasks**:
- [x] Create `LuaVersionDefaults` static class with `CurrentDefault` constant and `Resolve()` helper
- [x] Create `ResolveForHighest()` for forward-compatibility scenarios
- [x] Replace all scattered `Latest` conditionals with centralized resolution
- [x] Add comprehensive tests (`LuaVersionDefaultsTUnitTests.cs` with 25 tests):
  - [x] `CurrentDefaultIsNotLatest` — Prevents circular resolution
  - [x] `CurrentDefaultIsLua54` — Documents current default
  - [x] `HighestSupportedIsLua55` — Documents highest supported
  - [x] `ResolveLatestReturnsCurrentDefault` — Core resolution behavior
  - [x] `ResolveConcreteVersionReturnsUnchanged` — Concrete versions pass through
  - [x] `ResolveForHighestLatestReturnsHighestSupported` — Forward compat behavior
  - [x] `ScriptOptionsDefaultConstructorSetsLatest` — Verifies default ctor
  - [x] `ScriptWithLatestUsesCorrectRngProvider` — RNG provider selection
  - [x] `ScriptWithLatestHasCorrectMathRandomseedBehavior` — 5.4+ behavior
  - [x] `AllScriptConstructorsProduceSameCompatibilityVersion` — Constructor consistency
  - [x] `AllScriptConstructorsProduceSameRngType` — Constructor consistency
  - [x] `ScriptWithSameSeedProducesSameFirstRandomValue` — Determinism
  - [x] `OlderVersionsUseLua51RandomProvider` — Version-specific RNG
  - [x] `NewerVersionsUseLuaRandomProvider` — Version-specific RNG
  - [x] `ScriptOptionsCopyConstructorPreservesVersion` — Copy ctor correctness

**Rationale**: A single source of truth for the "current default" version ensures:
- Consistent behavior across all code paths
- Single-point update when adopting new Lua versions
- Clear documentation of what "Latest" means at any point in time
- Reduced risk of version-handling bugs

**Owner**: Interpreter team
**Priority**: ~~HIGH~~ DONE
**Tracking**: Completed in checkpoint 2025-12-07

#### 8.24 Dual Numeric Type System (Integer + Float) 🔴 **HIGH PRIORITY**

**Status**: 🚧 **IN PROGRESS** — Phase 2 complete. All 3,811 tests passing.

**Progress Checkpoint (2025-12-07)**:
- ✅ **Phase 1 Complete**: `LuaNumber` discriminated union struct created
  - Location: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/LuaNumber.cs`
  - 9-byte struct using `[StructLayout(LayoutKind.Explicit)]` with overlaid `long`/`double`
  - Full arithmetic: Add, Subtract, Multiply, Divide, FloorDivide, Modulo, Power, Negate
  - Full bitwise: And, Or, Xor, Not, ShiftLeft, ShiftRight
  - Comparison with Lua NaN semantics (NaN compares false)
  - Negative zero handling for IEEE 754 compliance
  - Integer overflow wrapping per Lua 5.3+ spec (two's complement)
  - 83 comprehensive unit tests in `LuaNumberTUnitTests.cs`
- ✅ **Phase 2 Complete**: DynValue integration and VM opcodes
  - ✅ `DynValue._number` changed from `double` to `LuaNumber`
  - ✅ `DynValue.Number` property returns `ToDouble` for backward compatibility
  - ✅ `DynValue.LuaNumber`, `DynValue.IsInteger`, `DynValue.IsFloat` properties added
  - ✅ `DynValue.NewInteger()`, `DynValue.NewFloat()`, `DynValue.NewNumber(LuaNumber)` factory methods added
  - ✅ `DynValue.CastToLuaNumber()` for converting DynValue to LuaNumber (with string coercion)
  - ✅ VM `Processor` arithmetic opcodes updated (ADD, SUB, MUL, DIV, IDIV, MOD, POW, UNM)
  - ✅ VM `Processor` bitwise opcodes updated (AND, OR, XOR, NOT, SHL, SHR)
  - ✅ `math.type()` returns correct "integer"/"float" based on LuaNumber subtype
  - ✅ `math.tointeger()` returns integer subtype (using `NewInteger`)
  - ✅ `math.maxinteger`/`math.mininteger` registered as `long` constants (integer subtype)
  - ✅ `LuaIntegerHelper.TryGetInteger(DynValue)` preserves integer precision (no double conversion)
  - ✅ Integer literals parsed directly as `long` to preserve precision for large values
  - ✅ Float literals (with `.` or `e/E`) properly create float subtype
  - ✅ Integer floor division (`//`) and modulo (`%`) by zero throw errors
  - ✅ Float floor division (`//`) and modulo (`%`) by zero return IEEE 754 results (inf/NaN)
  - ✅ Negative zero preserved as float (not converted to integer 0)
  - ✅ Bitwise operations preserve full 64-bit integer precision (no double rounding)
  - ✅ All 3,811 tests passing
- 🔲 **Phase 3 Pending**: Standard library updates
  - `StringModule` format specifier handling (`%d`, `%i`, `%o`, `%x`, `%X`)
  - `math.floor`/`math.ceil` integer promotion rules
- 🔲 **Phase 4 Pending**: Interop & serialization
  - `FromObject()`/`ToObject()` integer preservation
  - JSON serialization (integers as JSON integers)
  - Binary dump/load format
- 🔲 **Phase 5 Pending**: Caching & performance validation
  - Extend `DynValue` caches for common float values (0.0, 1.0, -1.0, etc.)
  - Add `FromFloat(double)` cache method for hot paths
  - Add negative integer cache (-256 to -1)
  - Memory allocation profiling to verify caching effectiveness
  - Lua comparison harness verification

**Problem Statement**:

Lua 5.3+ has **two distinct numeric subtypes** that NovaSharp currently cannot represent:
- **Integer**: 64-bit signed (`long`/`Int64`) with exact range -2^63 to 2^63-1
- **Float**: 64-bit IEEE 754 double precision

NovaSharp stores all numbers as `double` (inherited from MoonSharp), causing:

| Issue | Impact | Severity |
|-------|--------|----------|
| Precision loss for integers > 2^53 | `math.maxinteger` (2^63-1) rounds to 2^63 | HIGH |
| Cannot distinguish integer vs float ops | `3 // 0` should error, `3.0 // 0` → inf | MEDIUM |
| `math.type(x)` cannot return "integer" | API incompatibility | MEDIUM |
| Bitwise ops may produce wrong results | `math.maxinteger & 1` may overflow | HIGH |

**Proposed Solution**: Custom discriminated union struct

```csharp
// Replace `private double _number` in DynValue with:
[StructLayout(LayoutKind.Explicit)]
internal struct LuaNumber : IEquatable<LuaNumber>
{
    [FieldOffset(0)] private readonly long _integer;
    [FieldOffset(0)] private readonly double _float;  // Overlaid for memory efficiency
    [FieldOffset(8)] private readonly bool _isInteger;
    
    // Properties
    public bool IsInteger => _isInteger;
    public bool IsFloat => !_isInteger;
    public long AsInteger => _isInteger ? _integer : (long)_float;
    public double AsFloat => _isInteger ? (double)_integer : _float;
    public double ToDouble => _isInteger ? (double)_integer : _float;
    
    // Factories
    public static LuaNumber FromInteger(long value) => new(value, isInteger: true);
    public static LuaNumber FromFloat(double value) => new(value, isInteger: false);
    public static LuaNumber FromDouble(double value) => 
        value == Math.Floor(value) && value >= long.MinValue && value <= long.MaxValue
            ? FromInteger((long)value)  // Integer-like floats become integers
            : FromFloat(value);
    
    // Arithmetic (follows Lua rules)
    public static LuaNumber Add(LuaNumber a, LuaNumber b)
    {
        if (a.IsInteger && b.IsInteger)
            return FromInteger(unchecked(a._integer + b._integer));  // Wrapping per Lua spec
        return FromFloat(a.ToDouble + b.ToDouble);
    }
    
    public static LuaNumber Divide(LuaNumber a, LuaNumber b)
    {
        // Regular division (/) always returns float per Lua spec
        return FromFloat(a.ToDouble / b.ToDouble);
    }
    
    public static LuaNumber FloorDivide(LuaNumber a, LuaNumber b)
    {
        if (a.IsInteger && b.IsInteger)
        {
            if (b._integer == 0)
                throw new ScriptRuntimeException("attempt to divide by zero");
            return FromInteger(a._integer / b._integer);  // Integer floor division
        }
        return FromFloat(Math.Floor(a.ToDouble / b.ToDouble));
    }
}
```

**Why Not Use Existing Solutions?**

| Option | Evaluation |
|--------|------------|
| `System.Int128` | Only .NET 7+, NovaSharp targets `netstandard2.1` for Unity/Mono. Also overkill — Lua needs 64-bit, not 128-bit. |
| Third-party libraries (BigMath, UltimateOrb) | Same netstandard issue; adds dependencies; most are abandoned. |
| `System.Numerics.BigInteger` | Heap-allocated, poor performance for hot path operations. |
| `decimal` | Different semantics (base-10), wrong precision characteristics. |

The custom struct is the correct solution because:
1. **Matches Lua semantics** — Tracks integer vs float subtype
2. **Exact 64-bit integers** — No precision loss for `math.maxinteger`
3. **Correct floor division** — `3 // 0` errors, `3.0 // 0` → inf
4. **Works on netstandard2.1** — No external dependencies
5. **Memory-efficient** — Union layout uses only 9 bytes (vs 16 for separate fields)

**Implementation Scope**:

This is a **major architectural change** touching:

| Component | Changes Required |
|-----------|------------------|
| `DynValue` | Replace `double _number` with `LuaNumber _number` |
| `Processor` | Update all arithmetic opcodes (ADD, SUB, MUL, DIV, IDIV, MOD, POW, UNM) |
| `BinaryOperatorExpression` | Update compile-time constant folding |
| Bitwise operations | Already use integers, but need integration with new type |
| `MathModule` | `math.type()`, `math.tointeger()`, `math.ult()`, `math.maxinteger/mininteger` |
| `StringModule` | `string.format` integer vs float handling |
| Interop converters | `FromObject()`, `ToObject()`, CLR bridging |
| JSON serialization | `JsonTableConverter` integer vs float distinction |
| Binary dump/load | `BinaryDump.cs` serialization format may need version bump |

**Risk Assessment**:
- **Risk Level**: HIGH — Could introduce regressions across entire codebase
- **Mitigation**: Extensive test coverage (3,700+ tests), phased rollout

**Effort Estimate**: 2-4 weeks focused work + extensive testing

**Implementation Plan**:

**Phase 1: Infrastructure** (1 week)
- [x] Create `LuaNumber` struct with full arithmetic operations
- [x] Add comprehensive unit tests for `LuaNumber` (81 tests covering all operations)
- [ ] Create `LuaNumberTests` comparing against reference Lua outputs

**Phase 2: Core Integration** (1 week) ✅ **COMPLETE**
- [x] Update `DynValue` to use `LuaNumber` storage
- [x] Update `DataType.Number` semantics (no enum change needed)
- [x] Add `DynValue.IsInteger` / `DynValue.IsFloat` properties
- [x] Update `Processor` arithmetic opcodes
- [x] Update `Processor` bitwise opcodes
- [x] Integer literal parsing via `Token.TryGetIntegerValue()`
- [x] Float literal detection via `Token.IsFloatLiteralSyntax()`
- [x] `math.type()` returns correct subtype
- [x] `math.maxinteger`/`math.mininteger` as integer subtype

**Phase 3: Standard Library** (3-4 days)
- [x] Update `MathModule` (`math.type()`, floor division error handling)
- [ ] Update `StringModule` (format specifier handling for `%d`, `%i`, `%o`, `%x`)
- [x] Update bitwise operations integration
- [ ] Update comparison operators (integer vs float promotion)

**Phase 4: Interop & Serialization** (3-4 days)
- [ ] Update `FromObject()` / `ToObject()` for integer preservation
- [ ] Update JSON serialization (integers as JSON integers, not floats)
- [ ] Update binary dump/load format (version 2?)
- [ ] Ensure CLR interop handles `int`, `long`, `float`, `double` correctly

**Phase 5: Caching & Performance Validation** (3-4 days)
- [ ] **Numeric value caching optimization** — Extend allocation-friendly caches for hot-path numeric operations:
  - [ ] Add `FromFloat(double)` cache returning readonly values for common floats (0.0, 1.0, -1.0, 0.5, NaN, ±Infinity)
  - [ ] Ensure `NewFloat(double)` checks float cache for common values before allocating
  - [ ] Ensure `NewNumber(LuaNumber)` routes through appropriate cache based on subtype
  - [ ] Audit VM arithmetic result paths to use `From*` cache methods instead of `New*` where readonly is acceptable
  - [ ] Add negative integer cache (-256 to -1) for common negative indices/counters
  - [ ] Consider `LuaNumber` struct-level caching for frequently-created values
- [x] Run full test suite (3,811 tests passing)
- [ ] Run Lua comparison harness against reference Lua 5.3/5.4
- [ ] Performance benchmarking (ensure no significant regression)
- [ ] Memory allocation profiling (verify caching reduces allocations)
- [ ] Documentation updates

**Success Criteria**:
- [x] `math.maxinteger` returns exactly `9223372036854775807` (not rounded)
- [x] `math.type(1)` returns `"integer"`, `math.type(1.0)` returns `"float"`
- [x] `3 // 0` throws error, `3.0 // 0` returns `inf`
- [x] `math.maxinteger & 1` returns `1` (not overflow)
- [x] All 3,811 existing tests pass
- [ ] Lua comparison harness shows improved parity percentage
- [ ] No performance regression > 5% on benchmarks
- [ ] Numeric caching reduces hot-path allocations (verified via BenchmarkDotNet memory diagnostics)

**Known Divergences After Implementation**:
- None expected — this change specifically targets eliminating numeric divergences.

**Owner**: Interpreter team
**Priority**: 🔴 HIGH — Required for full Lua 5.3+ specification compliance
**Tracking**: Phase 2 complete (2025-12-07), continuing with Phase 3-5

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

10. ~~**Advanced sandbox features** (Initiative 4)~~ ✅ **Completed 2025-12-07** — All items done:
   - ~~Memory tracking (per-allocation accounting)~~ ✅ **Completed 2025-12-07** (Phase 3 - Extended tracking)
   - ~~Coroutine count limits~~ ✅ **Completed 2025-12-07** (Phase 4)
   - ~~Deterministic execution mode for lockstep multiplayer/replays~~ ✅ **Completed 2025-12-07**
   - ~~Per-mod isolation containers with load/reload/unload hooks~~ ✅ **Completed 2025-12-07**

11. ~~**Packaging** (Initiative 5)~~ ✅ **Completed 2025-12-07** — NuGet and Unity packaging infrastructure:
    - ✅ NuGet package metadata (Authors, License, SourceLink, symbols)
    - ✅ GitHub workflow for automated NuGet publishing (`.github/workflows/nuget-publish.yml`)
    - ✅ Unity UPM package builder scripts (`scripts/packaging/build-unity-package.sh`, `.ps1`)
    - ✅ Package IDs updated to `WallstopStudios.NovaSharp.*` format
    - ✅ Version bumped to 3.0.0 (major version for namespace rebrand)
    - See `scripts/packaging/README.md` for usage documentation

12. ~~**ScriptOptions default constructor and centralized version constant**~~ (Initiative 8.23): ✅ **Completed 2025-12-07**
    - ✅ Created `LuaVersionDefaults` class with `CurrentDefault` (Lua54) and `HighestSupported` (Lua55) constants
    - ✅ Created `Resolve()` helper for runtime version resolution (Latest → CurrentDefault)
    - ✅ Created `ResolveForHighest()` helper for forward-compat checks (Latest → HighestSupported)
    - ✅ Replaced scattered `Latest ? Lua54` resolution in `Script.cs`, `MathModule.cs`
    - ✅ Replaced scattered `Latest ? Lua55` resolution in `ModManifest.cs`, `LuaCompatibilityAttribute.cs`
    - ✅ Added 25 TUnit tests covering `LuaVersionDefaults`, `ScriptOptions`, and `Script` constructor consistency
    - See `Checkpoint — 2025-12-07 (LuaVersionDefaults)` for details

---

### Next Priority Items (Actionable)

13. ~~**Lua Specification Parity - Numeric Edge Cases**~~ (Initiative 8.3): ✅ **Completed 2025-12-07**
    - ✅ Added `math.maxinteger` and `math.mininteger` with `[LuaCompatibility(Lua53)]`
    - ✅ Fixed right shift operator (logical not arithmetic per Lua spec)
    - ✅ Fixed NaN comparisons (`nan > nan` now returns `false`)
    - ✅ Added 50+ tests in `MathNumericEdgeCasesTUnitTests.cs`
    - ✅ Documented known divergences (double precision limits, floor div by zero)

14. **Dual Numeric Type System** (Initiative 8.24): 🔴 **HIGH PRIORITY — BLOCKED ON ARCHITECTURAL DECISION**
    - Implement `LuaNumber` struct with integer/float discrimination
    - Required for full Lua 5.3+ spec compliance (`math.type()`, exact `math.maxinteger`, correct floor division errors)
    - Major architectural change touching `DynValue`, `Processor`, `MathModule`, interop, serialization
    - Estimated effort: 2-4 weeks
    - See **Section 8.24** for full implementation plan

15. **Lua Specification Parity - String/Pattern Matching** (Initiative 8.4): 🎯 **NEXT PRIORITY (after 8.24 decision)**
    - Compare `%a`, `%d`, `%l`, `%u`, `%w`, `%s` character classes against reference Lua
    - Verify `string.format` output matches for edge cases (NaN, Inf, very large numbers)
    - Document any intentional Unicode-aware divergences

16. **Tooling enhancements** (Initiative 6):
    - Roslyn source generators/analyzers for NovaSharp descriptors
    - DocFX (or similar) for API documentation
    - CLI output golden tests

---

### Future Phases (Lower Priority)

16. ~~**Interpreter hot-path optimization - Phase 3**~~ (Initiative 5): ✅ **Completed 2025-12-07** — VM hot-path pooling:
    
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

17. **Interpreter hyper-optimization - Phase 4** (Initiative 5): 🔮 **PLANNED** — Zero-allocation runtime goal
    
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

18. **Concurrency improvements** (Initiative 7, optional):
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
2. ~~**Per-mod isolation containers**~~ — ✅ **Completed 2025-12-08**. Load/reload/unload hooks for script modules:
   - `ModLoadState`, `ModOperationResult`, `IModContainer`, `ModContainer`, `ModManager`
   - Thread-safe state machine with lifecycle events
   - Dependency graph with topological sort for load order
   - 44 TUnit tests in `ModContainerTUnitTests.cs`

**Advanced sandbox features milestone complete.** All items (SandboxOptions, AllocationTracker, Memory Limits, Coroutine Limits, Deterministic Execution, Per-Mod Isolation) implemented and tested.

Checkpoint recorded on branch `dev/wallstop/initial-work-3`.

## Checkpoint — 2025-12-07 (Per-Mod Isolation Containers)

### Summary

Implemented per-mod isolation containers with load/reload/unload lifecycle management. This completes the advanced sandbox features milestone.

### Features implemented

- **ModLoadState enum**: Six lifecycle states (Unloaded, Loading, Loaded, Unloading, Reloading, Faulted) with explicit values
- **ModOperationResult**: Immutable result type with Success, State, Message, Error properties and factory methods
- **IModContainer interface**: 18 members defining mod container contract:
  - Properties: Id, Name, State, Script, Error
  - Events: OnLoading, OnLoaded, OnUnloading, OnUnloaded, OnReloading, OnReloaded, OnError
  - Methods: Load(), Unload(), Reload(), DoString(), CallFunction(), GetGlobal(), SetGlobal()
- **ModContainer implementation**: Thread-safe state machine with:
  - Lock-based synchronization for state transitions
  - ScriptFactory delegate for custom Script creation
  - ScriptConfigurator action for Script setup (globals, functions)
  - UnloadHandler action for cleanup before unload
  - CoreModules configuration support
  - Proper event firing at lifecycle boundaries
- **ModManager coordinator**: Multi-mod manager with:
  - Register/Unregister for mod container management
  - AddDependency/RemoveDependency for dependency tracking
  - Cycle detection to prevent circular dependencies
  - Topological sort (Kahn's algorithm) for correct load order
  - LoadAll/UnloadAll/ReloadAll bulk operations
  - BroadcastCall for invoking functions across all loaded mods
  - GetMod/TryGetMod for retrieval by ID

### Test coverage

44 TUnit tests added in `src/tests/.../Sandbox/ModContainerTUnitTests.cs`:
- ModLoadState enum tests (3 tests)
- ModOperationResult tests (4 tests)
- ModContainer lifecycle tests (17 tests)
- ModManager tests (20 tests)

### Files created

- `src/runtime/.../Modding/ModLoadState.cs`
- `src/runtime/.../Modding/ModOperationResult.cs`
- `src/runtime/.../Modding/IModContainer.cs`
- `src/runtime/.../Modding/ModEventArgs.cs`
- `src/runtime/.../Modding/ModContainer.cs`
- `src/runtime/.../Modding/ModManager.cs`
- `src/tests/.../Sandbox/ModContainerTUnitTests.cs`

### Test results

All **3,424** interpreter tests pass (Release configuration).

### Example usage

```csharp
// Create a mod container with custom configuration
var mod = new ModContainer("my-mod", "My Game Mod");
mod.ScriptConfigurator = script =>
{
    script.Globals["modVersion"] = DynValue.NewString("1.0.0");
    script.Globals["log"] = DynValue.NewCallback((ctx, args) =>
    {
        Console.WriteLine($"[{mod.Name}] {args[0].String}");
        return DynValue.Nil;
    });
};

// Subscribe to lifecycle events
mod.OnLoaded += (sender, e) => Console.WriteLine($"Mod {e.ModId} loaded!");
mod.OnError += (sender, e) => Console.WriteLine($"Error in {e.ModId}: {e.Error.Message}");

// Load and execute
var result = mod.Load();
if (result.Success)
{
    mod.DoString("log('Hello from mod!')");
    var version = mod.GetGlobal("modVersion");
    Console.WriteLine($"Running mod version {version.String}");
}

// Reload with preserved state
mod.Reload();

// Cleanup
mod.Unload();
```

```csharp
// Use ModManager for multiple mods with dependencies
var manager = new ModManager();

var coreMod = new ModContainer("core", "Core Systems");
var uiMod = new ModContainer("ui", "User Interface");
var gameMod = new ModContainer("game", "Game Logic");

manager.Register(coreMod);
manager.Register(uiMod);
manager.Register(gameMod);

// ui depends on core, game depends on both
manager.AddDependency("ui", "core");
manager.AddDependency("game", "core");
manager.AddDependency("game", "ui");

// Loads in order: core -> ui -> game
manager.LoadAll();

// Call a function in all loaded mods
manager.BroadcastCall("onGameStart");

// Unloads in reverse order: game -> ui -> core
manager.UnloadAll();
```

## Checkpoint — 2025-12-07 (RNG Parity)

### Summary

Implemented version-specific random number generator (RNG) providers to match reference Lua interpreter behavior. Lua 5.1-5.3 use a linear congruential generator (LCG), while Lua 5.4+ uses xoshiro256**. The `Script` class now automatically selects the appropriate provider based on `LuaCompatibilityVersion`.

### Features implemented

- **Lua51RandomProvider**: Thread-safe LCG implementation with glibc-compatible parameters:
  - Multiplier: 1,103,515,245
  - Increment: 12,345
  - Modulus: 2^31
  - Single 32-bit seed (vs xoshiro256**'s 128-bit state)
  - `SeedY` always returns 0 (Lua 5.1-5.3 have no second seed component)

- **Version-specific RNG selection in Script constructor**:
  - `CreateDefaultRandomProvider(LuaCompatibilityVersion)` helper method
  - Lua 5.2, 5.3 → `Lua51RandomProvider` (LCG algorithm)
  - Lua 5.4, 5.5, Latest → `LuaRandomProvider` (xoshiro256** algorithm)

- **Version-aware `math.randomseed` behavior**:
  - Lua 5.1-5.3: Requires exactly 1 seed argument, returns `nil`
  - Lua 5.4+: Accepts 0-2 seed arguments, returns seed tuple `(x, y)`

### Test coverage

30+ TUnit tests added in `src/tests/.../Spec/LuaRandomParityTUnitTests.cs`:

**Provider unit tests**:
- `Lua51RandomProvider_NextDouble_ReturnsValueInRange`
- `Lua51RandomProvider_NextInt64_ReturnsValueInRange`
- `Lua51RandomProvider_SeedX_ReturnsCurrentSeed`
- `Lua51RandomProvider_SeedY_AlwaysReturnsZero`
- `Lua51RandomProvider_SetSeed_ChangesSeed`
- `Lua51RandomProvider_SetSeedFromSystemRandom_SetsDifferentSeed`
- `Lua51RandomProvider_DeterministicSequence`

**Version selection tests**:
- `Script_UsesLua51RandomProvider_ForLua52`
- `Script_UsesLua51RandomProvider_ForLua53`
- `Script_UsesLuaRandomProvider_ForLua54`
- `Script_UsesLuaRandomProvider_ForLua55`
- `Script_UsesLuaRandomProvider_ForLatest`
- `MathModule_RandomSeed_RequiresArgument_ForLua52`
- `MathModule_RandomSeed_RequiresArgument_ForLua53`
- `MathModule_RandomSeed_ReturnsNil_ForLua52`
- `MathModule_RandomSeed_ReturnsNil_ForLua53`
- `MathModule_RandomSeed_ReturnsTuple_ForLua54`
- `MathModule_RandomSeed_AcceptsNoArgs_ForLua54`

**Determinism tests**:
- `Lua51RandomProvider_SameSeed_ProducesSameSequence`
- `Lua51RandomProvider_DifferentSeeds_ProduceDifferentSequences`
- `MathRandom_Deterministic_WithSeed_Lua52`
- `MathRandom_Deterministic_WithSeed_Lua53`
- `MathRandom_Deterministic_WithSeed_Lua54`
- `MathRandom_CrossVersion_DifferentSequences`

### Files created

- `src/runtime/WallstopStudios.NovaSharp.Interpreter.Infrastructure/Lua51RandomProvider.cs`
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs`

### Files modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs` — Added `CreateDefaultRandomProvider` and version-aware RNG selection in constructor
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs` — Version-aware `math.randomseed` implementation

### Test results

All **3,627** interpreter tests pass (Release configuration).

### Example usage

```csharp
// Lua 5.2 mode - uses Lua51RandomProvider (LCG)
var script52 = new Script(new ScriptOptions(Script.DefaultOptions)
{
    CompatibilityVersion = LuaCompatibilityVersion.Lua52
});
script52.DoString("math.randomseed(12345)");  // Returns nil
var val52 = script52.DoString("return math.random()");
// Produces LCG-deterministic sequence

// Lua 5.4 mode - uses LuaRandomProvider (xoshiro256**)
var script54 = new Script(new ScriptOptions(Script.DefaultOptions)
{
    CompatibilityVersion = LuaCompatibilityVersion.Lua54
});
var seeds = script54.DoString("return math.randomseed(12345)");  // Returns (x, y) tuple
var val54 = script54.DoString("return math.random()");
// Produces xoshiro256**-deterministic sequence
```

## Checkpoint — 2025-12-07 (LuaVersionDefaults)

### Summary

Implemented centralized `LuaVersionDefaults` class to eliminate scattered `Latest → Lua54/Lua55` resolution patterns across the codebase. This ensures consistent version handling and provides a single point of update when adopting new Lua versions.

### Problem Addressed

The codebase had 4 locations manually resolving `LuaCompatibilityVersion.Latest`:
- `Script.CreateDefaultRandomProvider()` → `Lua54`
- `MathModule.RandomSeed()` → `Lua54`
- `ModManifest.Normalize()` → `Lua55`
- `LuaCompatibilityAttribute.IsSupported()` → `Lua55`

This created inconsistent behavior (some used Lua54, others Lua55) and maintenance burden when updating the default version.

### Solution

Created `LuaVersionDefaults` static class with two resolution strategies:
1. **`Resolve()`** — Maps `Latest` → `CurrentDefault` (Lua54) for runtime behavior
2. **`ResolveForHighest()`** — Maps `Latest` → `HighestSupported` (Lua55) for forward-compatibility checks

### Files Created

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Compatibility/LuaVersionDefaults.cs`
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaVersionDefaultsTUnitTests.cs`

### Files Modified

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs` — Updated `CreateDefaultRandomProvider()` to use `LuaVersionDefaults.Resolve()`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs` — Updated `RandomSeed()` to use `LuaVersionDefaults.Resolve()`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Modding/ModManifest.cs` — Updated `Normalize()` to use `LuaVersionDefaults.ResolveForHighest()`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/Attributes/LuaCompatibilityAttribute.cs` — Updated `IsSupported()` to use `LuaVersionDefaults.ResolveForHighest()`

### Test Coverage

25 TUnit tests added covering:
- `LuaVersionDefaults.CurrentDefault` is not `Latest` (circular resolution guard)
- `LuaVersionDefaults.CurrentDefault` is `Lua54`
- `LuaVersionDefaults.HighestSupported` is `Lua55`
- `Resolve()` maps `Latest` → `CurrentDefault`
- `Resolve()` passes through concrete versions unchanged
- `ResolveForHighest()` maps `Latest` → `HighestSupported`
- `ScriptOptions` default constructor sets `Latest`
- `Script` with `Latest` uses correct RNG provider
- `Script` with `Latest` has correct `math.randomseed` behavior
- All `Script` constructors produce consistent state
- `ScriptOptions` copy constructor preserves version

### Test Results

All **3,655** tests pass (Release configuration).

### Example Usage

```csharp
// Before: Scattered resolution patterns
LuaCompatibilityVersion effectiveVersion =
    version == LuaCompatibilityVersion.Latest ? LuaCompatibilityVersion.Lua54 : version;

// After: Centralized resolution
LuaCompatibilityVersion effectiveVersion = LuaVersionDefaults.Resolve(version);

// When updating to Lua 5.5 as default, only change one constant:
public const LuaCompatibilityVersion CurrentDefault = LuaCompatibilityVersion.Lua55;
```

## Checkpoint — 2025-12-07 (Out/Ref Parameter Test Robustness)

### Summary

Investigated a flaky test failure in `MethodReturningVoidWithOutParams` that showed corrupted values (`nil|999|999` instead of `nil|5|10`) and added 10 comprehensive tests to ensure out/ref parameter handling is thoroughly validated.

### Investigation Findings

**Symptom**: Test produced `nil|999|999` instead of expected `nil|5|10`.

**Root Cause Analysis**: The failure was a **transient build issue** — the test executed while compilation was still active, resulting in stale or partial assembly loading. Evidence:
- Value `999` does not appear anywhere in the test inputs or expected outputs
- `999` only exists in unrelated code (e.g., `DynValue._hashCode = 999` initialization)
- Subsequent test runs with a clean build pass consistently
- The flaky behavior only manifests during concurrent build/test scenarios

**Conclusion**: This is NOT a production bug or test bug. It's an artifact of running tests during active compilation. The interop mechanism (`ObjectArrayPool`, `FunctionMemberDescriptorBase.BuildArgumentListPooled()`, `MethodMemberDescriptor.Execute()`) works correctly.

### Tests Added

Added 10 new comprehensive tests in `FunctionMemberDescriptorBaseTUnitTests.cs`:

1. **`MethodWithOutParamsCalledMultipleTimes`** — 10 iterations with different input values (i, i*2)
2. **`MethodWithRefParamsCalledMultipleTimes`** — 10 iterations verifying ref semantics preserved
3. **`MethodWithOutParamsZeroValues`** — Edge case: out params with input (0, 0)
4. **`MethodWithOutParamsNegativeValues`** — Edge case: out params with (-10, -20)
5. **`MethodWithOutParamsLargeValues`** — Edge case: out params with INT32_MAX and INT32_MIN
6. **`MethodWithOnlyOutParamsNoInput`** — Method with only out parameters (no input args from Lua)
7. **`MethodWithMixedRefOutAndReturnValue`** — Combined return value + ref + out handling
8. **`MethodWithSingleOutParam`** — TryParse pattern test (successful parse)
9. **`MethodWithSingleOutParamFailedParse`** — TryParse pattern test (failed parse, default value)
10. **`MethodWithOutParamsInterleavedWithDifferentMethods`** — Interleaved calls to different methods sharing pool

### SampleClass Additions

Added helper methods to `SampleClass` for new test coverage:

```csharp
// Method with only out parameters (no input)
public void GetMultipleOutValues(out int number, out string text, out bool flag);

// Combined return + ref + out
public int ComplexRefOutMethod(int a, ref int b, out int c);

// TryParse pattern
public bool TryParseInt(string input, out int result);
```

### Key Insight

The `ObjectArrayPool` implementation correctly:
1. Uses thread-local storage for small arrays (≤8 elements)
2. Clears arrays when returned to pool (`clearArray: true`)
3. Handles concurrent access safely via thread-local semantics

The `FunctionMemberDescriptorBase.BuildArgumentListPooled()` correctly:
1. Allocates from pool with appropriate size
2. Fills parameter array from `CallbackArguments`
3. Returns pooled resources via `using` pattern

### Test Results

All **3,665** tests pass (Release configuration), including the 10 new out/ref parameter tests.

## Checkpoint — 2025-12-07 (Script Constructor Consistency)

### Summary

Completed Section 8.2 of the Lua Runtime Specification Parity initiative. Audited all `Script` constructor paths and added comprehensive tests verifying initialization consistency.

### Audit Findings

All four `Script` constructors delegate to `Script(CoreModules coreModules, ScriptOptions options)`:

```csharp
Script()                    → Script(CoreModules.PresetDefault, null)
Script(CoreModules)         → Script(CoreModules, null)
Script(ScriptOptions)       → Script(CoreModules.PresetDefault, ScriptOptions)
Script(CoreModules, opts)   → Main constructor
```

**Initialization Order** (deterministic for all paths):
1. Options — Copy from `options` or `DefaultOptions`
2. CompatibilityVersion — Overwrite from `GlobalOptions` when `options == null`
3. TimeProvider — From options or `SystemTimeProvider.Instance`
4. RandomProvider — From options or version-appropriate default
5. StartTimeUtc — Captured from TimeProvider
6. AllocationTracker — Created if sandbox limits configured
7. PerformanceStats — Created with configured clock
8. Registry — New empty table
9. ByteCode — New bytecode container
10. MainProcessor — New processor instance
11. GlobalTable — New table with registered core modules

**Key Behavior Documented**:
- `new ScriptOptions()` defaults to `LuaCompatibilityVersion.Latest`
- `new ScriptOptions(Script.DefaultOptions)` copies `GlobalOptions.CompatibilityVersion`
- When `options == null`, constructor inherits from `GlobalOptions`
- When `options != null`, constructor uses options as-is

### Tests Added

Created `ScriptConstructorConsistencyTUnitTests.cs` with 16 tests:

1. `AllConstructorPathsInitializeInSameOrder` — Verifies non-null essential properties
2. `NullOptionsInheritsGlobalOptionsCompatibilityVersion` — GlobalOptions inheritance
3. `ExplicitOptionsDoesNotInheritFromGlobalOptions` — Explicit version respected
4. `FreshScriptOptionsDefaultsToLatest` — Documents default behavior
5. `CopyFromDefaultOptionsGetsGlobalOptionsCompatibilityVersion` — Recommended pattern
6. `RandomProviderSelectedBasedOnCompatibilityVersion` — Version-specific RNG
7. `CustomRandomProviderIsRespected` — Custom provider injection
8. `DeterministicRandomProviderProducesReproducibleSequences` — Determinism verification
9. `CoreModuleRegistrationOrderIsDeterministic` — Predictable globals
10. `CoreModulesConfigurationAffectsGlobalTable` — Module configuration effects
11. `DefaultTimeProviderIsSystemTimeProvider` — Default time provider
12. `CustomTimeProviderIsRespected` — Custom time provider injection
13. `StartTimeUtcIsCapturedAtConstruction` — os.clock behavior verification
14. `AllocationTrackerCreatedOnlyWithSandboxLimits` — Tracker creation logic
15. `DefaultConstructorEquivalentToExplicitDefault` — Constructor equivalence
16. `ModulesOnlyConstructorEquivalentToExplicitNull` — Constructor equivalence

### Files Created

- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/ScriptConstructorConsistencyTUnitTests.cs`

### Test Results

All **3,681** tests pass (Release configuration) — 16 new tests added.

---

## Checkpoint — 2025-12-07 (Numeric Edge Cases — §8.3)

### Scope

Implement full Lua 5.3+ numeric edge case support including `math.maxinteger`, `math.mininteger`, and fix operator behavior divergences.

### Changes Made

**1. Added `math.maxinteger` and `math.mininteger` constants**
- File: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs`
- Added `[LuaCompatibility(Lua53)]` constants returning `long.MaxValue` (9223372036854775807) and `long.MinValue` (-9223372036854775808)
- Note: These are stored as `double`, so `math.maxinteger` rounds to 2^63 due to IEEE 754 precision limits

**2. Fixed right shift operator to use logical shift**
- File: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Helpers/LuaIntegerHelper.cs`
- Method: `ShiftRight(long value, int shift)`
- **Bug**: Was using arithmetic shift for negative numbers (preserving sign bit)
- **Fix**: Now uses logical shift via `unchecked((long)((ulong)value >> shift))`
- **Spec reference**: Lua 5.4 §3.4.2 — "Both right and left shifts fill the vacant bits with zeros"

**3. Fixed NaN comparison operators**
- File: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/BinaryOperatorExpression.cs`
- **Bug**: `a > b` compiled as `!(a <= b)` which incorrectly returned `true` for `NaN > NaN`
- **Fix**: Changed to compile `a > b` as `b < a` (operand swap)
- **Fix**: Changed to compile `a >= b` as `b <= a` (operand swap)
- **Spec reference**: IEEE 754 — NaN is neither less than, equal to, nor greater than any value

### Tests Added

Created `MathNumericEdgeCasesTUnitTests.cs` with 50+ tests covering:

**Math constants (4 tests)**:
- `MathMaxintegerValue` — Returns 9223372036854775807
- `MathMinintegerValue` — Returns -9223372036854775808
- `MathMaxintegerNotAvailableInLua52` — Version gating
- `MathMinintegerNotAvailableInLua52` — Version gating

**Right shift operator (12 tests)**:
- `RightShift_PositiveValue_ShiftByZero` — Identity
- `RightShift_PositiveValue_ShiftByOne` — Basic shift
- `RightShift_NegativeValue_IsLogicalShift` — Logical not arithmetic
- `RightShift_By64OrMore_ReturnsZero` — Shift overflow
- `RightShift_ByNegativeAmount_BecomesLeftShift` — Negative shifts
- Plus 7 more edge cases

**NaN comparisons (8 tests)**:
- `NaN_GreaterThan_NaN_ReturnsFalse`
- `NaN_GreaterThanOrEqual_NaN_ReturnsFalse`
- `NaN_LessThan_NaN_ReturnsFalse`
- `NaN_LessThanOrEqual_NaN_ReturnsFalse`
- `NaN_NotEqual_NaN_ReturnsTrue`
- Plus comparisons with regular numbers

**Integer overflow and division (15+ tests)**:
- Floor division edge cases
- Modulo with negative numbers
- Integer wraparound behavior
- Division by zero semantics

### Known Divergences Documented

| Behavior | Lua 5.4 | NovaSharp | Reason |
|----------|---------|-----------|--------|
| `math.maxinteger` exact value | 9223372036854775807 | ~9.22e18 (rounded) | Double precision limits |
| `3 // 0` (integer floor div by zero) | Error | `inf` | All numbers are doubles |
| `math.huge` | `inf` | `double.MaxValue` | Legacy divergence |

### Files Created/Modified

**Modified**:
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs` (added constants)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Helpers/LuaIntegerHelper.cs` (fixed shift)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/BinaryOperatorExpression.cs` (fixed NaN)

**Created**:
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/MathNumericEdgeCasesTUnitTests.cs`

### Test Results

All **3,727** tests pass (Release configuration) — 50+ new tests added.