# Session 183 — A5 Bit32 callback views

Date: 2026-08-30 to 2026-08-31

## Objective

Advance Phase A5 with one bounded CoreLib slice: move Lua 5.2's complete `bit32`
surface to stack-only callback views, remove its superseded host callback API, and
preserve exact reference-Lua behavior and table topology.

## Starting evidence

- `main` and `origin/main` were both at `f6e033f7`, the merge of PR #123.
- Exhaustive REST pagination and an independent connector query agreed on 22 open
  issues. A5 issue #108 remained the highest direct gameplay-impact work; #93 and
  #92 remain its measured follow-ons.
- Main had 44 successful checks and two conditional skips across its latest Tests,
  Benchmarks, CSharpier, and Pages runs.
- There were no draft or prior-session pull requests. Dependabot PR #115 remained
  unsafe to incorporate: Coverlet 10.0.1 still failed coverage loading, its
  high-severity automated review was unresolved, and the branch was behind main.

## Implementation

- Converted all 12 `bit32` registrations to private
  `(ScriptExecutionContext, CallbackArgumentsView)` callbacks and removed the public
  callback methods plus the public `Bitwise` helper instead of retaining adapters.
- Added an internal exact-name path to `NovaSharpModuleMethodAttribute` and
  `ModuleRegister`. Bit32 uses it to expose exactly the 12 standard Lua keys while
  leaving existing name-variant behavior unchanged for every other registration.
- Corrected Lua 5.2 behavior discovered through the official implementation and
  reference interpreter: zero-operand reducer identities, default-build operand
  rounding, C-integer truncation, wide logical shifts, unsigned arithmetic-shift
  results, rotation normalization, optional-width NaN handling, and field/width
  validation priority.
- Normalized operands and displacements through Lua 5.2's double number model even
  when NovaSharp retains an internal integer subtype. Non-finite values and
  magnitudes at or beyond 2^63 now avoid architecture-dependent unchecked .NET
  conversions while preserving the zero narrowing produced by the reference build.
- Kept missing or explicit-nil field widths optional while making a supplied value
  fail with Lua's concrete `number expected` diagnostic instead of advertising
  `nil or number` as the expected type.
- Moved module tests through registered callbacks, added reflection and exact-table
  topology checks, strengthened boundary behavior, and removed duplicate tests whose
  names encoded the old incorrect conversion assumptions.
- Regenerated deterministic comparable Lua fixtures and updated the fixture manifest.
  The main fixture now exercises exact exports, all reducer identities, conversion
  paths, wide shifts, rotations, field operations, and error priority.
- Corrected the Bit32 argument domain, field constraints, arithmetic-shift example,
  and `0xABFF` decimal value in the Lua 5.2 reference documentation.
- Removed Bit32 from PLAN's remaining A5 module queue. Table is now the next module.

## Local verification

- Red gates were observed before their fixes: the callback-view topology failed at
  `bit32.arshift`; `bit32.band(5.7, 3)` returned `1` instead of reference `2`; and
  the exact-export gate rejected the legacy `Extract` alias. Exact optional-width
  assertions also rejected `nil or number expected` for supplied booleans. The final
  boundary review then showed that positive displacements at 2^63 behaved as `-1`
  instead of reference zero; the new boundary regression failed before the explicit
  range handling and passed afterward. A subsequent representation gate showed that
  the exact internal integer `9007199254740993` bypassed Lua 5.2 double rounding;
  shared operand, position, width, and displacement assertions failed before the
  normalization fix and passed afterward.
- The rebuilt exact topology regression passed 1/1. `Bit32ModuleTUnitTests` passed
  87/87, `ModuleRegisterTUnitTests` passed 57/57, and
  `Bit32AvailabilityTUnitTests` passed 14/14.
- `./scripts/build/quick.sh` passed. `./scripts/test/quick.sh` passed 15,284 tests
  with zero failures or skips.
- Corpus regeneration was idempotent at 1,961 snippets: 492 NovaSharp-only and
  1,469 comparable.
- The full Lua 5.2 lane executed 886 compatible fixtures. Enforced comparison
  reported 716 matches, 170 unchanged both-error cases, zero mismatches, zero
  one-sided failures, zero missing outputs, and no error-ratchet changes.
- PLAN hygiene, Markdown formatting and links, skill-index tests, strict index
  validation, and `git diff --check` passed on their then-current inputs.

## Independent review

Two architecture reviews independently required a private view callback for every
export, removal rather than shimming of the public host surface, registration-path
tests, and reference-Lua behavior coverage. Their oracle probes exposed the reducer,
conversion, shift, and arithmetic-shift defects fixed above.

