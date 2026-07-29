# Session 171 — Phase A4: Table array part + open-addressed hash part

**Date**: 2026-07-29
**Branch**: `a4-table-array-hash-parts`
**Phase**: Strategic Roadmap Workstream A, Phase A4 (Table rewrite)

______________________________________________________________________

## Objective

Replace `Table`'s `LinkedList<TablePair>` plus three `LinkedListIndex` dictionaries with
PUC-Lua's storage shape — a contiguous array part for the dense positive-integer prefix and an
open-addressed hash part for everything else — while keeping the public `Table` API and every
Lua-observable behaviour intact.

This is Root Cause #4 from the roadmap: one `LinkedListNode` heap allocation per entry plus three
dictionary indexes, measured at ~149 B retained per entry.

______________________________________________________________________

## Why A4 rather than A1c

A1c (the `DynValue` class to `LuaValue` struct conversion) is the roadmap's next sequential item
and its highest-impact change, but it is not a one-session change: 13,089 `DynValue` references
across 363 files and 718 `== null` comparison sites in the runtime alone, each of which the plan
requires be audited by hand rather than by regex. Landing it half-done would leave the tree red.

A4 is the largest self-contained win that does not depend on A1c, is explicitly marked
parallelizable with A3, and is bounded to one file plus its enumerators.

______________________________________________________________________

## De-risking probe (run before writing any new storage)

The plan flags table iteration order as a spec-compliance risk. Rather than guess at the blast
radius, the existing storage was temporarily patched to iterate in **fully reversed** order and the
whole suite was run. That is a strict upper bound on order sensitivity.

Result: **28 of 15,110 tests failed**, across only 7 distinct test methods
(`NextKeySkipsNilEntriesAndHandlesTermination`, `NextKeyHandlesNonIntegralNumberKeys`,
`ConvertTableIteratesOverTableValues`, `RunTestMoreSuite`, `VarArgsTupleAdvanced`,
`VarArgsTupleAdvanced2`, `VarArgsTupleIntermediate`). The suite also runs in ~40 s, so iteration
is cheap.

That made the phase clearly affordable and set the design constraint: keep ordering *deterministic*
even though it is allowed to change. The probe was reverted before implementation began.

______________________________________________________________________

## Design

`TableStorage` (new, `DataTypes/TableStorage.cs`) is a mutable struct held inline by `Table`, so it
adds no allocation of its own.

**Array part** — `DynValue[]`, where slot `i` holds the value for integer key `i + 1`. Values only;
no keys stored. A `null` slot means the key is absent, a non-null slot (including `DynValue.Nil`)
means an entry exists. That distinction is what preserves the previous storage's behaviour where
writing nil to a fresh key creates an entry that `Table.Count` counts and `next` can use as a
cursor.

Sizing follows PUC's `computesizes`: the array part is the largest power of two `n` for which more
than `n / 2` of the integer keys in `[1, n]` are present, recomputed on rehash. This is what makes a
single far-out key such as `t[1 << 25]` cost nothing instead of demanding a 32M-slot allocation.

**Hash part** — a dense `Node[]` (`key`, `value`, `hash`, `next`) plus an `int[]` bucket table, the
shape .NET's own `Dictionary` uses. Entries are appended and never reordered, so hash-part iteration
is insertion-ordered. Removal marks a node dead rather than recycling its slot, which keeps both the
iteration order and any in-flight `next` cursor stable; dead slots are reclaimed at the next rehash.

**Key routing** — three disjoint routes (positive `int`, `string`, everything else), each with its
own hash function, so hashes only ever need to agree with themselves.

**Iteration order** — array part in index order, then hash nodes in insertion order. Deterministic,
and closer to reference Lua than the previous pure-insertion order.

______________________________________________________________________

## Measurements

Isolated C# harness against `Table` directly, 200k entries, best of 5 reps, old and new builds run
interleaved in the same session to cancel machine drift.

