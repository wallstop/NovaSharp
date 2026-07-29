# Session 170 — A1a: slot/value split and immutable `DynValue`

**Date**: 2026-07-29
**Phase**: Workstream A → Phase A1 (`LuaValue` struct), sub-step **A1a (prep)** — completed
**Branch**: `a1a-slot-value-split`

______________________________________________________________________

## Problem

`DynValue` was simultaneously the *value* and the *storage cell*. A local variable was a mutable
`DynValue` object; a closure captured that same object so assignments stayed visible. Because the
cell was mutable, every read had to defend against later assignment:

```csharp
case OpCode.Local:
    DynValue[] scope = _executionStack.Peek().LocalScope;
    _valueStack.Push(scope[index].AsReadOnly());   // clones whenever the slot is writable
```

`AsReadOnly()` clones any non-read-only value, and local/upvalue slots are always writable — so
**every single local and upvalue read allocated a fresh `DynValue`**. Measured on this machine, that
was ~48 B per read. The same defensive-copy tax appeared on table-key insertion
(`key.ReadOnly ? key : key.AsReadOnly()`), instruction-literal emission, and vararg capture.

This is a direct contributor to the "recursive compute allocation incident" tracked in `PLAN.md`
(`fib(30)` at 2,132,514,592 B/op): `fib` reads `n` several times per call, and each read allocated.

## Change

Split the two roles, as `PLAN.md`'s A1a step calls for.

- **New `Execution/Scopes/ValueSlot.cs`** — an internal `sealed class` holding one `DynValue`. This
  is the mutable identity a closure captures.
- `CallStackItem.LocalScope` is now `ValueSlot[]`; `ClosureContext` stores `ValueSlot` cells and
  exposes `GetSlot`/`SetSlot` alongside the existing value-returning indexer.
- Reading a local/upvalue is now a plain field load (`slot.Value`) with **no allocation**.
- `debug.upvaluejoin` rebinds the cell (`SetSlot`) rather than copying a value, which is what the
  Lua 5.2+ contract actually means. `setfenv` and `Closure.SetUpValue` write through the cell.
- `Closure.GetUpValueMutable(int)` → `Closure.GetUpValueSlot(int)` returning the cell.
- `GetUpValueSymbol` now materializes the cell when a closure captures a local that has not been
  assigned yet, so the capture and the later assignment share one cell (previously the closure
  captured `null`).

With the cells extracted, `DynValue` became genuinely immutable, so the whole read-only machinery
went away:

- Removed `DynValue.AssignSlot`, both `AssignNumber` overloads, `_readOnly`, `ReadOnly`,
  `AsReadOnly()`, `Clone()`, `Clone(bool)`, and `CloneAsWritable()`.
- `ExecIncr` publishes a fresh counter onto the value stack instead of mutating the previous one —
  required, because the previous counter may already have been stored into the loop variable's cell.
- Vararg capture stopped cloning scalars; table-key insertion stopped snapshotting keys; instruction
  literals, `LiteralExpression`, and binary-chunk undump stopped freezing values. All of these were
  guarding against a mutation that can no longer happen.
- `DynValue.NewNil()` now returns the shared `Nil` instance.
- Removed the dead `CallStackItem._localScopeSize` field (written, never read).

### Bug found and fixed: NaN vs. the identity fast path

`ExecEq` (and the constant-folding path in `BinaryOperatorExpression`) short-circuited on
`ReferenceEquals(r, l)`. That was accidentally safe before only because every local read produced a
*distinct* clone. Once values are shared, `local nan = 0/0; return nan == nan` compared one instance
against itself and returned `true`, violating IEEE 754 and Lua semantics. Both sites now exclude
NaN from the identity fast path. This was caught by the existing `NaNNotEqualToItself` and
`ModfNaNReturnsBothNaN` suites, and the whole runtime was swept for other reference-identity
comparisons (`DynValue.Equals` already routes numbers through `LuaNumber.Equal`, which is correct).

## Results

Allocation numbers are deterministic (`GC.GetAllocatedBytesForCurrentThread`). Timings are
best-of-5×25 iterations from a back-to-back A/B on the same machine and are directional only — this
box showed up to ±25% run-to-run variance on wall time, so the benchmark CI leg is the arbiter.

| Scenario | Before B/op | After B/op | Δ alloc | Before ms | After ms |
| --------------------- | ----------: | ---------: | ------: | --------: | -------: |
| fib(20) recursive | 15,937,353 | 11,383,945 | −28.6% | 13.024 | 11.212 |
| numeric loop 100k | 30,400,721 | 23,200,553 | −23.7% | 18.523 | 16.922 |
| local read ×10, 100k | 59,200,817 | 37,600,601 | −36.5% | 27.425 | 23.519 |
| upvalue read 100k | 30,401,377 | 23,201,113 | −23.7% | 20.044 | 17.305 |
| table field loop 100k | 30,401,361 | 23,201,169 | −23.7% | 22.463 | 19.146 |
| table set 100k | 49,241,265 | 37,242,567 | −24.4% | 86.588 | 61.437 |
| closure alloc 20k | 20,160,721 | 15,840,553 | −21.4% | 15.487 | 12.111 |
| string concat 20k | 8,223,169 | 7,743,001 | −5.8% | 5.352 | 4.607 |

## Tests

- New `EndToEnd/ValueSlotSemanticsTUnitTests.cs` (5 cases × 5 Lua versions) covering the invariants
  the split newly guarantees: a value read out of a local/upvalue is a snapshot; the numeric `for`
  control variable is snapshotted per iteration (both by value and by closure capture); sibling
  closures share one cell; and a closure that captures a local before its first assignment still
  observes the later value. Each expectation was verified against reference `lua5.1`–`lua5.5` before
  being asserted.
