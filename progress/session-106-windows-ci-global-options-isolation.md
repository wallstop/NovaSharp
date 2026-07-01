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
- [ ] Push hook
- [ ] PR CI
- [ ] Copilot review request after follow-up push

## Status

Implementation in progress. The isolation fix is applied locally; targeted tests, runtime build, repo-wide tests, and pre-commit pass.

## Implementation Log

- `./scripts/test/quick.sh --full -c FunctionMemberDescriptorBaseTUnitTests` passed: 27 tests, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --full -c ClosureTUnitTests` passed: 143 tests, 0 failed, 0 skipped.
- `./scripts/build/quick.sh` completed successfully.
- `./scripts/test/quick.sh` passed: 14,329 tests, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` completed successfully. Documentation audit and LLM skill metadata checks emitted existing warnings, but no errors.

## Current Risks

- The Windows failure was not reproducible on Linux before the fix, so PR CI is the decisive validation for this specific race.
- The `ScriptGlobalOptionsIsolation` attribute serializes this class with other global-options-sensitive tests, trading a small amount of test parallelism for deterministic global state.
