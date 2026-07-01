# Session 121: Same-Run Comparison Deltas

## Scope

- Replace frozen Markdown MoonSharp baseline deltas in benchmark CI with same-run BenchmarkDotNet artifact comparisons.
- Expand external runtime coverage from NLua-only comparison benchmarks to NovaSharp, MoonSharp, and NLua across compile and execute scenarios.
- Add a self-comparison lane that activates when checked-in NovaSharp BenchmarkDotNet artifacts are added under `docs/performance-history/current-baseline`.

## Changes

- Added the MoonSharp NuGet package to the comparison tooling and added MoonSharp compile/execute benchmark methods.
- Expanded the comparison script corpus with numeric loop and table mutation scenarios in addition to Tower of Hanoi, Eight Queens, and coroutine ping-pong.
- Reworked `scripts/benchmarks/render-benchmark-deltas.py` to parse BenchmarkDotNet JSON artifacts directly, render mean, P95, Gen0/Gen1/Gen2, and allocated-byte deltas, and emit external/self row counts for CI.
- Kept external runtime deltas report-only; `regressed=true` is reserved for NovaSharp self-baseline regressions once checked-in baseline artifacts exist.
- Updated `.github/workflows/benchmarks.yml` so runtime and external comparison benchmarks run in the same benchmark job and same runner, while the legacy `comparison` check is now a proxy for branch-protection continuity.
- Updated local benchmark scripts and docs to render same-run external deltas and optional checked-in self deltas.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-restore`
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py tools/test_render_benchmark_deltas.py`
- `python3 -m unittest tools/test_render_benchmark_deltas.py`
- `bash -n scripts/benchmarks/run-benchmarks.sh`
- PowerShell parser check for `scripts/benchmarks/run-benchmarks.ps1`
- BenchmarkDotNet smoke: `MoonSharp Execute` comparison benchmarks ran for all five scenarios with separate artifacts under `artifacts/comparison-smoke`.