- Matching `.lua` fixtures under `LuaFixtures/ValueSlotSemanticsTUnitTests/`.
- `VmCorrectnessRegressionTUnitTests.GetUpValueReturnsReadonlyCopy` was rewritten as
  `GetUpValueSnapshotIsUnaffectedByLaterSetUpValue`, which states the property that still matters.
- Tests that existed only to exercise the deleted machinery (`AssignSlot*`, `AssignNumber*`,
  `Clone*`, `AsReadOnly*`, the `Emit*FreezesWritable*` trio, `TableKeySafetySnapshotsMutableNumericKeys`)
  were removed; the behaviour they protected is now structural. `Emit*` tests were kept in a reduced
  form that asserts the emitted instruction carries the right value.

## Verification

| Check | Result |
| ---------------------------------------------------- | ----------------------------------------------- |
| `./scripts/build/quick.sh --all` | clean, 0 warnings |
| `./scripts/test/quick.sh --full` | **15,071 passed, 0 failed** |
| `scripts/ci/check-vm-hotpath-allocations.sh` | OK (ExecIncr allowlist pattern updated in place) |
| `tools/test_lua_fixture_metadata.py` | OK |
| `compare-lua-outputs.py --enforce`, Lua 5.1/5.2/5.3/5.4 | 0 mismatch, 0 lua-only, 0 nova-only, ratchet unchanged |
| `compare-lua-outputs.py --monitor`, Lua 5.5 | 0 mismatch |

## Notes / follow-ups

- `LocalScope` arrays now come from `SystemArrayPool<ValueSlot>` (`ArrayPool<T>.Shared`) rather than
  `DynValueArrayPool`'s thread-local exact-size cache, because that cache is typed to `DynValue`.
  `DynValueArrayPool` and `ObjectArrayPool` are already near-duplicates of the same design; if the
  benchmark leg shows call-path regression, the fix is to generalize that small-array cache once
  rather than add a third copy.
- The committed `LuaFixtures` corpus is stale relative to the test sources independently of this
  change: a full `lua_corpus_extractor_v2.py` run rewrites `@source` path separators and comment
  wording in 1,759 files and adds 142 fixtures for pre-existing tests. That churn was left out of
  this PR to keep the diff reviewable; only the 5 new fixtures and their `manifest.json` entries
  were added. Worth a dedicated cleanup PR.
- `DynValue.ReferenceId` still exists as a no-field compatibility getter and is removed by A1c.
- Remaining A1 work is unchanged: **A1c** (class→struct conversion) and **A1d** (tuning). The slot
  boxes introduced here are exactly what A1c needs, since a `readonly struct LuaValue` cannot be
  captured by reference.

______________________________________________________________________

## Review round 1 (PR #86)

**Cursor Bugbot — 1 issue, confirmed real and fixed.** *Accidental `List.AsReadOnly` removal*
(`HardwireParameterDescriptor.cs:111`). The sweep that stripped `DynValue.AsReadOnly()` call sites
was a text substitution on `.AsReadOnly()`, which also matched `list.AsReadOnly()` — a
`List<T>.AsReadOnly()` returning a `ReadOnlyCollection<T>`, entirely unrelated to `DynValue`.
`LoadDescriptorsFromTable` was silently widened to hand back the mutable backing list. Restored, and
every other site the substitution touched was audited: all remaining removals were on `DynValue`
receivers. Lesson: a regex over a method *name* cannot distinguish receivers — the audit should have
been part of the original edit, not a follow-up.

**GitHub Copilot** declined to review: the requesting account has hit its review quota.

**CI — `benchmark aggregate report` failed**, 16 identical Phase A0 gate rows: `Cached Compile`
allocation rose from **208 B to 216 B** (+8 B against a 0 B tolerance — exact enforcement for
sub-1 KiB baselines, per the methodology rule). Real and attributable: binding `_ENV` at chunk entry
now allocates a `ValueSlot` cell, partly offset by `DynValue` shrinking after `_readOnly` was
removed.

Fixed rather than re-baselined. `Table` gained a `CachedDynValue` wrapper and `DynValue.FromTable`,
mirroring the existing `FromClosure`/`FromCallback` pattern, and the two chunk-entry sites in
`Script.cs` now reuse it. Caching one wrapper per table is *newly* safe precisely because of this
PR: with mutable values a shared wrapper was an aliasing hazard, and with immutable values it cannot
be reassigned out from under a holder. Equality is unchanged — `DynValue.Equals` compares tables by
their underlying `Table` reference either way.

Local probe replicating `NovaSharpCachedCompile`: **216 B/op → 168 B/op**, i.e. 40 B *below* the
committed baseline rather than 8 B above it.

**Added coverage.** An adversarial pass on the riskiest property — per-iteration cell freshness —
found the original tests only covered numeric `for`. Cell freshness depends on the block-clear that
nulls the slot at each iteration's scope entry, so a miss in any other loop form would alias every
closure in that loop onto one cell. Added `EveryLoopFormGivesEachIterationItsOwnCell` (numeric
`for`, generic `for`, body-local in `for`, `while`, `repeat`) and
`PerIterationCellsStayIndependentAcrossMutatingCalls`. All expectations were taken from reference
lua5.1-5.5 first; NovaSharp matched all five versions on all six forms.

Re-verified: **15,106 tests pass**, `compare-lua-outputs.py --enforce` clean on 5.1 and 5.4, VM
hot-path allocation gate OK.

The generated fixture for `EveryLoopFormGivesEachIterationItsOwnCell` was dropped: the test is
data-driven via C# interpolation, so the extractor emitted an unresolved `{body}` placeholder rather
than runnable Lua. The other six fixtures round-trip cleanly.
