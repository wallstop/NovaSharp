# Session 117: Userdata Indexer Direct Overloads

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `72690071`
- PR: `#43`
- Worktree: clean and aligned with `origin/dev/wallstop/api-perf` at session start.
- PR baseline: latest observed required checks were passing for `72690071`; expected optional comparison/lint-autofix jobs were skipped.
- Review baseline: latest Copilot review reported the PR exceeded the 20,000-line review limit and had no actionable feedback. No current unresolved non-outdated review threads were observed.

## Goal

Remove avoidable callback wrapper materialization from bracket userdata indexer execution when the registered indexer member is the standard overload wrapper created by `DispatchingUserDataDescriptor.AddMember`.

## Rationale

Session 116 removed argument-array allocation for fixed non-tuple callback views, but standard reflection/hardwired userdata indexers still flow through `IMemberDescriptor.GetValue`, which creates a callback delegate, `CallbackFunction`, and `DynValue` before immediately invoking it. Normal overloadable member registration stores these members as `OverloadedMethodMemberDescriptor`, so exposing a direct execution path there can preserve overload resolution while bypassing the callback wrapper.

The alternative `ArrayMemberDescriptor` fixed-index slice is still relevant, but this session keeps the next change close to the session 116 indexer work and targets the broader C# userdata path first.

## Implementation Plan

- Add an internal direct execution entry point on `OverloadedMethodMemberDescriptor` that forwards to existing overload resolution.
- Teach `DispatchingUserDataDescriptor.ExecuteIndexer` to call that direct path before falling back to callback-valued descriptor validation.
- Keep custom non-overload descriptors on the existing `GetValue` callback path so invalid callback diagnostics and callback-view optimizations remain unchanged.
- Add focused TUnit coverage that proves registered overload indexers no longer call `GetValue` during bracket getter/setter execution and that the hot path allocation stays below a diagnostic threshold.

## Notes

- A parallel explorer recommended the `ArrayMemberDescriptor` fixed-index slice as the most isolated alternative and warned against descriptor-level callback caching.
- This session intentionally avoids caching `DynValue` or `CallbackFunction` instances, preserving per-script/per-object callback identity and mutable callback state semantics. The direct path only applies to `OverloadedMethodMemberDescriptor`, which is the standard wrapper created by overloadable member registration.

## Validation Plan

- Targeted descriptor tests for the new direct indexer behavior.
- Interpreter build and full quick tests before commit.
- Pre-commit validation before push.
- After push, request Copilot review and poll PR CI; fix actionable production or test issues based on the actual failure source.

## Status

- Implemented direct overload wrapper execution for userdata indexers.
- Added data-driven getter/setter tests for the direct wrapper path and registered indexer allocation diagnostics.
- Targeted validation:
  - `./scripts/test/quick.sh --full RegisteredOverloadedIndexer` passed.
  - `./scripts/test/quick.sh ExecuteIndexerUsesDirectOverloadWrapperPath` passed.
  - `./scripts/test/quick.sh -c DispatchingUserDataDescriptorTUnitTests` passed.
  - `./scripts/test/quick.sh -c ArrayMemberDescriptorTUnitTests` passed.
- Broader local validation:
  - `dotnet tool restore && dotnet tool run csharpier format .` completed.
  - `./scripts/build/quick.sh` passed.
  - `./scripts/test/quick.sh` passed.
  - `./scripts/dev/pre-commit.sh` completed successfully, with existing documentation and skill metadata warnings.
- Push, Copilot review, and PR CI polling are still pending.
