# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-05 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true>` enforced.
- **Tests**: **3,182** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.7% branch / 97.8% method**. Branch coverage still below 95% target for enabling `COVERAGE_GATING_MODE=enforce`.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage and test depth
- **Current**: 3,182 tests, **96.2% line / 93.7% branch / 97.8% method** coverage.
- **Target**: Branch coverage >= 95% to enable `COVERAGE_GATING_MODE=enforce`.
- **Priority targets** (remaining low-branch coverage files):
  1. **DebugModule** (86.4% line / 75.4% branch): Most uncovered paths are in debug.debug REPL loop. **Note (2025-12-05)**: The REPL loop within `debug.debug()` cannot be tested because using `ReplInterpreter` inside a running script triggers a VM state issue (`ArgumentOutOfRangeException` in `ProcessingLoop`). Added 17 new tests for debug.traceback (with coroutine), debug.sethook/gethook (with coroutine target), debug.setmetatable (type metatables, unsupported types), debug.upvalueid/upvaluejoin edge cases. Branch coverage improved from 73.2% to 75.4%.
  2. **StreamFileUserDataBase** (77.6% line / 75.8% branch): Stream operations - most remaining branches are Windows-specific (CRLF normalization) and cannot be tested on Linux CI. Exception rethrow paths are also effectively dead code since underlying .NET stream classes don't throw `ScriptRuntimeException`.
  3. **LuaCompatibilityProfile** (100% line / 100% branch): ✅ Now fully covered with 37 new tests for ForVersion, GetDisplayName, and all profile properties.
  4. **CharPtr** (~95%+): String pointer operations - added 30 null-argument tests covering all constructors and operators.
  5. **OverloadedMethodMemberDescriptor** (82.9% line / 87.1% branch): Overload resolution branches - added 8 tests covering out/ref params, extra args, Script/Context injection, and CallbackArguments.
  6. **MathModule** (~91.3% branch → improved): ✅ Added 6 tests for math.frexp covering zero, negative zero, negative numbers, subnormal numbers, and round-trip. Frexp now at 100% coverage.
  7. **StringModule**: ✅ Added 4 tests for string.char with NaN, +Infinity, -Infinity, and numeric strings. NormalizeByte now at 100% coverage.
  8. **BasicModule**: ✅ Added 4 tests for tonumber with NaN/Infinity/non-integer base values.
  9. **BinaryEncoding**: ✅ Added 4 tests for destination index validation (negative index, index exceeds length).
  10. **ModuleRegister**: ✅ Added 1 test for RegisterConstants null argument check.
  11. **DynValue**: ✅ Added tests for NewTuple null check, ToDebugPrintString with null AsString, and GetHashCode for Boolean.
  12. **NumericConversions**: ✅ Added 8 tests for sbyte, ushort, uint, ulong conversions (DoubleToType and TypeToDouble).
- **Next step**: Continue exploring remaining testable branches in Processor, ScriptToClrConversions, and other core modules.

### 2. Codebase organization & namespace hygiene
- **Problem**: Monolithic layout mirrors legacy MoonSharp; contributors struggle to locate feature-specific code.
- **Objectives**:
  1. Split into feature-scoped projects (e.g., `NovaSharp.Interpreter.Core`, `NovaSharp.Interpreter.IO`).
  2. Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`).
  3. Add guardrails so new code lands in correct folders with consistent namespaces.

### 3. Analyzer and warning debt
- Build is clean with `<TreatWarningsAsErrors>true>`.
- Audit remaining suppressions (CA1051, CA1515, IDE1006) and convert to real fixes where possible.
- BenchmarkDotNet classes must remain `public`; keep targeted CA1515 suppressions.

### 4. Debugger and tooling automation
- DAP test harness for VsCodeDebugger (launch, attach, breakpoints, watches).
- CLI integration tests with stdin/stdout golden files.
- Remote-debugger smoke tests with golden payload assertions.
- Expand CI to Windows and macOS.

### 5. Runtime safety, sandboxing, and determinism
- Lua sandbox profiles toggling risky primitives via `ScriptOptions`.
- Configurable ceilings for time, memory, recursion depth, coroutine counts.
- Deterministic execution mode for lockstep multiplayer/replays.
- Per-mod isolation containers with load/reload/unload hooks.

### 6. Packaging and performance
- Unity UPM/embedded packaging with IL2CPP/AOT documentation.
- NuGet package pipeline with versioning/signatures.
- Enum allocation audit to remove `Enum.HasFlags`/`ToString()` on hot paths.
- Performance regression harness with BenchmarkDotNet in CI.
- Interpreter hot-path optimization (zero-allocation strategies, pooling).

### 7. Tooling, docs, and contributor experience
- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.
- Expand CI to run Lua TAP suites across Windows, macOS, Linux, Unity.

### 8. Outstanding investigations
- Confirm `pcall`/`xpcall` semantics when CLR callbacks yield.
- Decide on `SymbolRefAttributes` rename vs. CA1711 suppression.

### 9. Concurrency and synchronization audit
- Inventory all `lock`/`Monitor`/`SemaphoreSlim` usage.
- Identify shared collections needing `ConcurrentDictionary`/`ImmutableArray`.
- Validate dispose paths and async callbacks for deadlock potential.
- Produce `docs/modernization/concurrency-inventory.md`.

## Lua Specification Parity

### Reference Lua comparison harness
- **Goal**: Run test scripts against both NovaSharp and canonical Lua interpreters, diff outputs.
- **Approach**: Docker or native `lua5.x` install; `scripts/tests/compare-lua-outputs.sh`.
- **Priority**: High—would catch semantic bugs automatically.
- **Status**: Not started.

### Full Lua specification audit
- **Goal**: Audit every module/function against Lua manuals.
- **Scope**: Core libraries (`bit32`, `string`, `table`, `math`, `io`, `os`, `coroutine`, `debug`, `utf8`, `package`), language semantics, edge cases.
- **Tracking**: `docs/testing/spec-audit.md` contains detailed tracking table with status per feature.
- **Progress**: Most core features verified against Lua 5.4 manual; `string.pack`/`unpack` extended options remain unimplemented.

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- Golden-file assertions for debugger payloads and CLI output.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.
- Consolidate `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` into unified format.

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
