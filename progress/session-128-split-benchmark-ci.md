# Session 128: Split Benchmark CI

Date: 2026-07-02

## Summary

- Split the benchmark workflow into bounded measurement legs:
  - `runtime-benchmark` runs the NovaSharp runtime benchmark suite.
  - `comparison-scenario` runs one pure-Lua comparison scenario per matrix leg.
  - `comparison-interop` runs Lua-to-CLR and CLR-to-Lua interop comparison legs separately.
  - `benchmark-report` downloads all split artifacts and renders the aggregate benchmark delta report and PR comment.
- Set every measurement leg timeout to 10 minutes so no single CI leg can silently grow into the previous 50-minute monolith.
- Preserved the existing benchmark-action storage/comment path in the aggregate job.
- Made BenchmarkDotNet JSON export explicit in both benchmark configs so CI cannot run a benchmark successfully while leaving the aggregate without parseable JSON.
- Removed redundant `--exporters json` flags from CI and local benchmark launchers now that JSON export is part of the benchmark configs.
- Addressed Cursor Bugbot feedback on commit `5245eeba` by making the aggregate job fail when any expected split artifact is missing after successful measurement jobs.
- Kept the Phase A0 scoreboard/gate/IL2CPP items open in `PLAN.md`; this change only fixes CI orchestration and artifact reliability.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release` completed with 0 warnings and 0 errors.
- `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release` completed with 0 warnings and 0 errors.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --filter "*NumericLoops*" --launchCount 1 --warmupCount 0 --iterationCount 1 --artifacts artifacts/benchmarkdotnet/split-filter-json-smoke` selected 12 rows and emitted `WallstopStudios.NovaSharp.Comparison.LuaPerformanceBenchmarks-report-full-compressed.json`.
- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir artifacts/benchmarkdotnet/split-lua-cli-json-smoke --output-root artifacts/benchmarkdotnet/split-filter-json-smoke --warmup-count 0 --iteration-count 1 --lua-cmd lua5.4` emitted one reference `lua` CLI row.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --filter "*LuaToClrInterop*" --launchCount 1 --warmupCount 0 --iterationCount 1 --artifacts artifacts/benchmarkdotnet/interop-json-smoke` selected 4 rows and emitted `WallstopStudios.NovaSharp.Comparison.LuaInteropBenchmarks-report-full-compressed.json`.
- `python3 scripts/benchmarks/render-benchmark-deltas.py --current-root BenchmarkDotNet.Artifacts --comparison-root artifacts/benchmarkdotnet/split-filter-json-smoke --self-baseline-root docs/performance-history/current-baseline --output artifacts/benchmark-deltas-split-smoke.md --expect-lua-cli` reported `external_rows=10`, `missing_external_runtime_cells=0`, and `missing_lua_cli_rows=0`.
- `python3 tools/test_render_benchmark_deltas.py` completed with 9 tests passing.
- `python3 tools/test_run_lua_cli_context.py` completed with 6 tests passing.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py scripts/benchmarks/run-lua-cli-context.py tools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py` completed successfully.
- `bash -n scripts/benchmarks/run-benchmarks.sh` completed successfully.
- `pwsh -NoProfile -Command '[scriptblock]::Create((Get-Content -Raw scripts/benchmarks/run-benchmarks.ps1))'` parsed successfully.
- `/tmp/novasharp-actionlint/actionlint .github/workflows/benchmarks.yml` completed successfully after downloading the release binary with `gh release download -R rhysd/actionlint`.
- `git diff --check` completed successfully.
- `./scripts/build/quick.sh` completed successfully.
- `./scripts/test/quick.sh` completed with 14,529 tests passing.
- `bash ./scripts/dev/pre-commit.sh` completed successfully.
- After the Bugbot aggregate-artifact fix, `/tmp/novasharp-actionlint/actionlint .github/workflows/benchmarks.yml`, YAML parsing, and `git diff --check` completed successfully.

## Remaining Work

- Push the Bugbot follow-up commit, trigger GitHub Copilot and Cursor Bugbot reviews, and wait for CI/reviewer feedback.
