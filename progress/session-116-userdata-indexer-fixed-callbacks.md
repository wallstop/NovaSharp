# Session 116 - Userdata Indexer Fixed Callbacks

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `66bb696d`
- PR: `#43`
- Starting worktree: clean and aligned with `origin/dev/wallstop/api-perf`.
- Prior PR state: CI passed on the latest head, Copilot reported that the PR exceeds the 20,000-line review limit, and thread-aware review inspection found no current unresolved non-outdated review threads.

## Goal

Remove avoidable argument materialization from common userdata indexer get/set callback dispatch for non-tuple indexes while preserving tuple indexer semantics and existing CLR callback behavior.

## Rationale

- `DispatchingUserDataDescriptor.ExecuteIndexer` currently allocates a `DynValue[]` and `CallbackArguments` for every non-tuple indexer get/set callback.
- No-context callback-view indexers can already be invoked through fixed-arity helpers without creating an argument array or dynamic execution context.
- Legacy callbacks still need a `CallbackArguments` instance, but the fixed helper avoids the extra per-call `DynValue[]` array.
- Tuple and multi-index cases are correctness-sensitive and keep the existing argument-list construction in this slice.

## Planned Scope

1. Validate the callback member before building indexer argument storage.
2. Route non-tuple getter callbacks through one-argument fixed invocation.
3. Route non-tuple setter callbacks through two-argument fixed invocation.
4. Keep tuple indexer behavior unchanged.
5. Add focused data-driven allocation coverage for non-tuple getter and setter callback-view dispatch, with diagnostics that report measured bytes.

## Review Checklist

- [x] Explorer review completed.
- [x] Implementation added.
- [x] Targeted tests run.
- [x] Build/broader checks run as appropriate.
- [x] Commit, push, request Copilot review, and poll PR CI.

## Status

Implementation commit `057493f4` was pushed, Copilot review was requested, and PR CI passed for that head. This progress-only update records the observed remote validation.

## Implementation Notes

- `DispatchingUserDataDescriptor.ExecuteIndexer` now validates the callback member before constructing argument storage.
- Non-tuple indexer get/set callbacks route through fixed one- or two-argument callback helpers.
- No-context callback-view indexers use script-based fixed invocation so they avoid dynamic context allocation.
- Contextful callback-view and legacy callbacks keep the existing callback-less dynamic context shape, preserving `ScriptExecutionContext.AdditionalData` behavior.
- Tuple and multi-index argument materialization remains unchanged.
- Added data-driven coverage for getter and setter allocation shape.
- Added data-driven coverage for legacy and contextful callback-view dynamic context shape.

## Review Notes

- Explorer review confirmed this was a valid next allocation slice and identified the context-shape risk for legacy/contextful callbacks.
- The implementation was adjusted so only no-context callback-view callbacks use script-based fixed invocation; legacy/contextful callbacks still create the same style of dynamic context as before.
- The explorer also noted separate future allocation work in standard indexer callback wrapper creation and array indexer `int[]` materialization; those are intentionally out of scope for this session.

## Validation

- `./scripts/test/quick.sh --full NonTupleIndexerCallbackViewAvoidsArgumentArrayAllocation` passed before the context-shape regression test was added: 2 total, 2 succeeded, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --full "NonTupleIndexer"` passed after the production context-shape adjustment: 4 total, 4 succeeded, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --no-build -c DispatchingUserDataDescriptorTUnitTests` passed: 127 total, 127 succeeded, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --no-build -c UserDataIndexerTUnitTests` passed: 8 total, 8 succeeded, 0 failed, 0 skipped.
- `dotnet build src/NovaSharp.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14,474 total, 14,474 succeeded, 0 failed, 0 skipped. The command printed expected negative shell-command diagnostics from command-execution tests.
- `bash ./scripts/dev/pre-commit.sh` completed successfully. It reported existing documentation and skill metadata warnings but no errors.
- `git diff --check` passed.
- Lua comparison was not run locally because this slice preserves Lua language behavior and changes CLR userdata indexer callback allocation only; PR CI comparison remains part of remote validation.

## Remote Validation

- Pushed implementation commit `057493f4` to PR `#43`.
- Requested Copilot review with `gh pr edit 43 --add-reviewer copilot-pull-request-reviewer`.
- Copilot's latest review response at `2026-07-01T14:36:37Z` said the PR exceeds the 20,000-line review limit, so no new actionable Copilot code comments were produced for this slice.
- Thread-aware review inspection found 0 current unresolved non-outdated review threads. Older unresolved Copilot threads are outdated, and current non-outdated threads are resolved.
- PR CI on `057493f4` passed: `benchmark`, `code-coverage`, `dotnet-tests` on Ubuntu/macOS/Windows, `format-check`, `lint`, and all Lua comparison matrix jobs for Lua 5.1 through 5.5 on Ubuntu/macOS/Windows. Expected skipped jobs: `comparison`, `lint-autofix`.
