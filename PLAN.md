# Modern Testing & Coverage Plan

## 🔴 Lua Spec Compliance Core Principle

NovaSharp's PRIMARY GOAL is to be a **faithful Lua interpreter** that matches the official Lua reference implementation. When fixture comparisons reveal behavioral differences:

1. **ASSUME NOVASHARP IS WRONG** until proven otherwise
2. **FIX THE PRODUCTION CODE** to match Lua behavior
3. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua
4. **NEVER adjust tests to accommodate bugs** — fix the runtime instead

**Current Status**: 🟡 Lua fixture comparisons have remaining mismatches to investigate (5.1: 34, 5.2: 30, 5.4: 17, 5.5: 19). Many are error format differences or NovaSharp-specific edge cases.

---

## 🟢 COMPLETED: TUnit Test Multi-Version Coverage Audit (§8.39)

**Status**: ✅ **COMPLETE** — 1,985 tests compliant, **0 Lua execution tests remaining**.

**Problem Statement**:
NovaSharp supports Lua 5.1, 5.2, 5.3, 5.4, and 5.5. Every TUnit test MUST explicitly declare which `LuaCompatibilityVersion` values it targets via `[Arguments]` or helper attributes. Tests without version coverage will be rejected.

### Final Metrics (Session 048)

| Metric | Count |
|--------|-------|
| Files analyzed | 251 |
| Total tests | ~3,669 |
| Compliant tests | **1,985** |
| **✅ Lua execution tests needing version** | **0** |
| ⚪ Infrastructure tests (no Lua) | ~1,684 |
| **Compliance %** | **54.1%** |

### Progress
- Started with **357** non-compliant Lua execution tests
- Session 047: Reduced to **24** non-compliant (93% reduction)
- Session 048: Reduced to **0** non-compliant (**100% complete!**)

### Completed Tasks (Session 048)
- [x] Fixed lint script to handle multi-line Arguments attributes (added `re.DOTALL` flag)
- [x] Fixed PlatformAutoDetectorTUnitTests (1 test)
- [x] Fixed IntegerBoundaryTUnitTests (10 tests with `[LuaTestMatrix]` / `[LuaVersionsFrom]`)
- [x] Fixed ScriptExecution tests (11 tests across 9 files)
- [x] Fixed ScriptRunTUnitTests duplicate LuaTestMatrix attribute issue

### Commands

```bash
python3 scripts/lint/check-tunit-version-coverage.py
python3 scripts/lint/check-tunit-version-coverage.py --detailed  # Show all non-compliant tests
python3 scripts/lint/check-tunit-version-coverage.py --lua-only --fail-on-noncompliant  # CI mode
```

### Remaining Tasks

- [ ] Negative test gap analysis for version-specific features
- [ ] Add CI lint rule that rejects PRs with tests missing version coverage

**Owner**: Test infrastructure team
**Priority**: 🟢 LOW (audit complete, only CI integration remains)

---

## 🟡 MEDIUM Priority: Test Data-Driving Helper Migration (§8.42)

**Status**: 🟡 **IN PROGRESS** — Core helpers complete, migration ongoing.

**Problem Statement**:
NovaSharp tests require explicit manual `[Arguments]` entries for every Lua version combination. The helper attributes reduce 5+ lines of attributes per test to a single line.

### Available Helpers

All helpers are in `src/tests/TestInfrastructure/TUnit/`:

| Helper | Description |
|--------|-------------|
| `[AllLuaVersions]` | Expands to all 5 Lua versions (5.1-5.5) |
| `[LuaVersionsFrom(5.3)]` | Versions from 5.3+ (inclusive) |
| `[LuaVersionsUntil(5.2)]` | Versions up to 5.2 (inclusive) |
| `[LuaVersionRange(5.2, 5.4)]` | Specific version range |
| `[LuaTestMatrix]` | Full Cartesian product of versions × inputs |

### Migration Status

