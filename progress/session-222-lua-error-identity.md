# Session 222 — Lua error-value identity through protected calls and unwinding

Issue: #168 (unblocks #169, #174).

## Changes

- `ScriptRuntimeException` now carries the originating Lua error object alongside the
  host-facing decorated message. `GetErrorObject()` falls back to the decorated string
  for runtime failures that have no explicit object, and caught errors normalize a nil
  object to Lua 5.5's `<no error object>` marker.
- `error` accepts any Lua value (not just strings). Lua 5.1/5.2 coerce numeric
  messages to strings only when level decoration is requested, preserving numbers for
  level 0 and below; Lua 5.3+ preserve numbers, booleans, tables, and nil. Source
  decoration follows reference Lua's level rules, including CLR callers contributing
  no location.
- `assert` shares the same error-construction path. Lua 5.1/5.2 keep the
  `luaL_optstring` contract; Lua 5.3+ preserve non-string messages and explicit nil.
- `pcall`/`xpcall` pass the raw error object to `xpcall` message handlers and return the
  handler's first result (or the original object) from protected calls. `__close` handlers
  receive the same object, and a closer that raises replaces the active error object.
- Coroutines track failure separately from the error value so a dead coroutine closed
  after `error(nil)` still reports `false, nil` under Lua 5.4.
- `coroutine.resume`, `coroutine.wrap`, and `coroutine.close` preserve the error object
  and now return the decorated location text for string failures instead of the bare
  message.
- Added `WatchItem.IsClrFunction` and `ScriptExecutionContext.CurrentCallback` so error
  level decoration can distinguish Lua callers from CLR callbacks.

## Evidence

- Red: `pcall(function() error({}) end)` returned a `string` with a bad-argument error,
  `__close` and coroutine paths received decorated strings, and `xpcall` handlers saw
  strings instead of the original objects.
- Green: paired TUnit coverage across Lua 5.1–5.5 in
  `PcallPreservesLuaErrorValues`, `XpcallPassesErrorObjectsToHandlerAndUsesItsResult`,
  `AssertPreservesLuaErrorValues`, `CloseMetamethodsReceiveLuaErrorObjects`,
  `ResumePreservesLuaErrorValues`, `CloseReturnsThePreservedLuaErrorValue`,
  `XpcallUsesOnlyTheFirstHandlerResult`, and `CloseReportsFailureForNilErrorValue`.
  Full TUnit suite: `16163/16163` passed.
- Fixtures: six generated `.lua` fixtures produce stable printed outcomes that match
  reference Lua 5.1–5.5 exactly (including `<no error object>` under 5.5).
- Lua comparison: zero unexpected mismatches for 5.1, 5.2, 5.3, and 5.5; the two
  local Lua 5.4.4 differences (`OperationsUseVersionedIndexingAndLength`,
  `GenericForClosingValueFollowsLexicalExitOrder`) are the pre-existing 5.4.4-vs-5.4.7
  reference deltas noted in session 221.
- Both-error ratchet: no new, changed, or missing entries. Five stale
  `SortThrowsWhenComparatorIsInvalidType` baseline entries were already passing on
  `main` before this session and remain harmless removals.
- Benchmark: keeping `coroutine.resume` on the no-context callback path restored
  CoroutinePingPong Execute allocations to `80.55 KB` (Phase A0 baseline `80.5 KB`)
  after a context-taking signature briefly added `12 KB/op`.
