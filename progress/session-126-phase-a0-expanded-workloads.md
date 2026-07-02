# Session 126: Phase A0 Expanded Pure-Lua Workloads

Date: 2026-07-02

## Summary

- Expanded the comparison benchmark scenario catalog from 5 to 16 pure-Lua workloads.
- Added compute scenarios for `fib(30)`, n-body, binary-trees, and spectral-norm while retaining hanoi and EightQueens coverage.
- Added table-heavy scenarios for integer fill/iterate, string-key lookup, `next` traversal, and insert/remove churn.
- Added string-heavy scenarios for concat chains, `string.gsub`/`string.find`, and `string.format`.
- Kept all new scripts Lua 5.1-compatible so every managed comparison runtime and reference `lua` CLI can share the same exported scenario text.
- Replaced the duplicated BenchmarkDotNet `[Params]` scenario list with a shared `ParamsSource` backed by `BenchmarkScripts.GetScenarioNames()`, so benchmark rows and CLI scenario export stay in sync.
- Kept the stable scenario display names short enough to avoid BenchmarkDotNet parameter truncation, and exported reference `lua` CLI scenarios with the same names used by BenchmarkDotNet.
- Updated `PLAN.md` to split completed pure-Lua workload coverage from the still-open interop and cached-compile Phase A0 rows.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release -v:minimal` passed.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --export-scenarios artifacts/benchmarkdotnet/lua-cli-scenarios-smoke` exported 16 scenarios.
- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir artifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root artifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4 --warmup-count 0 --iteration-count 1 --timeout-seconds 30` produced 16 reference `lua` CLI rows.
- A temporary ignored harness under `artifacts/comparison-smoke-harness` instantiated `LuaPerformanceBenchmarks` for each exported scenario and called every managed comparison runtime's compile/execute methods once; all 16 scenarios completed.
- `python3 tools/test_render_benchmark_deltas.py` passed.
- `python3 tools/test_run_lua_cli_context.py` passed.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py scripts/benchmarks/run-lua-cli-context.py tools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py` passed.
- `git diff --check` passed.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed with 14,529 succeeded, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` passed.
- After the Copilot-requested fail-fast switch change, the comparison build, benchmark renderer tests, Lua CLI context tests, `git diff --check`, and 16-scenario export smoke passed again.
- PR #46 benchmark CI passed after the Copilot fix, but its delta comment reported 3 missing expected external cells and 3 missing reference `lua` CLI rows because BenchmarkDotNet truncated scenario names longer than 20 characters while exported `lua` CLI files still used enum names.
- After the display/export alignment fix, the comparison build, benchmark renderer tests, Lua CLI context tests, 16-scenario export smoke, and one-iteration reference `lua` CLI context smoke passed again; the smoke artifact included `ScenarioName=StringPatternOps`, `ScenarioName=TableInsertRemove`, and `ScenarioName=TableIntFillIter`.

## Notes

- `dotnet run ... -- --filter "*LuaPerformanceBenchmarks*" --job Dry` was stopped because the project-level BenchmarkDotNet config still added the normal `Comparison` job, turning the intended dry smoke into a full 256-benchmark run.
- Copilot review on PR #46 flagged silent `TowerOfHanoi` fallbacks in the scenario name/script switches. Both default arms now throw so missing scenario mappings fail during development or CI instead of exporting or benchmarking the wrong workload.
- Cross-runtime Lua-to-C# and C#-to-Lua interop rows remain separate work because every engine needs a different host binding path and reference `lua` needs explicit skip/report behavior.
- Full BenchmarkDotNet JSON baselines, one-command scoreboard markdown generation, ratio/allocation gates, and the Unity IL2CPP stopwatch scene remain open Phase A0 work.
