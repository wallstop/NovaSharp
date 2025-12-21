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
- **11,713** interpreter tests via TUnit (Microsoft.Testing.Platform)
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

### Initiative 9: Version-Aware Lua Standard Library Parity 🟡 **MOSTLY COMPLETE**
**Goal**: ALL Lua functions must behave according to their version specification (5.1, 5.2, 5.3, 5.4).
**Scope**: Math, String, Table, Basic, Coroutine, OS, IO, UTF-8, Debug modules + metamethod behaviors.
**Status**: Most modules complete. String.pack/unpack extended options and documentation remaining.
**Effort**: Ongoing

### Initiative 10: KopiLua Performance Hyper-Optimization 🎯 **HIGH**
**Goal**: Zero-allocation string pattern matching. Replace legacy KopiLua allocations with modern .NET patterns.
**Scope**: `CharPtr` → `ref struct`, `MatchState` pooling, `ArrayPool<char>`, `ZString` integration.
**Target**: <50 bytes/match, <400ns latency for simple patterns.
**Status**: Planned. See **Section 10** for detailed implementation plan.
**Effort**: 6-8 weeks

### Initiative 11: Comprehensive Helper Performance Audit 🎯
**Goal**: Audit and optimize ALL helper methods called from interpreter hot paths.
**Scope**: `Helpers/`, `DataTypes/`, `Execution/VM/`, `CoreLib/`, `Interop/`.
**Status**: Planned. See **Section 11** for scope.
**Effort**: 2-3 weeks audit + ongoing optimization

### Initiative 12: Deep Codebase Allocation Analysis & Reduction 🎯 **HIGH**
**Goal**: Comprehensive codebase-wide analysis to identify and eliminate unnecessary heap allocations using value types, buffers, ZString, and closure avoidance.
**Scope**: Entire `src/runtime/` codebase — DataTypes, VM, CoreLib, Interop, Loaders, Helpers.
**Target**: >50% allocation reduction in hot paths, >80% zero-allocation methods.
**Status**: Planned. See **Section 12** for detailed implementation plan.
**Effort**: 7-10 weeks

### Initiative 13: Magic String Consolidation 🟡 **MEDIUM**
**Goal**: Eliminate all duplicated string literals ("magic strings") by consolidating them into named constants with a single source of truth.
**Scope**: All runtime, tooling, and test code.
**Status**: Planned. Incremental enforcement during code changes.
**Effort**: Ongoing (apply during code reviews and new development)

**Key Areas to Audit**:
1. **Metamethod names**: `__index`, `__newindex`, `__call`, `__tostring`, etc.
2. **Lua keywords**: `nil`, `true`, `false`, `and`, `or`, `not`, `function`, etc.
3. **Error messages**: `bad argument`, `attempt to`, `number has no integer representation`, etc.
4. **Module names**: `string`, `table`, `math`, `io`, `os`, `debug`, `coroutine`, etc.

