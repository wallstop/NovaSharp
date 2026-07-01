# Session 111 - Prepare API Aliases

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `3ff62761`
- PR: `#43`
- Starting worktree: clean and aligned with `origin/dev/wallstop/api-perf`.
- Latest PR state from the prior push: checks passing, no active unresolved current review threads, and Copilot unable to review because the PR exceeds the 20,000-line limit.

## Goal

Make the existing low-allocation reusable execution path easier to discover by adding `Prepare*` public APIs that forward to the already-implemented `Compile*` and `Bind*` handles.

## Rationale

- `CompiledScript` already supports the intended "prepare once, execute many" workflow with fixed-arity and span execution overloads.
- `Compile*` and `Bind*` are technically correct but split the mental model between source compilation and callable binding.
- Unity/game-loop callers benefit from IntelliSense-visible APIs that describe the hot-loop pattern directly.
- The change should be additive and behavior-preserving: no Lua semantics, cache policy, bytecode, or ownership rules should change.

## Proposed Scope

1. Add `PrepareString`, `PrepareStream`, `PrepareFile`, and `PrepareFunction(string, ...)` aliases.
2. Add `PrepareCallable(DynValue)`, `PrepareGlobalFunction`, and `PrepareGlobalFunctionPath` aliases for already-resolved or global callable values.
3. Keep names explicit rather than adding a broad overloaded `Prepare(...)` entry point.
4. Add focused tests proving aliases preserve caching/source counts, stream ownership, global table binding, callable validation, and nested global path behavior.

## Review Checklist

- [x] Initial sub-agent API recommendation completed.
- [x] Implementation added.
- [x] Adversarial review completed.
- [x] Targeted tests run.
- [x] Build/broader checks run as appropriate.
- [ ] Commit, push, request Copilot review, and poll PR CI.

## Status

In progress.

## Validation

- `./scripts/test/quick.sh ScriptLoad`: passed, 28 tests.
- `./scripts/test/quick.sh Prepare`: passed, 78 tests.
- Initial `dotnet build src/NovaSharp.sln -c Release --no-restore`: failed on CA1720 for the `PrepareSourceKind.String` test enum value.
- Renamed the enum values to domain names and reran `./scripts/test/quick.sh Prepare`: passed, 78 tests.
- `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- After adversarial review cleanup, `./scripts/test/quick.sh Prepare`: passed, 88 tests.
- After adversarial review cleanup, `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- `./scripts/test/quick.sh`: passed, 14,383 tests.
- `bash ./scripts/dev/pre-commit.sh`: completed successfully; it reported existing documentation and skill metadata warnings and refreshed `docs/audits/documentation_audit.log`.

## Review Notes

- The adversarial review found no behavior-changing alias issues.
- It recommended aligning this note with the actual `PrepareCallable(DynValue)` API, updating `CompiledScript` XML docs to point at the new `Prepare*` names, and making the stream ownership assertion conditional on the stream source case.
- `PrepareCallable` was chosen instead of `PrepareFunction(DynValue)` to avoid a `PrepareFunction(null)` overload ambiguity with `PrepareFunction(string, ...)`.
