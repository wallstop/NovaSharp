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

Review hardening added the remaining boundary rules:

- defer only when the error reaches the coroutine resume boundary, while
  protecting local unwind across direct CLR `pcall`/`xpcall` boundaries;
- close a failed `coroutine.wrap` result and propagate a replacement handler
  error;
- reject reentrant `coroutine.resume` while handlers run, and reject reentrant
  `coroutine.close` in Lua 5.4 while reproducing Lua 5.5's silent handler abort;
- keep the original deferred error when a later resume is rejected;
- report `running` and non-yieldable status inside close handlers.

## Evidence

- Red on the base revision: the reproduction printed `resume false boom 2` and
  `close2 false boom`; reference Lua 5.4/5.5 print `resume false boom 0` and
  `close2 true nil`.
- Green TUnit: `ProcessorCoroutineCloseTUnitTests` 74/74,
  `CoroutineModuleTUnitTests` 348/348, full suite `16192/16192`.
- Reference Lua 5.4 and 5.5 outputs match for the new deferred-close,
  suspended-close, reentrant-close, self-close, self-resume, status, wrap,
  protected CLR callback, and unprotected CLR callback fixtures.
- Lua comparison 5.1, 5.2, 5.3, and 5.5: zero unexpected mismatches and no
  ratchet changes. Lua 5.4 has the two pre-existing 5.4.4-vs-5.4.7 reference
  deltas (`OperationsUseVersionedIndexingAndLength` and
  `GenericForClosingValueFollowsLexicalExitOrder`), unchanged by this session.
- `bash ./scripts/dev/pre-commit.sh`: passed.
