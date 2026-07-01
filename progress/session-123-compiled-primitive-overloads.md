# Session 123: Compiled Primitive Overloads

## Scope

- Make the prepared-handle API easier to use from Unity-style hot loops without falling into boxed `object` argument overloads.
- Keep the change additive and focused on the one-argument `Update(dt)` style path.
- Ensure same-run external benchmark comparisons measure NovaSharp through the prepared-handle path.

## Changes

- Added one-argument primitive overloads to `CompiledScript.Execute` for `double`, `float`, `int`, `long`, and `bool`.
- Added matching one-argument primitive overloads to `CompiledScript.ExecuteNumber` and `CompiledScript.ExecuteBoolean`.
- Updated the same-run comparison benchmark to compile NovaSharp scenarios with `PrepareString` and execute with `CompiledScript.Execute`.
- Added local benchmark rows comparing prepared-handle `DynValue`, primitive `double`, and forced `object` double argument paths.
- Confirmed the benchmark delta renderer still presents NovaSharp raw results per scenario before external-runtime deltas.

## Measurement Notes

- This slice improves the pit-of-success public API and avoids the per-call boxed `object` converter path for primitive one-argument prepared calls.
- It does not yet make primitive calls zero-allocation. The current implementation still materializes a transient `DynValue` argument before entering the VM.
- The benchmark smoke showed the forced per-call boxed object path is slower and allocates more than the prepared primitive overload. Cached `DynValue` remains the lowest-allocation path until the VM gets a deeper value/register redesign.
- The same-run comparison harness now measures NovaSharp through the same public prepared-handle path users should prefer: `PrepareString` once, then `CompiledScript.Execute`.
- The PR benchmark matrix renderer keeps NovaSharp readable with raw mean/P95 and allocation/GC columns per scenario, then shows external runtime raw values and NovaSharp-vs-runtime deltas.

## Validation

- Passed: `dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj -c Release --no-restore`
- Passed: `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release --no-restore`
- Passed: `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-restore`
- Passed: `./scripts/test/quick.sh --full CompiledScript`
- Passed: `./scripts/test/quick.sh PrimitiveArgumentOverloads`
- Passed: `python3 tools/test_render_benchmark_deltas.py`
- Passed: `git diff --check`
- Passed: benchmark smoke for prepared double-handle variants under `BoundFunctionBenchmarks`.
- Passed: comparison smoke for all NovaSharp `LuaPerformanceBenchmarks` compile/execute scenarios.
- Passed: `./scripts/dev/pre-commit.sh`
- Pending: push and PR CI observation.
