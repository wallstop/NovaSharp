# Numeric `for` Loop Reference Matrix

Verified facts about reference Lua's numeric `for` across 5.1-5.5, established while
fixing [#126](https://github.com/wallstop/NovaSharp/issues/126) (PR #131). Sources:
empirical probes on the installed `lua5.1`-`lua5.5`, plus `lvm.c` from the official
`lua-5.1.5` and `lua-5.4.4` tarballs. NovaSharp reimplements this matrix in
`Processor.ExecForPrep`/`ExecJFor`/`ExecIncr`.

## Instruction shape

- Lua 5.4+ `forprep` prepares a counter-driven integer loop and decides entry; the
  bottom `forloop` checks a precomputed remaining-iteration counter
  (`((u64)limit-(u64)init)/step` ascending, `((u64)init-(u64)limit)/(-(step+1)+1)`
  descending) and never compares the index against the limit, so the visible control
  variable cannot wrap at `mininteger`/`maxinteger`.
- Lua 5.1-5.3 have one comparison-driven loop shape; 5.1's `forprep` pre-subtracts the
  step (`ra = init - step`) and jumps directly to the bottom check, so the first
  decision evaluates `(init - step) + step`, not `init`.

## Verified corner behavior

- **Zero-crossing ranges** (`for i = -2, 2`) iterate every value on all versions;
  any sign-based overflow heuristic is unrepairable because it cannot distinguish a
  legitimate zero-crossing from a wrapped counter without iteration state.
- **Reference 5.3.6 loops forever** on ranges reaching either integer extreme
  (`for i = maxinteger-2, maxinteger`, `0, maxinteger, maxinteger`, `mininteger`
  descents) — a known upstream bug fixed by 5.4's counter. NovaSharp follows 5.4
  semantics in 5.3 profiles deliberately; comparison fixtures covering extremes are
  therefore scoped 5.4+.
- **NaN bounds, float loops**: 5.4+ enter for exactly one iteration (entry uses
  lt-forms that never reject NaN); 5.1-5.3 never start (entry effectively demands the
  le-condition, and a NaN step poisons 5.1-5.3's `(init-step)+step` entry index).
- **NaN limit, integer loop (5.3+)**: ascending skips; descending clamps the limit to
  `mininteger` via `forlimit` and effectively never terminates.
- **Float limits in integer loops** convert with floor (ascending) / ceil (descending)
  rounding; limits beyond the integer range clamp to the boundary the loop walks
  toward, or skip when no integer limit can satisfy the condition.
- **Zero step**: 5.4+ raise `'for' step is zero` for `0`, `0.0`, and `-0.0`; 5.1-5.3
  run zero iterations ascending and loop forever descending. 5.4+ check a zero
  integer step *before* validating the limit; 5.3 validates the limit first.
- **Control error messages**: ≤5.3 say `'for' limit must be a number`; 5.4+ say
  `bad 'for' limit (number expected, got table)`.
- **The loop variable is block-scoped** (nil after the loop on every version) and is
  `const` in 5.5 (assignment errors — see
  [#130](https://github.com/wallstop/NovaSharp/issues/130)).
- **Float accumulation drift is observable**: `for i = 1, 0, -0.1` must reproduce
  reference drift bit-for-bit (down to `1.3877787807814457e-16`) because the visible
  values come from repeated floating addition.
- **`goto` out of a loop must pop the construct's value-stack slots** (numeric loop:
  three, generic loop: one) — reference reclaims them; NovaSharp records the count on
  `RuntimeScopeBlock.ValueStackSlots` and `GotoStatement` pops on block exit.
