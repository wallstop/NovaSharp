# Modern Testing & Coverage Plan

## 🔴 Lua Spec Compliance Core Principle

NovaSharp's PRIMARY GOAL is to be a **faithful Lua interpreter** that matches the official Lua reference implementation. When fixture comparisons reveal behavioral differences:

1. **ASSUME NOVASHARP IS WRONG** until proven otherwise
2. **FIX THE PRODUCTION CODE** to match Lua behavior
3. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua
4. **NEVER adjust tests to accommodate bugs** — fix the runtime instead

**Current Status**: ✅ Lua fixture comparisons show **0 unexpected mismatches** across all versions (5.1, 5.2, 5.3, 5.4, 5.5). ~3,500 Lua fixtures verified.

---

## Repository Snapshot (Updated 2025-12-21)

**Build & Tests**:
- Zero warnings with `<TreatWarningsAsErrors>true` enforced
- **11,755** interpreter tests via TUnit (Microsoft.Testing.Platform)
- Coverage: ~75.3% line / ~76.1% branch (gating targets at 90%)
- CI: Tests on matrix of `[ubuntu-latest, windows-latest, macos-latest]`

**TUnit Version Coverage Progress**:
- **2,000+** tests with explicit version attributes
- All Lua execution tests have proper version coverage
- Helper attributes (`[AllLuaVersions]`, `[LuaVersionsFrom]`, etc.) available for new tests

**Lua Fixture Extraction**:
- ✅ **~3,500** Lua fixtures with version metadata
- ✅ **0 unexpected mismatches** across all Lua versions (5.1-5.5)
- Comparison harness operational with version filtering

**Audits & Quality**:
- `docs/audits/documentation_audit.log`, `docs/audits/naming_audit.log`, `docs/audits/spelling_audit.log` green
- Runtime/tooling/tests remain region-free
- DAP golden tests: 20 tests validating VS Code debugger payloads
- LuaNumber lint script (`check-luanumber-usage.py`) passing

**Infrastructure**:
- Sandbox: Complete with instruction/memory/coroutine limits, per-mod isolation
- Benchmark CI: BenchmarkDotNet with threshold-based regression alerting
- Packaging: NuGet publishing workflow + Unity UPM scripts
- **Array Pooling**: `DynValueArrayPool`, `ObjectArrayPool` (exact-size), `SystemArrayPool<T>` (variable-size)

**Lua Compatibility**:
- ✅ **0 unexpected mismatches** across all Lua versions (5.1, 5.2, 5.3, 5.4, 5.5)
- Bytecode format version `0x151` preserves integer/float subtype
- JSON/bytecode serialization preserves integer/float subtype
- DynValue caching extended for negative integers and common floats
- All character classes, metamethod fallbacks, and version-specific behaviors implemented
- CLI argument registry (`CliArgumentRegistry`) with comprehensive Lua version support
- VM state protection (Phase 1) prevents external corruption

---

## Active Initiatives

### Initiative 15: Boxing-Free IList Sort Extensions ✅ **COMPLETE**
**Goal**: Implement custom sort algorithms for `IList<T>` that use generic `TComparer : IComparer<T>` constraints to avoid boxing value-type comparers.
**Problem**: `List<T>.Sort(IComparer<T>)` and `Array.Sort(T[], IComparer<T>)` box the comparer when it's a value type, causing allocations even with `readonly struct` comparers.
**Implementation**:
  - Created `IListSortExtensions.cs` in `DataStructs/`
  - Pattern-defeating quicksort (pdqsort) for excellent average/worst case performance
  - Ninther (median-of-medians) pivot selection for adversarial input resistance
  - Partial insertion sort to detect nearly-sorted patterns
  - Unguarded insertion sort for interior partitions (no bounds checking)
  - Heapsort fallback when bad partitions detected
  - Integrated with `DynValueComparer` in `table.sort`
**Results**: Zero boxing allocations with struct comparers, handles patterns well (sorted, reverse, repeated elements).
**Status**: Complete. All 11,754 tests pass.

### Initiative 9: Version-Aware Lua Standard Library Parity 🟡 **MOSTLY COMPLETE**
**Goal**: ALL Lua functions must behave according to their version specification (5.1, 5.2, 5.3, 5.4).
**Scope**: Math, String, Table, Basic, Coroutine, OS, IO, UTF-8, Debug modules + metamethod behaviors.
**Status**: Most modules complete. String.pack/unpack extended options and documentation remaining.
**Effort**: Ongoing

