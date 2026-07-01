# Session 104: ScriptExecutionContext Arity Parity

## Goal

Continue the public API and runtime performance redesign by extending `ScriptExecutionContext.Call` fixed-argument parity to six and seven arguments while preserving Lua `__call` correctness, callback adjustment semantics, and Unity/AOT-friendly low-allocation paths.

## Inputs

- Current branch: `dev/wallstop/api-perf`
- Current PR: `#43`
- Current head before this slice: `75fabd0a`
- User request: trigger Copilot review after each remote push, fix relevant feedback or failing CI, and keep progress notes in the repo-level `progress/` directory using `session-NUMBER-brief-description` naming.

## Research And Review Notes

- Repo priority remains Lua correctness first, then speed, then allocation reduction, then Unity compatibility.
- Codebase review showed `Script.Call`, `CompiledScript.Execute`, `Processor.Call`, `CallbackFunction`, and `FixedCallArguments` already support fixed six/seven argument calls. The public `ScriptExecutionContext.Call` surface stopped at five.
- Sub-agent code review recommended this slice because callback-to-Lua host calls through `ScriptExecutionContext` were the remaining arity mismatch after the host-facing API gained six/seven overloads.
- Adversarial diff review found no blocker, no Lua correctness regression, no callback-adjustment bug, no allocation regression, and no Unity/AOT concern in the scoped changes.
- Competitor/API research reinforced keeping a pure-managed explicit binding path: hardwired managed Lua interop, Lua-CSharp, NLua/KeraLua, xLua, slua, and NeoLua all point to a tradeoff between fast explicit interop and reflection/native runtime constraints.
- Unity documentation review reinforces avoiding runtime code generation assumptions and keeping hot paths allocation-conscious.

## Planned Changes

1. Add six- and seven-argument `ScriptExecutionContext.Call` overloads.
1. Route `ReadOnlySpan<DynValue>` lengths six and seven through fixed overloads before falling back to general span execution.
1. Extend direct callable-table `__call` dispatch when `self + six user arguments` fits the existing fixed seven-value buffer.
1. Add fixed six/seven non-function call handling while preserving chained `__call` behavior.
1. Extend context-call tests for Lua functions, callback views, legacy callback argument adjustment, callable-table dispatch, and allocation probes.
1. Add benchmark rows for context six/seven fixed calls.

## Validation Checklist

- [x] Targeted `ScriptExecutionContextTUnitTests`
- [x] Runtime build
- [x] Benchmark project build
- [x] Repo-wide tests
- [x] Pre-commit
- [ ] Push hook
- [ ] PR CI
- [ ] Copilot review request after push

## Status

Implementation in progress. Focused context-call tests, runtime quick build, benchmark project build, repo-wide tests, and pre-commit pass; remote validation is still pending.

## Implementation Log

- Added fixed six/seven `ScriptExecutionContext.Call` overloads that dispatch Lua functions, CLR callback views, legacy CLR callbacks, and callable non-functions without routing through `params` arrays.
- Added span dispatch cases for six and seven arguments so callback-tail paths and caller-owned spans reuse fixed overloads where possible.
- Added direct first-hop `__call` handling for six user arguments, where the callable self plus six arguments still fits the seven-value fixed buffer.
- Added a pooled eight-value fallback for seven-user-argument callable-table calls, matching the existing host `Script.Call` pattern.
- Extended data-driven tests for fixed Lua calls, callback-view calls, span-backed calls, legacy callback arity, special argument adjustment, and callable-table dispatch.
- Extended allocation probes with diagnostic assertion messages for fixed six/seven callback calls and six-user-argument callable-table calls.
- Added context-call benchmark rows for six fixed arguments, seven fixed arguments, and seven span arguments.
- Formatted touched C# files and fixed the initial TUnit data-source issue by keeping Lua-version matrix tests version-driven and looping over arities inside each test.
- `./scripts/test/quick.sh --full -c ScriptExecutionContextTUnitTests` passed: 129 tests, 0 failed, 0 skipped.
- `./scripts/build/quick.sh` completed successfully.
- `dotnet build -c Release src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj` completed successfully with 0 warnings and 0 errors.
- `./scripts/test/quick.sh` passed: 14,313 tests, 0 failed, 0 skipped.
- Sub-agent diff review completed with no findings.
- `bash ./scripts/dev/pre-commit.sh` completed successfully. It reported existing documentation/LLM skill metadata warnings but no errors and did not add unrelated tracked changes.
- The commit hook initially rejected the staged progress note for a banned legacy branding term; the note was reworded without changing code.

## Current Risks

- Push, PR CI, and Copilot review are still pending.
- The change is API additive and does not alter Lua source semantics directly. Focused callable-table dispatch and callback argument-adjustment tests now pass.