A separate zero-knowledge verifier and nested oracle/adversarial chain exercised all
12 functions across coercion, arity, return count, shift/rotation boundaries,
extract/replace boundaries, error priority, and version exposure. The oracle chain
approved the runtime semantics and identified four stale documentation facts, all of
which were corrected. The verifier then found the extra CLR-derived Lua aliases; its
exact-surface finding produced the final registration design and red-to-green gate.
An independent optional-argument audit approved the localized presence check,
rejected a globally unsafe error-format change, and requested the explicit-nil
`replace` symmetry case now covered in both C# and the comparable fixture.

The verifier also isolated a pre-existing runtime-wide diagnostic gap: reference Lua
chooses qualified or unqualified argument-error function names from the call context,
while NovaSharp callbacks receive hardcoded names. A Bit32-only literal change would
fix one call form and regress another, so the central follow-up is tracked in
[#124](https://github.com/wallstop/NovaSharp/issues/124) with an exact reproduction,
root cause, and cross-library acceptance criteria.

The optional-width correction also exposed that the shared `allowNil` validator
conflates defaulted arguments, true nil unions, arbitrary/truthy values, and
version-specific contracts across dozens of callbacks. A global message change
would regress real `nil or table` errors and other semantics, so the complete
call-site classification and API redesign is tracked in
[#125](https://github.com/wallstop/NovaSharp/issues/125).

Finally, an adversarial exhaustive probe exposed a pre-existing VM bug in numeric
`for` loops crossing zero. The Bit32 matrix was rewritten to avoid depending on
that loop shape and all 917 rows then matched Lua 5.2; the independently reproduced
VM defect and its overflow-heuristic root cause are tracked in
[#126](https://github.com/wallstop/NovaSharp/issues/126).

The first frozen aggregate review rejected the positive 2^63 displacement boundary,
which was reproduced against stock Lua 5.2 and fixed with C# plus comparable-fixture
coverage spanning the adjacent in-range value, both signed limits, larger finite
magnitudes, infinities, and NaN. A second independent boundary review found the
internal-integer bypass above 2^53; after its correction, a 46-displacement by
five-function differential matched all 230 non-hex reference results.

That review also exposed two pre-existing parser-wide literal-model defects that
cannot be repaired inside Bit32 after the original source spelling is lost. Per the
session's scope stop, version-aware large hexadecimal source materialization is
tracked in [#127](https://github.com/wallstop/NovaSharp/issues/127), and the broader
pre-Lua-5.3 decimal literal audit is tracked in
[#128](https://github.com/wallstop/NovaSharp/issues/128). Both issues include exact
reproductions, root causes, version matrices, and closure criteria; intentionally red
parser expansion tests were removed before the scoped snapshot was frozen.

The corrected scoped snapshot initially passed 87 Bit32, 57 registration, and 14
version-exposure tests; a fresh enforced Lua 5.2 lane; 917 shift/rotation rows; every
valid field/width combination over representative values; and retained
operand-normalization boundaries. The final aggregate review then found that the
Lua 5.2 IEEE operand path diverged for non-degenerate values above the ordinary
precision range (for example, `1e20` and `DBL_MAX`), even though the original
internal-integer regression was degenerate. The root cause was `Math.Round` plus
arithmetic modulo; Lua 5.2's default `LUA_IEEE754TRICK` instead adds
`6755399441055744.0` and extracts the low 32 bits of the resulting IEEE double.
`ToUInt32` now implements that exact allocation-free primitive with
`BitConverter.DoubleToInt64Bits`, while retaining the separate signed narrowing
path for displacements, fields, and widths. The red regression was reproduced on
the old implementation and is green after the fix; focused coverage now includes
large finite, internal-subtype, numeric-string, subnormal, infinity, NaN, and
shared-consumer operand paths. The final independent aggregate review and nested
portability review approved the exact corrected staged snapshot with zero actionable
in-scope findings; parser follow-ups #127 and #128 remain accepted tracked scope.

The first hosted macOS ARM64 Lua 5.2 comparison then isolated one remaining
fixture-only mismatch in the most extreme displacement cases: reference builds
narrow C integers and non-finite values differently across architectures. The
comparable fixture keeps the portable boundary cases and documents that the
complete extreme displacement matrix remains in
`ExtremeDisplacementsMatchLua52Narrowing`, a NovaSharp C# regression test. The
corrected fixture passed all five Bit32 Lua 5.2 snippets on both interpreters
locally, and the focused Bit32 suite remained green at 88/88.

## Release-note-ready summary

Breaking/Fixed: remove the public `Bit32Module` callback/helper host surface and make
Lua 5.2 `bit32` expose only its 12 standard names while correcting reducer,
conversion, extreme-displacement, shift, rotation, and field-boundary behavior.
