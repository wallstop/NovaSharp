# Session 144: B0 Samples and Overhead

Date: 2026-07-04

## Summary

- Continued Phase B0 after the first facade PR reached a green current-head state.
- Added B0 facade overhead BenchmarkDotNet cases to the existing runtime benchmark project:
  - `RuntimeBenchmarksB0FacadeRunOverhead` compares cached `Script.DoString` to cached `LuaEngine.Run`.
  - `RuntimeBenchmarksB0FacadeCallOverhead` compares fixed-arity `Script.Call` to `LuaEngine.Call` and `LuaFunction.Call` across arities 0-3.
- Added a runnable .NET sample project at `src/samples/WallstopStudios.NovaSharp.B0Samples` covering:
  - hello world with facade `Print` capture and scalar return reads;
  - per-frame-style cached `LuaFunction.Call(deltaTime)` usage;
  - sandboxed host globals using `LuaEngineOptions.HardSandbox`.
- Refreshed the Unity `BasicUsage` sample to use the root `NovaSharp` facade API instead of the old `Script`/`DynValue` surface.
- Updated `PLAN.md` to mark the public API baseline and sample compile/run B0 items complete while keeping exception contracts and the 5% overhead exit criterion open.

## Validation

- `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release` passed with 0 warnings and 0 errors.
- `dotnet build src/samples/WallstopStudios.NovaSharp.B0Samples/WallstopStudios.NovaSharp.B0Samples.csproj -c Release` passed with 0 warnings and 0 errors.
- `dotnet run --project src/samples/WallstopStudios.NovaSharp.B0Samples/WallstopStudios.NovaSharp.B0Samples.csproj -c Release --no-build` passed and produced:
  - `hello: Hello from Lua!; answer=42`
  - `per-frame: elapsed=2.000`
  - `sandbox: host=NovaSharp; io=nil; load=nil`
- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -- --filter '*B0Facade*'` completed for 14 benchmark cases.
  - Cached `LuaEngine.Run` measured 0.97x the cached `Script.DoString` baseline with the same 464 B allocation.
  - Fixed-arity `LuaEngine.Call` measured 0.96x, 0.97x, 1.16x, and 0.91x the `Script.Call` baseline for arities 0-3 with matching allocation counts.
  - Fixed-arity `LuaFunction.Call` measured 1.02x, 1.16x, 0.89x, and 0.82x the `Script.Call` baseline for arities 0-3 with matching allocation counts.
  - These first B0 measurements add the requested guardrail but do not close the 5% overhead exit criterion yet because some call rows are above 1.05x and the short-run variance is high.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 40 tests, 0 failures.
- `./scripts/test/quick.sh` passed: 14,569 tests, 0 failures.
- `git diff --check` passed with only expected line-ending warnings for touched files.
- `bash ./scripts/dev/pre-commit.sh` completed successfully and refreshed the naming audit counts for the new sample types.

## Open Work

- Investigate the fixed-arity call rows above 1.05x before marking the B0 5% overhead criterion complete.
