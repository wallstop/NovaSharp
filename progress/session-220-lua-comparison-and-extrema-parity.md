# Less-than local reads and Lua 5.3+ extrema parity

The delivery starts from `f2cb3a1b`. GitHub listed 58 open issues across three
pages and no open pull requests, including drafts and dependency updates. The
local checkout had only stat-dirty fixture files from an earlier corpus run;
tracked content matched `main`.

## Issue triage

The slice groups comparison-time reads with the extrema implementation that
depends on the same ordering path. Each issue reproduced from its reported
revision before the change.

- [#191](https://github.com/Ambiguous-Interactive/NovaSharp/issues/191)
  Lua reads a direct local from its register after the right operand runs, so
  `value < update()` observes the value written by `update`. NovaSharp pushed
  a snapshot before the call and returned `true` for both `<` and `<=` in all
  five profiles. The compiler now emits the right operand first, reloads the
  direct local, and swaps the comparison operands. Parenthesized locals are
  covered by the same unwind. Computed operands, globals, upvalues, and indexed
  operands keep their earlier snapshot. The AST evaluator follows the same
  order.
- [#178](https://github.com/Ambiguous-Interactive/NovaSharp/issues/178)
  Lua 5.3+ `math.min` and `math.max` compare operands with relational semantics
  and return the selected original value. NovaSharp validated every argument
  as a number and synthesized the result, losing subtype, precision, string
  ordering, and table identity. The modern profiles now select the first
  operand on ties and lone arguments, preserve mixed integer/float values and
  signed bounds, compare strings as strings, and follow `__lt` operand order.
  Lua 5.1 and 5.2 keep their numeric validation and coercion.
- [#206](https://github.com/Ambiguous-Interactive/NovaSharp/issues/206)
  The direct comparison and default sort paths already used raw `__name`
  labels. The extrema path discarded them with the numeric conversion; the
  shared ordering diagnostic now supplies `Actor`, equal-name, non-string-name,
  unnamed, and locked-metatable results for `math.min` and `math.max` as well.

## Red-to-green observations

Reference Lua 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0 produced the expected
output on the paired fixtures. Before the change, NovaSharp printed `true`
twice for the less-than probe, returned `9007199254740992` as an integer for
`math.max(9007199254740992.0, 9007199254740993)`, coerced `math.min('10','2')`
to the number `2` in 5.3+, and reported `bad argument #2 to 'max' (number
expected, got table)` instead of `attempt to compare number with Actor`.

The fixtures cover live and snapshotted operands, hook argument order, error
precedence, direct-local dispatch counts, mixed numeric types, signed bounds,
ties, lone operands, strings, comparable tables, signed zero, NaN, and the
`__name` diagnostic variants. A bytecode test confirms the common
local-to-local comparison keeps its existing dispatch count while a call on
the right adds the reload swap.

## Validation

- Focused suites: 299 binary-operator, 679 math-module, 20 ordering, 240
  table-module, five stdin-number, and 17 less-than cases passed.
- The full quick suite passed all 16,031 tests. The interpreter build reported
  zero warnings and errors. CSharpier passed all 846 files.
- Fixture metadata and error-ratchet tests passed. Both new fixtures passed
  the five reference interpreters before the paired TUnit runs.
- The TestMore stdin suite now pins Lua 5.1 because it asserts pre-5.3 float
  formatting for an integral `math.max` result; its five parameterized cases
  also exercise the actual requested profile instead of the default one.

## Blocker

This container has no GitHub credentials: `gh` is unauthenticated, no
`GH_TOKEN`/`GITHUB_TOKEN` is present, and the `origin` SSH remote rejects the
key. The change is verified locally but cannot be pushed, opened as a PR, or
merged from this session. The work is committed on `fix/session-220-lua-parity`
so a credentialed session can push it unchanged.
