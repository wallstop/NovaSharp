# Session 225 — Lua parallel assignment order and review audit

Issue: [#194](https://github.com/Ambiguous-Interactive/NovaSharp/issues/194).

## Carry-forward inventory

- Open pull requests: none (`gh pr list --state open` returned `[]`).
- Main was green at `07c35604`: Tests, Benchmarks, CSharpier, and
  `pages-build-deployment` completed successfully.
- Open issues: 53 on page 1 and zero on page 2 of the paginated REST inventory,
  so pagination is exhausted even though the first page is close to 50.
- A thread-level audit covered the 25 pull requests merged since 2026-11-20.
  It found unresolved threads only on #222 (1), #224 (22), and #228 (15).
  Most #224 findings were superseded by the later #228 commits. The #222
  dead-coroutine double-close case and the #228 later-outer-sibling error case
  now have regressions and pass on main. Remaining candidate close-handling
  cases are tracked in [#229](https://github.com/Ambiguous-Interactive/NovaSharp/issues/229).
  The review audit also folded in the pending-rollup wording fix from #224 and
  removed the prohibited null operators from `CoroutineModule.Resume`.

## Runtime change

Parallel assignments now evaluate indexed targets left to right before the
right-hand side, then perform the stores right to left, matching PUC Lua
5.1–5.5. Target operands keep the reference compiler's read timing: direct
locals stay register-like, unconflicted local keys are read at store time,
Lua 5.1 upvalues are snapshotted, and Lua 5.4–5.5 snapshot dynamic upvalue-table
keys. A later direct assignment to a local or upvalue forces the reference
compiler's conflict copy.

Stores leave prepared target operands and pending right-hand values on the VM
stack. A table setter result now replaces its assigned slot instead of being
pushed at the top, so a discarded `__newindex` return cannot displace another
assignment value or lose a yieldable setter's pending results.

## Evidence

- Red on the base revision: `p[mark('i')], q[mark('j')] = mark('x'), mark('y')`
  logged `xyij` instead of `ijxy`; `proxy.x, kept = true, false` left `kept` as
  the setter's return instead of `false`; conflicting table/key aliases and
  duplicate local targets also diverged from PUC Lua.
- Green after the fix: all five reference interpreters and NovaSharp now agree
  on target order, right-to-left store order, setter return discard, snapshot
  conflicts, direct local/key reads, duplicate targets, and version-specific
  upvalue-table timing. A yieldable setter preserves all pending values across
  suspension on Lua 5.2–5.5.
- Added fixtures `AssignmentPreservesParallelTargetsAndValues` (all versions)
  and `AssignmentPreservesValuesAcrossYieldableSetter` (5.2+), plus paired
  coroutine-close regressions generated into the corpus.
- `AssignmentStatement` and `ProcessorCoroutineClose` filters: 24/24 and
  106/106. Full TUnit suite: 16240/16240.
- Lua comparison 5.1, 5.2, 5.3, and 5.5: zero unexpected mismatches and no
  ratchet additions. The only 5.4 mismatches are the two pre-existing
  5.4.4-vs-5.4.7 reference deltas recorded in session 224. Ten both-error
  entries were refreshed because the new assignment prologue shifted the
  fallback bytecode instruction pointer in a main-chunk yield diagnostic; the
  error classification is unchanged.
- CSharpier check passed on all changed C# files.
