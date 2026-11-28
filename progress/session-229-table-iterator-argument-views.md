# Session 229 — Table iterator argument views

Issue: [#108](https://github.com/Ambiguous-Interactive/NovaSharp/issues/108) Phase A5
CoreLib registration migration.

## Slice

Migrate `TableIteratorsModule` (`next`, `pairs`, `ipairs`, and the internal
`!!next_i!!` iterator) to stack-only `CallbackArgumentsView` entry points. The
reflection binder now selects the view overload for `next`, and the custom
`pairs`/`ipairs`/iterator callbacks are created with `NewCallbackView`, so no
legacy `CallbackArguments` wrapper is materialized per call.

Public legacy overloads remain as thin wrappers for direct C# callers. No new
public surface was added; `ScriptFunctionCallbackView` is already the preferred
module callback shape used by `math`, `table`, and other migrated modules.

## Evidence

- Allocation probe (Lua 5.5, 200,000 `next` calls over a 1,000-entry table,
  20 warmup traversals): `400.144 B` per call before, `136.144 B` per call
  after — a 66% reduction (264 B/call). `NewCallbackView` removes the
  per-invocation argument wrapper and its pooled backing list.
- New regression test
  `NextCallsUseStackOnlyArgumentsWithoutWrapperAllocations` fails on the legacy
  path (`400.144 B`, threshold 200 B) and passes with the view path
  (`136.144 B`).
- `RegisteredTableIteratorsUseArgumentViews` asserts `next`, `pairs`, `ipairs`,
  and the iterator returned by `pairs`/`ipairs` all carry
  `HasArgumentViewCallback` across Lua 5.1–5.5.
- Full TUnit suite: 16325/16325.
- Full Lua 5.5 corpus against PUC Lua 5.5.0: 1172 matches, 0 mismatches, 0
  Lua-only, 0 Nova-only, 314 unchanged both-error entries, 1 removed.
- Full Lua 5.4 corpus against the host `/usr/bin/lua5.4` (5.4.4): 1120 matches
  and the same 2 pre-existing mismatches with and without this change
  (`OperationsUseVersionedIndexingAndLength`, `GenericForClosingValueFollowsLexicalExitOrder`);
  the project's reference is Lua 5.4.7, so this is host-version variance rather
  than a regression.

## Files

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/TableIteratorsModule.cs`
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableIteratorsModuleTUnitTests.cs`
- `PLAN.md` (remaining A5 CoreLib migration list)