**Validation Commands**:
```bash
# Find potential duplicated magic strings (metamethods)
grep -rn '"__' src/runtime/WallstopStudios.NovaSharp.Interpreter/ | sort | uniq -c | sort -rn | head -20

# Find string literals in ArgumentException/ArgumentNullException (should use nameof)
grep -rn 'ArgumentNullException("' src/runtime/
grep -rn 'ArgumentException.*"[a-z]' src/runtime/
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

### Remaining Tasks

- [ ] Migrate remaining ~100 StringModuleTUnitTests with version+data coupled patterns
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

## Initiative 10: KopiLua Performance Hyper-Optimization 🎯 **HIGH PRIORITY**

**Status**: 🔲 **PLANNED** — Critical for interpreter hot-path performance.

**Priority**: HIGH — KopiLua code is called from string pattern matching hot paths.

**Goal**: Dramatically reduce allocations and improve performance of all KopiLua-derived code. Target: zero-allocation in steady state, match or exceed native Lua performance.

### Key Performance Issues Identified

| Issue | Location | Impact | Fix Strategy |
|-------|----------|--------|--------------|
| `CharPtr` class allocations | Throughout | HIGH | Convert to `ref struct` or `ReadOnlySpan<char>` |
| `MatchState` class allocations | Every pattern match | HIGH | Object pooling or struct conversion |
| `new char[]` allocations | `Scanformat`, `str_format` | MEDIUM | Use `ArrayPool<char>` or stack allocation |
| String concatenation | `LuaLError` calls, error messages | MEDIUM | Use `ZString` |
| `Capture[]` array allocation | `MatchState` constructor | HIGH | Pre-allocate static pool |
| `LuaLBuffer` allocations | `str_gsub`, `str_format` | HIGH | Pool or `StringBuilder` replacement |

### Implementation Phases

**Phase 1: Infrastructure (1 week)**
- [ ] Add benchmarking infrastructure for KopiLua operations
- [ ] Establish baseline measurements
- [ ] Document current allocation patterns

**Phase 2: Critical Path Optimization (2 weeks)**
- [ ] Implement `CharSpan` ref struct replacement
- [ ] Implement `MatchState` pooling
- [ ] Replace `new char[]` with `ArrayPool<char>`

**Phase 3: Comprehensive Optimization (2 weeks)**
- [ ] Modernize `LuaLBuffer`
- [ ] Integrate `ZString` for error messages
- [ ] Optimize character classification methods

**Phase 4: Validation (1 week)**
- [ ] Run full benchmark suite
- [ ] Verify allocation targets met
- [ ] Test on all target platforms

### Success Metrics

| Metric | Current (Estimated) | Target |
|--------|---------------------|--------|
| Allocations per `string.match` | ~500 bytes | <50 bytes |
| Allocations per `string.gsub` | ~2000 bytes | <200 bytes |
| Allocations per `string.format` | ~1500 bytes | <100 bytes |
| `string.match` latency (simple) | ~800 ns | <400 ns |

**Owner**: Interpreter team
**Effort Estimate**: 6-8 weeks total

---

## Initiative 11: Comprehensive Helper Performance Audit 🎯

**Status**: 🔲 **PLANNED**

**Priority**: HIGH — All interpreter hot-path helpers need audit.

**Goal**: Identify and optimize ALL helper methods called from interpreter hot paths.

### Scope

All code in these namespaces/directories that is called from VM execution:
- `LuaPort/` (KopiLua-derived, covered by Initiative 10)
- `Helpers/` (LuaIntegerHelper, LuaStringHelper, etc.)
- `DataTypes/` (DynValue, Table, Closure operations)
- `Execution/VM/` (Processor instruction handlers)
- `CoreLib/` (Standard library module implementations)
- `Interop/` (CLR bridging, type conversion)

### Optimization Patterns to Apply

- Use `[MethodImpl(AggressiveInlining)]` for small methods
- Replace LINQ with manual loops in hot paths
- Use `Span<T>` for buffer operations
- Pool any allocated objects
- Cache computed values where safe

**Owner**: Interpreter team
**Effort Estimate**: 2-3 weeks for comprehensive audit + ongoing optimization work

---

## Initiative 12: Deep Codebase Allocation Analysis & Reduction 🎯 **HIGH PRIORITY**

**Status**: 🔲 **PLANNED** — Critical for interpreter performance and GC pressure reduction.

**Priority**: 🔴 **HIGH** — Reducing allocations directly improves runtime performance, reduces GC pauses, and is essential for Unity/game use cases.

**Goal**: Perform a comprehensive, codebase-wide analysis to identify and eliminate unnecessary heap allocations.

### Analysis Scope

Audit the entire `src/runtime/WallstopStudios.NovaSharp.Interpreter/` codebase for:

| Category | What to Look For |
|----------|------------------|
| **Value Type Migration** | Classes that could be `readonly struct` or `ref struct` (small, immutable, no inheritance) |
| **Buffer Utilization** | `new T[]` → `ArrayPool<T>.Shared`, `stackalloc`, or `Span<T>` |
| **String Building** | `StringBuilder`, `$""` interpolation → `ZStringBuilder.Create()` in hot paths |
| **Closure Avoidance** | Lambda captures causing allocations; convert to static lambdas or struct-based callbacks |
| **Boxing Elimination** | Interface casts on value types, generic constraints missing `struct` |
| **LINQ Removal** | `.ToList()`, `.ToArray()`, `.Where().Select()` chains → manual loops |
| **Object Pooling** | Frequently allocated short-lived objects → `ObjectPool<T>` |
| **String Operations** | `string.Substring()` → `ReadOnlySpan<char>`, `string.Split()` → span-based parsing |

### Key Areas to Audit

1. **DataTypes/** — `DynValue`, `Table`, `Closure`, `UserData` creation and manipulation
2. **Execution/VM/** — Processor instruction handlers, call stack management
3. **CoreLib/** — Standard library implementations (string, table, math, io, etc.)
4. **Interop/** — CLR bridging, type converters, method dispatch
5. **Tree/** — AST nodes, parser output (if retained at runtime)
6. **Loaders/** — Module resolution, script loading
7. **Helpers/** — Utility methods called from hot paths

### Implementation Phases

**Phase 1: Profiling & Baseline (1 week)**
- [ ] Run allocation profiler (dotMemory, PerfView, or BenchmarkDotNet `[MemoryDiagnoser]`)
- [ ] Identify top 20 allocation sites by volume
- [ ] Document baseline allocation rates for key scenarios

**Phase 2: Quick Wins (1-2 weeks)**
- [ ] Replace obvious `new T[]` with `ArrayPool<T>.Shared.Rent()`/`Return()`
- [ ] Add `static` keyword to lambdas that don't capture state
- [ ] Replace `StringBuilder` with `ZStringBuilder` in error paths and logging
- [ ] Add `readonly struct` to small immutable types where applicable

**Phase 3: Value Type Migration (2-3 weeks)**
- [ ] Identify candidate classes for struct conversion (analysis tool)
- [ ] Convert high-impact types to `readonly struct` or `ref struct`
- [ ] Add `in` parameters for large structs passed by value
- [ ] Audit generic constraints for missing `struct`/`class` specifiers

**Phase 4: Deep Optimization (2-3 weeks)**
- [ ] Implement object pooling for frequently allocated types
- [ ] Replace LINQ chains with manual iteration in hot paths
- [ ] Convert closure-heavy patterns to struct-based alternatives
- [ ] Migrate string operations to span-based APIs

**Phase 5: Validation (1 week)**
- [ ] Re-run allocation profiler, compare to baseline
- [ ] Run full test suite to ensure no regressions
- [ ] Benchmark key scenarios (script load, pattern match, interop calls)
- [ ] Document wins and remaining opportunities

### Success Metrics

| Metric | Current (Baseline TBD) | Target |
|--------|------------------------|--------|
| Allocations per simple script execution | TBD | <50% of baseline |
| GC Gen0 collections per 10k operations | TBD | <30% of baseline |
| Hot-path methods with zero allocations | TBD | >80% |
| `readonly struct` coverage for small types | TBD | >90% eligible types |

### Tooling & Resources

- **Profilers**: dotMemory, PerfView, VS Diagnostic Tools
- **Benchmarking**: BenchmarkDotNet with `[MemoryDiagnoser]`
- **Libraries**: ZString (already available), `System.Buffers.ArrayPool<T>`
- **Guidelines**: See [.llm/skills/high-performance-csharp.md](/.llm/skills/high-performance-csharp.md)

**Owner**: Interpreter team
**Effort Estimate**: 7-10 weeks total (can run partially in parallel with Initiatives 10-11)

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
