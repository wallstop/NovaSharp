# Session 166: A5 Shrink VM Stacks

Date: 2026-07-06

## Summary

- Advanced Phase A5 by replacing the eager 131,072-slot processor stacks with shared VM defaults: 512 value slots and 64 call frames.
- Kept the existing geometric `FastStack` growth behavior so the smaller defaults are not behavioral limits.
- Applied the same initial capacities to main processors, child coroutine processors, and `ExecutionState` snapshots.
- Added a `FastStack` capacity test hook and targeted coverage for small defaults, growth beyond defaults, coroutine-created processor stacks, deep non-tail recursion past 64 frames, and large vararg calls past 512 values.
- Added standalone Lua fixtures for the two new Lua behavior probes.
- Added scoped manifest entries for the two new fixtures and corrected source-line metadata for the `ProcessorCoreLifecycleTUnitTests` fixtures shifted by this session.
- Left the configurable VM stack ceiling open as the next A5 guardrail.

## Review Loop

- A focused sub-agent audit found that the initial stack shrink is safe if covered by growth tests and actual coroutine-created processor assertions.
- The audit recommended deferring configurable ceilings until deterministic overflow semantics are designed; this session split that remaining work into its own open PLAN item.
- Manual follow-up added the missing runtime growth tests and coroutine-created processor coverage.

## Validation

- `./scripts/test/quick.sh --full -c ProcessorCoreLifecycleTUnitTests` completed with exit code 0: 44 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c FastStackTUnitTests` completed with exit code 0: 13 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c InfrastructureTUnitTests` completed with exit code 0: 6 tests passed, 0 failed.
- `./scripts/build/quick.sh` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0: 15,013 tests passed, 0 failed.
- `bash ./scripts/dev/pre-commit.sh` completed with exit code 0.
- `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` completed with exit code 0; broad generated fixture churn was discarded, and the two scoped fixtures were kept manually.
- `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/manifest.json` was updated for the two scoped fixtures and current `ProcessorCoreLifecycleTUnitTests` source lines.
- `python3 tools/test_lua_fixture_metadata.py`, scoped manifest assertions, and `git diff --check` completed with exit code 0.
- Scoped fixture runs for `ProcessorCoreLifecycleTUnitTests/` completed for Lua 5.1, 5.2, 5.3, 5.4, and 5.5. The two new fixtures returned exit code 0 with empty output/error for both reference Lua and NovaSharp in all five versions. The raw directory run still reports the existing `YieldingFromMainChunkThrowsCannotYieldMain` error fixture as failing for both runtimes, which is unrelated to this change.
- `compare-lua-outputs.py --enforce` was attempted against the scoped output directories but is not a valid scoped check because it assumes the default full fixture directory and reported missing outputs for unrelated fixtures.
