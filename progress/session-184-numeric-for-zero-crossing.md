# Session 184 — Numeric for-loop zero-crossing correctness

Date: 2026-09-03

## Objective

Close [#126](https://github.com/wallstop/NovaSharp/issues/126): integer numeric `for`
loops whose initial value and limit straddle zero executed zero iterations in every
compatibility profile, and the sign-only overflow heuristic that caused it could not be
repaired in place. Replace it with reference Lua's counter-based loop protocol.

## Starting evidence

- `main` was green (Tests, Benchmarks, CSharpier, Pages) at `f31d973e`, the merge of
  PR #129. No draft or prior-session PRs were open; only dependabot #115 remained.
- 27 open issues were enumerated; #126 was the highest gameplay-impact item (silent
  wrong iteration counts in ordinary control flow).
- A previous work stream left uncommitted devcontainer AI-backend tooling in the tree;
  all four of its suites passed locally and it was carried forward as the session's
  first commit.
- Reference oracle established empirically on the installed `lua5.1`-`lua5.5`:
  - Every reference version iterates the full zero-crossing range; NovaSharp printed
    empty output (`asc=[]`, `desc=[]` through the CLI).
  - Lua 5.4/5.5 reject a zero step with `'for' step is zero`; 5.1-5.3 run zero
    iterations ascending (and loop forever descending).
  - Reference Lua 5.3.6 loops forever on ranges reaching either integer extreme
    (`maxinteger` ascent, `mininteger` descent, maximal steps) — a known upstream bug
    fixed by 5.4's counter design. NovaSharp follows the corrected 5.4 semantics.
  - Lua 5.5 rejects assigning the numeric loop control variable (`const variable`);
    5.1-5.4 allow it. NovaSharp allows it in every profile — filed as
    [#130](https://github.com/wallstop/NovaSharp/issues/130).

## Implementation

- Added opcode `ForPrep` (appended at 66; `Invalid` stays last in declaration order),
  its field-usage classification, `ByteCode.EmitForPrep`, and emission in
  `ForLoopStatement.Compile` after the three `ToNum` stages.
- `ExecForPrep` mirrors reference Lua 5.4 `forprep`/`forlimit` (lvm.c): integer loops
  (integer index and step) replace the limit slot with a remaining-iteration counter
  computed with unsigned arithmetic — ascending
  `((ulong)limit - (ulong)init) / step`, descending
  `((ulong)init - (ulong)limit) / (-(step + 1) + 1)` — plus one for the iteration
  about to run, capped at `ulong.MaxValue` when the +1 would wrap. Float limits
  convert with direction-aware rounding (floor ascending, ceil descending), clamp to
  the integer boundary the loop walks toward, or skip the loop when no integer limit
  can satisfy the condition; NaN follows Lua's `forlimit` branch. Every other loop is
  comparison-driven with its limit slot forced to the float subtype, which doubles as
  the protocol marker.
- `ExecJFor` now tests the counter for integer loops (continue iff nonzero) and keeps
  the existing comparison semantics for float loops; the sign-only heuristic is
  deleted. `ExecIncr` consumes one counter unit per completed iteration and then adds
  the step to the index, so the visible control variable can never wrap around (Lua
  5.4 manual §3.3.5) and zero-crossing ranges iterate every value.
- Zero steps now follow the version matrix: `ScriptRuntimeException.ForStepIsZero()`
  (`'for' step is zero`) for 5.4+, zero iterations for 5.1-5.3 (including the
  descending direction that reference 5.1-5.3 never terminates).
- The white-box `ExecIncrPublishesFreshCounterWithoutMutatingPrevious` test was
  updated to the three-slot protocol and now also asserts the counter decrement.

## Local verification

- Red gates were observed before the fix: zero-crossing suites returned empty output
  on every profile, `for i = 0, 2e63, math.maxinteger` never terminated (the same
  wrap class reference 5.3 exhibits), and the 5.4 zero-step error was absent.
- `NumericForLoopTUnitTests` passed 144/144 after the fix (data-driven zero-crossing
  and standard-range matrices across all profiles, float loops, boundary loops from
  both extremes, maximal steps, float-limit clamping, zero-step matrix, control
  variable scope/mutation, coroutine suspension inside a zero-crossing loop, and a
  binary dump round-trip through the new opcode).
- Full suite: 15,428/15,428 passed.
- New comparable fixtures verified against reference Lua first: `ZeroCrossingRanges`
  and `FloatRanges` (5.1-5.5 identical), `ControlVariableMutation` (5.1-5.4; 5.5
  rejects the assignment), `IntegerBoundaries` (5.4+; reference 5.3 hangs),
  `ZeroStepSilentBeforeLua54` (5.1-5.3), `ZeroStepErrorsFromLua64` (5.4+,
  expects error). The extractor additionally generated per-test fixtures from the new
  C# tests; their heuristic version scopes were wrong (attribute context bleed, the
  [#99](https://github.com/wallstop/NovaSharp/issues/99) class) and were curated —
  curated headers are authoritative across regenerations, verified by re-running the
  extractor.
- The interpolated `RunLoop` helper in the test was rewritten as concatenation so the
  extractor stops emitting a placeholder `Unknown.lua` fixture.
- Corpus regeneration is idempotent at 1,978 snippets. The enforced comparison
  matrix passed on all five lanes with zero mismatches, zero one-sided failures, and
  zero missing outputs; the both-error ratchet was rebaselined (+66 lines) for the
  new zero-step fixtures that both interpreters reject with version-appropriate but
  differently formatted messages.
- The loop check stays allocation-free by construction: the new paths use only
  value-type `LuaNumber` arithmetic, integer compares, and in-place stack writes.

## Independent review

An adversarial sub-agent reviewed the protocol against reference Lua 5.4.4's `lvm.c`
(raw sources for 5.1.5 and 5.4.4 were fetched and compared) and probed protocol
soundness, counter arithmetic at every extreme, subtype mixing, float drift, zero-step
matrix, suspension/unwinding paths, dump round-trips, and the test suite. It confirmed
the counter protocol sound on every reachable case and confirmed the float-accumulation
drift is bit-exact against all five references, then reported four actionable findings,
all addressed in this session:

1. NaN float-limit clamping had regressed the 5.1/5.2 profiles (a NaN limit with an
   integer-descending loop ran ~forever where reference lua5.1/5.2 run zero
   iterations). Fixed by restructure (3): pre-5.3 profiles now never take the integer
   counter path.
2. A float loop exposed an integer-subtype control variable on its first iteration
   (`for i = 1, 3, 1.0` yielded integer `1` where reference yields float `1.0`). Fixed
   by normalizing float-loop controls to the float subtype from 5.3 on; 5.1/5.2 keep
   the coerced subtypes because NovaSharp's `table.concat` does not yet apply the
   pre-5.3 integral-float formatting there (filed as
   [#132](https://github.com/wallstop/NovaSharp/issues/132)).
3. The zero-step error lost reference Lua's priority race against an invalid limit
   (`for i = 1, {}, 0` reported the limit error first). Fixed by restructuring to
   reference Lua's instruction shape: `ForPrep` now performs every validation and the
   entry decision (jumping past the loop when the body must not run) while `JFor`
   moved to the bottom of the body as the pure continuation check. This also produced
   exact entry semantics for non-finite bounds: Lua 5.4+ enter a NaN-bounded float
   loop for one iteration, while 5.1-5.3 — whose `forprep` computes the entry index as
   `(init - step) + step` — never start, NaN steps included; and it made the control
   error messages version-exact (`bad 'for' limit (number expected, got table)` on
   5.4+, `'for' limit must be a number` before, with 5.3 validating the limit before
   its tolerated zero step and 5.4+ reversing that order).
4. A pre-existing `goto` out of a numeric or generic `for` loop leaked the loop's
   value-stack slots on every jump until the stack overflowed (reproduced identically
   on the parent commit). Fixed by recording each construct's live value-stack slots
   on its `RuntimeScopeBlock` and making `GotoStatement` pop them when it exits the
   block; the original five-million-jump reproduction now completes in constant memory
   like reference Lua.

Nits also addressed: the counter-cap comment now states the lost terminal iteration;
`ForPrep` joined `OpCodeStrings`; the stale "Invalid must be last" note was corrected.
Acknowledged and left open: reference 5.3 loops forever at the integer extremes where
NovaSharp follows the corrected 5.4 semantics (documented in tests and fixtures), and
Lua 5.5's const control variable ([#130](https://github.com/wallstop/NovaSharp/issues/130)).

## Release-note-ready summary

Fixed: numeric `for` loops whose bounds straddle zero now iterate every value like
reference Lua, integer loops can no longer expose wrapped control variables at the
`mininteger`/`maxinteger` boundaries, out-of-range float limits clamp like Lua
5.4+, and a zero step raises Lua's `'for' step is zero` error on 5.4+ profiles
instead of silently running zero iterations (or forever).
