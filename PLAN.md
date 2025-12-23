# Modern Testing & Coverage Plan

## 🔴 Lua Spec Compliance Core Principle

NovaSharp's PRIMARY GOAL is to be a **faithful Lua interpreter** that matches the official Lua reference implementation. When fixture comparisons reveal behavioral differences:

1. **ASSUME NOVASHARP IS WRONG** until proven otherwise
2. **FIX THE PRODUCTION CODE** to match Lua behavior
3. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua
4. **NEVER adjust tests to accommodate bugs** — fix the runtime instead

**Current Status**: ✅ Lua fixture comparisons show **0 unexpected mismatches** across all versions (5.1, 5.2, 5.3, 5.4, 5.5). ~1,800 Lua fixtures verified.

---

## Repository Snapshot (Updated 2025-12-22)

**Build & Tests**:
- Zero warnings with `<TreatWarningsAsErrors>true` enforced
- **11,901** interpreter tests via TUnit (Microsoft.Testing.Platform)
- Coverage: ~75.3% line / ~76.1% branch (gating targets at 90%)
- CI: Tests on matrix of `[ubuntu-latest, windows-latest, macos-latest]`

**TUnit Version Coverage Progress**:
- **2,000+** tests with explicit version attributes
- All Lua execution tests have proper version coverage
- Helper attributes (`[AllLuaVersions]`, `[LuaVersionsFrom]`, etc.) available for new tests

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
- **Zero-Allocation Strings**: ZString integration complete, span-based operations for hot paths

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

### Initiative 13: Magic String Consolidation 🟡 **IN PROGRESS**
**Goal**: Eliminate all duplicated string literals ("magic strings") by consolidating them into named constants with a single source of truth.
**Scope**: All runtime, tooling, and test code.
**Status**: Phases 1-2 (Metamethods + Keywords) complete. Incremental enforcement during code changes.

**Completed**:
- ✅ **Phase 1: Metamethods** — Created `Metamethods` static class with 25 `const string` fields.
- ✅ **Phase 2: Lua Keywords** — Created `LuaKeywords` static class with 22 `const string` fields. Pre-interned in `LuaStringPool`.

**Remaining Areas to Consolidate** (Lower Priority):
1. **Error messages**: `bad argument`, `attempt to`, `number has no integer representation`, etc.
2. **Module names**: `string`, `table`, `math`, `io`, `os`, `debug`, `coroutine`, etc.

### Initiative 21: Performance Parity Analysis — NovaSharp vs Native Lua 🔬

**Status**: 🟡 **IN PROGRESS** — Phase 1 (script caching) and Phase 2 (VM pooling) complete.

**Priority**: 🟡 **MEDIUM** — Strategic performance initiative.