| Category | Converted | Remaining |
|----------|-----------|-----------|
| MathModule tests | ✅ ~60 | - |
| Core EndToEnd (Simple, Closure, Coroutine, etc.) | ✅ ~145 | - |
| DispatchingUserDataDescriptorTUnitTests | ✅ 22 | - |
| StandardEnumUserDataDescriptorTUnitTests | ✅ 21 | - |
| UserDataEventsTUnitTests | ✅ 7 | - |
| ErrorHandlingModuleTUnitTests | ✅ 19 | - |
| CoroutineLifecycleTUnitTests | ✅ 11 | - |
| CompositeUserDataDescriptorTUnitTests | ✅ 8 | - |
| UserDataMethodsTUnitTests | ✅ 6 | - |
| UserDataMetaTUnitTests | ✅ 6 | - |
| UserDataNestedTypesTUnitTests | ✅ 7 | - |
| ProxyObjectsTUnitTests | ✅ 1 | - |
| ProxyUserDataDescriptorTUnitTests | ✅ 4 | - |
| DebugModuleTapParityTUnitTests | ✅ 17 | - |
| IoStdHandleUserDataTUnitTests | ✅ 15 | - |
| Other UserData tests | - | ~150+ |
| Other EndToEnd tests | - | ~100+ |

### Remaining Tasks

- [ ] Migrate remaining UserData tests (Methods overload patterns, Properties, Fields)
- [ ] Migrate remaining EndToEnd tests
- [ ] Migrate Sandbox tests
- [ ] Create automated migration script for common patterns
- [ ] Add lint rule to flag verbose patterns

**Owner**: Test infrastructure team
**Priority**: 🟡 MEDIUM

---

## 🔴 HIGH Priority: TUnit Lua Test Extraction Audit (§8.40)

**Status**: 📋 **AUDIT REQUIRED** — Extract inline Lua from TUnit tests into standalone fixtures.

**Goal**: Every TUnit test executing Lua code must have a corresponding `.lua` fixture file for cross-interpreter verification against lua5.1, lua5.2, lua5.3, lua5.4, lua5.5.

### Implementation Tasks

- [ ] **Phase 1**: Run corpus extractor to inventory inline Lua code
- [ ] **Phase 2**: Compare against existing `LuaFixtures/` directories
- [ ] **Phase 3**: Extract missing fixtures with metadata headers
- [ ] **Phase 4**: Run comparison harness against all Lua versions
- [ ] **Phase 5**: Fix NovaSharp bugs revealed by comparison
- [ ] **Phase 6**: Add CI validation for fixture coverage

### Commands

```bash
# Inventory
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --dry-run

# Cross-interpreter verification
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4 --results-dir artifacts/lua-comparison-5.4
```

**Owner**: Test infrastructure team
**Priority**: 🔴 HIGH

---

## 🔴 CRITICAL Priority: Lua 5.3+ Integer Representation Errors (§8.34)

**Status**: 📋 **DOCUMENTED** — Investigation complete, implementation pending.

**Problem Statement (2025-12-09)**:
Lua 5.3 introduced the concept of "integer representation" for numeric arguments to certain functions. Values that cannot be represented as integers (NaN, Infinity, non-integral floats in some contexts) must throw specific errors.

### Affected Functions (Partial List)

| Function | Parameter | Lua 5.1/5.2 Behavior | Lua 5.3+ Behavior |
|----------|-----------|---------------------|-------------------|
| `string.char(x)` | x | Treats NaN/Inf as 0 | Error |
| `string.byte(s, i, j)` | i, j | Floor truncation | Floor + validation |
| `string.rep(s, n)` | n | Floor truncation | Must be integer |
| `string.sub(s, i, j)` | i, j | Floor truncation | Floor + validation |
| `table.concat(t, sep, i, j)` | i, j | Floor truncation | Must be integer |
| `table.insert(t, pos, v)` | pos | Floor truncation | Must be integer |
| `table.remove(t, pos)` | pos | Floor truncation | Must be integer |
| `table.move(a1, f, e, t, a2)` | f, e, t | Floor truncation | Must be integer |
| `math.random(m, n)` | m, n | Floor truncation | Must be integer |
| `utf8.char(...)` | all args | N/A (5.3+) | Must be integer |
| `utf8.codepoint(s, i, j)` | i, j | N/A (5.3+) | Must be integer |

