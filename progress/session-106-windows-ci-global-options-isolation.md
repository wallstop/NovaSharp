# Session 106: Windows CI Global Options Isolation

## Goal

Diagnose and fix the Windows PR CI failure from the closure object-argument span push without weakening Lua correctness or hiding a production regression.

## Inputs

- Current branch: `dev/wallstop/api-perf`
- Current PR: `#43`
- Failed head: `111fabf8`
- Failed check: `Tests / dotnet-tests (windows-latest)`
- User request: after each push, request Copilot review, inspect failing CI or relevant review feedback, and provide the appropriate fix.

## Diagnosis

- PR CI reported one failure on Windows while benchmark, formatting, lint, coverage, macOS dotnet tests, Ubuntu dotnet tests, and all Lua comparison jobs passed.
- The failing test was `MethodWithOutParamsInterleavedWithDifferentMethods` in `FunctionMemberDescriptorBaseTUnitTests`.
- The observed result was `nil|custom:1|custom:2;7;nil|5|6;X|XY|x;nil|7|8` instead of `nil|1|2;7;nil|5|6;X|XY|x;nil|7|8`.
- The `custom:` prefix matches a test-only CLR-to-script custom converter for primitive values. The out/ref descriptor tests exercise conversion paths that read `Script.GlobalOptions`, but the class only had `UserDataIsolation`.
- Existing converter-focused test classes already use `ScriptGlobalOptionsIsolation`, which serializes and snapshots global script options. The failing class should use the same guard because its assertions depend on default global conversion state.

## Fix

- Added `[ScriptGlobalOptionsIsolation]` to `FunctionMemberDescriptorBaseTUnitTests`.
- This is a test-infrastructure isolation fix. The production behavior for overload resolution and closure calls is unchanged.

## Validation Checklist

- [x] Targeted descriptor test
- [x] Targeted closure tests
- [x] Runtime build
- [x] Repo-wide tests
- [x] Pre-commit
- [x] Push hook
- [x] PR CI
- [x] Copilot review request after follow-up push

## Status

Fix commit `95ecbc11` pushed. PR CI was observed green for the follow-up head, including the previously failing Windows dotnet test job.

## Implementation Log

- `./scripts/test/quick.sh --full -c FunctionMemberDescriptorBaseTUnitTests` passed: 27 tests, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --full -c ClosureTUnitTests` passed: 143 tests, 0 failed, 0 skipped.
- `./scripts/build/quick.sh` completed successfully.
- `./scripts/test/quick.sh` passed: 14,329 tests, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` completed successfully. Documentation audit and LLM skill metadata checks emitted existing warnings, but no errors.
- Fix commit `95ecbc11` (`Isolate global options in descriptor tests`) pushed to `dev/wallstop/api-perf`; pre-push checks passed.
- Copilot review was requested after the follow-up push. Copilot responded at `2026-07-01T06:14:34Z` that the PR exceeds its 20,000 changed-line review limit, so there was no actionable new Copilot feedback from that request.
- GitHub PR checks passed for `95ecbc11`: benchmark, format-check, lint, code coverage, dotnet tests on ubuntu/windows/macos, and Lua comparisons for 5.1-5.5 across ubuntu/windows/macos. Optional benchmark comparison and lint-autofix jobs were skipped.
- Thread-aware PR comment inspection found `0` unresolved, non-outdated review threads after the follow-up push.

## Current Risks

- The `ScriptGlobalOptionsIsolation` attribute serializes this class with other global-options-sensitive tests, trading a small amount of test parallelism for deterministic global state.
