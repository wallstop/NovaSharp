# Session 169: A5 Configurable VM Stack Ceiling

Date: 2026-07-16

## Summary

Closed the remaining Phase A5 coroutine-cost guardrail: **a configurable VM stack ceiling with
deterministic overflow errors**. After session 166 shrank the initial per-processor stacks and left them
growing geometrically without bound, runaway recursion (or expression blow-up) grew the value/call stacks
until the host process exhausted memory. Reference Lua instead raises a *catchable* `stack overflow` error.
This session makes NovaSharp match that contract.

## Changes

- `FastStack<T>` (array-backed and the profiling-only dynamic variant) gained an optional `maxCapacity`
  ceiling. Growth past the ceiling raises `ScriptRuntimeException.StackOverflow()` (message `stack
  overflow`); geometric growth is clamped to the ceiling; an over-large initial capacity is clamped down to
  the ceiling. `maxCapacity <= 0` preserves the previous unbounded behavior, so all non-VM call sites are
  unaffected.
- Added `ScriptRuntimeException.StackOverflow()` producing the reference-compatible `stack overflow`
  message. It flows through the existing interpreter-exception decoration, so a mid-execution overflow is
  reported as `chunk:line: stack overflow` and is catchable via `pcall`, exactly like reference Lua.
- Added `VmStackDefaults.ValueStackMaxCapacity` (1,000,000, mirroring reference `LUAI_MAXSTACK`) and
  `ExecutionStackMaxCapacity` (1,000,000, a call-frame backstop).
- Added `ScriptOptions.MaxVmValueStackSize` and `ScriptOptions.MaxVmCallStackSize` (defaulting to the
  constants above; `<= 0` disables the ceiling). Both are copied by the `ScriptOptions` copy constructor.
  These are a hard safety ceiling, independent of and complementary to the opt-in
  `SandboxOptions.MaxCallStackDepth` security limit.
- Plumbed the ceilings into every per-coroutine stack construction site: main processor, child coroutine
  processor, and the `ExecutionState` snapshot (which uses the default constants directly, having no
  `Script`). The recycle constructor inherits ceilings with the reused stacks.

## Behavior (measured)

- At the default ceiling, runaway non-tail recursion (`local function f() return 1 + f() end`) overflows at
  ~249,999 frames (~4 value slots/frame), between reference Lua 5.1 (~16,380) and 5.4 (~499,993) — a
  faithful, version-plausible depth — and returns a catchable `stack overflow` error with source location.
- Reference Lua 5.1-5.4 and the NovaSharp CLI both print `PASS` for the new fixture's normalized contract
  (pcall returns `false` + a string error containing `stack overflow`).

## Tests

- `FastStackTUnitTests`: ceiling defaults to unbounded; growth within the ceiling succeeds; `Push`/`Expand`
  past the ceiling throw `stack overflow` without corrupting the stack; geometric growth clamps the backing
  array to the ceiling.
- `VmStackCeilingTUnitTests` (new, `[AllLuaVersions]` where behavioral): default options expose the
  ceilings; copy constructor preserves them; infinite recursion throws `stack overflow`; the error is
  catchable via `pcall`; deep-but-bounded recursion (20,000 frames) succeeds under the default ceiling; a
  low `MaxVmValueStackSize` and a low `MaxVmCallStackSize` each trip early — proving configurability.
- Standalone fixture `VmStackCeilingTUnitTests/StackOverflowIsCatchableViaPcall.lua` (+ manifest entry),
  asserting the version-stable contract so it compares equal against reference Lua 5.1-5.5.

## Validation

- `./scripts/build/quick.sh`: exit 0.
- `./scripts/test/quick.sh -c FastStackTUnitTests`: passed.
- `./scripts/test/quick.sh -c VmStackCeilingTUnitTests`: 27 passed.
- `./scripts/test/quick.sh -c SandboxRecursionLimitTUnitTests`: 28 passed (opt-in sandbox limit unaffected).
- `./scripts/test/quick.sh -c ProcessorCoreLifecycleTUnitTests`: 44 passed.
- `./scripts/test/quick.sh -c InfrastructureTUnitTests`: 6 passed (`ExecutionState` snapshot ceilings).
- `python3 tools/test_lua_fixture_metadata.py`: OK.
- Reference Lua 5.1-5.4 + NovaSharp CLI parity confirmed for the fixture.

## Review loop

- Cursor Bugbot flagged two issues on the first push, both fixed in a follow-up commit:
  - Medium: child coroutine processors re-read live `ScriptOptions` instead of the ceiling baked at script
    creation, so a post-creation option mutation could give coroutines a different ceiling. Fixed by having
    child processors inherit `parentProcessor._valueStack.MaxCapacity` / `._executionStack.MaxCapacity`.
  - Low: `FastStack.EnsureCapacity`'s geometric-doubling loop could integer-overflow and spin forever for a
    ceiling above ~`Int32.MaxValue / 2`. Fixed by growing to exactly the required size (bounded by the
    ceiling) when doubling is insufficient or would overflow. Added regression tests for both.
  - Copilot was unable to review (account quota reached).
- Pre-existing flaky test surfaced on macOS CI and hardened in this PR (fragile-check rewrite):
  `MemoryPoolLifecycleTUnitTests.CoroutineMemoryStatisticsTrackingPrunesDeadReferencesOnRegistration`
  asserted `before >= 300` on the raw weak-reference list count, but pruning fires once the 256 prune
  interval is crossed mid-creation, and GC collected the transient coroutines first (macOS found 118). The
  first batch is now held strongly alive through the `before` read; the assertion is deterministic and the
  behavior under test is unchanged. Independent of the stack-ceiling change.

## Remaining A5 work

- `CallStackItem` -> struct frames in a growable stack; delete `CallStackItemPool(s)`.
- Args as stack windows; `ReadOnlySpan<LuaValue>` CLR callbacks; span-based CoreLib migration.
