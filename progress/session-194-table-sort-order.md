# Session 194 — Exact numeric ordering in table.sort

Default `table.sort` now distinguishes adjacent 64-bit integers and mixed
integer and float operands in Lua 5.3–5.5. Numeric pairs bypass `__lt` in
all profiles. The Lua 5.1 and 5.2 path compares doubles, including when
the host injects integer values. Custom comparators and public
`LuaNumber.CompareTo` retain their existing contracts.

Main at `b1a05d3c` passed Tests, CSharpier, Benchmarks, and Pages deployment.
There were no open draft, regular, or dependency PRs. The residual local
guidance draft is tracked in [#163](https://github.com/Ambiguous-Interactive/NovaSharp/issues/163).

## Evidence

- On base `b1a05d3c`, the wide sort regression failed in 5.3–5.5 at
  the first adjacent-integer pair. On the same base CLI, the number
  metatable reversed a numeric sort in all five profiles. Both fixtures
  passed on their corresponding PUC releases (5.1.5, 5.2.4, 5.3.6, 5.4.4,
  and 5.5.0).
- The independent matrix runs both operand orders and checks integer
  bounds, mixed ties, fractions, infinities, signed zero, and explicit
  comparators. Each modern reference passed 992 checks. Each early
  reference passed 156. The base NovaSharp missed ten checks per modern
  profile and passed the early controls.
- PUC Lua compares built-in numeric operands before considering
  metamethods. In mixed cases it avoids converting integers that would
  lose precision. The new fixtures preserve operand subtypes and
  characterize repeat sorting and an explicit comparator.
  [Lua 5.4 VM comparison](https://www.lua.org/source/5.4/lvm.c.html#LTnum)
- An independent arithmetic probe checked 800,198 directed mixed pairs
  against exact rational ordering with seed `20261124`. The comparer
  stayed within the signed conversion range and matched the oracle.
  The numeric helper uses value arithmetic supported by the interpreter's
  .NET Standard target.

- The focused checks passed all 15 numeric sort cases and 241 TableModule
  cases. The quick build and full quick run passed, with 15,804 tests.
  The 5.4.4 system Lua failed the pre-existing generic-for closing
  fixture. The hosted workflow pins 5.4.7, which passes that fixture.
- The independent large-list probe checked 3,687 distinct numbers against
  an exact rational oracle in the modern profiles, including their Lua
  subtypes and binary representations.

## Issue triage

The paginated inventory returned 40 open issues. Gameplay and Lua caller
priority was 168, 174, 169, 175, 172, 162, 167, 176, 130, 125, 159, 138,
124, and 122. Runtime priority was 108, 95, 93, 92, 118, 105, 87, 88, 104,
and 103. Tooling and maintenance priority was 113, 114, 135, 119, 94, 165,
154, 98, 99, 106, 84, 102, 85, 121, 163, and 157. The execution queue
selected [#167](https://github.com/Ambiguous-Interactive/NovaSharp/issues/167)
as a bounded slice with a direct reference comparison.

The remaining numeric comparison paths are tracked in
[#177](https://github.com/Ambiguous-Interactive/NovaSharp/issues/177) and
[#178](https://github.com/Ambiguous-Interactive/NovaSharp/issues/178).
String sort dispatch and sort result count are tracked in
[#179](https://github.com/Ambiguous-Interactive/NovaSharp/issues/179).
