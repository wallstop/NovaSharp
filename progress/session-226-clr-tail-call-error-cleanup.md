# Session 226 — CLR tail-call error-handler cleanup

Issue: [#218](https://github.com/Ambiguous-Interactive/NovaSharp/issues/218).
Pull request: [#233](https://github.com/Ambiguous-Interactive/NovaSharp/pull/233).

## Carry-forward inventory

- Open pull requests: none (`gh pr list --state open` returned `[]`).
- Main was green at `d9ef40af`: Tests, Benchmarks, CSharpier, and
  `pages-build-deployment` completed successfully.
- Open issues: 50 on page 1, 4 on page 2, and 0 on page 3 of the paginated
  REST inventory, so pagination is exhausted with 54 issues. Priorities were
  ranked by gameplay impact: Lua runtime/coroutine correctness first, then
  CLR binding call paths, then tooling/documentation.
- Selected [#218](https://github.com/Ambiguous-Interactive/NovaSharp/issues/218)
  because a failed CLR tail target could leave its function and arguments on
  the VM value stack, corrupting the next Lua call. The other high-impact
  candidates remain tracked: [#231](https://github.com/Ambiguous-Interactive/NovaSharp/issues/231),
  [#229](https://github.com/Ambiguous-Interactive/NovaSharp/issues/229),
  [#227](https://github.com/Ambiguous-Interactive/NovaSharp/issues/227), and
  [#217](https://github.com/Ambiguous-Interactive/NovaSharp/issues/217).

## Runtime change

CLR callback frames keep their function and inputs on the value stack until the
callback returns normally. The error-handler path resumed at the call site
without consuming those slots, so the failed target and its arguments leaked
under the handler result. The next call then treated the stale target as its
callee (or consumed the wrong value).

- Record the argument count on CLR frames and consume `function + arguments`
  when an error handler resumes at the call site.
- Track whether the normal-return path already consumed those inputs. Forced
  yields are rejected by `ValidateCallYield` after that cleanup, and unwinding
  must not remove the slots a second time.

## Evidence

- Red on `main`: the host tail-target fixture threw `tail target failed` for
  both a direct `bridge()` call and an `object + object` metamethod call.
- With only the input cleanup, the existing
  `UnaryMetamethodTUnitTests.CallbackCannotForwardChildAutoYield` cases failed
  with `attempt to call a number value`, proving the forced-yield path needed
  the consumed-input guard.
- Green after the fix: the fixture covers a thrown CLR tail target, a recovered
  result, a zero-result recovery, a target called with forwarded arguments, a
  callable-table `__call` target, an operator metamethod tail call, and caller
  arguments surrounding the protected call. The handler ran once per failed
  target and the next call saw exactly the expected values.
- New fixture:
  `LuaFixtures/TailCallTUnitTests/ClrTailTargetErrorHandlerCleansUpCallInputs.lua`
  (NovaSharp-only because it uses injected host callbacks) with the paired
  `TailCallTUnitTests.ClrTailTargetErrorHandlerCleansUpCallInputs` test across
  all five Lua profiles.
- Focused suites: `TailCallTUnitTests` 117/117, `UnaryMetamethodTUnitTests`
  23/23, `ErrorHandlingModuleTUnitTests` 183/183, `CoroutineModuleTUnitTests`
  348/348, and `ProcessorCoroutineCloseTUnitTests` 106/106.
- Full TUnit suite: 16245/16245.
- Corpus extraction reports 2104 snippets and leaves the manifest unchanged;
  the new host fixture is intentionally NovaSharp-only and is exercised by the
  TUnit host test.
- Lua comparison: Lua 5.1, 5.2, 5.3, and 5.5 report zero unexpected mismatches
  and no ratchet additions. The host system Lua 5.4.4 shows only the two
  pre-existing 5.4.4-vs-5.4.7 deltas recorded in session 224; a locally built
  Lua 5.4.7 reports zero mismatches, matching CI's pinned reference.
- Adversarial review confirmed the #218 fix and found one pre-existing
  reentrancy bug: a protected CLR tail target that calls back into Lua and fails
  can resume the outer frame inside the nested `ProcessingLoop`, after which the
  outer normal-return cleanup removes the same inputs again. Reproduced the
  `IndexOutOfRangeException` on this revision with a two-frame Lua callee and
  filed [#232](https://github.com/Ambiguous-Interactive/NovaSharp/issues/232)
  with the exact host reproduction and acceptance criteria.