### Implementation Tasks

- [ ] Create `LuaNumberHelpers.ToIntegerStrict()` helper
- [ ] Audit all functions in the affected list
- [ ] Add version-aware validation to each function
- [ ] Create data-driven tests with NaN/Infinity/fractional inputs
- [ ] Add Lua fixtures for CI comparison testing

---

## 🟡 MEDIUM Priority: Comprehensive Numeric Edge-Case Audit (§8.36)

**Status**: 📋 **PARTIAL** — Core fixes done, remaining edge cases to audit.

**Problem**: Values beyond 2^53 cannot be exactly represented as doubles. Lua 5.3+ distinguishes integer vs float subtypes; NovaSharp must preserve this distinction through the validation pipeline.

**Completed**:
- ✅ `LuaNumber` struct preserves integer/float distinction
- ✅ Core validation uses `LuaNumber` not `double`
- ✅ Lint script prevents `DynValue.Number` usage in CoreLib

**Remaining**:
- [ ] Audit `Interop/Converters/*.cs` for precision loss patterns
- [ ] Create `NumericEdgeCaseTUnitTests.cs` with boundary values
- [ ] Document version-specific behavior in `docs/testing/numeric-edge-cases.md`

---

## Repository Snapshot (Updated 2025-12-19)

**Build & Tests**:
- Zero warnings with `<TreatWarningsAsErrors>true` enforced
- **10,219** interpreter tests via TUnit (Microsoft.Testing.Platform)
- Coverage: ~75.3% line / ~76.1% branch (gating targets at 90%)
- CI: Tests on matrix of `[ubuntu-latest, windows-latest, macos-latest]`

**TUnit Version Coverage Progress**:
- **1,985** tests with explicit version attributes (54.1% compliance)
- ✅ **0 Lua execution tests** need version coverage (100% complete!)
- **1,684** infrastructure tests (no Lua execution, exempt from version requirement)

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
- Fixture comparisons have some remaining mismatches (5.1: 34, 5.2: 30, 5.4: 17, 5.5: 19) — primarily error format differences
- ~1,260+ Lua fixtures extracted from C# tests, parallel runner operational
- Bytecode format version `0x151` preserves integer/float subtype
- JSON/bytecode serialization preserves integer/float subtype
- DynValue caching extended for negative integers and common floats
- All character classes, metamethod fallbacks, and version-specific behaviors implemented
- CLI argument registry (`CliArgumentRegistry`) with comprehensive Lua version support
- VM state protection (Phase 1) prevents external corruption

## Critical Initiatives

### Initiative 9: Version-Aware Lua Standard Library Parity 🟡 **MOSTLY COMPLETE**
**Goal**: ALL Lua functions must behave according to their version specification (5.1, 5.2, 5.3, 5.4).
**Scope**: Math, String, Table, Basic, Coroutine, OS, IO, UTF-8, Debug modules + metamethod behaviors.
**Status**: Most modules complete. See **Section 9** for remaining items.
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

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage improvement opportunities
Current coverage (~75% line, ~76% branch) has significant room for improvement. Key areas with low coverage include:
- **NovaSharp.Hardwire** (~54.8% line): Many generator code paths untested
- **CLI components**: Some command implementations have partial coverage
- **DebugModule**: REPL loop branches not easily testable
- **StreamFileUserDataBase**: Windows-specific CRLF paths cannot run on Linux CI

### 2. Codebase organization (future)
- Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
- Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
- Add guardrails so new code lands in correct folders with consistent namespaces

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

### 8. Lua Runtime Specification Parity (CRITICAL)

**Goal**: Ensure NovaSharp behaves identically to reference Lua interpreters across all supported versions (5.1, 5.2, 5.3, 5.4) for deterministic, reproducible script execution.

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

