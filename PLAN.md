# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-05 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true>` enforced.
- **Tests**: **3,097** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.0% line / 93.3% branch / 97.8% method**. Branch coverage still below 95% target for enabling `COVERAGE_GATING_MODE=enforce`.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage and test depth
- **Current**: 3,097 tests, **96.0% line / 93.3% branch / 97.8% method** coverage.
- **Target**: Branch coverage >= 95% to enable `COVERAGE_GATING_MODE=enforce`.
- **Priority targets** (remaining low-branch coverage files):
  1. **DebugModule** (~85%): Most uncovered paths are in debug.debug REPL loop. **Note (2025-12-05)**: The REPL loop within `debug.debug()` cannot be tested because using `ReplInterpreter` inside a running script triggers a VM state issue (`ArgumentOutOfRangeException` in `ProcessingLoop`). Added 18 tests for debug.getinfo, debug.getlocal, debug.setlocal, debug.gethook/sethook covering CLR frame placeholders, function placeholders, and invalid indices.
  2. **StreamFileUserDataBase** (77.6%): Stream operations - most remaining branches are Windows-specific (CRLF normalization).
  3. **LuaCompatibilityProfile** (92.2%): Version-specific feature gates - mostly covered.
  4. **CharPtr** (~95%+): String pointer operations - added 30 null-argument tests covering all constructors and operators.
  5. **OverloadedMethodMemberDescriptor** (~85%): Overload resolution branches - added 8 tests covering out/ref params, extra args, Script/Context injection, and CallbackArguments.
- **Next step**: Continue with DebugModule remaining paths and other uncovered branches.

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
