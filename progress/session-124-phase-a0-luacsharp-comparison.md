# Session 124: Phase A0 Lua-CSharp Comparison Target

Date: 2026-07-01

## Summary

- Added NuGet `LuaCSharp` 0.5.5 to the comparison benchmark project.
- Added `LuaCSharp Compile` and `LuaCSharp Execute` BenchmarkDotNet rows alongside the existing external runtime comparisons.
- Used Lua-CSharp's standard-library setup and stack-based execution path so execute rows avoid the result-array helper while still consuming a returned value.
- Normalized compile rows so every runtime creates a fresh runtime state before loading the scenario.
- Updated the benchmark delta renderer to recognize `LuaCSharp` as a first-class external runtime.
- Added missing expected external-runtime diagnostics so omitted comparison cells appear in stdout and the markdown report.
- Extended the renderer regression test to assert Lua-CSharp time and memory columns and missing-runtime diagnostics.
- Kept missing expected external-runtime cells report-only in benchmark CI until Phase A0's remaining ratio and allocation gates are added.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release -v:minimal` passed.
- `python3 tools/test_render_benchmark_deltas.py` passed.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py tools/test_render_benchmark_deltas.py` passed.
- Direct smoke of `LuaPerformanceBenchmarks.Setup`, `LuaCSharpCompile`, `LuaCSharpExecute`, and `Cleanup` for `NumericLoops` passed.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed.
- `bash ./scripts/dev/pre-commit.sh` passed.

## Remaining Phase A0 Work

- Commit full BenchmarkDotNet JSON baselines under `progress/`.
- Add reference `lua` CLI wall-time context to the scoreboard.
- Expand the workload list to the full Phase A0 suite.
- Add ratio-vs-NLua and allocation gates after the baseline is committed.
- Add the minimal Unity IL2CPP stopwatch scene.
