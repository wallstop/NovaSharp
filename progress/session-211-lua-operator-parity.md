# Session 211 — Lua operator parity

The session started at 05:53 UTC on November 25, 2026, with an 08:53 UTC hard
stop. GitHub main was `561a9892`. Its applicable checks passed, including Tests, CSharpier, Benchmarks, and
Pages. GitHub marked conditional report and autofix jobs skipped. GitHub listed no open PRs, including drafts and dependency
updates. The previous runtime work landed in PR #209. The original checkout
contained guidance edits, including the draft already tracked in #163, so this
session used a clean worktree based on current main.

## Issue triage

The issues API returned 25, 25, and 7 open issues and no further page. The
initial 57 were prioritized in this order by gameplay and delivery impact.

- Runtime semantics and compatibility — 168, 174, 169, 194, 201, 199, 178,
  204, 205, 206, 207, 175, 192, 193, 185, 176, 130, 125, 124, 138, 195, 188,
  191, 122.
- Runtime performance and host features — 208, 108, 95, 93, 92, 118, 203,
  105, 87, 88, 104, 103.
- Validation, tooling, and infrastructure — 210, 197, 113, 114, 135, 94,
  119, 165, 154, 98, 99, 84, 102, 106, 184, 183, 182.
- Project upkeep — 85, 121, 163, 157.

The slice covers VM operator dispatch and diagnostics. The larger error-value
and parallel-assignment changes remain queued in [PLAN.md](../PLAN.md).

## Red-to-green observations

- Lua 5.1 table length uses the raw length even with `__len`. Other Lua 5.1
  length hooks receive the operand and nil. From Lua 5.2, both arguments are
  the operand. Unary minus and bitwise NOT duplicate the operand in the
  corresponding profiles. The exact base failed the 13 original fixture and
  profile combinations by passing one argument or calling Lua 5.1 table
  hooks. Follow-up probes exercised no-result operands, direct hook yields,
  and single-result resumption. The string fast path and Lua 5.1 raw table
  length have explicit controls.
  [#204](https://github.com/Ambiguous-Interactive/NovaSharp/issues/204)

- Lua 5.1 ordering rejects mixed Lua operand types before invoking Lua hooks
  and requires matching raw hook values for operands of the same type.
  The base let a boolean hook handle a number and selected one-sided table
  hooks. Tests also cover operand evaluation order, nil and false results,
  callable hook identity, reversed fallback, direct coroutine yield,
  built-in comparisons, and preserved CLR descriptor dispatch.
  [#205](https://github.com/Ambiguous-Interactive/NovaSharp/issues/205)

- Failed comparisons and default table sort use raw string `__name` from
  the metatable on Lua 5.3–5.5. The base used `table` in both diagnostics.
  Tests cover operand order, equal labels, non-string and inherited
  metafields, NUL termination, native file labels, and successful hooks.
  The extrema part remains queued. Implementation of operand-preserving
  `math.min` and `math.max` is tracked in #178 and depends on retaining Lua
  string byte identity.
  [#206](https://github.com/Ambiguous-Interactive/NovaSharp/issues/206)
  [#178](https://github.com/Ambiguous-Interactive/NovaSharp/issues/178)
  [PUC Lua comparison diagnostics](https://www.lua.org/source/5.4/ldebug.c.html#luaG_ordererror)

- Lua 5.4–5.5 bitwise operators give strings to applicable hooks and report
  type errors when no hook applies. The base used the Lua 5.3 coercion policy
  in modern profiles and bypassed those hooks. Lua 5.3 retains exact numeral
  conversion, including hex and 64-bit integer strings. The fixture covers
  one unary and five binary operators, operand sides, directly loaded chunks,
  integer-representation errors, nil operands, and coroutine result arity.
  [#211](https://github.com/Ambiguous-Interactive/NovaSharp/issues/211)

## Follow-ups

The reference probes reproduced separate pre-existing gaps in native file
metatable replacement, Lua 5.1 yields that cross a deeper Lua hook frame,
shared native and Lua function metatables, and ordinary hook return
cardinality. These are tracked with standalone reproductions.
[#212](https://github.com/Ambiguous-Interactive/NovaSharp/issues/212)
[#213](https://github.com/Ambiguous-Interactive/NovaSharp/issues/213)
[#214](https://github.com/Ambiguous-Interactive/NovaSharp/issues/214)
[#215](https://github.com/Ambiguous-Interactive/NovaSharp/issues/215)

## Validation and reflection

The focused quick runs passed 88 metamethod, 20 ordering, 45 bitwise, five
CLR comparison, and 40 TestMore cases. The quick interpreter build reported
zero warnings and errors. The full quick run passed 15,970 tests.

The independent verification pass replayed 43 new fixture/profile cells,
34 existing controls, and 2,369 synthetic cases. Adversarial review
exposed three conversion boundaries. Hexadecimal minimum shift counts reached
an overflowing negation before either shift helper checked bounds. Unicode
whitespace reached the shared parser's broader trimming rule. Numeric and
string forms of negative zero needed the operation conversion rather than the
conversion used to preserve the storage subtype. After the
fix, the reviewer replayed 5,950 numeral inputs, 300 shift operand/count
rows, and 14 protected immediate-yield scenarios without a mismatch.
The TAP fixture's file comparison labels also passed all five stock
interpreters.

The 6,175 fixture/profile comparisons passed enforced comparison using
PUC Lua 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0. The only initial ratchet
differences were two Lua 5.1 file-load diagnostics that included the
arbitrary worktree path. Both the exact base and candidate emitted that
path. The final comparison reran those two candidate files under a
recognized working directory and used their actual outputs with the
unmodified ratchet. The worktree-root normalization defect remains tracked.
[#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165)

An interleaved, CPU-affined smoke check used the same Lua 5.4 Release CLI
workload on both revisions. The loop ran three million iterations of
integer XOR and AND and checked the same result in each run. After two
warmups per revision, the base and candidate median wall times were
1.919 and 2.026 seconds over five samples. The suite and corpus ran
concurrently, and the timing variation exceeded the median difference.

The discovered cases are captured in fixtures or in the linked issues.
The existing guidance already covers cross-version before-and-after
probes and independent falsification.
