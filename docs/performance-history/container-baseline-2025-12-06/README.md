# Container Baseline - 2025-12-06

**Purpose**: Pre-optimization baseline measurements for interpreter hot-path optimization work.

## Environment

```
BenchmarkDotNet v0.15.6, Linux Debian GNU/Linux 12 (bookworm) (container)
Intel Core Ultra 9 285K 3.69GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.308
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
Job=ShortRun  IterationCount=10  LaunchCount=1  WarmupCount=2
```

## Summary Results

### Script Loading Benchmarks

| Method              | Complexity | Mean    | Allocated |
| ------------------- | ---------- | ------- | --------- |
| Execute Precompiled | Tiny       | 148 ns  | 696 B     |
| Execute Precompiled | Small      | 8.4 µs  | 21.5 KB   |
| Execute Precompiled | Medium     | 50 ms   | 170 MB    |
| Execute Precompiled | Large      | 987 ms  | 3.5 GB    |
| Compile Only        | Tiny       | 1.38 ms | 2.7 MB    |
| Compile Only        | Small      | 1.36 ms | 2.7 MB    |
| Compile Only        | Medium     | 1.25 ms | 2.8 MB    |
| Compile Only        | Large      | 1.95 ms | 3.2 MB    |

### Runtime Benchmarks

| Scenario          | Mean    | Allocated |
| ----------------- | ------- | --------- |
| NumericLoops      | 195 ns  | 928 B     |
| CoroutinePipeline | 278 ns  | 1.2 KB    |
| UserDataInterop   | 374 ns  | 1.5 KB    |
| TableMutation     | 5.24 µs | 31 KB     |

## Key Observations

1. **Compilation is allocation-heavy**: Even "Tiny" scripts allocate ~2.7 MB during compilation
1. **Execution scales with complexity**: Precompiled execution ranges from 148 ns (Tiny) to 987 ms (Large)
1. **Runtime operations are fast**: Core scenarios like NumericLoops are under 200 ns
1. **Table operations are slowest**: TableMutation is 25x slower than NumericLoops

## Optimization Targets

Based on this baseline, the following optimizations were implemented:

1. **DynValue.FromBoolean(bool)**: Returns cached True/False instances instead of allocating
1. **DynValue.SmallIntegerCache**: Pre-allocated cache for indices 0-255
1. **DynValue.FromNumber(double)**: Uses small integer cache when applicable

## Files

- `ScriptLoadingBenchmarks-report-full-compressed.json`: Full ScriptLoadingBenchmarks data
- `ScriptLoadingBenchmarks-report-github.md`: GitHub-formatted ScriptLoadingBenchmarks table
- `RuntimeBenchmarks-report-full-compressed.json`: Full RuntimeBenchmarks data
- `RuntimeBenchmarks-report-github.md`: GitHub-formatted RuntimeBenchmarks table

## Context

This baseline was captured **after** the Phase 1 hot-path optimizations were implemented (FromBoolean, SmallIntegerCache, FromNumber caching). It serves as a reference point for measuring additional optimizations.

The optimizations in this session targeted:

- Boolean result caching in VM ToBool, ExecNot, ExecCNot
- Small integer caching for array indices and loop counters
- Direct use of DynValue.Nil instead of allocation in GetStoreValue/ExecArgs
