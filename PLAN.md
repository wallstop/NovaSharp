# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-06 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **3,287** interpreter tests + **72** debugger tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **96.2% line / 93.69% branch / 97.88% method**.
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 96% line / 93% branch / 97% method thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads (initialize, threads, breakpoints, events, evaluate, scopes, stackTrace, variables).
- **Sandbox infrastructure**: `SandboxOptions` with instruction limits, recursion limits, module/function restrictions, callbacks, and presets.

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
- **Remaining**:
  - Memory tracking (per-allocation accounting)
  - Deterministic execution mode for lockstep multiplayer/replays
  - Per-mod isolation containers with load/reload/unload hooks
  - Coroutine count limits

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
- **Gating**: ✅ Now in `enforce` mode (2025-12-06). Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory. See `StringModuleFixtureBasedTUnitTests.cs` for examples.

Key infrastructure:
- `src/tests/NovaSharp.Interpreter.Tests/LuaFixtures/` – 855 Lua fixtures with metadata headers
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

1. ~~**DAP golden payload tests**~~ (Initiative 3): ✅ **Completed 2025-12-06** — 12 initial golden payload tests added validating initialize, threads, breakpoints, events, and evaluate responses.

2. ~~**Extend golden payload coverage**~~: ✅ **Completed 2025-12-06** — 8 additional tests for scopes, stackTrace, and variables DAP responses. Total: 20 golden payload tests. Also fixed JSON serialization bug (empty arrays as `{}`) in `JsonTableConverter.ObjectToJson`.

3. ~~**Runtime sandboxing profiles**~~ (Initiative 4): ✅ **Completed 2025-12-06** — Implemented comprehensive sandbox infrastructure:
   - Created `SandboxOptions` class with instruction limits, call stack depth limits, and module/function restrictions
   - Added `SandboxViolationException` and `SandboxViolationType` for typed violation reporting
   - Integrated sandbox into `ScriptOptions` with copy-on-write semantics for the `Unrestricted` singleton
   - Added instruction counting in VM `ProcessingLoop` with callback support for custom handling
   - Added call stack depth checking in `InternalExecCall` before function invocations
   - Added function access checks to `load`, `loadfile`, `dofile` in `LoadModule`
   - Added module access checks to `require` via `__require_clr_impl`
   - Created preset factories: `SandboxOptions.CreateRestrictive()` and `SandboxOptions.CreateModerate()`
   - Added 39 TUnit tests covering instruction limits, recursion limits, access restrictions, and presets

4. ~~**Lua comparison gating**~~: ✅ **Completed 2025-12-06** — Promoted `lua-comparison` CI job from `warn` to `enforce` mode:
   - Documented 23 known divergences in `docs/testing/lua-divergences.md`
   - Updated `compare-lua-outputs.py` with built-in allowlist for known divergences
   - Added `--enforce` flag for CI gating (fails on unexpected mismatches only)
   - Enhanced output normalization for NovaSharp-specific address formats
   - Effective match rate: 76.2% of comparable fixtures (excluding CLR-dependent tests)

5. **Namespace restructuring** (Initiative 2): Begin splitting monolithic interpreter project, starting with `NovaSharp.Interpreter.IO`.

6. **Performance regression CI** (Initiative 5): Add BenchmarkDotNet runs to CI with threshold-based alerting.

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.
