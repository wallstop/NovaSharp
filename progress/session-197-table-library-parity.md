# Session 197 — Lua table library parity

The session started at 00:25:38 UTC on November 25, 2026, with a hard stop at
03:25:38 UTC. The delivery branch started at `130a0632`. The Tests, Benchmarks,
CSharpier, and Pages workflows had passed on that revision of `main`. GitHub
showed no open pull requests, drafts, or dependency updates. The original
checkout contained earlier local guidance edits, so work used a fresh worktree
from the verified remote revision. The broader guidance reconciliation remains
tracked in [#163](https://github.com/Ambiguous-Interactive/NovaSharp/issues/163).

## Issue triage

GitHub REST returned 52 open issues across two pages. The priority order was
168, 174, 169, 194, 176, 191, 162, 179, 178, 175, 192, 193, 186, 185, 130, 125,
124, 159, 138, 195, 188, 122, 108, 95, 93, 92, 118, 119, 197, 113, 114, 135,
94, 165, 154, 98, 99, 105, 87, 88, 104, 103, 106, 84, 102, 85, 121, 184, 183,
182, 163, and 157. The three table-library issues share one delivery slice.
The higher-priority error propagation and compiler changes remain queued in
[PLAN.md](../PLAN.md).

## Changes and evidence

- The table library registers its reference names and profile-specific
  exports, including Lua 5.1 iteration helpers, Lua 5.2 compatibility
  `unpack`, and Lua 5.5 `table.create`. The export fixture recognizes both
  pinned Lua 5.2 build configurations. NovaSharp uses the standard upstream
  make configuration with `LUA_COMPAT_ALL`. The pinned Windows build omits
  both optional exports. The base failed the export fixture in every profile.
  [#159](https://github.com/Ambiguous-Interactive/NovaSharp/issues/159)
- Lua 5.3–5.5 insertion respects indexed reads and writes and accepts eligible
  proxies. The base skipped the hook calls and rejected the non-table proxy.
  The reference observes the length call, descending reads and writes, nil
  assignments, and the first value from a read hook. Lua 5.1 and 5.2 keep raw
  indexing. The raw shift retains its sparse-table path.
  [#162](https://github.com/Ambiguous-Interactive/NovaSharp/issues/162)
- Default sorting compares string pairs directly and every successful
  `table.sort` call returns zero Lua values. On all five profiles the base
  probe returned a table and invoked the string metatable six times. Reference
  Lua sorted ascending with zero hook calls. Independent verification also
  checked comparator validation order, rejected fractional and oversized
  lengths, and confirmed the same boundary behavior after the fix.
  [#179](https://github.com/Ambiguous-Interactive/NovaSharp/issues/179)

Remaining indexed operations, non-ASCII string ordering, missing thread
metatables, and string byte lengths have independent reproductions and issue
records.
[#198](https://github.com/Ambiguous-Interactive/NovaSharp/issues/198)
[#199](https://github.com/Ambiguous-Interactive/NovaSharp/issues/199)
[#200](https://github.com/Ambiguous-Interactive/NovaSharp/issues/200)
[#201](https://github.com/Ambiguous-Interactive/NovaSharp/issues/201)

## Local verification

- The final quick build passed with zero warnings. The focused table and
  instruction-limit suites passed 1,176 and 42 tests, respectively. The
  full quick suite passed all 15,893 tests.
- Corpus regeneration found 2,029 extracted snippets. The width and
  comparator fixtures use curated profiles so the extractor's lexical
  heuristics cannot narrow the intended coverage.
- The enforced Linux corpus passed 6,110 fixture and profile combinations
  against PUC Lua 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0. There were zero
  mismatches, missing outputs, one-sided errors, or new error signatures.
  The legacy unclassified sort error was reduced to a passing diagnostic
  assertion in each profile. A separate 5.2.4 build with compatibility
  exports disabled passed the export fixture.
- Independent verification and adversarial review covered the 2,000-hop
  indexed-access boundary, reentrant setters, protected CLR callbacks, sort
  result selection, and record-only growth of hinted tables. Regression
  tests keep these findings executable.

## Hosted review follow-up

Indexed reads, writes, and hook traversal now charge the active VM
instruction budget. The first PR revision completed a bounded indexed
insert under a 64-unit ceiling without invoking the budget callback. The
regression observes the structured violation, explicit host renewal,
cumulative work across calls, restored outer accounting after reentry, and
the protected-call result. Lua 5.1 and 5.2 retain the raw insert path.
Lua 5.1 `foreach` and `foreachi` charge their iteration work through the
same budget, with bounded regressions for both entry points.
A wall-clock watchdog is listed separately in the
[sandbox threat model](../docs/security/sandbox-threat-model.md).
