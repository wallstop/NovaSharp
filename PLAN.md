# Modern Testing & Coverage Plan

## 🎯 Current Priority: Dual Numeric Type System (§8.24 — HIGH PRIORITY)

**Status**: 🚧 **IN PROGRESS** — Phase 3 Standard Library complete, Phase 4-5 remaining.

**Progress (2025-12-07)**:
- ✅ **Phase 1 Complete**: `LuaNumber` struct with 83 tests
- ✅ **Phase 2 Complete**: DynValue integration, VM arithmetic opcodes, `math.type()` correct, bitwise operations preserve precision
- ✅ **Phase 3 Complete**: StringModule format specifiers, math.floor/ceil integer promotion
- 🔲 **Phase 4 Pending**: Interop & serialization
- 🔲 **Phase 5 Pending**: Numeric value caching & performance validation

**Key Achievements**:
- `math.maxinteger`/`math.mininteger` return exact values (no precision loss)
- `math.type(1)` → "integer", `math.type(1.0)` → "float" (correct subtype detection)
- Integer arithmetic wraps correctly (two's complement)
- Integer `//` and `%` by zero throw errors; float versions return IEEE 754 values
- Bitwise operations preserve full 64-bit integer precision
- `string.format('%d', math.maxinteger)` outputs exact "9223372036854775807" (no precision loss)
- `math.floor(3.7)` and `math.ceil(3.2)` return integer subtypes
- All **4,069** tests passing

See **Section 8.24** for the complete implementation plan.

**Next actionable item**: Phase 4 — Update interop converters (`FromObject`/`ToObject`) for integer preservation.

---

## Repository Snapshot — 2025-12-07 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **4,069** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads.
- **Sandbox infrastructure**: Complete with instruction/memory/coroutine limits, per-mod isolation, callbacks, and presets.
- **Benchmark CI**: `.github/workflows/benchmarks.yml` with BenchmarkDotNet, threshold-based regression alerting.
- **Packaging**: NuGet publishing workflow + Unity UPM scripts in `scripts/packaging/`.

### Recent Improvements (2025-12-07)
- **Platform detection thread safety**: Fixed race condition in `PlatformAutoDetector.AotProbeOverride` by making it volatile
- **Test diagnostics**: Enhanced `PlatformDetectorScope.DescribeCurrentState()` to include AOT override status
- **New test infrastructure**: Added verification assertions for AOT probe override registration

## Critical Initiatives

### Initiative 12: VM Correctness and State Protection 🔴 **CRITICAL**
**Goal**: Make the VM bulletproof against external state corruption while maintaining full Lua compatibility.
**Scope**: `DynValue` mutability controls, public API audit, table key safety, closure upvalue protection.
**Status**: Analysis complete. See [`docs/proposals/vm-correctness.md`](docs/proposals/vm-correctness.md) for detailed findings.
**Effort**: 1-2 weeks implementation + comprehensive testing

**Key Changes Required**:
1. Make `DynValue.Assign()` internal (prevents external corruption)
2. Fix `Closure.GetUpValue()` to return readonly; add `SetUpValue()` method
3. Ensure table keys are readonly in `_valueMap` (prevents hash corruption)
4. Fix UserData/Thread hash codes (performance)
5. **Full public API audit**: Review all public methods returning `DynValue` for potential corruption vectors

**API Breaking Changes**: Acceptable if required for VM correctness and Lua compatibility.

**Follow-up Task**: Comprehensive audit of all public APIs on VM types (`Script`, `Table`, `Closure`, `Coroutine`, `DynValue`, `UserData`, `CallbackArguments`, etc.) to identify any additional vectors where external code could corrupt or cause unexpected VM state.

### Initiative 9: Version-Aware Lua Standard Library Parity 🔴 **CRITICAL**
**Goal**: ALL Lua functions must behave according to their version specification (5.1, 5.2, 5.3, 5.4).
**Scope**: Math, String, Table, Basic, Coroutine, OS, IO, UTF-8, Debug modules + metamethod behaviors.
**Status**: Comprehensive audit required. See **Section 9** for detailed tracking.
**Effort**: 4-6 weeks

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

### 2. Codebase organization (future)
- Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
- Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
- Add guardrails so new code lands in correct folders with consistent namespaces

### 2.5. Test modernization: TUnit data-driven attributes (future)
- Migrate loop-based parameterized tests to TUnit `[Arguments]` attributes where compile-time constants allow
- Use `[MethodDataSource]` or `[ClassDataSource]` for runtime data (e.g., `Type` parameters, complex objects)
- Benefits: Better test discovery/reporting in IDEs, clearer test naming per parameter set
- Candidate tests:
  - `IsRunningOnAotTreatsProbeExceptionsAsAotHosts` (exception types)
  - Tests using inline `foreach` loops over test cases
- Reference: [TUnit Data-Driven Tests](https://tunit.dev/)

### 3. Tooling, docs, and contributor experience
- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.

### 4. Concurrency improvements (optional)
- Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics.
- Split debugger locks for reduced contention.
- Add timeout to `BlockingChannel`.

See `docs/modernization/concurrency-inventory.md` for the full synchronization audit.

## Lua Specification Parity

### Official Lua Specifications (Local Reference)

**IMPORTANT**: For all Lua compatibility work, consult the local specification documents first:
- [`docs/lua-spec/lua-5.1-spec.md`](docs/lua-spec/lua-5.1-spec.md) — Lua 5.1 Reference Manual
- [`docs/lua-spec/lua-5.2-spec.md`](docs/lua-spec/lua-5.2-spec.md) — Lua 5.2 Reference Manual
- [`docs/lua-spec/lua-5.3-spec.md`](docs/lua-spec/lua-5.3-spec.md) — Lua 5.3 Reference Manual
- [`docs/lua-spec/lua-5.4-spec.md`](docs/lua-spec/lua-5.4-spec.md) — Lua 5.4 Reference Manual (primary target)
- [`docs/lua-spec/lua-5.5-spec.md`](docs/lua-spec/lua-5.5-spec.md) — Lua 5.5 (Work in Progress)

These documents contain comprehensive details on:
- Language syntax and semantics
- Type system (nil, boolean, number, string, table, function, userdata, thread)
- Standard library functions with exact signatures and behaviors
- Metamethods and metatable behavior
- Error handling and message formats
- Version-specific changes and breaking changes

**Use these specs** when:
- Implementing or auditing standard library functions
- Verifying VM behavior against spec
- Understanding version-specific differences
- Writing tests for Lua compatibility
- Debugging divergences from reference Lua

### Reference Lua comparison harness
- **Status**: Fully implemented. CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4.
- **Gating**: `enforce` mode. Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory.

### Full Lua specification audit
- **Tracking**: `docs/testing/spec-audit.md` contains detailed tracking table with status per feature.
- **Progress**: Most core features verified against Lua 5.4 manual; `string.pack`/`unpack` extended options remain unimplemented.

### 8. Lua Runtime Specification Parity (CRITICAL)

**Goal**: Ensure NovaSharp behaves identically to reference Lua interpreters across all supported versions (5.1, 5.2, 5.3, 5.4) for deterministic, reproducible script execution.

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

**Tasks**:
- [ ] Verify NovaSharp behavior matches the target `LuaCompatibilityVersion`
- [ ] Ensure string metatable has arithmetic metamethods for 5.4 compatibility
- [ ] Add tests for string arithmetic operations per version
- [ ] Document the coercion change in `docs/LuaCompatibility.md`

#### 8.10 print/tostring Behavior Changes (Lua 5.4)

**Breaking Change in 5.4**: `print` no longer calls the global `tostring` function; it directly uses the `__tostring` metamethod.

**Tasks**:
- [ ] Verify `print` behavior matches target Lua version
- [ ] Add tests for custom `tostring` function interaction with `print`
- [ ] Document behavior difference

#### 8.11 Numerical For Loop Semantics (Lua 5.4)

**Breaking Change in 5.4**: Control variable in integer `for` loops never overflows/wraps.

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

**Tasks**:
- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing
- [ ] Document literal parsing behavior

#### 8.18 bit32 Library Deprecation (Lua 5.3+)

**Breaking Change in 5.3**: The `bit32` library was deprecated in favor of native bitwise operators.

**Tasks**:
- [ ] Verify `bit32` availability matches target version
- [ ] Add compatibility warning when using `bit32` on 5.3
- [ ] Document migration path from `bit32` to native operators

#### 8.19 Environment Changes (Lua 5.2+)

**Breaking Change in 5.2**: The concept of function environments was fundamentally changed.

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

#### 8.24 Dual Numeric Type System (Integer + Float) 🔴 **HIGH PRIORITY**

**Status**: 🚧 **IN PROGRESS** — Phase 3 complete. All 4,069 tests passing.

**Problem Statement**:

Lua 5.3+ has **two distinct numeric subtypes** that NovaSharp currently cannot fully represent:
- **Integer**: 64-bit signed (`long`/`Int64`) with exact range -2^63 to 2^63-1
- **Float**: 64-bit IEEE 754 double precision

The `LuaNumber` struct has been implemented to track integer vs float subtype.

**Phase 4: Interop & Serialization** (3-4 days)
- [ ] Update `FromObject()` / `ToObject()` for integer preservation
- [ ] Update JSON serialization (integers as JSON integers, not floats)
- [ ] Update binary dump/load format (version 2?)
- [ ] Ensure CLR interop handles `int`, `long`, `float`, `double` correctly

**Phase 5: Caching & Performance Validation** (3-4 days)
- [ ] Extend `DynValue` caches for common float values (0.0, 1.0, -1.0, etc.)
- [ ] Add `FromFloat(double)` cache method for hot paths
- [ ] Add negative integer cache (-256 to -1)
- [ ] Run Lua comparison harness against reference Lua 5.3/5.4
- [ ] Performance benchmarking (ensure no significant regression)
- [ ] Memory allocation profiling (verify caching reduces allocations)
- [ ] Documentation updates

**Success Criteria**:
- [x] `math.maxinteger` returns exactly `9223372036854775807` (not rounded)
- [x] `math.type(1)` returns `"integer"`, `math.type(1.0)` returns `"float"`
- [x] `3 // 0` throws error, `3.0 // 0` returns `inf`
- [x] `math.maxinteger & 1` returns `1` (not overflow)
- [x] `string.format('%d', math.maxinteger)` returns "9223372036854775807" (exact)
- [x] `math.floor(3.7)` returns integer subtype (value 3)
- [x] `math.ceil(3.2)` returns integer subtype (value 4)
- [x] All 4,069 existing tests pass
- [ ] Lua comparison harness shows improved parity percentage
- [ ] No performance regression > 5% on benchmarks
- [ ] Numeric caching reduces hot-path allocations

**Owner**: Interpreter team
**Priority**: 🔴 HIGH — Required for full Lua 5.3+ specification compliance

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- CLI output golden tests.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

## Recommended Next Steps (Priority Order)

### Active/Upcoming Items

1. **Dual Numeric Type System - Phase 4-5** (Initiative 8.24): 🔴 **HIGH PRIORITY**
    - Phase 4: Update interop converters (`FromObject`/`ToObject`) for integer preservation
    - Phase 5: Caching & performance validation
    - See **Section 8.24** for full plan

2. **Lua Specification Parity - String/Pattern Matching** (Initiative 8.4): 🎯 **NEXT PRIORITY**
    - Compare `%a`, `%d`, `%l`, `%u`, `%w`, `%s` character classes against reference Lua
    - Verify `string.format` output matches for edge cases (NaN, Inf, very large numbers)
    - Document any intentional Unicode-aware divergences

3. **Tooling enhancements** (Initiative 6):
    - Roslyn source generators/analyzers for NovaSharp descriptors
    - DocFX (or similar) for API documentation
    - CLI output golden tests

### Future Phases (Lower Priority)

4. **Interpreter hyper-optimization - Phase 4** (Initiative 5): 🔮 **PLANNED** — Zero-allocation runtime goal
    
    **Target:** Match or exceed native Lua performance; achieve <100 bytes/call allocation overhead.
    
    See `docs/performance/optimization-opportunities.md` for comprehensive plan covering:
    - VM dispatch optimization (computed goto, opcode fusion)
    - Table redesign (hybrid array+hash like native Lua)
    - DynValue struct conversion (optional breaking change)
    - Span-based APIs throughout
    - Roslyn source generators for interop

5. **Concurrency improvements** (Initiative 7, optional):
    - Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics
    - Split debugger locks for reduced contention
    - Add timeout to `BlockingChannel`

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.

---

## Initiative 9: Version-Aware Lua Standard Library Parity 🔴 **CRITICAL**

**Status**: 🚧 **IN PROGRESS** — Comprehensive audit required to ensure ALL Lua functions behave correctly per version.

**Priority**: CRITICAL — Core interpreter correctness for production use.

**Goal**: Every Lua function and language feature must behave according to the specification for the configured `LuaCompatibilityVersion`. This is not just about API surface (whether a function exists) but about behavioral semantics that differ between versions.

### 9.1 Math Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `math.random()` | LCG | LCG | LCG | xoshiro256** | ✅ Completed | Version-specific RNG |
| `math.randomseed(x)` | 1 arg, nil return | 1 arg, nil return | 1 arg, nil return | 0-2 args, returns (x,y) | ✅ Completed | Version-aware behavior |
| `math.type(x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Returns "integer"/"float" |
| `math.tointeger(x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Integer conversion |
| `math.ult(m, n)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Unsigned comparison |
| `math.maxinteger` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | 2^63-1 |
| `math.mininteger` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | -2^63 |
| `math.log(x [,base])` | 1 arg only | 1-2 args | 1-2 args | 1-2 args | 🔲 Verify | Check 5.1 signature |
| `math.log10(x)` | ✅ | ⚠️ Deprecated | ⚠️ Deprecated | ⚠️ Deprecated | 🔲 Verify | Warn in 5.2+ |
| `math.ldexp(m, e)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `math.frexp(x)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `math.pow(x, y)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Use `x^y` in 5.3+ |
| `math.mod(x, y)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `x%y` in 5.1+ |
| `math.fmod(x, y)` | ✅ | ✅ | ✅ | ✅ | ✅ Available | Float modulo |
| `math.modf(x)` | Float parts | Float parts | Int+Float parts | Int+Float parts | 🔲 Verify | Integer promotion in 5.3+ |
| `math.floor(x)` | Float | Float | Integer if fits | Integer if fits | ✅ Completed | Integer promotion |
| `math.ceil(x)` | Float | Float | Integer if fits | Integer if fits | ✅ Completed | Integer promotion |

**Tasks**:
- [ ] Audit all `math` functions for version-specific behavior
- [ ] Implement `[LuaCompatibility]` gating for deprecated/removed functions
- [ ] Add version-specific tests for each function
- [ ] Implement deprecation warnings for 5.2+ deprecated functions
- [ ] Verify `math.modf` returns integer+float in 5.3+

### 9.2 String Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `string.pack(fmt, ...)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.unpack(fmt, s [,pos])` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.packsize(fmt)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.format('%a', x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🔲 Verify | Hex float format |
| `string.format('%d', maxint)` | Double precision | Double precision | Integer precision | Integer precision | ✅ Completed | LuaNumber precision |
| `string.gmatch(s, pattern [,init])` | No init | No init | No init | ✅ init arg | 🔲 Verify | 5.4 added init parameter |
| Pattern `%g` (graphical) | ❌ N/A | ✅ | ✅ | ✅ | 🔲 Verify | Added in 5.2 |
| Frontier pattern `%f[]` | ✅ | ✅ | ✅ | ✅ | ✅ Available | All versions |

**Tasks**:
- [ ] Complete `string.pack`/`unpack` extended format options (`c`, `z`, alignment)
- [ ] Implement `string.format('%a')` hex float format specifier
- [ ] Add `init` parameter to `string.gmatch` for Lua 5.4
- [ ] Verify `%g` character class availability per version
- [ ] Document string pattern differences between versions

### 9.3 Table Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `table.pack(...)` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Sets `n` field |
| `table.unpack(list [,i [,j]])` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Replaces global `unpack` |
| `table.move(a1, f, e, t [,a2])` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Metamethod-aware |
| `table.maxn(table)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `table.getn(table)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `#table` |
| `table.setn(table, n)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Removed |
| `table.foreachi(t, f)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `ipairs` |
| `table.foreach(t, f)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `pairs` |

**Tasks**:
- [ ] Implement `[LuaCompatibility]` gating for deprecated/removed table functions
- [ ] Add global `unpack` alias for Lua 5.1 mode
- [ ] Verify `table.maxn` available only in 5.1-5.2

### 9.4 Basic Functions Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `setfenv(f, table)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | 5.1 only |
| `getfenv(f)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | 5.1 only |
| `unpack(list [,i [,j]])` | ✅ Global | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | Moved to `table.unpack` |
| `module(name [,...])` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | 5.1 module system |
| `loadstring(string [,chunkname])` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `load(string)` |
| `load(chunk [,chunkname [,mode [,env]]])` | 2-3 args | 4 args | 4 args | 4 args | 🔲 Verify | Signature change |
| `loadfile(filename [,mode [,env]])` | 1 arg | 3 args | 3 args | 3 args | 🔲 Verify | Signature change |
| `rawlen(v)` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Added in 5.2 |
| `xpcall(f, msgh [,...])` | 2 args | Extra args | Extra args | Extra args | 🔲 Verify | 5.2+ passes args to f |
| `print(...)` behavior | Calls tostring | Calls tostring | Calls tostring | Uses __tostring | 🔲 Implement | 5.4 change |
| String-to-number coercion | Implicit | Implicit | Implicit | Metamethod | 🔲 Implement | 5.4 breaking change |

**Tasks**:
- [ ] Implement `setfenv`/`getfenv` for Lua 5.1 compatibility mode
- [ ] Add global `unpack` for Lua 5.1 mode
- [ ] Implement `print` behavior change for Lua 5.4 (`__tostring` directly)
- [ ] Implement string-to-number coercion via metamethods for Lua 5.4
- [ ] Verify `xpcall` argument passing per version
- [ ] Verify `load`/`loadfile` signature per version

### 9.5 Coroutine Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `coroutine.isyieldable()` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Added in 5.3 |
| `coroutine.close(co)` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | ✅ Available | Added in 5.4 |
| `coroutine.running()` | Returns co only | Returns co, bool | Returns co, bool | Returns co, bool | 🔲 Verify | Return shape |

**Tasks**:
- [ ] Verify `coroutine.running()` return value per version

### 9.6 OS Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `os.execute(command)` | Returns status | Returns (ok, signal, code) | Returns tuple | Returns tuple | ✅ Available | |
| `os.exit(code [,close])` | 1 arg | 2 args | 2 args | 2 args | 🔲 Verify | `close` param |

**Tasks**:
- [ ] Verify `os.execute` return value per version
- [ ] Verify `os.exit` `close` parameter support

### 9.7 IO Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `io.lines(filename, ...)` | Returns iterator | Returns iterator | Returns iterator | Returns 4 values | 🔲 Implement | 5.4 breaking change |
| `io.read("*n")` | Number | Number | Number | Number | ✅ Available | Hex parsing in 5.3+ |
| `file:setvbuf(mode [,size])` | ✅ | ✅ | ✅ | ✅ | 🔲 Verify | Buffer modes |

**Tasks**:
- [ ] Implement `io.lines` 4-return-value for Lua 5.4
- [ ] Verify `io.read("*n")` hex parsing per version

### 9.8 UTF-8 Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `utf8.char(...)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| `utf8.codes(s [,lax])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | 🔲 Verify | `lax` mode in 5.4 |
| `utf8.codepoint(s [,i [,j [,lax]]])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | 🔲 Verify | `lax` mode in 5.4 |
| `utf8.len(s [,i [,j [,lax]]])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | 🔲 Verify | `lax` mode in 5.4 |
| `utf8.offset(s, n [,i])` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| Surrogate rejection | ❌ N/A | ❌ N/A | By default | By default | 🔲 Verify | 5.4 `lax` accepts |

**Tasks**:
- [ ] Implement `lax` mode parameter for UTF-8 functions in Lua 5.4
- [ ] Verify surrogate handling per version

### 9.9 Debug Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `debug.setcstacklimit(limit)` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | 🔲 Implement | 5.4 only |
| `debug.setmetatable(value, table)` | 1st return | 1st return | 1st return | boolean | 🔲 Verify | Return type change |
| `debug.getuservalue(u [,n])` | ❌ N/A | ✅ (1 value) | ✅ (1 value) | ✅ (n-th value) | 🔲 Implement | 5.4 multi-user-values |
| `debug.setuservalue(u, value [,n])` | ❌ N/A | ✅ | ✅ | ✅ (n-th value) | 🔲 Implement | 5.4 multi-user-values |

**Tasks**:
- [ ] Implement `debug.setcstacklimit` for Lua 5.4
- [ ] Verify `debug.setmetatable` return value per version
- [ ] Implement multi-user-value support for 5.4

### 9.10 Bitwise Operations Version Parity

| Feature | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|---------|-----|-----|-----|-----|------------------|-------|
| `bit32` library | ❌ N/A | ✅ | ⚠️ Deprecated | ❌ Removed | ✅ Available | Version-gated |
| Native `&`, `|`, `~` operators | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| `~` unary (bitwise NOT) | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| `<<`, `>>` operators | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |

**Tasks**:
- [ ] Emit deprecation warning when `bit32` used in 5.3 mode
- [ ] Verify `bit32` unavailable in 5.4 mode

### 9.11 Metamethod Behavior Version Parity

| Metamethod | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|------------|-----|-----|-----|-----|------------------|-------|
| `__lt` emulates `__le` | ✅ | ✅ | ✅ | ❌ No | 🔲 Implement | 5.4 breaking change |
| `__gc` non-function error | Silent | Silent | Silent | Error | 🔲 Implement | 5.4 breaking change |
| `__pairs`/`__ipairs` | ❌ N/A | ✅ | ✅ (no __ipairs) | ✅ (no __ipairs) | 🔲 Verify | `__ipairs` deprecated 5.3 |
| `__close` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | ✅ Available | |

**Tasks**:
- [ ] Implement `__lt` emulation removal for Lua 5.4
- [ ] Implement `__gc` validation for Lua 5.4
- [ ] Verify `__ipairs` behavior per version

### 9.12 Testing Infrastructure

**Tasks**:
- [ ] Create comprehensive version matrix tests for all modules
- [ ] Create `LuaFixtures/VersionParity/` test directory with per-function fixtures
- [ ] Add CI jobs that run test suite with each `LuaCompatibilityVersion`
- [ ] Create version migration guide (`docs/LuaVersionMigration.md`)
- [ ] Document all version-specific behaviors in `docs/LuaCompatibility.md`

**Success Criteria**:
- All Lua standard library functions behave according to their version specification
- Version-gated functions raise appropriate errors or deprecation warnings
- CI validates all behaviors against reference Lua interpreters (5.1, 5.2, 5.3, 5.4)
- Documentation clearly explains behavior differences per version

**Owner**: Interpreter team
**Effort Estimate**: 4-6 weeks comprehensive audit and implementation

---

## Initiative 10: KopiLua Performance Hyper-Optimization 🎯 **HIGH PRIORITY**

**Status**: 🔲 **PLANNED** — Critical for interpreter hot-path performance.

**Priority**: HIGH — KopiLua code is called from string pattern matching hot paths.

**Goal**: Dramatically reduce allocations and improve performance of all KopiLua-derived code. Target: zero-allocation in steady state, match or exceed native Lua performance.

### 10.1 KopiLua String Library Analysis

**Key Performance Issues Identified**:

| Issue | Location | Impact | Fix Strategy |
|-------|----------|--------|--------------|
| `CharPtr` class allocations | Throughout | HIGH | Convert to `ref struct` or `ReadOnlySpan<char>` |
| `MatchState` class allocations | Every pattern match | HIGH | Object pooling or struct conversion |
| `new char[]` allocations | `Scanformat`, `str_format` | MEDIUM | Use `ArrayPool<char>` or stack allocation |
| String concatenation | `LuaLError` calls, error messages | MEDIUM | Use `ZString` |
| `Capture[]` array allocation | `MatchState` constructor | HIGH | Pre-allocate static pool |
| `LuaLBuffer` allocations | `str_gsub`, `str_format` | HIGH | Pool or `StringBuilder` replacement |

### 10.2 Implementation Phases

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

### 10.3 Success Metrics

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

**Goal**: Identify and optimize ALL helper methods called from interpreter hot paths, not just KopiLua.

### 11.1 Scope

All code in these namespaces/directories that is called from VM execution:
- `LuaPort/` (KopiLua-derived, covered by Initiative 10)
- `Helpers/` (LuaIntegerHelper, LuaStringHelper, etc.)
- `DataTypes/` (DynValue, Table, Closure operations)
- `Execution/VM/` (Processor instruction handlers)
- `CoreLib/` (Standard library module implementations)
- `Interop/` (CLR bridging, type conversion)

### 11.2 Optimization Patterns to Apply

- Use `[MethodImpl(AggressiveInlining)]` for small methods
- Replace LINQ with manual loops in hot paths
- Use `Span<T>` for buffer operations
- Pool any allocated objects
- Cache computed values where safe

**Owner**: Interpreter team
**Effort Estimate**: 2-3 weeks for comprehensive audit + ongoing optimization work
