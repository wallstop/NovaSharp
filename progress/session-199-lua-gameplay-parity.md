# Session 199 — Lua gameplay parity

The session began at 04:14:29 UTC on November 25, 2026, with a hard stop at
07:14:29 UTC. GitHub identified `99007158` as the head of `main`. Tests,
Benchmarks, CSharpier, and Pages passed for that revision. GitHub listed zero
open PRs, including drafts and dependency updates. The latest session's
candidate had already merged in PR #202, and its local branch tree matched
`main`. The primary checkout had existing guidance edits, so this session used
a separate worktree from `99007158`.

## Issue triage

GitHub listed 54 unique open issues across three pages. In gameplay-impact
order the backlog was 168, 174, 169, 194, 201, 198, 200, 191, 199, 178, 175,
192, 193, 186, 185, 176, 130, 125, 124, 138, 195, 188, 122, 108, 95, 93, 92, 118,
119, 203, 197, 113, 114, 135, 94, 165, 154, 98, 99, 105, 87, 88, 104, 103, 106, 84,
102, 85, 121, 184, 183, 182, 163, and 157.

The selected work continued the table-library slice and resolved related
observable Lua profile differences. The higher-priority error propagation and
assignment changes remain queued separately in [PLAN.md](../PLAN.md).

## Changes and evidence

- Lua 5.1–5.3 use the rounded result of subtracting and re-adding the step
  as the running index of a floating numeric loop. The exact-base test
  failed on those three profiles and passed on 5.4–5.5. The same probe
  passed on all five profiles after the fix. The reference cases
  distinguish cancellation in the first two values, signed zero, and
  bounded entry with infinite or NaN steps.
  [#186](https://github.com/Ambiguous-Interactive/NovaSharp/issues/186)


- Lua 5.3–5.5 table-library operations now respect indexed reads and writes
  for proxy tables and eligible non-table values. Reference probes cover
  length-hook timing, nil writes, integer-width boundaries, overlap order,
  partial writes on failure, and observable changes during sort
  partitioning. The older profiles keep raw reads and writes.
  [#198](https://github.com/Ambiguous-Interactive/NovaSharp/issues/198)
- Thread metatables are shared by the main thread and coroutines in each
  script. The base rejected `debug.setmetatable` for every profile.
  Lua 5.1 returns true from the successful call. Later profiles return
  the target. The regression checks assignment, lookup, replacement,
  removal, and `table.insert` where the proxy is supported. Separate host
  coverage verifies isolation and the existing CLR descriptor dispatch.
  [#200](https://github.com/Ambiguous-Interactive/NovaSharp/issues/200)

## Deferred extrema and string ordering

The initial extrema candidate preserved the selected operand, including
integers above the exact double range and objects selected through `__lt`.
The hosted review showed a newly exposed ordering discrepancy for U+E000 and
U+10000. A shared byte comparator corrected that case, then a second review
showed a regression for `'é' < 'Ā'`. Reference Lua distinguishes the UTF-8
source literal `'é'` from `string.char(233)`. NovaSharp represents both as the
same value. The two comparisons need different results, so changing their
shared comparator cannot recover source provenance.

Both experimental changes were removed from the delivery. The two candidates
and their reproductions remain in the PR history. Byte identity needs to
survive source loading, concatenation, and the host boundary before the
ordering and extrema changes can ship.
[#178](https://github.com/Ambiguous-Interactive/NovaSharp/issues/178)
[#199](https://github.com/Ambiguous-Interactive/NovaSharp/issues/199)
[#201](https://github.com/Ambiguous-Interactive/NovaSharp/issues/201)

## Independent verification

The floating-loop review compared 26 bounded cases on each pinned Lua build.
The base differed on seven cases per affected profile. The fixed results
matched on all five, including signed zeros, subnormals, infinities, and NaNs.

Independent table verification matched all 13 paired fixture/profile
cases, including native file access and the bounded Lua 5.2 remove check.
In-place sort matched 90 additional normal, write-error, and
invalid-comparator traces per modern profile.

## Follow-up issues

Two VM behavior gaps reproduced on the base with boolean operands. They also
apply to thread operands once their type metatable is available.
[#204](https://github.com/Ambiguous-Interactive/NovaSharp/issues/204)
[#205](https://github.com/Ambiguous-Interactive/NovaSharp/issues/205)

Independent probes also reproduced custom comparison type labels and the
raw sort callback schedule on Lua 5.1–5.2.
[#206](https://github.com/Ambiguous-Interactive/NovaSharp/issues/206)
[#207](https://github.com/Ambiguous-Interactive/NovaSharp/issues/207)

Three interleaved Lua 5.4 CLI runs on one CPU sorted 256 plain integer tables
of 2,048 entries. Construction was outside the timed region and every run
checked ascending order. The median was 0.800 seconds on the base and 1.085
seconds on the candidate. The throughput follow-up includes the workload.
[#208](https://github.com/Ambiguous-Interactive/NovaSharp/issues/208)
