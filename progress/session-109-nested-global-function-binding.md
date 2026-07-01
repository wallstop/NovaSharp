# Session 109 - Nested Global Function Binding

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `66cb9b8b`
- PR: `#43`
- Starting PR state: clean merge state, 22 checks passing, 2 expected jobs skipped, and no active unresolved current review threads.
- Latest Copilot review at `2026-07-01T07:10:33Z` reported the PR exceeds the 20,000-line review limit, with no actionable feedback.

## Goal

Make nested Lua entry points easy to bind once for repeated host calls. This targets Unity/game loops where script entry points commonly live under tables such as `Game.Update`, `Systems.Player.Tick`, or mod namespaces.

## Rationale

- `CompiledScript` already gives a low-allocation execution handle once a function or chunk has been resolved.
- `Script.BindGlobalFunction(string name)` currently supports only top-level string globals.
- Callers can manually resolve nested globals through `script.Globals.Get(...)` and then call `BindFunction`, but that is less discoverable and can encourage repeated path lookup in hot loops.
- Jint's public performance guidance recommends preparing scripts and caching the prepared handle for repeated execution.
- Lua-CSharp positions its public API around low-allocation/high-performance C# interop, which matches NovaSharp's active API direction.

## Proposed Scope

1. Add nested global binding entry points to `Script`:
   - `BindGlobalFunction(object key1, object key2)`
   - `BindGlobalFunction(object key1, object key2, object key3)`
   - `BindGlobalFunctionPath(object[] keys)`
   - `BindGlobalFunctionPath(ReadOnlySpan<object> keys)`
2. Preserve the existing `BindGlobalFunction(string name)` overload and its null/empty name behavior.
3. Route all new overloads through the already-validated `Table.Get` nested key paths and `BindFunction` callable validation.
4. Add TUnit coverage for fixed paths, span slices, array paths, initially resolved function caching, empty/null path validation, missing path diagnostics, and non-callable targets.
5. Add benchmark rows comparing repeated nested global lookup/call with a bound nested handle.

## Review Checklist

- [x] Sub-agent API review completed.
- [x] Existing top-level string overload behavior preserved.
- [x] Tests cover fixed, array, span/slice, missing, empty/null, and non-callable paths.
- [x] Benchmark coverage added for repeated nested lookup versus bound nested execution.
- [x] Targeted tests run.
- [x] Build/broader checks run as appropriate.
- [ ] Commit, push, request Copilot review, and poll PR CI.

## Status

In progress.

## Design Note

The sub-agent review found that adding single-argument `object[]` or span overloads directly to `BindGlobalFunction` would make source calls like `BindGlobalFunction(null)` ambiguous. The implementation therefore keeps the common allocation-free fixed two/three-key overloads on `BindGlobalFunction` and uses `BindGlobalFunctionPath` for arbitrary-depth caller-owned array/span paths.

## Validation

- `./scripts/test/quick.sh ScriptLoad`: passed, 28 tests.
- `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `./scripts/test/quick.sh`: initially passed, 14,372 tests, before later test-isolation edits.
- After formatting and the hardwire registry test-isolation fix, `./scripts/test/quick.sh`: passed, 14,373 tests.
- `bash ./scripts/dev/pre-commit.sh`: completed successfully; it reported existing documentation and skill metadata warnings.
- Final `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- Final `./scripts/test/quick.sh`: passed, 14,373 tests.

## Follow-up From Broader Test Run

The post-format full suite exposed a hardwire generator registry test race unrelated to the nested binding API. Details are recorded separately in `progress/session-110-hardwire-registry-test-isolation.md`.
