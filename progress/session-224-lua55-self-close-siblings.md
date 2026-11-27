# Session 224 — drain sibling closers when Lua 5.5 self-closes

Issue: [#226](https://github.com/Ambiguous-Interactive/NovaSharp/issues/226).

## Carry-forward inventory

- Open pull requests: none (`gh pr list --state open` returned `[]`), so there was no
  draft or in-progress PR to resume.
- Main was green at `7a0d96a3`: Tests, Benchmarks, CSharpier, and
  `pages-build-deployment` all completed successfully. Local `main` matches
  `github/main`; the stale `origin` remote was not used for delivery.
- Open issues: 54 after this session's follow-up, inventoried through page 2
  (`per_page=100`); page 2 returned zero entries, so pagination is exhausted even
  though page 1 is not a multiple of 50.
- Highest-impact remaining gameplay parity work is unchanged: assignment evaluation
  order/value preservation ([#194](https://github.com/Ambiguous-Interactive/NovaSharp/issues/194)),
  yieldable close handlers ([#169](https://github.com/Ambiguous-Interactive/NovaSharp/issues/169)),
  plain integer sort throughput ([#208](https://github.com/Ambiguous-Interactive/NovaSharp/issues/208)),
  and the string-byte/ordering backlog ([#201](https://github.com/Ambiguous-Interactive/NovaSharp/issues/201),
  [#199](https://github.com/Ambiguous-Interactive/NovaSharp/issues/199)).

## Runtime change

`CloseCurrentBlock` and `CloseAllPendingBlocks` now hand their detached closer list to
one helper. If a Lua 5.5 closer re-enters `coroutine.close` and aborts the handler, the
helper drains the siblings that were already detached with that block before the abort
leaves the frame. The active close-block frame is kept alive until the siblings finish;
the self-close unwind no longer returns it to the call-stack pool while its local
slots are still in use.

The self-close also resets the error passed to the aborted closers, matching Lua 5.5.
The first self-close preserves a failure that happened before it, while a later
self-close during the sibling drain clears errors raised since the first one. A
sibling error that remains after the last self-close replaces the reset error. Lua
5.4 continues to report the running-coroutine error and to pass that error to the
remaining closers.

## Evidence

- Red on the base revision: the #226 reproduction under `-v 5.5` printed
  `true` and `outer-start,b`, dropping the `a` closer. Reference Lua 5.5.0 prints
  `true` and `outer-start,b,a`.
- Green after the fix: the same reproduction prints `outer-start,b,a`; Lua 5.4.4
  still prints `false <cannot close a running coroutine>` with the same
  `outer-start,b,a` log.
- Paired regression tests (Lua 5.4 and 5.5) cover the reported sibling drain, a
  prior-handler-error reset in the same block, a prior nested-block error that must
  still make the close report failure, a repeated self-close during the drain
  clearing a later error, a nested handler whose ancestor frame still owns detached
  closers, and a nested closer whose self-close must preserve an ancestor failure.
  The generated fixtures match PUC Lua byte-for-byte, and stale fixture source
  locations for this test class were refreshed.
- `ProcessorCoroutineCloseTUnitTests`: 92/92.
- `-c Coroutine` filter: 785/785. Full TUnit suite: 16217/16217.
- Lua comparison 5.1, 5.2, 5.3, 5.5: zero unexpected mismatches and no both-error
  ratchet additions. Lua 5.4 has only the two pre-existing 5.4.4-vs-5.4.7 reference
  deltas (`OperationsUseVersionedIndexingAndLength` and
  `GenericForClosingValueFollowsLexicalExitOrder`).
- `dotnet tool run csharpier check` on all changed C# files: passed.

## Follow-up

Opened [#227](https://github.com/Ambiguous-Interactive/NovaSharp/issues/227) for the
separate Lua 5.5 case where `coroutine.close` is called from ordinary coroutine
execution rather than from a close handler. Reference Lua 5.5 aborts the running
coroutine and later reports `true`; NovaSharp still reports the Lua 5.4
running-coroutine error. `PLAN.md` now records that gap beside #169.
