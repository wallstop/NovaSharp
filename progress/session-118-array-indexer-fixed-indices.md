# Session 118: Array Indexer Fixed Indices

## Baseline

- Branch: `dev/wallstop/api-perf`
- Starting head: `f621d034`
- PR: `#43`
- Worktree: clean and aligned with `origin/dev/wallstop/api-perf` at session start.
- PR baseline: latest observed required checks passed for `f621d034`; expected optional benchmark `comparison` and tests `lint-autofix` jobs were skipped.
- Review baseline: Copilot review was requested for `f621d034` and responded with the 20,000-line PR limit message. No current unresolved non-outdated review threads were observed in the previous session.

## Goal

Remove per-call `int[]` index materialization from common CLR array userdata indexer get/set paths for rank 1, 2, and 3 arrays.

## Rationale

`ArrayMemberDescriptor` currently builds a fresh `int[]` for every array indexer get and set, even though `System.Array` exposes scalar `GetValue` and `SetValue` overloads for one, two, and three indices. CLR arrays are registered as userdata indexers, so this allocation appears in Unity-facing host interop loops that index arrays from Lua.

Session 117 removed the standard userdata indexer callback wrapper allocation. This session continues the same indexer path by eliminating the remaining array-index storage allocation where the CLR has direct overloads.

## Implementation Plan

- Switch `ArrayIndexerGet` over argument count and call `Array.GetValue(int)`, `Array.GetValue(int,int)`, or `Array.GetValue(int,int,int)` for ranks 1-3.
- Switch `ArrayIndexerSet` over index count and call the matching scalar `Array.SetValue` overloads for ranks 1-3.
- Preserve current fallback behavior for higher-rank arrays by retaining exact-length `BuildArrayIndices`.
- Preserve conversion/error order by converting all index arguments before converting the assigned value.
- Add 3D array get/set behavior coverage and a focused allocation diagnostic for rank 1 direct descriptor execution.

## Validation Plan

- Targeted `ArrayMemberDescriptor` tests.
- Fixture/catalog regeneration for new Lua fixture coverage.
- Interpreter build, full quick tests, formatting/pre-commit before commit.
- After push, request Copilot review and poll PR CI; fix actionable production or test issues based on actual evidence.

## Status

- Implemented rank-guarded scalar `Array.GetValue` / `Array.SetValue` paths for rank 1, 2, and 3 userdata array indexers.
- Preserved the existing vector-index fallback for higher-rank or wrong-rank access so CLR exception behavior remains anchored to the old path.
- Kept setter conversion ordering explicit: index conversion first, assigned-value conversion second, CLR bounds/rank validation last.
- Added 3D array get/set coverage, direct rank-one getter/setter allocation diagnostics, assigned-value/index conversion-order tests, and wrong-rank fallback parity coverage.
- Ran a read-only subagent review of the array indexer slice; incorporated its rank-guard and exception-order recommendations.
- Reverted broad Lua fixture churn produced by an extractor run and kept only the two new array fixture files for this slice.

## Validation

- `./scripts/test/quick.sh --full RankOneArraySetterAvoidsIndexArrayAllocation` passed.
- `./scripts/test/quick.sh -c ArrayMemberDescriptorTUnitTests` passed before adding the rank/fallback diagnostics.
- `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` completed and reported 1900 snippets; generated unrelated fixture churn was removed from the working diff.
- `./scripts/test/quick.sh --full -c ArrayMemberDescriptorTUnitTests` passed after the full targeted test set was added.
- `dotnet tool restore && dotnet tool run csharpier format .` completed; resulting diff stayed scoped to the intended C# files.
- `./scripts/test/quick.sh --full -c ArrayMemberDescriptorTUnitTests` passed after formatting: 41 total, 0 failed, 0 skipped.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 14493 total, 0 failed, 0 skipped.
- `./scripts/dev/pre-commit.sh` completed successfully. It reported existing documentation and LLM skill metadata warnings but no errors and produced no unrelated tracked file changes.

## Next Steps

- Commit, push, request Copilot review, and poll PR CI.