**Goal**: Systematically reduce the performance gap between NovaSharp (pure C# interpreter) and NLua (native Lua via P/Invoke).

**Completed Phases**:
- ✅ **Phase 1**: Script caching with hash-based lookup, lazy line-splitting
- ✅ **Phase 2**: VM execution pooling (LocalScope arrays, BlocksToClose, Varargs arrays)

**Phase 1 Deferred Items** (Session 092 Investigation):
- ❌ **SourceRef → struct**: NOT FEASIBLE — Debugger mutation patterns, ReferenceEquals usage, null semantics
- ❌ **SymbolRef → struct**: NOT FEASIBLE — Two-phase construction, binary deserialization back-patching
- ❌ **Instruction list pooling**: NOT APPLICABLE — ByteCode persists for Script lifetime

**Remaining Phases**:
- 🔲 **Phase 3**: High-impact structural changes (DynValue struct experiment, register-based VM)
- 🔲 **Phase 4**: String and pattern matching optimizations
- 🔲 **Phase 5**: Benchmark validation and documentation

See local progress reports: session-086 (script compilation cache), session-087 (VM execution pooling), session-088 (lazy line splitting), session-092 (ipairs metamethod parity).

---

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

---

## 🟡 MEDIUM Priority: Test Data-Driving Helper Migration

**Status**: 🟡 **IN PROGRESS** — Core helpers complete, migration ongoing.

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

- [ ] Migrate remaining UserData tests (Methods overload patterns)
- [ ] Migrate remaining EndToEnd tests
- [ ] Migrate Sandbox tests
- [ ] Create automated migration script for common patterns

---

## 🟡 MEDIUM Priority: Comprehensive Numeric Edge-Case Audit

**Status**: 📋 **PARTIAL** — Core fixes done, remaining edge cases to audit.

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

### os.time and os.date Semantics ✅ **COMPLETE**

**Status**: ✅ Complete — 149 tests verify os.time/os.date behavior across all Lua versions.

**Verified**:
- [x] `os.time()` returns epoch-based timestamp (integer in 5.3+, float in 5.1/5.2)
- [x] All `os.date` format strings match reference Lua outputs
- [x] Timezone handling via `!` prefix for UTC, local time conversion working
- [x] `*t` table format returns all required fields (year, month, day, hour, min, sec, wday, yday, isdst)

**Completed**: 2025-12-22

### Coroutine Semantics ✅ **COMPLETE**

**Status**: ✅ Complete — 596 tests verify coroutine behavior across all Lua versions.

**Verified**:
- [x] State transition tests for coroutine lifecycle (suspended, running, dead, normal)
- [x] Error message formats match Lua ("cannot resume dead coroutine", etc.)
- [x] `coroutine.close` (5.4) cleanup order with to-be-closed variables

**Completed**: 2025-12-22

### Error Message Parity

**Tasks**:
- [ ] Catalog all error message formats in `ScriptRuntimeException`
- [ ] Create error message normalization layer for Lua-compatible output

### Numerical For Loop Semantics (Lua 5.4)

**Tasks**:
- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds

### __gc Metamethod Handling (Lua 5.4)

**Tasks**:
- [ ] Document NovaSharp's current `__gc` handling
- [ ] Decide on validation strategy (strict vs. Lua-compatible)

### utf8 Library Differences (Lua 5.3 vs 5.4) ✅ **COMPLETE**

**Status**: ✅ Complete — 218 tests verify utf8 library behavior across Lua 5.3+.

**Verified**:
- [x] `utf8.offset` bounds handling is complete
- [x] Lax mode for invalid UTF-8 sequences (Lua 5.4+)
- [x] All utf8 functions match reference Lua behavior

**Completed**: 2025-12-22

### collectgarbage Options (Lua 5.4)

**Tasks**:
- [ ] Support deprecated options with warnings when targeting 5.4
- [ ] Implement `incremental` option for 5.4

### Literal Integer Overflow (Lua 5.4)

**Tasks**:
- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing

### ipairs Metamethod Changes (Lua 5.3+) ✅ **COMPLETE**

**Status**: ✅ Complete — Lua 5.3+ now respects `__index` metamethods during `ipairs` iteration.

**Implementation**:
- Added `GetMetamethodAwareIndex()` helper in `BasicModule.cs`
- `__ipairs_callback` now checks `LuaCompatibilityVersion` and uses metamethod-aware indexing for 5.3+
- Lua 5.1/5.2 continue using raw access (spec-compliant)
- 4 new Lua fixtures, 11 new TUnit tests

**Completed**: 2025-12-22

### table.unpack Location (Lua 5.2+) ✅ **COMPLETE**

**Status**: ✅ Complete — 18 tests verify unpack availability matches target version.

**Verified**:
- [x] `unpack` is global function in Lua 5.1 only (via `[LuaCompatibility(Lua51, Lua51)]`)
- [x] `table.unpack` available in Lua 5.2+ (via `TableModule`)
- [x] Both versions use same underlying implementation

**Completed**: 2025-12-22

### Documentation

- [ ] Update `docs/LuaCompatibility.md` with version-specific behavior notes
- [ ] Add "Determinism Guide" for users needing reproducible execution
- [ ] Create version migration guides (5.1→5.2, 5.2→5.3, 5.3→5.4)
- [ ] Add "Breaking Changes by Version" quick-reference table

---

## Testing Infrastructure

**Tasks**:
- [ ] Create comprehensive version matrix tests for all modules
- [ ] Add CI jobs that run test suite with each `LuaCompatibilityVersion`
- [ ] Create version migration guide (`docs/LuaVersionMigration.md`)

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

### 🟢 LOWER PRIORITY: Polish and Infrastructure

3. **CI Integration**
   - Add CI job that runs `compare-lua-outputs.py --enforce` on PRs
   - Add CI lint rule that rejects PRs with tests missing version coverage

---

## Future Research Initiatives

### 🔴 HIGH PRIORITY: Struct-Based AST for Zero-Allocation Parsing 🔬

**Status**: 🔲 **FEASIBILITY STUDY COMPLETE** — Investigation needed before implementation.

**Goal**: Investigate converting the class-based AST (NodeBase, Statement, Expression hierarchy) to structs + interfaces + generics to achieve zero-allocation parsing.

#### Feasibility Analysis Summary

**Current Architecture**:
- **27 node types**: 1 abstract base (`NodeBase`), 2 intermediate (`Statement`, `Expression`), 16 statement types, 11 expression types
- **Inheritance depth**: 2 levels (shallow, which is good for conversion)
- **Polymorphism**: Virtual `Compile()` method on all nodes, virtual `Eval()` on expressions
- **Allocation pattern**: Fresh `new` allocation per node, no pooling
- **Lifetime**: AST is **discarded after compilation** — only bytecode is retained

**Key Finding: AST Optimization Has Zero Runtime Impact** ⚠️

NovaSharp is a **bytecode VM**, not an AST interpreter:
1. Source → Parser → **AST (temporary)** → Compiler → **Bytecode (permanent)** → VM
2. AST nodes become garbage immediately after `Compile()` finishes
3. Runtime execution uses only the `Instruction` stream, not AST

**This means struct-based AST would only reduce GC pressure during script loading, NOT during execution.**

#### Technical Challenges

| Challenge | Severity | Mitigation |
|-----------|----------|------------|
| **Recursive struct references** | 🔴 Critical | Index-based children (store `int ChildIndex` into arrays) |
| **Polymorphism without boxing** | 🔴 Critical | Discriminated union + switch dispatch, OR generic visitors |
| **27 node types with different fields** | 🟡 High | Tagged union with parallel payload arrays per type |
| **Parser builds via constructors** | 🟡 High | Requires arena allocator pattern rewrite |
| **Virtual Compile() method** | 🟡 High | Replace with switch on `NodeKind` enum |
| **Script/SourceRef references in nodes** | 🟡 Medium | Store as indices into context arrays |
| **DynamicExprExpression keeps AST for Eval()** | 🟠 Medium | Exempt from conversion OR special handling |

#### Recommended Approach (If Proceeding)

**Option A: Tagged Union + Index-Based References** (Recommended if pursued)
```csharp
public enum NodeKind : byte { BinaryExpr, IfStmt, LiteralExpr, ... }

public readonly struct AstNode  // 16 bytes
{
    public readonly NodeKind Kind;
    public readonly byte Flags;
    public readonly ushort PayloadIndex;  // Index into kind-specific array
    public readonly int ChildStart;       // Index into children array
    public readonly int SpanStart;
    public readonly int SpanLength;
}

// Separate storage per node kind
struct BinaryExprPayload { byte Op; int LeftChild; int RightChild; }
struct LiteralPayload { DynValue Value; }
```

**Option B: Keep Classes, Add Node Pooling** (Lower risk, faster implementation)
```csharp
// Use ObjectPool<T> for common node types
var expr = LiteralExpressionPool.Get();
expr.Initialize(value, sourceRef);
// ... use node ...
// After Compile(), return to pool
```

#### Effort Estimate & ROI Analysis

| Approach | Effort | Risk | Performance Gain |
|----------|--------|------|------------------|
| **Full struct conversion** | 3-4 weeks | 🔴 High | Parsing only (no runtime impact) |
| **Node pooling** | 3-5 days | 🟢 Low | Similar to struct approach |
| **Arena allocator** | 1-2 weeks | 🟡 Medium | Similar, cleaner GC profile |

**Recommendation**: ❌ **NOT RECOMMENDED** for immediate implementation.

**Rationale**:
1. AST is discarded after compilation — zero runtime benefit
2. Script caching (`ScriptBytecodeCache`) already eliminates repeated parsing
3. Effort/risk ratio unfavorable compared to node pooling
4. Roslyn (much larger AST) uses classes with pooling successfully

**Alternative High-Value Targets** (same effort, runtime impact):
- `DynValue` class → struct conversion (Phase 3 of Initiative 21)
- `Instruction` class → struct conversion (bytecode execution hot path)
- String interning improvements

#### If Business Case Exists for Zero-Allocation Parsing

Proceed with **Option A** only if:
- Profiling shows parsing is a bottleneck in production
- Many small scripts compiled frequently (REPL, hot-reload scenarios)
- Memory-constrained environments (embedded, mobile)

**Implementation Plan** (if approved):
1. Create `AstArena` class with pre-allocated node arrays
2. Convert leaf nodes first (`LiteralExpression`, `BreakStatement`)
3. Add `NodeKind` discriminated union wrapper
4. Implement switch-based `Compile()` dispatch
5. Convert remaining nodes incrementally
6. Benchmark parsing memory usage before/after

See progress reports from AST analysis: Initiative 12 (allocation analysis), Initiative 18 (compiler memory).

---

### Lua-to-C# Ahead-of-Time Compiler 🔬

**Status**: 🔲 **RESEARCH** — Long-term investigation item.

**Goal**: Investigate feasibility of creating an offline "Lua → C# compiler" tool for game developers.

**Risks**:
- Lua's extreme dynamism may resist static compilation
- Two execution paths doubles testing surface
- Unity IL2CPP constraints

**Effort Estimate**: Initial feasibility study: 2-4 weeks

### GitHub Pages Benchmark Dashboard Improvements 🎨

**Status**: 🔲 **PLANNED**

**Goal**: Prettify and configure the `gh-pages` branch for a readable benchmark dashboard.

**Tasks**:
- [ ] Expand README with benchmark methodology
- [ ] Configure chart options
- [ ] Add styled index.html

**Effort Estimate**: 1-2 days

---

## Completed Initiatives (Archived)

The following initiatives have been fully completed and their detailed documentation has been moved to the progress reports. They remain here as a summary for historical reference.

| Initiative | Description | Completed | Progress Report |
|------------|-------------|-----------|-----------------|
| **9** | Version-Aware Lua Standard Library Parity | 2025-12-22 | Multiple sessions |
| **10** | KopiLua Performance Hyper-Optimization | 2025-12-21 | session-074 - session-076 |
| **11** | Comprehensive Helper Performance Audit | 2025-12-22 | session-079 - session-082 |
| **12** | Deep Codebase Allocation Analysis | 2025-12-22 | session-083 |
| **14** | SystemArrayPool Abstraction | 2025-12-21 | session-063 |
| **15** | Boxing-Free IList Sort Extensions | 2025-12-21 | session-066 |
| **16** | Boxing-Free pdqsort Integration | 2025-12-21 | session-067 |
| **17** | Metamethod Enum Optimization | ❌ Closed | session-069 (Not beneficial) |
| **18** | Large Script Load/Compile Memory | 2025-12-22 | session-070 - session-084 |
| **19** | HashCodeHelper Migration | 2025-12-21 | session-072 |
| **20** | NLua Architecture Investigation | 2025-12-21 | session-077 |
| **22** | ZString Migration | 2025-12-22 | session-089 |
| **23** | Span-Based Array Operation Migration | 2025-12-22 | session-090 |

---

Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
