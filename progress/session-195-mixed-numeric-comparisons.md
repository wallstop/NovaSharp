# Session 195 — Exact mixed numeric comparisons

Lua 5.3–5.5 compare integers and floats without rounding away distinct values.
The same semantics cover bytecode, literal operands, dynamic expressions, and
`rawequal`. Lua 5.1 and 5.2 compare as doubles, including integers supplied by
the host. Their arithmetic uses double precision for internally stored integers,
and numeric keys in their script-owned tables follow the same equality rules. Signed zero keeps
its sign as a value and shares the hash of integer zero. The execution queue now
points to the remaining work on math extrema.
[#177](https://github.com/Ambiguous-Interactive/NovaSharp/issues/177)
[#178](https://github.com/Ambiguous-Interactive/NovaSharp/issues/178)

## Evidence

- The base revision `acd6857a` failed nine of 11 focused cases. Lua 5.3–5.5
  incorrectly compared 9,007,199,254,740,993 equal to 9,007,199,254,740,992.0.
  Dynamic expressions also ordered NaN in all five profiles and the host
  `LuaNumber` hash distinguished signed zeros.
- The reference comparison confirms exact ordering in both operand directions.
  NovaSharp uses the existing bounded integer-to-float comparator for integers
  outside the consecutive binary64 range. It keeps NaN unordered and distinguishes
  the exclusive upper bound at 2^63.
  [Lua 5.4 VM comparison](https://www.lua.org/source/5.4/lvm.c.html#LTnum)
- Independent review ran 600,110 mixed pairs against an exact rational oracle
  derived from IEEE significands and exponents, with seed `177535195`. The
  cases included machine limits, neighbor doubles, subnormals, signed zeros,
  infinities, NaNs, and hash equality.
- A pure Lua counterexample from independent review found that arithmetic
  involving `math.floor` or string length could create a nonrepresentable
  integer in Lua 5.1 and 5.2. The first candidate incorrectly exposed it in
  equality and ordering. The paired Lua fixture now exercises that path,
  subsequent arithmetic, table key aliases, and signed integer boundaries.
- Review also reproduced a float-loop case that exited early and dynamic
  negation that rounded a large integer before comparison. Lua fixtures cover
  integer and float `for` controls around 2^53. TUnit checks the same
  negated boundary in dynamic evaluation.
- A host can change the script profile at runtime. A table fixes numeric key
  equivalence at construction so a profile change keeps all stored keys
  accessible. TUnit exercises that invariant in both profile directions.
- A host bridge comparison showed that the PUC 5.1 and 5.2 C APIs round wide
  injected integers when pushing them onto the stack. The host-boundary test
  checks NovaSharp comparison and table key behavior in every profile.
  [Lua 5.1 C API](https://www.lua.org/source/5.1/lapi.c.html#lua_pushinteger)
  [Lua 5.2 C API](https://www.lua.org/source/5.2/lapi.c.html#lua_pushinteger)

- The quick build and all 15,820 quick tests passed. Focused checks passed 287
  binary expression, 60 unary expression, 220 numeric-loop, and 75 table
  storage tests. The corpus extractor ran and the enforced comparison passed
  for 6,051 fixture and profile combinations against Lua 5.1.5, 5.2.4, 5.3.6,
  5.4.7, and 5.5.0. The 5.1 runner used the standard GitHub workspace path
  for the existing error normalization limitation.
  [#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165)
- Independent verification matched 828 pairs per profile with seed `177`.
  Adversarial review also reran the exact arithmetic oracle against the
  rebuilt runtime and verified profile switches, dynamic negation, and
  floating numeric for loops.
- Four hosted Lua 5.1 and 5.2 jobs failed on one assertion. Their artifacts
  identified the large modulo probe. Official Lua 5.1.5 and 5.2.4 built
  without floating-point contraction returned 2. Fused builds returned 1.
  The paired fixture now uses a power-of-two divisor, and the typed-host test
  detects integer arithmetic where an older profile must round to double.
  [#188](https://github.com/Ambiguous-Interactive/NovaSharp/issues/188)
- Hosted review identified numeric-string negation in Lua 5.3. Reference probes
  confirmed both unary and binary arithmetic coerce a numeric string to double
  in 5.3, while the string metatable keeps integer numerals in 5.4 and 5.5.
  The VM and dynamic evaluator now share that conversion. The paired test
  checks negation, addition, subtraction, multiplication, modulo, floor
  division, and the result subtype.
- The corrected coercion exercised the floating modulo path on wide numerals.
  Lua 5.3 uses the remainder and tests the sign of its product with the divisor.
  Lua 5.4 and 5.5 compare signs directly. The paired fixture covers a wide
  dividend, signed zero, product underflow, and the integer minimum with
  divisor -1.
  [Lua 5.3 numeric macros](https://www.lua.org/source/5.3/llimits.h.html#luai_nummod)
  [Lua 5.4 numeric macros](https://www.lua.org/source/5.4/llimits.h.html#luai_nummod)

## Issue triage

The initial paginated inventory returned 42 issues, with empty second and third
pages. The working priority order was 168, 174, 169, 172, 177, 178, 175, 162, 179,
176, 130, 125, 159, 138, 124, 122, 108, 95, 93, 92, 118, 105, 87, 88, 104, 103,
113, 114, 135, 119, 94, 165, 154, 98, 99, 106, 84, 102, 85, 121, 163, and 157.
Issue 177 was selected because it affects ordinary Lua control flow and blocks
the correction to `math.min` and `math.max`.
The repeat inventory found 47 open issues before filing #186. The new runtime
follow-ups are ordered 181, 186, and 185. Three tooling items opened concurrently
are ordered 184, 183, and 182.

Main passed Tests, CSharpier, Benchmarks, and Pages deployment. GitHub had no
open regular, draft, or dependency PRs. The local guidance draft remains tracked
in [#163](https://github.com/Ambiguous-Interactive/NovaSharp/issues/163).

Follow-up reproductions cover compiled comparison operand evaluation order,
version-specific `next` cursor behavior for integral floats, and the first
visible floating loop value in Lua 5.1–5.3.
[#181](https://github.com/Ambiguous-Interactive/NovaSharp/issues/181)
[#185](https://github.com/Ambiguous-Interactive/NovaSharp/issues/185)
[#186](https://github.com/Ambiguous-Interactive/NovaSharp/issues/186)
