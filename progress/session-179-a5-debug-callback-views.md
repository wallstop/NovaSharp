# Session 179 — A5 Debug callback views

Date: 2026-08-29

## Objective

Advance Phase A5 with the first remaining CoreLib registration slice: migrate
every `debug.*` built-in callback to stack-only argument views while retaining
the legacy public C# overloads and exact Lua 5.1–5.5 behavior.

## Starting evidence

- `main` and `origin/main` were both at `ac67dfe8`, the merge of PR #116.
- The complete paginated GitHub inventory contained 18 open issues and one open
  PR. A5 issue #108 remained the highest direct gameplay-impact item.
- Main's Tests, Benchmarks, CSharpier, and Pages workflows were successful.
- Dependency PR #115 remained unsafe to incorporate: coverage was cancelled and
  its known Coverlet 10/net8 incompatibility is already tracked by issue #114.
- The focused registration test failed all five cases because `debug.debug` was
  still backed by the legacy `CallbackArguments` callback.

## Implementation

- Added preferred private `CallbackArgumentsView` overloads for all 16 exported
  Debug functions; built-in registration now selects the stack-only signatures.
- Kept the public `CallbackArguments` methods as validated compatibility shims.
- Preserved the legacy null-argument behavior of direct `SetHook` and `GetHook`
  calls by adapting null to an empty view only in those public shims.
- Converted Debug-only argument helpers to consume the view and retained the
  exact legacy integer-conversion semantics.
- Added a multi-version registration regression and representative Lua execution
  path, regenerated the fixture corpus, and curated the new fixture to `5.1+`
  after direct reference-Lua verification.
- Added direct coverage for all 16 public legacy signatures: the 14 validating
  shims retain their `args` null rejection, while `SetHook` and `GetHook` retain
  their historical null-as-empty behavior.
- Removed Debug from PLAN's remaining A5 module queue; String is next.

## Local verification

- Red gate: `RegisteredDebugCallbacksUseArgumentViews` failed 5/5 cases before
  the production migration.
- Green gate: the same test passed 5/5 after the migration.
- Focused Debug suites: 607 Debug module tests and 85 TAP-parity tests passed.
- Extractor suites: 32 extractor tests and 6 metadata tests passed.
- The new fixture exited successfully on reference Lua 5.1, 5.2, 5.3, 5.4,
  and 5.5.
- Full Lua comparison with `--enforce`: zero mismatches, zero one-sided failures,
  and zero new, changed, or missing both-error ratchet entries for every version.
- `./scripts/build/quick.sh`: passed with zero warnings and errors.
- `./scripts/test/quick.sh`: 15,248 passed; zero failed; zero skipped.
- Repository formatting, `git diff --check`, PLAN hygiene, and skill-index checks
  passed.
- `bash ./scripts/dev/pre-commit.sh`: passed on the final staged aggregate;
  Docker-backed devcontainer verification was conditionally skipped because
  Docker is unavailable.

The zero-knowledge verifier approved the first candidate. Adversarial review then
found that the public compatibility shims lacked direct C# coverage; the added
five-version null-contract regression closed that gap. Independent adjudication
tightened the two null-tolerant assertions to require exact `SetHook` nil and
`GetHook` `(nil, "", 0)` results, then approved all 607 focused cases. Final
adversarial review found and closed a nil-versus-void/type false-positive in
those assertions; a fresh exact-contract reviewer then returned `APPROVE` with
zero findings.

## Pull request and hosted verification

- Opened PR #117, `Migrate debug callbacks to argument views`, from
  `dev/wallstop/a5-debug-callback-views` at implementation commit `5e969204`.
- CSharpier run `33264372687` passed its format-check job.
- Benchmarks run `33264372685` passed all 20 jobs; the aggregate report recorded
  zero Phase A0 gate failures.
- Tests run `33264372650` passed 21 jobs with the expected conditional
  `lint-autofix` skip: lint, coverage, .NET tests on Linux, macOS, and Windows,
  all 15 Lua comparison lanes, and the aggregate Lua report succeeded.
- Hosted coverage was 84.60% line, 81.60% branch, and 88.00% method; every Lua
  5.1–5.5 comparison lane on all three operating systems reported zero
  unexpected deltas.
- GitHub reported the PR mergeable. Cursor Bugbot found no actionable issue,
  no inline review threads were open, and Copilot's quota notification contained
  no code finding to address.

That closure-receipt commit was documentation-only and triggered the same hosted
PR workflows before session handoff.

The first receipt-head rerun then exposed a Windows-only test-isolation failure:
`WarnWritesToConsoleWhenNoHandlerOrConfiguredStderr(Lua54)` captured a concurrent
`UnityAssetsScriptLoader` initialization diagnostic. TUnit runs tests in parallel
by default, while this assertion temporarily redirects the process-wide console;
the console-capture semaphore only coordinates other capture helpers. The test
now uses keyless `NotInParallel`, making its exact fallback-console assertion run
alone instead of weakening the expected output. A forced full rebuild passed
with zero warnings or errors, and all 15,248 tests passed with the new attribute
compiled. An independent investigator approved the root cause and minimal fix;
the corrected head receives a fresh hosted verification cycle.
