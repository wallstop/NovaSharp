# Session 192 — Lua table insertion arity

The session advanced [#160](https://github.com/Ambiguous-Interactive/NovaSharp/issues/160).
Both forms of Lua `table.insert` return zero values. The normalized argument
count selects append or positional insertion, so explicit nil and final
expressions yielding zero or multiple values retain their Lua semantics.
Wrong arity fails without mutation. Lua 5.1 uses the raw length and allows
positions outside the sequence. Later profiles validate the position after
evaluating length. The Lua 5.1 negative shift visits live entries when the
span of non-positive keys exceeds the table's entry count.

The initial main revision was `1b5212dd`. Tests, CSharpier, Benchmarks, and the
Pages deployment were green. The table change used main at `b63cded4`, after
the green PyYAML, ReportGenerator, and markdown-it-py pull requests
[#152](https://github.com/Ambiguous-Interactive/NovaSharp/pull/152),
[#149](https://github.com/Ambiguous-Interactive/NovaSharp/pull/149), and
[#147](https://github.com/Ambiguous-Interactive/NovaSharp/pull/147) merged.

## Evidence

- `./scripts/test/quick.sh InsertUsesArgumentCountAndReturnsNoValues` failed
  all five profile cases on the baseline at the append result-count assertion.
  The same assertion passed on PUC Lua 5.1.5, 5.2.4, 5.3.6, 5.4.4, and 5.5.0.
- The [Lua 5.1 implementation](https://www.lua.org/source/5.1/ltablib.c.html)
  and [Lua 5.2 implementation](https://www.lua.org/source/5.2/ltablib.c.html)
  established where bounds enforcement starts. The independent comparison
  identified Lua 5.1 truncation toward zero for negative fractions and the
  profile difference in string-to-position conversion at embedded NUL.
- The paired regression covers argument expansion, explicit nil, wrong arity
  without mutation, length evaluation order, numeric string coercion,
  positional bounds, and overlapping sparse Lua 5.1 shifts. Regenerating the
  corpus adds one comparable snippet to every profile and expands the existing
  length fixture to all five profiles.
- The quick build, 221 focused TableModule tests, 15,726 full quick tests,
  CSharpier, pre-commit, and enforced full corpus comparisons with PUC Lua 5.1
  through 5.5 passed. The pre-commit Docker-only devcontainer check and Unity
  player execution were unavailable. The existing callback registration and
  preservation attribute cover the implementation entry point.

## Issue triage

The initial paginated inventory contained 31 open issues. The gameplay and Lua
caller order was 160, 130, 125, 159, 138, 124, and 122. The runtime order was
108, 95, 93, 92, 118, 105, 87, 88, 104, and 103. The tooling and maintenance
order was 113, 114, 135, 119, 94, 154, 98, 99, 106, 84, 102, 85, 121, and 157.

A matched-reference probe also reproduced uninitialized generic-loop locals
reading iterator state on the base. It is now first in the execution queue
[#164](https://github.com/Ambiguous-Interactive/NovaSharp/issues/164).
Table insertion's length and indexing metamethod differences are tracked in
[#162](https://github.com/Ambiguous-Interactive/NovaSharp/issues/162).
The remaining local session 189 guidance is tracked in
[#163](https://github.com/Ambiguous-Interactive/NovaSharp/issues/163).
The Lua error ratchet depends on the checkout directory for two file-loading
fixtures. The comparison succeeded when launched from the primary checkout.
The normalization fix is tracked in
[#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165).