| Scenario | Before | After | Delta |
| ----------------------------- | ------------------- | ------------------- | ------------------- |
| int fill (sequential) | 49-61 ms / 45.70 MB | 10-11 ms / 13.16 MB | 5.3x, 3.5x less alloc |
| int read x5 (1M reads) | 2 ms | 0 ms | >=2x |
| string fill | 77-91 ms / 60.20 MB | 70-81 ms / 46.80 MB | 1.1x, 1.29x less alloc |
| string read x5 (1M reads) | 20-22 ms | 22-26 ms | parity |
| small field read x2M | 13-16 ms | 9-10 ms | 1.5x |
| next traversal x5 | 22-26 ms / 0 B | 21-22 ms / 0 B | 1.1x |
| insert/remove churn | 19-21 ms / 29.66 MB | 6 ms / 14.17 MB | 3.4x, 2.1x less alloc |
| retained bytes per entry (int) | 149.4 B | 10.5 B | 14.2x smaller |

"small field read" is a 8-key object-like table read 2M times — the shape real gameplay code hits
constantly (`entity.health`), and the one that matters most.

`string read x5` on a 200k-entry table is at parity; that case is memory-latency bound rather than
hash bound.

______________________________________________________________________

## Three findings that changed the implementation

Each was caught by measurement, not review.

**1. Traversal started allocating.** Storing values only means the array part has no key object, so
the first implementation synthesized `DynValue.FromNumber(slot + 1)` per traversal step. `next` went
from 0 B to 45.72 MB per five passes over a 200k table — a real regression on `for k, v in pairs(t)`,
which gameplay code runs every frame. Fixed by memoizing array keys in a lazily allocated parallel
array: a table that is only indexed never allocates it, and one that is traversed repeatedly pays
once instead of once per step. This matches what the old storage cost (it allocated the same key
objects, just at insert time) and disappears entirely once A1c makes values a struct.

**2. String probes chased the key `DynValue`.** Reading `nodeKey.Type` and `nodeKey.String` costs a
dereference plus an `isinst` per probe, where the old `Dictionary<string, ...>` compared an inline
string reference. Added an internal `DynValue.ReferencePayload` so probes compare the payload by
reference first, which is what a repeated field name or script constant hits.

**3. A fast string hash collided catastrophically.** Switching from Marvin
(`string.GetHashCode`) to a four-chars-per-iteration hash made 200k structured keys
(`"key0".."key199999"`) **6x slower** — 22 ms to 175-213 ms. The accumulator has weak low bits, and
the bucket table masks with a power of two; `Dictionary` gets away with the same hash only because
it uses prime-modulo buckets. Fixed by running both hashes through a splitmix32 finalizer. The
string hash is also seeded per process, so a script cannot precompute colliding keys — the
hash-flooding denial of service a mod host has to assume. Nothing observable depends on the seed,
because iteration order comes from insertion order rather than bucket layout.

______________________________________________________________________

## Allocation tracking

`AllocationTracker` now reports the array, node, bucket, and memoized-key tables actually retained
instead of a flat 64 bytes per entry. This matters for the sandbox: under the old accounting, a mod
that filled a table with a million entries and then nil'd them all reported its memory back to
roughly zero while the table still held every slot, so the memory limit could be evaded. Retained
capacity now keeps counting, and an emptied table releases its tables.

Because the memoized key table is populated during traversal rather than mutation, the tracker
picks it up on the table's next write. The under-report is bounded by the array capacity, which is
already counted.

______________________________________________________________________

## Behaviour fix found along the way

`Table.Set/RawGet/Remove(int)` used a key space of their own for non-positive integers, while
`Set(DynValue)` routed those same keys elsewhere. A host calling `table.Set(0, v)` wrote an entry
that the script could not see as `t[0]`. Both now route to the same entry. No existing test covered
this; `NonPositiveHostIntegerKeysAddressTheSameEntryAsScript` does now.

`RawGet((string)null)` also used to throw or not depending on whether the table had ever held a
string key. It now reports absence consistently, which is what `t[nil]` means on a read.

______________________________________________________________________

## Test coverage added

`TableNextContractTUnitTests` (8 methods x 5 versions) pins the traversal contract the plan asks
for: every key visited exactly once across a mixed key space, rewriting and clearing fields
mid-traversal, `next` resolving a key it already returned after that key was cleared, `#` as a valid
border for holey and shrinking tables, dense and sparse integer keys coexisting, and remove/re-add
churn never reporting a stale live set.

No assertion depends on hash ordering, which the manual leaves unspecified. Each snippet prints its
outcome so the extracted fixture gives the comparison harness something to diff; all eight produce
identical output under reference Lua 5.1, 5.2, 5.3, 5.4, and 5.5.

