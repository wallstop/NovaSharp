# Session 115 - Chained Call Fallback Pooling

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `5a7edc0a`
- PR: `#43`
- Starting worktree: clean and aligned with `origin/dev/wallstop/api-perf`.
- Prior PR state: CI passing on the latest head, merge state clean, Copilot unable to review because the PR exceeds the 20,000-line limit, and no active unresolved current review threads were known from the previous session.

## Goal

Remove the remaining fixed-call `__call` fallback argument arrays for chained non-direct callable values while preserving Lua call semantics and existing fixed overload behavior.

## Rationale

- Recent fixed overload work removed most per-call argument array allocations for direct Lua functions, direct CLR callbacks, and short chained `__call` paths.
- The fixed non-function fallback still builds small `DynValue[]` arrays when a chained callable value outgrows the inline fixed-argument buffer.
- The first practical uncovered boundary is six user arguments plus a two-hop callable table chain, which can occur through host API calls in Unity-facing hot loops.
- Pooling this fallback keeps the compatibility path correct without promoting a rare path into a new public API surface.

## Planned Scope

1. Add pooled fallback helpers for fixed non-function call overloads in `Script`.
2. Add the equivalent pooled fallback helpers in `ScriptExecutionContext`.
3. Replace the current small-array fallback sites for one through six fixed arguments.
4. Add focused allocation coverage for the six-user-argument chained `__call` boundary in both host `Script.Call` and `ScriptExecutionContext.Call`.
5. Keep existing behavior for argument expansion, self insertion, chained metamethod loop detection, and null/void adjustment.

## Review Checklist

- [x] Explorer review completed.
- [x] Implementation added.
- [x] Adversarial review completed.
- [x] Targeted tests run.
- [x] Build/broader checks run as appropriate.
- [ ] Commit, push, request Copilot review, and poll PR CI.

## Status

Local implementation and validation complete. Pending commit, push, Copilot review request, and PR CI observation.

## Implementation Notes

- Replaced the fixed non-function fallback array paths in `Script` with `CallChainedNonFunction`, which keeps chained `__call` arguments inline until the fixed buffer fills and then continues the same chain in a pooled `DynValue[]`.
- Added the equivalent pooled overflow helper in `ScriptExecutionContext`.
- Added `CopyTo` helpers on the fixed argument structs so the overflow path can seed the pooled buffer without re-entering the public `params` overloads.
- Added allocation probes for the six-user-argument, two-hop callable table boundary in both `Script.Call` and `ScriptExecutionContext.Call`.
- Tightened the overflow probes after review so the five-user-argument baseline validates the fixed view by index, while the six-user-argument overflow path must expose a contiguous span with exactly eight arguments: proxy self, target self, and the six user values.

## Review Notes

- Explorer review identified userdata indexer callback invocation as a separate high-value future slice: `DispatchingUserDataDescriptor.ExecuteIndexer` still builds `DynValue[]`/`CallbackArguments` for common non-tuple get/set paths. That was recorded as a follow-up and intentionally not mixed into this chained-call fallback change.
- Adversarial review found no blockers in the pooled overflow helper. It confirmed argument ordering, loop-budget decrementing, and pooled array lifetime were sound.
- The reviewer flagged that the initial six-argument tests accepted both seven and eight arguments. The tests now require exact overflow shape and `TryGetSpan` success on the pooled span path.

## Validation

- `./scripts/test/quick.sh --full FixedSixDynValueCallToChainedCallbackViewMetamethodAvoidsFallbackArgumentArrayAllocation` passed after tightening the probe.
- `./scripts/test/quick.sh --no-build FixedSixArgumentCallOverloadAvoidsChainedCallMetamethodFallbackArgumentArrayAllocation` passed.
- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests` passed: 581 total, 581 succeeded, 0 failed, 0 skipped.
- `./scripts/test/quick.sh --no-build -c ScriptExecutionContextTUnitTests` passed: 130 total, 130 succeeded, 0 failed, 0 skipped.
- `dotnet build src/NovaSharp.sln -c Release --no-restore` passed with 0 warnings and 0 errors.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14,470 total, 14,470 succeeded, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` completed successfully. It refreshed `docs/audits/documentation_audit.log` for line-number drift from the new helper summaries.
- `git diff --check` and `git diff --cached --check` passed.
- Lua comparison was not run because this slice changes host/API chained-call dispatch allocation behavior, not Lua language semantics.

## Remote Validation

- Pending.
