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
- Close-handler error state is split: `PendingError` preserves the error that
  was already unwinding when the coroutine aborted (Lua 5.5 reports it from
  resume even when remaining handlers succeed), while `CloseError` tracks the
  latest handler failure reported by a later `coroutine.close`.
- `CloseValueAndTrackError` rethrows the tagged abort so the remaining siblings
  in the active block and outer frames are drained in stack order exactly once.
- Before the abort throws, handlers belonging to the requesting handler's own
  frame are drained first (`_closeBlockFrameDepth`), matching Lua's top-down
  close order. A failure there becomes the error seen by outer handlers and the
  later close result, but the pending resume error still wins. A nested
  self-close during that drain resets the error and keeps closing remaining
  blocks, matching Lua's repeat-until-clean close loop.
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
  self-close discards the error raised while draining), and
  `RunningCoroutineSelfCloseHandlerFrameErrorReachesOuterHandlersInLua55`
  (drained-frame error propagation to outer handlers).
- Lua 5.4 counterpart: `CloseRunningCoroutineThrows` now runs only under 5.4
  and asserts the rejected self-close plus the deferred final handler.
  `NormalCoroutineCloseIsRejected` covers 5.4/5.5.
- Focused suites: `ProcessorCoroutineCloseTUnitTests` 130/130,
  corpus classes containing `Coroutine` 838/838, `TailCall` 127/127.
- Corpus extraction reports 2113 registered snippets; the hand-authored
  fixtures are intentionally outside the manifest. The stale auto-generated
  `CloseRunningCoroutineThrows.lua` entry (which inferred 5.1) was removed.
- Lua comparison for the changed fixtures: 10/10 matches on 5.5 and 2/2 on 5.4,
  with no mismatches, Lua-only, Nova-only, or unexpected error entries.
- Full 5.5 corpus through the CI batch runner: 1167 matches, 0 mismatches,
  314 unchanged both-error entries, 1 removed, 0 new/changed/missing.
- Full TUnit suite: 16307/16307.
