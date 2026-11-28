# Session 228 — Lua 5.5 running-coroutine self-close

Issue: [#227](https://github.com/Ambiguous-Interactive/NovaSharp/issues/227).

## Carry-forward inventory

- Open pull requests: none (`gh pr list --state open` returned 0). No draft or
  in-progress PRs from prior sessions.
- `main` was green at `10cd3ae1`: Tests, Benchmarks, CSharpier, and
  `pages-build-deployment` completed successfully.
- Open issues: 50 on page 1, 4 on page 2, and 0 on page 3 of the paginated
  REST inventory (54 total), so pagination is exhausted. Ranked by gameplay
  impact: host-visible crashes and Lua-state corruption first, then
  coroutine/close semantics, then library/performance/tooling work.
- Selected [#227](https://github.com/Ambiguous-Interactive/NovaSharp/issues/227):
  `coroutine.close` on the currently running coroutine must abort under Lua
  5.5 instead of raising Lua 5.4's "cannot close a running coroutine" error.

## Root cause

`CoroutineModule.Close` routed every target through `Processor.CloseCoroutine`.
The only self-close path required `_isClosingCoroutine`, which is set while a
suspended coroutine's close handlers run from `coroutine.close` or deferred
cleanup. A call made from ordinary coroutine execution reached the generic
`CoroutineState.Running` guard and threw the Lua 5.4 error, so the coroutine
never aborted and resume reported `false`.

PUC Lua 5.5's `luaB_close` on `COS_RUN` calls `lua_closethread(co, L)` and then
`luaD_throwbaselevel`; `luaE_resetthread` drains all pending close handlers and
the throw lands at the resume boundary. If the drain raises, resume reports the
error, while a later `coroutine.close` reports the last close error (which can
be nil when the error object was already consumed).

## Runtime change

- `Processor.CloseCoroutine` now recognizes a Lua 5.5 self-close from ordinary
  execution (`caller == this`, `_state == Running`) and throws a
  `CoroutineSelfCloseException` tagged `AbortResume`.
- `ResumeCoroutine` catches that tag, drains every remaining frame through the
  existing `UnwindFramesForClose` path with `_isClosingCoroutine` set (so close
  handlers observe `coroutine.status == "running"` and cannot yield), then
  returns a void result so `coroutine.resume` yields exactly `true` and
  `coroutine.wrap` yields zero values.
- Close-handler error state is split: `PendingError` preserves the close-handler
  failure that was active when the coroutine aborted (Lua 5.5 reports it from
  resume even when remaining handlers succeed), while `CloseError` tracks the
  latest handler failure reported by a later `coroutine.close`. An ordinary
  execution error that is still unwinding is reset by the self-close, matching
  PUC Lua 5.5's `L->status` behavior.
- `CloseValueAndTrackError` rethrows the tagged abort so the remaining siblings
  in the active block and outer frames are drained in stack order exactly once.
- Before the abort throws, handlers belonging to the requesting handler's own
  frame are drained first (`_closeBlockFrameDepth`), matching Lua's top-down
  close order. A failure there becomes the error seen by outer handlers and the
  later close result, but the pending resume error still wins. A nested
  self-close during that drain resets the error and keeps closing remaining
  blocks, matching Lua's repeat-until-clean close loop.
- When error unwinding has already popped a frame, `CloseAllPendingBlocks`
  continues through that frame's remaining blocks before letting the abort
  escape, because the popped frame is no longer reachable from the execution
  stack. A later self-close in one of those blocks resets the carried failure.
- Closing a running ancestor coroutine (`caller != this`) now reports Lua's
  "cannot close a normal coroutine" instead of the running-coroutine error.

## Evidence

- PUC Lua 5.5.0 self-close reproduction: `resume1 true`, `status dead`,
  `close true`; both explicit close handlers run once in reverse declaration
  order and code after `coroutine.close` never runs. Lua 5.4.4 keeps
  `cannot close a running coroutine` and defers the remaining handler to the
  later close call.
- New 5.5 fixtures (`@lua-versions: 5.5`):
  `RunningCoroutineSelfCloseAbortsToResumeInLua55` (nested blocks, handler
  locals, direct `coroutine.close`, stack counts after the drain),
  `RunningCoroutineSelfCloseRunsGenericForCloserInLua55`,
  `RunningCoroutineSelfCloseHandlerErrorFailsResumeInLua55`,
  `RunningCoroutineSelfCloseAgainDuringDrainAbortsHandlerInLua55`,
  `RunningCoroutineSelfCloseKeepsPendingErrorInLua55` (error before and after
  the abort), `RunningCoroutineSelfCloseHandlerFrameErrorKeepsPendingErrorInLua55`
  (handler-local failure plus pending error),
  `RunningCoroutineSelfCloseAgainInHandlerDrainsLocalsInLua55` (nested
  self-close while draining the handler's own locals),
  `RunningCoroutineSelfCloseAgainDuringHandlerDrainResetsErrorInLua55` (a second
  self-close discards the error raised while draining),
  `RunningCoroutineSelfCloseAgainDuringHandlerDrainKeepsLaterFailureInLua55`
  (a failure raised after that reset still surfaces),
  `RunningCoroutineSelfCloseDuringUnwindRunsPoppedFrameBlocksInLua55` (every
  block of a popped frame closes before the abort escapes), and
  `RunningCoroutineSelfCloseDuringUnwindResetsPriorFailureInLua55` (a later
  self-close in the popped frame resets the carried failure),
  `RunningCoroutineSelfCloseKeepsFirstHandlerFailureInLua55` (the first handler
  failure wins over later ones), and
  `RunningCoroutineSelfCloseHandlerFrameErrorReachesOuterHandlersInLua55`
  (drained-frame error propagation to outer handlers).
- Host catch guard: `HostCaughtRunningSelfCloseStillAborts` (NovaSharp-only)
  injects a host callback that catches the internal abort exception and then
  verifies the coroutine still aborts with empty stacks.
- Lua 5.4 counterpart: `CloseRunningCoroutineThrows` now runs only under 5.4
  and asserts the rejected self-close plus the deferred final handler.
  `CloseWithoutArgumentRequiresCoroutineInLua54` keeps the 5.4 argument
  requirement. `NormalCoroutineCloseIsRejected` covers 5.4/5.5.
- Lua 5.5 accepts an omitted `coroutine.close` argument as the running
  coroutine (`RunningCoroutineCloseWithoutArgumentSelfClosesInLua55`);
  `CoroutineModule.Close` only applies that default under 5.5.
- Host catch guard: `Processor.Call`/`CallChunkCore` mark a pending abort only
  at host boundaries, `InternalExecCall` and `CompleteDirectClrCall` rethrow it
  after the host callback returns, and the marker is cleared before the rethrow
  so close-handler callbacks cannot re-fire it.
  `HostCaughtRunningSelfCloseStillAborts` injects a host callback that catches
  `CoroutineSelfCloseException`, then asserts the abort still completes with
  empty execution and value stacks.
- Focused suites: `ProcessorCoroutineCloseTUnitTests` 137/137,
  corpus classes containing `Coroutine` 838/838, `TailCall` 127/127.
- Corpus extraction reports 2113 registered snippets; the hand-authored
  fixtures are intentionally outside the manifest. The stale auto-generated
  `CloseRunningCoroutineThrows.lua` entry (which inferred 5.1) was removed.
- Lua comparison for the changed fixtures: 15/15 matches on 5.5 and 3/3 on
  5.4, with no mismatches, Lua-only, Nova-only, or unexpected error entries.
- Full 5.5 corpus through the CI batch runner: 1172 matches, 0 mismatches,
  314 unchanged both-error entries, 1 removed, 0 new/changed/missing.
- Full 5.4 corpus against Lua 5.4.7: 1122 matches, 0 mismatches with an
  unchanged both-error ratchet (1 removed, 0 new/changed/missing).

## Review follow-ups

- CodeRabbit found that the first self-close catch discarded a failure raised
  by remaining handlers; fixed by carrying `CloseError` out of
  `DrainHandlerFramesBeforeSelfClose`.
- CodeRabbit's `PendingError`/`activeError` suggestion was not applied: PUC
  Lua 5.5 resets an ordinary execution error that is still unwinding when a
  closer self-closes. Verified with direct and pcall-protected repros plus the
  cross-frame variant, and documented in the `PendingError` comment.
- CodeRabbit found that `CloseAllPendingBlocks` skipped outer blocks when error
  unwinding had already popped the frame; fixed by continuing through the
  remaining blocks and rethrowing the abort with later failures.
- The independent adversarial review found the same popped-frame gap, plus:
  a later self-close during the abort drain incorrectly left `failed=true`
  (fixed by resetting abort failures when no later error exists), and the
  latest rather than first close-handler failure was retained (fixed by
  tracking the first failure in the `CloseClosersFrom` pass).
- The remaining adversarial finding — host callbacks can catch the internal
  abort exception and leave nested frames on the execution stack — is fixed by
  marking pending aborts only at host boundaries and rethrowing them after the
  callback returns. `HostCaughtRunningSelfCloseStillAborts` covers the catch
  case and asserts both stacks are empty; a stale marker is cleared before the
  rethrow so close-handler callbacks cannot re-fire it.
- Full TUnit suite: 16314/16314.
