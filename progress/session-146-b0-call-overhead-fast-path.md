# Session 146: B0 Call Overhead Fast Path

Date: 2026-07-04

## Summary

- Continued Phase B0 by closing the known facade call overhead gap locally.
- Added a trusted internal `LuaValue` unwrap for callers that already checked the target `LuaEngine`, so ownerless scalar arguments avoid repeated disposed-engine checks on fixed-arity hot paths.
- Routed `LuaFunction.Call(...)` through owner-direct `LuaEngine.CallOwned(...)` helpers instead of re-entering the public `LuaEngine.Call(...)` overloads.
- Preserved resource safety by keeping same-owner and disposed-owner validation for engine-owned values.
- Extended facade smoke coverage so foreign-engine resource arguments are rejected through both `LuaEngine.Call(...)` and the optimized `LuaFunction.Call(...)` path.

## Local Benchmark Evidence

Command:

```bash
NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -- --filter '*B0Facade*'
```

Observed B0 facade ratios after the fast path:

| Scenario | Ratio | Allocations |
| --- | ---: | ---: |
| `LuaEngine.Run Cached` | 0.99x | 464 B, same as `Script.DoString` |
| `LuaEngine.Call`, arity 0 | 0.99x | 192 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 1 | 1.04x | 400 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 2 | 1.04x | 512 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 3 | 1.04x | 624 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 0 | 1.00x | 192 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 1 | 1.02x | 400 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 2 | 1.03x | 512 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 3 | 1.04x | 624 B, same as `Script.Call` |

This local run puts the measured B0 `Run`/fixed-arity `Call` facade overhead within the 5% target. PR benchmark CI still needs to confirm the same threshold before B0 can be marked complete.

## PR Benchmark Evidence

PR benchmark run `28696439454` confirmed the same B0 exit criterion on the GitHub runner:

| Scenario | Ratio | Allocations |
| --- | ---: | ---: |
| `LuaEngine.Run Cached` | 0.97x | 464 B, same as `Script.DoString` |
| `LuaEngine.Call`, arity 0 | 1.03x | 192 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 1 | 1.01x | 400 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 2 | 1.04x | 512 B, same as `Script.Call` |
| `LuaEngine.Call`, arity 3 | 0.93x | 624 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 0 | 1.01x | 192 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 1 | 1.03x | 400 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 2 | 1.02x | 512 B, same as `Script.Call` |
| `LuaFunction.Call`, arity 3 | 0.93x | 624 B, same as `Script.Call` |

This closes the Phase B0 facade `Run`/fixed-arity `Call` 5% overhead exit criterion for the current benchmark surface.

## Validation

- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 42 tests, 0 failures.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeExceptionTUnitTests` passed: 12 tests, 0 failures.
- Local B0 facade BenchmarkDotNet run completed all 14 benchmarks and showed every B0 facade `Run`/fixed-arity `Call` row within 5% of its `Script` baseline.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed: 14,583 tests, 0 failures.
- `bash ./scripts/dev/pre-commit.sh` passed.

## Open Work

- The current benchmark still measures prewrapped `LuaValue` arguments; a follow-up B0 performance hardening slice should add explicit rows for implicit scalar conversion used by per-frame samples.
- CI currently reports B0 facade overhead but does not enforce it as a dedicated B0 gate.
