# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-06 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **3,243** interpreter tests + **64** debugger tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 12 new tests validating VS Code debugger protocol payloads added 2025-12-06.

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
- **Problem**: Monolithic layout mirrors legacy MoonSharp; contributors struggle to locate feature-specific code.
- **Objectives**:
  1. Split into feature-scoped projects (e.g., `NovaSharp.Interpreter.Core`, `NovaSharp.Interpreter.IO`).
  2. Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`).
  3. Add guardrails so new code lands in correct folders with consistent namespaces.

### 3. Debugger DAP testing
- **Current**: **64 TUnit tests** in `NovaSharp.RemoteDebugger.Tests.TUnit/` covering handshake, breakpoints, stepping, watches, expressions, VS Code server lifecycle, **and golden payload validation**.
- **Completed (2025-12-06)**:
  - Added `GoldenPayloads/` directory with reference JSON files for initialize, threads, setBreakpoints, initialized event, and evaluate responses.
  - Created `GoldenPayloadHelper.cs` with JSON comparison utilities supporting case-insensitive property matching, semantic normalization, and configurable ignored properties (sequence numbers).
  - Added `VsCodeDebugSessionGoldenTUnitTests.cs` with 12 new tests validating DAP protocol responses against golden files:
    - Initialize response capabilities verification
    - Threads response structure validation
    - SetBreakpoints response with verified breakpoint entries
    - Initialized event emission
    - Evaluate response for number, nil, boolean, and function types
    - Multi-breakpoint verification
  - All tests use actual DAP protocol fixtures to ensure wire-format correctness.
- **Remaining**: Additional golden tests for stackTrace, scopes, and variables responses.

### 4. Runtime safety, sandboxing, and determinism
- Lua sandbox profiles toggling risky primitives via `ScriptOptions`.
- Configurable ceilings for time, memory, recursion depth, coroutine counts.
- Deterministic execution mode for lockstep multiplayer/replays.
- Per-mod isolation containers with load/reload/unload hooks.

### 5. Packaging and performance
- Unity UPM/embedded packaging with IL2CPP/AOT documentation.
- NuGet package pipeline with versioning/signatures.
- Performance regression harness with BenchmarkDotNet in CI.
- Interpreter hot-path optimization (zero-allocation strategies, pooling).

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
- **Gating**: Currently in `warn` mode. Promote to `enforce` once baseline divergences are documented.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory. See `StringModuleFixtureBasedTUnitTests.cs` for examples.

Key infrastructure:
- `src/tests/NovaSharp.Interpreter.Tests/LuaFixtures/` – 855 Lua fixtures with metadata headers
- `src/tests/TestInfrastructure/LuaFixtures/LuaFixtureHelper.cs` – Test helper for loading fixtures
- `src/tooling/NovaSharp.LuaBatchRunner/` – Batch execution tool (32s for 830 files)
- `scripts/tests/run-lua-fixtures-fast.sh` – Multi-version fixture runner
- `scripts/tests/compare-lua-outputs.py` – Diff engine with semantic normalization
- `docs/testing/lua-comparison-harness.md` – Contributor guide

### Full Lua specification audit
- **Tracking**: `docs/testing/spec-audit.md` contains detailed tracking table with status per feature.
- **Progress**: Most core features verified against Lua 5.4 manual; `string.pack`/`unpack` extended options remain unimplemented.

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- ~~Golden-file assertions for debugger payloads~~ (completed 2025-12-06) and CLI output.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

## Recommended Next Steps (Priority Order)

1. ~~**DAP golden payload tests**~~ (Initiative 3): ✅ **Completed 2025-12-06** — 12 new golden payload tests added validating initialize, threads, breakpoints, events, and evaluate responses.

2. **Extend golden payload coverage**: Add tests for stackTrace, scopes, and variables DAP responses to complete debugger protocol validation.

3. **Runtime sandboxing profiles** (Initiative 4): Implement `ScriptOptions` extensions for instruction limits, memory tracking, recursion depth, and module restrictions.

4. **Lua comparison gating**: Promote `lua-comparison` CI job from `warn` to `enforce` once baseline divergences are documented.

4. **Namespace restructuring** (Initiative 2): Begin splitting monolithic interpreter project, starting with `NovaSharp.Interpreter.IO`.

5. **Performance regression CI** (Initiative 5): Add BenchmarkDotNet runs to CI with threshold-based alerting.

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