`TableStorageTUnitTests` (11 methods x 5 versions) covers the host-visible side: key routing,
traversal ordering, insertion order surviving hash growth, removed versus nil'd traversal cursors,
sparse keys outside the array part, null string keys, retained-storage accounting, dense integer
cost per entry, and structured string keys staying individually addressable.

______________________________________________________________________

## Verification

- `./scripts/build/quick.sh --all` — clean.
- `./scripts/test/quick.sh --full` — **15,205 / 15,205 passing** (15,110 before, 95 added).
- `compare-lua-outputs.py --enforce` on Lua 5.1, 5.2, 5.3, 5.4, 5.5 — **0 mismatch, 0 lua_only,
  0 nova_only**, both-error ratchet unchanged (204/152/264/264/272 against a 1156 baseline, 0 new,
  0 changed).
- `bash ./scripts/dev/pre-commit.sh` — passes.
- PR CI: see the PR record below.

______________________________________________________________________

## Exit criteria status

| Criterion | Status |
| ------------------------------- | ------------------------------------------------------------ |
| ~24-40 B per entry | Met and beaten: 10.5 B for dense integer keys |
| `next`-contract fixtures green | Met, and verified against all five reference interpreters |
| Table-heavy suite >=3-5x | Partially met: 5.3x int fill, 3.4x churn, 1.1-1.5x string |
| binary-trees >=2x | **Not yet measurable** — see below |

The two speed criteria are stated against the Phase A0 scoreboard, which measures whole Lua
programs. At the Lua level the table change is currently invisible: a `t[i] = i` loop is dominated
by the `DynValue` heap allocation that every arithmetic op and loop counter performs, which is
exactly the allocation incident A1 exists to remove. The isolated harness above is the honest
measurement of the data structure until A1c lands, at which point the scoreboard rows should move.

Recorded rather than papered over.

______________________________________________________________________

## Side finding: the corpus extractor clobbers curated fixture metadata

Adding fixtures requires running `tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`, which
rewrites the whole corpus. Re-running the comparison afterwards produced mismatches on Lua 5.1
through 5.4 that had nothing to do with this branch. The extractor overwrites hand-curated headers:

- `MyObject/IndexSetDoesNotWrackStack.lua` — `@novasharp-only: true` became `false`, dropping the
  note that table iteration order is implementation-defined. PLAN.md records that exact marker
  being added on 2026-01-02 to resolve this divergence.
- `ParserTUnitTests/UnicodeEscapeSequenceIsDecoded.lua` — `@lua-versions: 5.3+` became `5.1+`, so a
  `\u{...}` escape that only exists from 5.3 was compared against 5.1 and 5.2.

140 fixtures acquired `@novasharp-only: false` this way, and `tools/test_lua_fixture_metadata.py`
(run by `tests.yml`) fails five assertions against the regenerated corpus. Committing the
regeneration would have turned CI red for reasons unrelated to A4.

The regeneration also extracted 143 fixtures for tests added in earlier sessions that were never
committed to the corpus. Two `VmStackCeilingTUnitTests` fixtures genuinely diverge from reference
Lua — NovaSharp's configurable stack ceiling is deeper than reference Lua's, so bounded recursion
that errors under `lua5.1` succeeds here, and one repeated-overflow fixture exceeds the harness's
5 second timeout. That is session 169's A5 work and needs its own metadata decision.

This branch therefore reverts the corpus and manifest to their state on `main` and adds only the
eight new fixtures. Filed as a follow-up.

______________________________________________________________________

## Follow-ups

- **Fix the extractor** so it preserves `@lua-versions`, `@novasharp-only`, and `@expects-error`
  when a fixture already exists, then commit the 143 missing fixtures with correct metadata. Until
  then, adding a fixture means reverting the collateral by hand, which is how this session lost
  time.
- Decide the `@novasharp-only` / `@lua-versions` metadata for the two `VmStackCeilingTUnitTests`
  divergences, and give the harness a way to express "this fixture is expected to run longer than
  the default timeout".
- `string read` on very large string-keyed tables is at parity rather than ahead. Worth revisiting
  after A6 introduces string interning, which would make the reference-equality fast path hit
  almost always.
- The array-key memo doubles array-part memory for traversed tables. It disappears with A1c and
  should be deleted then, not carried forward.
- `LinkedListIndex<TKey, TValue>` now has no production consumer. Left in place with its tests for
  this PR; removing it is a separate cleanup.
