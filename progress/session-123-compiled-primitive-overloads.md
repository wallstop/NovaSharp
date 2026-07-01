# Session 123: Compiled Primitive Overloads

## Scope

- Make the prepared-handle API easier to use from Unity-style hot loops without falling into boxed `object` argument overloads.
- Keep the change additive and focused on the one-argument `Update(dt)` style path.
- Ensure same-run external benchmark comparisons measure NovaSharp through the prepared-handle path.

## Changes

- Added one-argument primitive overloads to `CompiledScript.Execute` for `double`, `float`, `int`, `long`, and `bool`.
- Added exact `Execute` overloads for `char`, `byte`, `sbyte`, `short`, `ushort`, `uint`, and `ulong` so C# overload resolution does not promote those values into a different Lua conversion.
- Added matching one-argument primitive overloads to `CompiledScript.ExecuteNumber` and `CompiledScript.ExecuteBoolean`.
- Updated the same-run comparison benchmark to compile NovaSharp scenarios with `PrepareString` and execute with `CompiledScript.Execute`.
- Added local benchmark rows comparing prepared-handle `DynValue`, primitive `double`, and forced `object` double argument paths.
- Confirmed the benchmark delta renderer still presents NovaSharp raw results per scenario before external-runtime deltas.
- Added a dedicated narrow NovaSharp raw-results table before the wider same-run runtime matrix so PR comments remain readable when external runtime columns make the matrix wide.
- Added benchmark report copy explaining that NovaSharp comparison rows intentionally use the prepared-handle public API.
- Added regression coverage for primitive overload custom-converter precedence, `char` string conversion, and checked `ulong` conversion.

## Review Fixes

- An adversarial review found that the first overload implementation changed source-level behavior: `Execute('x')` could bind to the numeric overload and pass `120`, and `Execute(42)` bypassed registered primitive custom converters.
- The production fix keeps the ergonomic overloads but routes each primitive through a converter-preserving helper. If no custom converter is registered for that exact CLR type, the helper uses the direct Lua primitive conversion without boxing. If a converter is registered, it wins and may return `null` to fall back to standard conversion.
- The custom-converter regression is intentionally single-version because `Script.GlobalOptions.CustomConverters` is global host state. Running one converter-mutation test per Lua version in parallel corrupted the shared dictionary, so the test now validates the host conversion contract once under Lua 5.4.
- PR CI on macOS later showed the all-version primitive happy-path test could still observe a concurrently registered `int` converter from another test and read a string result as numeric zero. Both primitive overload tests now use `ScriptGlobalOptionsIsolation`; the happy-path test also clears converters inside that isolated scope.

## Measurement Notes

- This slice improves the pit-of-success public API and avoids the per-call boxed `object` converter path when no exact-type custom converter is registered.
- It does not yet make primitive calls zero-allocation. The current implementation still materializes a transient `DynValue` argument before entering the VM.
- The revised benchmark smoke showed cached `DynValue` remains the lowest-allocation path: 456 B/op. Primitive and pre-boxed object paths both allocate 512 B/op under the current VM design; per-call boxed object allocates 536 B/op.
- The same-run comparison harness now measures NovaSharp through the same public prepared-handle path users should prefer: `PrepareString` once, then `CompiledScript.Execute`.
- The PR benchmark matrix renderer keeps NovaSharp readable with raw mean/P95 and allocation/GC columns per scenario, then shows external runtime raw values and NovaSharp-vs-runtime deltas.
- The renderer now repeats those NovaSharp raw values in a dedicated `NovaSharp Raw Results` section before the cross-runtime delta tables, so each scenario is readable without horizontal scanning.

## Validation

- Passed: `dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj -c Release --no-restore`
- Passed: `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release --no-restore`
- Passed: `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-restore`
- Passed: `./scripts/test/quick.sh --full CompiledScript`
- Passed: `./scripts/test/quick.sh --full PrimitiveArgumentOverloads`
- Passed: `./scripts/test/quick.sh --full PrimitiveExecuteHonorsCustomConverters`
- Passed: `python3 tools/test_render_benchmark_deltas.py`
- Passed: `git diff --check`
- Passed: benchmark smoke for prepared double-handle variants under `BoundFunctionBenchmarks`.
- Passed: comparison smoke for all NovaSharp `LuaPerformanceBenchmarks` compile/execute scenarios.
- Passed: `./scripts/dev/pre-commit.sh`
- Passed after review fix: `git diff --check`.
- Passed after review fix: `./scripts/dev/pre-commit.sh`.
- Passed after macOS CI test-isolation fix: `./scripts/test/quick.sh --full PrimitiveArgumentOverloads`.
- Passed after macOS CI test-isolation fix: `./scripts/test/quick.sh --full PrimitiveExecuteHonorsCustomConverters`.
- Passed after macOS CI test-isolation fix: `./scripts/dev/pre-commit.sh`.
- Passed after raw-results report update: `python3 tools/test_render_benchmark_deltas.py`.
- Passed after raw-results report update: `git diff --check`.
- Passed after raw-results report update: `./scripts/dev/pre-commit.sh`.
- Pending: push the raw-results report improvement and observe PR CI.