#### 8.11 Numerical For Loop Semantics (Lua 5.4)

**Breaking Change in 5.4**: Control variable in integer `for` loops never overflows/wraps.

**Tasks**:
- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds
- [ ] Document loop semantics per version

#### 8.14 __gc Metamethod Handling (Lua 5.4)

**Status**: 🔬 **INVESTIGATION COMPLETE** — NovaSharp doesn't have true finalizers, so `__gc` is not called during GC. Lua 5.4+ generates warnings for non-callable `__gc` values during finalization.

**Tasks**:
- [ ] Document NovaSharp's current `__gc` handling
- [ ] Decide on validation strategy (strict vs. Lua-compatible)

#### 8.15 utf8 Library Differences (Lua 5.3 vs 5.4)

**Remaining Tasks**:
- [ ] Verify `utf8.offset` bounds handling is complete
- [ ] Document utf8 library version differences

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

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- CLI output golden tests.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

## Recommended Next Steps (Priority Order)

### 🔴 IMMEDIATE: High-Priority Test Infrastructure Items

1. ✅ ~~**TUnit Multi-Version Coverage Audit** (Section §8.39)~~ — **COMPLETE**
   - All Lua execution tests now have explicit version coverage
   - **Next**: Add CI lint rule to enforce version coverage on new PRs

2. **TUnit Lua Test Extraction Audit** (Section §8.40) — 📋 **AUDIT REQUIRED**
   - Extract ALL inline Lua code from TUnit tests into standalone `.lua` fixture files
   - Enable cross-interpreter verification against lua5.1, lua5.2, lua5.3, lua5.4, lua5.5
   - Fix any NovaSharp bugs revealed by comparison (Lua spec is authoritative)

3. **Fixture Mismatch Investigation** — 📋 **INVESTIGATION REQUIRED**
   - Remaining mismatches: 5.1=34, 5.2=30, 5.4=17, 5.5=19
   - Many are error format differences; prioritize behavioral mismatches

### 🟡 MEDIUM: Remaining Version Parity Items

4. **`debug.setcstacklimit` for Lua 5.4** (Section 9.9)
   - New function, may require VM infrastructure changes

5. **Multi-user-value support for 5.4** (Section 9.9)
   - `debug.getuservalue`/`setuservalue` take `n` parameter in 5.4

6. **Deprecation warnings for `bit32` in 5.3 mode** (Section 9.10)
   - Library is available but deprecated; should emit warnings

### 🟢 LOWER PRIORITY: Polish and Infrastructure

7. **Version migration guides** (Section 9.12)
    - `docs/LuaVersionMigration.md` with 5.1→5.2, 5.2→5.3, 5.3→5.4 guides

8. **CI jobs per LuaCompatibilityVersion** (Section 9.12)
    - Run test suite explicitly with each version setting

9. **`string.pack`/`unpack` extended options** (Section 9.2)
    - Complete implementation of all format specifiers

### Future Phases (Lower Priority)

- **Interpreter hyper-optimization - Phase 4** (Initiative 5): 🔮 **PLANNED** — Zero-allocation runtime goal
    
    **Target:** Match or exceed native Lua performance; achieve <100 bytes/call allocation overhead.
    
    See `docs/performance/optimization-opportunities.md` for comprehensive plan covering:
    - VM dispatch optimization (computed goto, opcode fusion)
    - Table redesign (hybrid array+hash like native Lua)
    - DynValue struct conversion (optional breaking change)
    - Span-based APIs throughout
    - Roslyn source generators for interop

- **Concurrency improvements** (Initiative 7, optional):
    - Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics
    - Split debugger locks for reduced contention
    - Add timeout to `BlockingChannel`

