# Session 113 - Load Function Cache

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `46e96473`
- PR: `#43`
- Starting worktree: clean and aligned with `origin/dev/wallstop/api-perf`.
- Prior PR state: CI passing on the latest head, merge state clean, Copilot unable to review because the PR exceeds the 20,000-line limit, and no active unresolved current review threads.

## Goal

Make `LoadFunction`, `CompileFunction`, and `PrepareFunction` participate in the existing compilation cache without changing Lua function closure semantics.

## Rationale

- `PrepareString`, `PrepareStream`, and `PrepareFile` can already reuse bytecode through the script compilation cache.
- `PrepareFunction` still recompiles the same function source every time, which leaves a pit-of-success gap for Unity setup and hot-reload code that prepares small callbacks repeatedly.
- Caching only the bytecode entry point should preserve independent closures, per-call environments, Lua 5.1 `setfenv`, and debug source names while avoiding repeated lexing/parsing/bytecode emission.

## Planned Scope

1. Harden `ScriptCompilationCache` keys so chunks and standalone function bodies cannot collide.
2. Include the function environment-shape flag in the cache key, but do not key by the actual environment table.
3. Reuse cached function bytecode in `LoadFunction` while still creating a fresh closure for every call.
4. Add focused coverage for source counts, cache counts, cache-key isolation, closure/environment independence, Lua 5.1 `setfenv`, debugger signaling, and prepare alias behavior.
5. Add benchmark rows for cold and cached `PrepareFunction` paths.

## Review Checklist

- [x] Sub-agent API/performance recommendation completed.
- [x] Implementation added.
- [x] Adversarial review completed.
- [x] Targeted tests run.
- [x] Build/broader checks run as appropriate.
- [x] Commit, push, request Copilot review, and poll PR CI.

## Status

Implemented, validated, pushed, and observed passing PR CI for `3a2499fc`.

## Validation

- `dotnet build src/NovaSharp.sln -c Release --no-restore` passed before the final cleanup.
- `./scripts/test/quick.sh --full -c ScriptCompilationCacheTUnitTests` passed with 116 tests.
- `./scripts/test/quick.sh --full -c ModuleRegisterTUnitTests` passed with 57 tests after rerunning serially.
- `./scripts/test/quick.sh --no-build -c ScriptLoadTUnitTests` passed with 648 tests.
- `./scripts/test/quick.sh --no-build Prepare` passed with 88 tests.
- `./scripts/build/quick.sh` passed after formatting.
- `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release --no-restore` passed after formatting.
- `./scripts/test/quick.sh` passed after formatting with 14,453 tests.
- `bash ./scripts/dev/pre-commit.sh` passed after documenting the internal helper.
- `git diff --check` passed after formatting.
- Local pre-push hook passed on the implementation push.
- PR `#43` CI passed for `3a2499fc`: format, lint, benchmark, coverage, Linux/macOS/Windows dotnet tests, and Lua comparison jobs passed; `lint-autofix` and benchmark `comparison` were skipped as expected.

## Review Notes

- The first sub-agent supported the slice, but called out the need for a compilation-kind key dimension and a function environment-shape dimension.
- The cache must not store or return `DynValue`/`Closure` instances; cache hits must only reuse bytecode and then create fresh closures.
- A later adversarial pass found no blocking issues and led to additional coverage for module script-field cache isolation, nested closure independence, Lua 5.1 `setfenv` independence, and documentation updates.
- Copilot PR review was requested after pushing `3a2499fc`. The reviewer responded at `2026-07-01T11:38:59Z` that the PR still exceeds Copilot's 20,000-line review limit, so there was no actionable Copilot feedback.
- A plain `@copilot` comment also triggered the Copilot coding agent rather than the PR reviewer and returned a generic processing error; subsequent requests should use the reviewer request path.
- Thread-aware review scan after the push found no active unresolved non-outdated review threads.
