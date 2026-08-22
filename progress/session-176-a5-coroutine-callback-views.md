# Session 176 — A5 coroutine module argument views + dependency closure

Date: 2026-08-22

This session carried forward the in-progress `dev/wallstop/a5-coroutine-callback-views` work: migrating every `coroutine.*` CoreLib callback from legacy `CallbackArguments` registrations to stack-only `CallbackArgumentsView` registrations, per Phase A5's one-module-per-PR convention. The coroutine library is the second fully migrated module after Math.

## Coroutine module migration

- Added `[NovaSharpModuleMethod]` argument-view implementations for `create`, `wrap`, `resume`, `yield`, `close` (5.4+), `running`, `status`, and `isyieldable` (5.3+). The legacy public methods remain as thin shims that delegate through `new CallbackArgumentsView(args)`, preserving the reflection surface; built-in registration (`ModuleRegister.SelectPreferredBuiltInModuleMethods`) now selects only the view callbacks.
- `ResumeCoroutineWithArguments` now takes a `CallbackArgumentsView` and, beyond the fixed 1-4 argument `ResumeValues` overloads, uses `TryGetSpan` to resume with a zero-copy slice for higher arities instead of materializing an array.
- `coroutine.wrap`'s returned closure is registered through `LuaValue.NewCallbackView`, so wrapped-function calls dispatch on the stack-only path; the old legacy-signature closure was deleted rather than duplicated.
- Retargeted `ResumeAndWrapDispatchThroughFixedArityCoroutinePaths`, which inspects IL bodies, from the retired `(Coroutine, CallbackArguments, int)` helper signature to the live registered view path. It now also asserts the helper references `TryGetSpan` exactly once and that nested closures make zero `GetArray` calls.
- Added `RegisteredCallbacksAndWrappedFunctionsUseArgumentViews` across Lua 5.1-5.5: every coroutine callback must report `HasArgumentViewCallback`/`HasArgumentViewNoContextCallback`, and a wrapped function must round-trip values (41 → yield → 42) through the new path.

## Dependency closure

- Merged PR #68 (coverlet.console 6.0.4 → 10.0.1); all checks were observed green before merging.
- Folded PR #69's intent into this PR: bumped csharpier to 1.3.0 in `.config/dotnet-tools.json` and adopted its output (CSharpier 1.3.0 adds MSBuild XML coverage). Only three files changed: `Directory.Build.props`, `Directory.Build.targets`, and the IL2CPP spot-check sample. Closes #69.
- RCA'd and closed PR #111: ilspycmd ≥10 NuGet packages no longer contain `DotnetToolSettings.xml`, so `dotnet tool restore` cannot install them (verified locally against 11.0.0.9375 and 10.1.1.8388). The bump requires either pinning to 9.1.x or moving decompilation off the dotnet-tool manifest.
- Closed draft PR #72 (autofix lint for #69) as superseded; it was generated against csharpier 1.2.4 output.
- Reverted coverlet.console back to 6.0.4 after PR CI exposed that the merged #68 bump broke coverage collection: on runners with a preinstalled .NET 8 runtime, coverlet 10.x instrumentation emits `System.Runtime 9.0.0.0` references that fail to load in the net8.0 test host, every test dies during attribute resolution, TUnit's message bus wedges at "34 tests running", and the job burns to its 35-minute timeout. Local 9.x-only boxes roll forward and mask this, which is why local verification passed. Filed [#114](https://github.com/wallstop/NovaSharp/issues/114) covering the re-attempt procedure and the skipped-check validation gap for dependabot branches.

## Remaining A5 CoreLib migration

16 modules still register legacy-only callbacks: Debug, String, Io, Bit32, Basic, Table, Load, Utf8, OsSystem, StringPack, MetaTable, ErrorHandling, TableIterators, OsTime, Json, Dynamic. Each should follow the same shim-plus-view pattern in its own PR.

## Verification

- Full TUnit suite passed 15,230/15,230 after the migration, merge of main, and reformatting.
- `./scripts/build/quick.sh` completed successfully.
- Full-corpus comparison against reference Lua ran locally for 5.1-5.5 with `--enforce`: 0 mismatch, 0 `lua_only`, 0 `nova_only` per version; both-error ratchet reported 0 new / 0 changed / 0 missing, plus one removed unclassified signature on 5.4 (`EvaluateSymbolByNameResolvesLocals.lua`, a strict reduction).
- Repository pre-commit checks completed successfully, including CSharpier 1.3.0 format validation.
- PR #112 CI was observed fully green (42 passing checks): all three OS test matrices, all 15 Lua comparison lanes, coverage (3m41s after the revert versus a 35-minute timeout before it), the full benchmark scenario matrix including the coroutine allocation gates, and the aggregate report. One transient `comparison NumericLoops` runner timeout at exactly its job timeout was cleared by a clean workflow rerun of identical content that had passed on the prior SHA.
