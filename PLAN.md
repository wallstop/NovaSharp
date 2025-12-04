# Modern Testing & Coverage Plan

## Repository Snapshot — 2025-12-04 (UTC)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true>` enforced.
- **Tests**: **2,982** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: Interpreter at **95.7% line / 92.9% branch / 97.7% method**. Branch coverage still below 95% target for enabling `COVERAGE_GATING_MODE=enforce`.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage and test depth
- **Current**: 2,982 tests, **95.7% line / 92.9% branch / 97.7% method** coverage.
- **Target**: Branch coverage >= 95% to enable `COVERAGE_GATING_MODE=enforce`.
- **Recent progress** (2025-12-04):
  - Added 17 tests for `FunctionMemberDescriptorBase`: CreateCallbackDynValue, GetCallbackAsDynValue, GetCallbackFunction, GetCallback, GetValue, SetValue (throws), MemberAccess, VarArgs with UserData array passthrough, ref/out parameters, VoidWithOutParams, SortDiscriminant, ExtensionMethodType.
  - Added 7 tests for `Slice<T>`: Reversed property, IsReadOnly, IndexOf, Contains.
  - Added 5 tests for `DotNetCorePlatformAccessor`: GetStandardStream invalid type, ExecuteCommand empty/whitespace, FilterSupportedCoreModules, GetPlatformNamePrefix.
  - Added 1 test for `CustomConverterRegistry`: ObsoleteTypedClrToScriptConversion null behavior (documents a bug in the obsolete method).
  - Previous: Added 41 tests for `ScriptRuntimeException`: LoopInIndex, LoopInNewIndex, LoopInCall, BadArgumentNoNegativeNumbers, AttemptToCallNonFunc with debugText, null-guard branches for ArithmeticOnNonNumber, BitwiseOnNonInteger, ConcatOnNonString, LenOnInvalidType, CompareInvalidType, IndexType, ConvertObjectFailed, UserDataArgumentTypeMismatch, AccessInstanceMemberOnStatics, constructor paths (Exception, ScriptRuntimeException), Rethrow with GlobalOptions.RethrowExceptionNested, CloseMetamethodExpected null/non-null. Coverage improved from 82.2% → 95%+ line, 66.6% → 100% branch for ScriptRuntimeException.
  - Previous: Added 9 tests for `StreamFileUserDataBase`, 6 tests for `OverloadedMethodMemberDescriptor`.
- **Priority targets** (remaining low-branch coverage files):
  1. **FunctionMemberDescriptorBase** (84.4% → ~85%): Some varargs UserData array passthrough edge cases still uncovered.
  2. **OverloadedMethodMemberDescriptor** (82.9%): Cache overflow, CheckMatch hasObject mismatch, varargs exact UserData match.
  3. **DotNetCorePlatformAccessor** (86.1%): Windows shell paths (platform-specific), ExitFast.
  4. **ExtensionMethodsRegistry** (50%): Extension method discovery paths.
  5. **TypeDescriptorRegistry** (83.3%): Descriptor cache paths.
- **Next step**: Continue targeting files under 90% branch coverage.

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

### Multi-version compatibility ✅
- **Status**: Implemented via `LuaCompatibilityProfile` class.
- **Supported versions**: Lua 5.2, 5.3, 5.4, 5.5 selectable via `Script.Options.CompatibilityVersion`.
- **Version-gated features**: `bit32` library (5.2 only), bitwise operators (5.3+), `utf8` library (5.3+), `table.move` (5.3+), `<const>`/`<close>` attributes (5.4+), `warn` function (5.4+).
- **Documentation**: See `docs/compatibility/compatibility-profiles.md` and `docs/LuaCompatibility.md`.

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