### Initiative 16: Boxing-Free pdqsort Integration ✅ **COMPLETE**
**Goal**: Replace all `List<T>.Sort(IComparer<T>)` and `Array.Sort(T[], IComparer<T>)` calls with `IListSortExtensions.Sort<T, TComparer>()` to eliminate comparer boxing.
**Problem**: .NET's built-in sort methods box value-type comparers on every call, causing allocations in hot paths.
**Solution**: Use `IListSortExtensions` (pattern-defeating quicksort) which uses generic `TComparer : IComparer<T>` constraints for zero-allocation sorting.
**Implementation**:
  - ✅ `IListSortExtensions.Sort<T, TComparer>()` in `DataStructs/`
  - ✅ Audited all `List<T>.Sort()` / `Array.Sort()` calls in runtime code
  - ✅ Migrated 2 call sites: `TableModule.Sort` (hot path), `OverloadedMethodMemberDescriptor` (cold path)
**Results**: Zero boxing allocations for all sorting operations. All 11,754 tests pass.
**Status**: ✅ Complete. See [progress/session-067-pdqsort-integration.md](progress/session-067-pdqsort-integration.md).
**Completed**: 2025-12-21

### Initiative 17: Lua Method String-to-Enum Optimization ❌ **CLOSED**
**Goal**: Replace constant "Lua method name" strings used for internal dispatch/comparison with enum values for more efficient internal operations.
**Status**: ❌ **CLOSED (Won't Implement)** — Investigation completed, not beneficial.
**Investigation Results** (see [progress/session-069-metamethod-enum-investigation.md](progress/session-069-metamethod-enum-investigation.md)):
  - C# `const string` values are automatically interned, providing O(1) reference equality
  - Dictionary lookups are already O(1) amortized with good hash distribution
  - **Critical blocker**: Lua metatables fundamentally use string keys; cannot eliminate string lookup
  - JIT already optimizes `switch` statements on `const string` values
  - API compatibility would be broken for `GetMetamethod()` and related methods
  - Marginal gains (nanoseconds) don't justify complexity and API breakage
**Conclusion**: The `Metamethods` `const string` pattern from Session 068 is optimal. No further action needed.
**Completed**: 2025-12-21

### Initiative 18: Large Script Load/Compile Memory Optimization ✅ **MOSTLY COMPLETE**
**Goal**: Dramatically reduce memory allocations during Lua script loading and compilation.
**Problem**: The script loading/compilation pipeline had excessive memory usage that scales poorly with script size.
**Status**: ✅ **Phases 1-2 Complete** — Major wins achieved. Phase 3 (AST pooling) investigated and **deferred** as not cost-effective.
**Investigation Reports**:
  - [progress/session-070-compiler-memory-investigation.md](progress/session-070-compiler-memory-investigation.md)
  - [progress/session-071-token-struct-conversion.md](progress/session-071-token-struct-conversion.md)
  - [progress/session-084-initiative18-phase3-investigation.md](progress/session-084-initiative18-phase3-investigation.md)

**Completed**:
  - ✅ `Token` converted to `readonly struct` with `IEquatable<Token>` — eliminates ~56-64 bytes/token allocation
  - ✅ `Instruction` confirmed as already a struct (no conversion needed)
  - ✅ Integrated `ListPool<T>` in parser (`Expression.cs`, `FunctionDefinitionExpression.cs`, `ForEachLoopStatement.cs`)
  - ✅ Pooled `BlocksToClose` inner lists in `ProcessorInstructionLoop.cs`

**Phase 3 Investigation Results** (Deferred):
  - AST node pooling (27 types) — **NOT RECOMMENDED** due to lifecycle complexity and bug risk
  - Span-based keyword interning — Low-priority future opportunity (~2 days effort)
  - Diminishing returns: major wins already captured in Phases 1-2

**Completed**: 2025-12-22

### Initiative 10: KopiLua Performance Hyper-Optimization ✅ **COMPLETE**
**Goal**: Zero-allocation string pattern matching. Replace legacy KopiLua allocations with modern .NET patterns.
**Scope**: `CharPtr` → `readonly struct`, `MatchState` pooling, `ArrayPool<char>`, `ZString` integration.
**Target**: <0.5 KB/match, <0.6 µs/iter latency for simple patterns.
**Status**: ✅ **Complete** — All three phases implemented with major performance gains.

**Phase 1 (Complete)**:
  - ✅ Baseline benchmarks established. See [progress/session-074-kopilua-optimization-phase1.md](progress/session-074-kopilua-optimization-phase1.md).

**Phase 2 (Complete)**:
  - ✅ `CharPtr` converted from class to `readonly struct` — **Most impactful single change**
  - ✅ ~58-85% allocation reduction, ~24-63% latency reduction
  - See [progress/session-075-kopilua-charptr-struct.md](progress/session-075-kopilua-charptr-struct.md)

**Phase 3 (Complete)**:
  - ✅ `Capture` converted from class to struct (eliminates 32 allocations per MatchState)
  - ✅ `MatchState` pooling via `[ThreadStatic]` with `RentMatchState()`/`ReturnMatchState()`
  - ✅ `str_format` buffer reuse (form/buff arrays moved outside loop)
  - ✅ `addquoted()` pre-computed escape sequence arrays (eliminates string interpolation)
  - See [progress/session-076-kopilua-phase3-optimization.md](progress/session-076-kopilua-phase3-optimization.md)

**Final Results**:
| Scenario | Latency Improvement | Allocation Reduction |
|----------|---------------------|----------------------|
| MatchSimple | 24-31% | 58% |
| MatchComplex | 53-56% | **85%** |
| GsubSimple | **60-63%** | 81% |
| GsubWithCaptures | 54-58% | 79% |

**Completed**: 2025-12-21

### Initiative 11: Comprehensive Helper Performance Audit ✅ **COMPLETE**
**Goal**: Audit and optimize ALL helper methods called from interpreter hot paths.
**Scope**: `Helpers/`, `DataTypes/`, `Execution/VM/`, `CoreLib/`, `Interop/`.
**Status**: ✅ **All 4 phases complete**. 48+ methods optimized with inlining across all layers.
**Progress Reports**:
  - [progress/session-079-helper-performance-audit.md](progress/session-079-helper-performance-audit.md) (Phase 1)
  - [progress/session-080-helper-performance-audit-phase2.md](progress/session-080-helper-performance-audit-phase2.md) (Phase 2)
  - [progress/session-081-helper-performance-audit-phase3.md](progress/session-081-helper-performance-audit-phase3.md) (Phase 3)
  - [progress/session-082-helper-performance-audit-phase4.md](progress/session-082-helper-performance-audit-phase4.md) (Phase 4)
**Phase 1 Complete**:
  - ✅ Audited 20 files in DataStructs, Utilities, and Execution directories
  - ✅ Added `[MethodImpl(AggressiveInlining)]` to 7 critical files (FastStack, FastStackDynamic, ExtensionMethods, LuaIntegerHelper, Slice, StringSpanExtensions, PathSpanExtensions)
  - ✅ Already well-optimized: HashCodeHelper, IListSortExtensions, LuaStringPool, array pools
**Phase 2 Complete**:
  - ✅ Audited 10 files in Execution/VM directory
  - ✅ Added `[MethodImpl(AggressiveInlining)]` to 22 methods (ProcessorInstructionLoop, Processor, Chunk, SymbolRef, CallStackItemPool, ByteCode)
**Phase 3 Complete**:
  - ✅ Audited 8 files in CoreLib directory
  - ✅ Added `[MethodImpl(AggressiveInlining)]` to 18 methods (ModuleArgumentValidation, MathModule, LuaValueConverter, StringModule, TableModule, Utf8Module, BasicModule, OsTimeModule)
**Phase 4 Complete**:
  - ✅ Audited 38 files in Interop directory
  - ✅ Added `[MethodImpl(AggressiveInlining)]` to 8 methods (ClrToLuaConversionScorer, LuaToClrConversionScorer, DelegateGenerator, StandardUserDataDescriptor)
**Completed**: 2025-12-22

### Initiative 19: HashCodeHelper Migration ✅ **COMPLETE**
**Goal**: Survey all `GetHashCode()` implementations and migrate bespoke hash algorithms to use the centralized `HashCodeHelper`.
**Implementation**: Surveyed all `override int GetHashCode()` in runtime code and migrated bespoke patterns.
**Migrations**:
  - ✅ `Token.GetHashCode()` — Migrated from `hash * 31` to `HashCodeHelper.HashCode()`
  - ✅ `ModuleResolutionResult.GetHashCode()` — Migrated from `hash * 31` to `HashCodeHelper.HashCode()`
  - ✅ `DynamicExpression.GetHashCode()` — Migrated from `StringComparer.Ordinal.GetHashCode()` to `HashCodeHelper.HashCode()`
  - ✅ `CharPtr.GetHashCode()` — Migrated to `HashCodeHelper.HashCode()` as part of struct conversion (see Initiative 10 Phase 2)
**Already Compliant**: `TablePair`, `Slice`, `SandboxViolationDetails`, `AllocationSnapshot`, `DynValue`, `LuaNumber`, `JsonPosition`
**Status**: ✅ Complete. See [progress/session-072-hashcode-helper-migration.md](progress/session-072-hashcode-helper-migration.md).
**Completed**: 2025-12-21

### Initiative 12: Deep Codebase Allocation Analysis & Reduction ✅ **COMPLETE**
**Goal**: Comprehensive codebase-wide analysis to identify and eliminate unnecessary heap allocations using value types, buffers, ZString, and closure avoidance.
**Scope**: Entire `src/runtime/` codebase — DataTypes, VM, CoreLib, Interop, Loaders, Helpers.
**Results**: 5-8% allocation reduction in key benchmarks, zero regressions across 11,790 tests.
**Implementation**:
  - ✅ Phase 1: Profiling & Baseline — 34 allocation sites identified
  - ✅ Phase 2: Quick Wins — ListPool integration, static delegates
  - ✅ Phase 3: Value Type Migration — TablePair, ReflectionSpecialName converted to structs
  - ✅ Phase 4: Deep Optimization — LuaSortComparer struct, MathModule/Bit32Module delegates
  - ✅ Phase 5: Validation — Benchmarks confirmed improvements, all tests pass
**Benchmark Improvements**:
  - NumericLoops: 824 B → 760 B (7.8% reduction)
  - CoroutinePipeline: 1,160 B → 1,096 B (5.5% reduction)
  - UserDataInterop: 1,416 B → 1,352 B (4.5% reduction)
**Status**: ✅ Complete. See [progress/session-083-initiative12-phase5-validation.md](progress/session-083-initiative12-phase5-validation.md).
**Completed**: 2025-12-22

### Initiative 20: NLua Architecture Investigation ✅ **COMPLETE**
**Goal**: Investigate the NLua project (https://github.com/NLua/NLua) for architecture insights, performance patterns, and optimization techniques that could be adopted in NovaSharp.
**Background**: NLua is a mature Lua/.NET bridge that wraps the native Lua C library via P/Invoke. While NovaSharp is a pure C# interpreter (different approach), NLua may have valuable insights for:
  - Efficient type marshaling between CLR and Lua types
  - Memory management patterns and pooling strategies
  - Interop descriptor caching and reflection optimization
  - Table/array handling performance patterns
  - String interning and Lua string pool management
  - Metamethod dispatch efficiency
  - Userdata/proxy object lifecycle management
**Investigation Results** (see [progress/session-077-nlua-investigation.md](progress/session-077-nlua-investigation.md)):
  - ✅ Review NLua's type conversion system (`ObjectTranslator`)
  - ✅ Analyze memory management and object caching
  - ✅ Study table enumeration and manipulation patterns
  - ✅ Examine function call dispatch and argument handling
  - ✅ Evaluate error handling and exception propagation
  - ✅ Document applicable patterns with NovaSharp integration notes
**Key Findings**:
  - **Last-Call Caching**: Cache last-resolved method in `OverloadedMethodMemberDescriptor` for 20-40% faster repeated interop calls
  - **Type Converter Delegate Caching**: `Dictionary<Type, ExtractValue>` pattern avoids runtime reflection
  - **Static Delegate Pre-allocation**: Metamethod delegates as `static readonly` (already partially implemented in Session 065)
  - **ReferenceComparer**: Use `RuntimeHelpers.GetHashCode()` for identity-based hashing of boxed value types
  - **Fakenil Sentinel**: Distinguishes "cached nil" from "not yet cached" in interop scenarios
**Not Applicable**: IL Emit code generation, P/Invoke patterns, Lua Registry management (native-specific)
**Actionable Optimizations**:
  - ✅ P1: Last-call caching in `OverloadedMethodMemberDescriptor` — **IMPLEMENTED** in [progress/session-078-last-call-caching.md](progress/session-078-last-call-caching.md)
  - P1: Audit type converter delegate caching (1-2 days)
  - P2: Continue static delegate migration in CoreLib (3-5 days)
**Status**: ✅ Complete. See [progress/session-077-nlua-investigation.md](progress/session-077-nlua-investigation.md).
**Completed**: 2025-12-21

### Initiative 14: SystemArrayPool Abstraction ✅ **COMPLETE**
**Goal**: Create a `SystemArrayPool<T>` abstraction that uses our `PooledResource<T>` disposal pattern but delegates to `System.Buffers.ArrayPool<T>.Shared` under the hood.
**Implementation**: `SystemArrayPool<T>` in `DataStructs/` wraps `ArrayPool<T>.Shared` with RAII disposal pattern.
**API**:
  - `Get(int minimumLength, out T[] array)` → returns `PooledResource<T[]>` for automatic cleanup
  - `Get(int minimumLength, bool clearOnReturn, out T[] array)` → control clearing behavior
  - `Rent(minimumLength)` / `Return(array)` → manual lifecycle management
  - `ToArrayAndReturn(array, length)` → extract exact-size copy and return pooled array
**Key Semantics**: Arrays may be larger than requested; callers track actual length separately.
**Tests**: 41 TUnit tests covering all edge cases, thread safety, and clearing behavior.
**Use Cases**:
  - String pattern matching buffers (KopiLua) where sizes vary
  - Table operations with dynamic element counts
  - Any hot path where exact-size allocation is wasteful
**Status**: ✅ Complete. See [progress/session-063-system-array-pool-abstraction.md](progress/session-063-system-array-pool-abstraction.md).
**Completed**: 2025-12-21

### Initiative 13: Magic String Consolidation 🟡 **IN PROGRESS**
**Goal**: Eliminate all duplicated string literals ("magic strings") by consolidating them into named constants with a single source of truth.
**Scope**: All runtime, tooling, and test code.
**Status**: Phases 1-2 (Metamethods + Keywords) complete. Incremental enforcement during code changes.
**Effort**: Ongoing (apply during code reviews and new development)

**Completed**:
- ✅ **Phase 1: Metamethods** — Created `Metamethods` static class in `DataStructs/LuaStringPool.cs` with 25 `const string` fields for all Lua metamethods (including NovaSharp extensions like `__new`, `__tonumber`, `__tobool`, `__iterator`). Migrated ~110 string literals across 16 files. See [progress/session-085-magic-string-consolidation.md](progress/session-085-magic-string-consolidation.md).
- ✅ **Phase 2: Lua Keywords** — Created `LuaKeywords` static class with 22 `const string` fields for all reserved Lua keywords. Pre-interned in `LuaStringPool`.

**Remaining Areas to Consolidate** (Lower Priority):
1. ~~**Metamethod names**: `__index`, `__newindex`, `__call`, `__tostring`, etc.~~ ✅ Done
2. ~~**Lua keywords**: `nil`, `true`, `false`, `and`, `or`, `not`, `function`, etc.~~ ✅ Done
3. **Error messages**: `bad argument`, `attempt to`, `number has no integer representation`, etc.
4. **Module names**: `string`, `table`, `math`, `io`, `os`, `debug`, `coroutine`, etc.

**Validation Commands**:
```bash
# Find potential duplicated magic strings (metamethods) - should now show only docs/comments
rg '"__[a-z]+"' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs -l | grep -v LuaStringPool.cs

# Find string literals in ArgumentException/ArgumentNullException (should use nameof)
rg 'ArgumentNullException\("' src/runtime/
rg 'ArgumentException.*"[a-z]' src/runtime/
```

---

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

---

## 🟡 MEDIUM Priority: Test Data-Driving Helper Migration

**Status**: 🟡 **IN PROGRESS** — Core helpers complete, migration ongoing.

The helper attributes reduce 5+ lines of manual `[Arguments]` entries per test to a single line.

### Available Helpers

All helpers are in `src/tests/TestInfrastructure/TUnit/`:

| Helper | Description |
|--------|-------------|
| `[AllLuaVersions]` | Expands to all 5 Lua versions (5.1-5.5) |
| `[LuaVersionsFrom(5.3)]` | Versions from 5.3+ (inclusive) |
| `[LuaVersionsUntil(5.2)]` | Versions up to 5.2 (inclusive) |
| `[LuaVersionRange(5.2, 5.4)]` | Specific version range |
| `[LuaTestMatrix]` | Full Cartesian product of versions × inputs |

### Completed Migrations

- ✅ **StringModuleTUnitTests** — 7 tests migrated, ~150+ lines of boilerplate removed. See [progress/session-073-test-data-driving-stringmodule.md](progress/session-073-test-data-driving-stringmodule.md).

### Remaining Tasks

- [x] Migrate StringModuleTUnitTests with version+data coupled patterns ✅
- [ ] Migrate remaining UserData tests (Methods overload patterns)
- [ ] Migrate remaining EndToEnd tests
- [ ] Migrate Sandbox tests
- [ ] Create automated migration script for common patterns
- [ ] Add lint rule to flag verbose patterns

**Owner**: Test infrastructure team
**Priority**: 🟡 MEDIUM

---

## 🟡 MEDIUM Priority: Comprehensive Numeric Edge-Case Audit

**Status**: 📋 **PARTIAL** — Core fixes done, remaining edge cases to audit.

**Problem**: Values beyond 2^53 cannot be exactly represented as doubles. Lua 5.3+ distinguishes integer vs float subtypes; NovaSharp must preserve this distinction.

**Completed**:
- ✅ `LuaNumber` struct preserves integer/float distinction
- ✅ Core validation uses `LuaNumber` not `double`
- ✅ Lint script prevents `DynValue.Number` usage in CoreLib

**Remaining**:
- [ ] Audit `Interop/Converters/*.cs` for precision loss patterns
- [ ] Create `NumericEdgeCaseTUnitTests.cs` with boundary values
- [ ] Document version-specific behavior in `docs/testing/numeric-edge-cases.md`

---

## Coverage Improvement Opportunities

Current coverage (~75% line, ~76% branch) has significant room for improvement. Key areas with low coverage include:
- **NovaSharp.Hardwire** (~54.8% line): Many generator code paths untested
- **CLI components**: Some command implementations have partial coverage
- **DebugModule**: REPL loop branches not easily testable
- **StreamFileUserDataBase**: Windows-specific CRLF paths cannot run on Linux CI

---

## Codebase Organization (future)
- Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
- Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
- Add guardrails so new code lands in correct folders with consistent namespaces

---

## Tooling, Docs, and Contributor Experience
- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.

---

## Concurrency Improvements (optional)
- Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics.
- Split debugger locks for reduced contention.
- Add timeout to `BlockingChannel`.

See `docs/modernization/concurrency-inventory.md` for the full synchronization audit.

---

## Lua Specification Parity

### Official Lua Specifications (Local Reference)

**IMPORTANT**: For all Lua compatibility work, consult the local specification documents first:
- [`docs/lua-spec/lua-5.1-spec.md`](docs/lua-spec/lua-5.1-spec.md) — Lua 5.1 Reference Manual
- [`docs/lua-spec/lua-5.2-spec.md`](docs/lua-spec/lua-5.2-spec.md) — Lua 5.2 Reference Manual
- [`docs/lua-spec/lua-5.3-spec.md`](docs/lua-spec/lua-5.3-spec.md) — Lua 5.3 Reference Manual
- [`docs/lua-spec/lua-5.4-spec.md`](docs/lua-spec/lua-5.4-spec.md) — Lua 5.4 Reference Manual (primary target)
- [`docs/lua-spec/lua-5.5-spec.md`](docs/lua-spec/lua-5.5-spec.md) — Lua 5.5 (Work in Progress)

### Reference Lua comparison harness
- **Status**: Fully implemented. CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4.
- **Gating**: `enforce` mode. Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory.

---

## Remaining Lua Runtime Spec Items

### os.time and os.date Semantics

**Requirements**:
- `os.time()` with no arguments returns current UTC timestamp
- `os.time(table)` interprets fields per §6.9
- `os.date("*t")` returns table with correct field names and ranges

**Tasks**:
- [ ] Verify `os.time()` return value matches Lua's epoch-based timestamp
- [ ] Test `os.date` format strings against reference Lua outputs
- [ ] Document timezone handling differences (if any)

### Coroutine Semantics

**Critical Behaviors**:
- `coroutine.resume` return value shapes
- `coroutine.wrap` error propagation
- `coroutine.status` state transitions
- Yield across C-call boundary errors

**Tasks**:
- [ ] Create state transition diagram tests for coroutine lifecycle
- [ ] Verify error message formats match Lua
- [ ] Test `coroutine.close` (5.4) cleanup order

### Error Message Parity

**Goal**: Error messages should match Lua's format for maximum compatibility.

**Tasks**:
- [ ] Catalog all error message formats in `ScriptRuntimeException`
- [ ] Add `ScriptOptions.LuaCompatibleErrors` flag (opt-in strict mode) — ✅ IMPLEMENTED
- [ ] Create error message normalization layer for Lua-compatible output

### Numerical For Loop Semantics (Lua 5.4)

**Breaking Change in 5.4**: Control variable in integer `for` loops never overflows/wraps.

**Tasks**:
- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds
- [ ] Document loop semantics per version

### __gc Metamethod Handling (Lua 5.4)

**Status**: NovaSharp doesn't have true finalizers, so `__gc` is not called during GC. Lua 5.4+ generates warnings for non-callable `__gc` values during finalization.

**Tasks**:
- [ ] Document NovaSharp's current `__gc` handling
- [ ] Decide on validation strategy (strict vs. Lua-compatible)

### utf8 Library Differences (Lua 5.3 vs 5.4)

**Remaining Tasks**:
- [ ] Verify `utf8.offset` bounds handling is complete
- [ ] Document utf8 library version differences

### collectgarbage Options (Lua 5.4)

**Deprecation in 5.4**: `setpause` and `setstepmul` options are deprecated (use `incremental` instead).

**Tasks**:
- [ ] Support deprecated options with warnings when targeting 5.4
- [ ] Implement `incremental` option for 5.4
- [ ] Add tests for GC option compatibility

### Literal Integer Overflow (Lua 5.4)

**Breaking Change in 5.4**: Decimal integer literals that overflow read as floats instead of wrapping.

**Tasks**:
- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing
- [ ] Document literal parsing behavior

### ipairs Metamethod Changes (Lua 5.3+)

**Breaking Change in 5.3**: `ipairs` now respects `__index` metamethods; the `__ipairs` metamethod was deprecated.

**Tasks**:
- [ ] Verify `ipairs` metamethod behavior per version
- [ ] Add tests for `ipairs` with `__index` metamethod tables
- [ ] Document iterator behavior differences

### table.unpack Location (Lua 5.2+)

**Breaking Change in 5.2**: `unpack` moved from global to `table.unpack`.

**Tasks**:
- [ ] Verify `unpack` availability matches target version
- [ ] Provide global `unpack` alias for 5.1 compatibility mode
- [ ] Document migration from `unpack` to `table.unpack`

### Documentation

- [ ] Update `docs/LuaCompatibility.md` with version-specific behavior notes
- [ ] Add "Determinism Guide" for users needing reproducible execution
- [ ] Document any intentional divergences with rationale
- [ ] Create version migration guides (5.1→5.2, 5.2→5.3, 5.3→5.4)
- [ ] Add "Breaking Changes by Version" quick-reference table

---

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- CLI output golden tests.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

---

## Recommended Next Steps (Priority Order)

### 🟡 MEDIUM: Remaining Version Parity Items

1. **Version migration guides**
   - `docs/LuaVersionMigration.md` with 5.1→5.2, 5.2→5.3, 5.3→5.4 guides

2. **CI jobs per LuaCompatibilityVersion**
   - Run test suite explicitly with each version setting

3. **`string.pack`/`unpack` extended options**
   - Complete implementation of all format specifiers

### 🟢 LOWER PRIORITY: Polish and Infrastructure

4. **CI Integration**
   - Add CI job that runs `compare-lua-outputs.py --enforce` on PRs
   - Add CI lint rule that rejects PRs with tests missing version coverage

---

## Initiative 9: Version-Aware Lua Standard Library Parity — Remaining Items

**Status**: 🟡 **MOSTLY COMPLETE** — Most modules implemented.

### String Module Version Parity 🟡

| Function | Status | Notes |
|----------|--------|-------|
| `string.pack/unpack/packsize` | 🚧 Partial | Extended options (`c`, `z`, alignment) missing |
| `string.format('%a')` | 🔲 Verify | Hex float format specifier |

**Tasks**:
- [ ] Complete `string.pack`/`unpack` extended format options
- [ ] Implement `string.format('%a')` hex float format

### Testing Infrastructure

**Tasks**:
- [ ] Create comprehensive version matrix tests for all modules
- [ ] Create `LuaFixtures/VersionParity/` test directory with per-function fixtures
- [ ] Add CI jobs that run test suite with each `LuaCompatibilityVersion`
- [ ] Create version migration guide (`docs/LuaVersionMigration.md`)
- [ ] Document all version-specific behaviors in `docs/LuaCompatibility.md`

---

## Initiative 12: Deep Codebase Allocation Analysis & Reduction ✅ **COMPLETE**

**Status**: ✅ **COMPLETE** — All 5 phases executed with measurable allocation improvements.

**Goal**: Perform a comprehensive, codebase-wide analysis to identify and eliminate unnecessary heap allocations.

**Progress Reports**:
- [docs/performance/allocation-analysis-initiative12-phase1.md](docs/performance/allocation-analysis-initiative12-phase1.md)
- [progress/session-062-initiative12-phase2-quick-wins.md](progress/session-062-initiative12-phase2-quick-wins.md)
- [progress/session-064-value-type-migration-phase3.md](progress/session-064-value-type-migration-phase3.md)
- [progress/session-065-initiative12-phase4-static-delegates.md](progress/session-065-initiative12-phase4-static-delegates.md)
- [progress/session-083-initiative12-phase5-validation.md](progress/session-083-initiative12-phase5-validation.md)

### Final Results

| Scenario | Phase 1 Baseline | Final | Reduction |
|----------|------------------|-------|-----------|
| NumericLoops | 824 B | 760 B | **7.8%** |
| CoroutinePipeline | 1,160 B | 1,096 B | **5.5%** |
| TableMutation | 25,888 B | 25,824 B | **0.2%** |
| UserDataInterop | 1,416 B | 1,352 B | **4.5%** |

### Implementation Phases (All Complete)

**Phase 1: Profiling & Baseline** ✅
- Identified 34 allocation sites by volume
- Documented baseline allocation rates for key scenarios

**Phase 2: Quick Wins** ✅
- Pool `BlocksToClose` inner lists via `ListPool<SymbolRef>.Rent()`/`Return()`
- Replace bitwise op lambdas with static readonly delegates
- Replace string metatable lambdas with static delegates

**Phase 3: Value Type Migration** ✅
- Convert high-impact types to `readonly struct` — `TablePair`, `ReflectionSpecialName`

**Phase 4: Deep Optimization** ✅
- `LuaSortComparer` struct in TableModule
- 7 static delegates in MathModule, 3 in Bit32Module

**Phase 5: Validation** ✅
- Re-ran allocation profiler, confirmed improvements
- All 11,790 tests pass with zero regressions

**Completed**: 2025-12-22

---

## Initiative 13: Lua-to-C# Ahead-of-Time Compiler (Offline DLL Generation) 🔬

**Status**: 🔲 **RESEARCH** — Long-term investigation item.

**Priority**: 🟢 **LOW** — Future optimization opportunity for game developers.

**Goal**: Investigate feasibility of creating an offline "Lua → C# compiler" tool.

### Concept Overview

Game developers using NovaSharp could ship an offline compilation tool with their game that allows players (or modders) to pre-compile their Lua scripts into native .NET assemblies. These compiled DLLs would:

- Load significantly faster than interpreted Lua (no parsing/compilation at runtime)
- Execute faster due to JIT-optimized native code
- Still integrate seamlessly with NovaSharp's runtime (tables, coroutines, C# interop)
- Be optional—interpreted Lua would remain fully supported

### Research Questions

1. **Feasibility**: Can Lua's dynamic semantics (metatables, dynamic typing, `_ENV` manipulation) be reasonably compiled to static C#?
2. **Performance Gains**: What speedup is realistic? (Likely 2-10x for compute-heavy scripts, minimal for I/O-bound)
3. **Compatibility**: How do compiled scripts interact with interpreted scripts, runtime `require()`, debug hooks?

### Risks & Challenges

- **Semantic Fidelity**: Lua's extreme dynamism may resist static compilation
- **Maintenance Burden**: Two execution paths (interpreted + compiled) doubles testing surface
- **Edge Cases**: Metamethod chains, `debug.setlocal`, `load()` with dynamic strings
- **Unity IL2CPP**: Compiled DLLs must work under Unity's AOT restrictions

**Owner**: TBD (requires dedicated research effort)
**Effort Estimate**: Unknown—initial feasibility study: 2-4 weeks; full implementation: 3-6 months

---

## Initiative 14: GitHub Pages Benchmark Dashboard Improvements 🎨

**Status**: 🔲 **PLANNED**

**Priority**: 🟢 **LOW** — Quality-of-life improvement for contributors and maintainers.

**Goal**: Prettify and configure the `gh-pages` branch to provide a readable, well-documented benchmark dashboard.

### Proposed Improvements

**Documentation**:
- [ ] Expand `README.md` with explanation of benchmark methodology
- [ ] Add descriptions of what each benchmark measures
- [ ] Document how to interpret regression alerts
- [ ] Link back to main repo's `docs/Performance.md`

**Visualization**:
- [ ] Configure `github-action-benchmark` chart options (title, axis labels, colors)
- [ ] Add index.html with styled benchmark chart display
- [ ] Include historical context (baseline establishment date, significant changes)

**Success Criteria**:
- [ ] Contributors can understand benchmark results without reading workflow code
- [ ] Performance trends are visually accessible via GitHub Pages URL
- [ ] Documentation explains threshold values and alert meanings

**Owner**: DevOps / CI team
**Effort Estimate**: 1-2 days

---

Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
