# Session 223 — defer coroutine close handlers after uncaught errors

Issue: [#174](https://github.com/Ambiguous-Interactive/NovaSharp/issues/174).

## GitHub interaction guidance

Inspected the sibling `DoxReloaded` and `IshoBoy` repositories. Their reusable
techniques were adopted in [.llm/workflows/github-interaction.md](../.llm/workflows/github-interaction.md):

- route remote reads and writes through the GitHub connector or MCP first, use
  local Git for local facts and transport, and keep `gh` as the fallback;
- inventory each object once, page to exhaustion, and filter a cached JSON
  result instead of repeating list calls;
- start one CI watch or read one final rollup instead of polling in a loop;
- read an object after an ambiguous write before retrying, and batch pushes so
  one push triggers one CI run;
- never print, export, or commit a token.

## Runtime change

Lua keeps a dead coroutine's pending `<close>` handlers until
`coroutine.close`, rather than running them while the uncaught error unwinds.
NovaSharp now preserves those frames at the coroutine entry point and runs them
at the close boundary with the original error object. A protected call in the
stack still unwinds immediately to its handler.

The same unwind helper now drains every pending frame when a suspended
coroutine is closed. If a handler raises, later handlers receive the replacement
error and close reports it. A repeated close reports success, matching reference
Lua, instead of replaying the earlier failure forever.

## Evidence

- Red on the base revision: the reproduction printed `resume false boom 2` and
  `close2 false boom`; reference Lua 5.4/5.5 print `resume false boom 0` and
  `close2 true nil`.
- Green TUnit: `ProcessorCoroutineCloseTUnitTests` 64/64; full suite
  `16169/16169`.
- Reference Lua 5.4 and 5.5 outputs match for the three new fixtures:
  `UncaughtErrorDefersCloseHandlersUntilClose`,
  `DeferredCloseHandlerErrorReplacesActiveErrorAndClosesOnce`, and
  `SuspendedCloseContinuesThroughOuterCloseHandlers`.
- Lua comparison 5.1, 5.2, 5.3, and 5.5: zero unexpected mismatches and no
  ratchet changes. Lua 5.4 has the two pre-existing 5.4.4-vs-5.4.7 reference
  deltas (`OperationsUseVersionedIndexingAndLength` and
  `GenericForClosingValueFollowsLexicalExitOrder`), unchanged by this session.
- `bash ./scripts/dev/pre-commit.sh`: passed.
