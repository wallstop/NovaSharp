# Session 127: Phase A0 Interop and Cached Compile Rows

Date: 2026-07-02

## Summary

- Added cached-compile comparison rows beside the existing cold compile and execute rows for NovaSharp and the third-party managed comparison runtimes.
- Added a dedicated `LuaInteropBenchmarks` suite for the Phase A0 call-boundary matrix:
  - Lua to CLR: one million calls to a registered two-argument `add` callback with a numeric return.
  - CLR to Lua: one million calls to a cached Lua `add(a, b)` function with two arguments and a numeric return.
- Used NovaSharp's stack-only callback view for its Lua-to-CLR row, and each external engine's closest public host-binding API.
- Left reference `lua` CLI scenarios pure-Lua only. Interop rows are host-bound and are intentionally not exported to reference `lua`.
- Added renderer coverage to ensure interop rows do not report missing reference `lua` CLI context when `--expect-lua-cli` is enabled.
- Updated benchmark CI to run the full comparison benchmark assembly so the new interop suite is included.
- Updated local benchmark runner scripts to use the same full comparison benchmark filter as CI.
- Added deterministic result assertions to every interop benchmark row so a broken host binding fails before producing misleading timing data.
- Raised the benchmark workflow timeout to accommodate the larger full comparison matrix without changing benchmark quality.
- Updated `PLAN.md` to mark Phase A0 interop and cached-compile rows complete.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release -v:minimal` completed with 0 warnings and 0 errors.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --list flat` listed the 8 interop rows and the new cached-compile rows.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --filter "*LuaInteropBenchmarks*" --launchCount 1 --warmupCount 0 --iterationCount 1 --artifacts artifacts/benchmarkdotnet/interopsmoke-postassertions` executed all 8 interop rows successfully with deterministic result assertions active.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --filter "*CachedCompile" --launchCount 1 --warmupCount 0 --iterationCount 1 --artifacts artifacts/benchmarkdotnet/cachedcompile-smoke-all` executed all 64 cached-compile rows across NovaSharp and the third-party managed comparison runtimes successfully.
- `python3 tools/test_render_benchmark_deltas.py` completed with 9 tests passing.
- `python3 tools/test_run_lua_cli_context.py` completed with 6 tests passing.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py scripts/benchmarks/run-lua-cli-context.py tools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py` completed successfully.
- `./scripts/build/quick.sh` completed successfully.
- `./scripts/test/quick.sh` completed with 14,529 tests passing.
- `bash ./scripts/dev/pre-commit.sh` completed successfully.

## Remaining Phase A0 Work

- Commit full BenchmarkDotNet JSON baselines under `progress/`.
- Add or extend one command that emits the full Phase A0 scoreboard markdown table.
- Add CI gates for ratio-vs-NLua drift and exact NovaSharp B/op assertions.
- Add the Unity IL2CPP stopwatch spot-check scene.