- **Coverage improvements** (Initiative 12): 🟢 **LOW PRIORITY**
    
    **Goal**: Improve coverage to meet and eventually exceed gates.
    
    **Current coverage (2025-12-11)**:
    - Line coverage: ~75.3%
    - Branch coverage: ~76.1%
    
    **Current thresholds**:
    - Line coverage: 90%
    - Branch coverage: 90%
    - Method coverage: 90%
    
    **Tasks**:
    - [ ] Investigate coverage gaps in major modules (Hardwire, CLI)
    - [ ] Add tests for uncovered code paths
    - [ ] Monitor coverage trends as new features and tests are added

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.

---

## Initiative 9: Version-Aware Lua Standard Library Parity — Remaining Items

**Status**: 🟡 **MOSTLY COMPLETE** — Most modules implemented. Remaining: `string.pack`/`unpack` extended options, `debug.setcstacklimit`, multi-user-value support.

### 9.2 String Module Version Parity 🟡

| Function | Status | Notes |
|----------|--------|-------|
| `string.pack/unpack/packsize` | 🚧 Partial | Extended options (`c`, `z`, alignment) missing |
| `string.format('%a')` | 🔲 Verify | Hex float format specifier |

**Tasks**:
- [ ] Complete `string.pack`/`unpack` extended format options
- [ ] Implement `string.format('%a')` hex float format

### 9.9 Debug Module Version Parity 🔲

| Function | Status | Notes |
|----------|--------|-------|
| `debug.setcstacklimit` | 🔲 Implement | 5.4 only |
| `debug.setmetatable` return | �� Verify | Boolean in 5.4 |
| `debug.getuservalue/setuservalue` | 🔲 Implement | Multi-user-value in 5.4 |

### 9.10 Bitwise Operations — Remaining

- [ ] Emit deprecation warning for `bit32` in 5.3 mode
- [ ] Verify `bit32` unavailable in 5.4 mode

### 9.12 Testing Infrastructure

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

---

## Initiative 12: Lua-to-C# Ahead-of-Time Compiler (Offline DLL Generation) 🔬

**Status**: 🔲 **RESEARCH** — Long-term investigation item.

**Priority**: 🟢 **LOW** — Future optimization opportunity for game developers.

**Goal**: Investigate feasibility of creating an offline "Lua → C# compiler" tool that can compile Lua scripts into .NET DLLs loadable by NovaSharp for improved runtime performance.

### 12.1 Concept Overview

Game developers using NovaSharp could ship an offline compilation tool with their game that allows players (or modders) to pre-compile their Lua scripts into native .NET assemblies. These compiled DLLs would:

- Load significantly faster than interpreted Lua (no parsing/compilation at runtime)
- Execute faster due to JIT-optimized native code
- Still integrate seamlessly with NovaSharp's runtime (tables, coroutines, C# interop)
- Be optional—interpreted Lua would remain fully supported

### 12.2 Research Questions

1. **Feasibility**: Can Lua's dynamic semantics (metatables, dynamic typing, `_ENV` manipulation) be reasonably compiled to static C#?
2. **Performance Gains**: What speedup is realistic? (Likely 2-10x for compute-heavy scripts, minimal for I/O-bound)
3. **Compatibility**: How do compiled scripts interact with interpreted scripts, runtime `require()`, debug hooks?

### 12.5 Risks & Challenges

- **Semantic Fidelity**: Lua's extreme dynamism may resist static compilation
- **Maintenance Burden**: Two execution paths (interpreted + compiled) doubles testing surface
- **Edge Cases**: Metamethod chains, `debug.setlocal`, `load()` with dynamic strings
- **Unity IL2CPP**: Compiled DLLs must work under Unity's AOT restrictions

**Owner**: TBD (requires dedicated research effort)
**Effort Estimate**: Unknown—initial feasibility study: 2-4 weeks; full implementation: 3-6 months

---

## Initiative 13: GitHub Pages Benchmark Dashboard Improvements 🎨

**Status**: 🔲 **PLANNED**

**Priority**: 🟢 **LOW** — Quality-of-life improvement for contributors and maintainers.

**Goal**: Prettify and configure the `gh-pages` branch to provide a readable, well-documented benchmark dashboard that makes performance trends easy to understand.

### 13.1 Proposed Improvements

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
