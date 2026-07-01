# Session 103: No-Context Callback View

## Goal

Continue the public API and compiler/runtime performance redesign by removing avoidable allocations from callback-view hot paths while preserving Lua correctness and Unity/AOT compatibility.

## Inputs

- Current branch: `dev/wallstop/api-perf`
- Current PR: `#43`
- Current head before this slice: `6177f1bc`
- User request: keep progress notes in the repo-level `progress/` directory using `session-NUMBER-brief-description` naming.

## Research And Review Notes

- Repo priority remains correctness first, then speed, then allocation reduction, then Unity compatibility.
- Sub-agent API analysis ranked a contextless callback-view API as the highest-impact next slice because the existing `ScriptFunctionCallbackView` path always needs a `ScriptExecutionContext`, even when the callback only reads arguments.
- Sub-agent adversarial review found a must-fix `NETFX_CORE` compile issue in `ClrToScriptConversions`: the delegate conversion branch references `d.GetMethodInfo()` even though the delegate variable is named `@delegate`.
- The same review called out Unity/AOT risk around ref-struct callback views and reflection-based delegate conversion. The selected slice is additive and keeps the current contextful callback-view behavior intact.
- Local benchmark probing showed cache-hot `LoadString` remains cheap, while actual cached execution cost is dominated by running the chunk body. That makes callback invocation overhead a better next runtime/API target than cache-hot load lookup.

## Planned Changes

1. Fix the `NETFX_CORE` delegate conversion typo.
1. Add a no-context callback-view delegate and factory path.
1. Route direct CLR callback calls and Lua-to-CLR callback calls through the no-context path without constructing `ScriptExecutionContext` when the callback does not need it.
1. Keep existing `ScriptFunctionCallbackView` and legacy `CallbackArguments` semantics unchanged.
1. Add focused tests for direct calls, Lua calls, method-call normalization, and allocation behavior.
1. Extend focused benchmarks for no-context callback-view calls.

## Validation Checklist

- [x] Targeted callback tests
- [x] Targeted script-call tests
- [x] Runtime build
- [x] Benchmark project build
- [x] Focused callback benchmarks
- [x] Pre-commit
- [x] Push hook
- [x] PR CI
- [x] Copilot review request after push

## Status

Implementation commit pushed and PR CI observed green.

## Implementation Log

- Added `ScriptFunctionCallbackViewNoContext` and `DynValue.NewCallbackView(...)` overloads for callbacks that only need `CallbackArgumentsView`.
- Fixed the `NETFX_CORE` delegate conversion typo by using the actual `@delegate` variable when reading method metadata.
- Routed direct callback-view calls through `CallbackFunction` overloads that accept the owning `Script`, creating `ScriptExecutionContext` only for legacy or contextful callbacks.
- Updated VM CLR-call dispatch to avoid constructing `ScriptExecutionContext` for no-context callback-view functions while preserving the existing execution-stack frame.
- Extended module registration so `[NovaSharpModuleMethod]` methods with `DynValue Method(CallbackArgumentsView args)` are cached and registered as no-context callback-view functions.
- Added focused TUnit coverage for no-context callback-view signature recognition, converter handling, method-call normalization, direct calls, Lua-to-CLR calls, span-backed calls, module registration, and context allocation avoidance.
- Added BenchmarkDotNet rows for no-context callback-view host calls, callable-table calls, and Lua-to-CLR callback calls beside the existing legacy and contextful callback-view rows.

## Current Risks

- Runtime release build succeeded after formatting via `./scripts/build/quick.sh`.
- Benchmark project build succeeded after formatting.
- Targeted `CallbackFunctionTUnitTests`, `ClrToScriptConversionsTUnitTests`, `ModuleRegisterTUnitTests`, and `ScriptCallTUnitTests` passed sequentially with `./scripts/test/quick.sh --full -c ...`.
- Repo-wide `./scripts/test/quick.sh` passed: 14,305 tests, 0 failed, 0 skipped.
- Focused BenchmarkDotNet filter `*NoContextThree*` ran 4 benchmarks. Host fixed/span no-context callback-view calls reported zero allocated bytes; params-array calls reported the expected 48 B caller array allocation; Lua-to-CLR no-context calls reported 304 B including VM-call overhead.
- Pre-commit completed successfully. It refreshed `docs/audits/documentation_audit.log` with a line-number-only update caused by the `Script.cs` edits.
- Commit `9c2f68dc` (`Add contextless callback view API`) pushed to `dev/wallstop/api-perf`; pre-push checks passed.
- GitHub PR checks passed for the pushed commit: CSharpier `format-check`, Benchmarks `benchmark`, Tests `lint`, `dotnet-tests` on ubuntu/windows/macos, `code-coverage`, and Lua comparisons for 5.1-5.5 across ubuntu/windows/macos. Optional `comparison` and `lint-autofix` jobs were skipped.
- Copilot review was requested after the push. Copilot responded that the PR exceeds its 20,000 changed-line review limit, so there was no actionable new Copilot feedback from this request.
- Thread-aware PR comment inspection found no unresolved, non-outdated review threads after the push.
