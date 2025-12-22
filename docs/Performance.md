# NovaSharp Performance Baselines

This document tracks benchmark snapshots used for regression analysis. Each OS section must follow this order:

1. `### NovaSharp Latest` – contains the NovaSharp vs MoonSharp comparison table at the top plus the detailed NovaSharp benchmark output.
1. `### MoonSharp Baseline` – a frozen point-in-time snapshot (do not overwrite).

When capturing new data, replace the entire `### NovaSharp Latest` section instead of appending new content to the top of the file.

## Benchmark Governance

### Workflow

- Restore tools once per clone with `dotnet tool restore`, then build the solution via `dotnet build src/NovaSharp.sln -c Release` to ensure dependencies are hot.
- Run `pwsh ./scripts/benchmarks/run-benchmarks.ps1` to restore tools, build the solution, and execute both the runtime and comparison BenchmarkDotNet suites. The script wraps the manual commands described below and prints the artifact locations when it completes.
- When you need to run only one suite manually, use the underlying commands:
  - Runtime benchmarks: `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release`
  - NLua/MoonSharp comparison: `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release`
- The runtime harness updates the `### NovaSharp Latest` block automatically; the comparison harness refreshes the NLua/MoonSharp tables referenced later in this document.
- After each run, inspect the generated markdown diff in this file and attach the raw BenchmarkDotNet artifacts (`BenchmarkDotNet.Artifacts`) to the PR when results change materially.

### Thresholds

- Treat any >5 % regression in `Mean` or `P95` for `RuntimeBenchmarks` scenarios as blocking unless a tracking issue documents the deviation.
- Treat any >10 % growth in `Allocated` for the same scenarios as blocking; smaller regressions should at least receive a follow-up issue.
- Record mitigations in the PR description and cross-link the relevant benchmark rows so reviewers can validate the impact.

### Reporting Cadence

- Run the governance workflow before each release branch cut, before merging large performance-sensitive features, and at least once per calendar month to keep the baseline fresh.
- When the cadence run produces no changes, add a short note to the release journal or sprint recap so the team can confirm the guardrail fired.
- If regressions are observed, file an issue tagged `performance-regression` within the same working session to keep the trail warm.

### Comparison Runs

- Summarize NLua parity measurements in `docs/Performance.md` by updating the comparison tables and noting the timestamp in the commit message.
- Mirror the headline deltas (fastest/slowest scenarios and any regressions) into the release notes for the corresponding milestone so downstream consumers understand compatibility impacts.
- Archive historical comparison snapshots in `docs/performance-history/` (create a date-stamped markdown file when results materially change) to keep the main document focused on current data.

## Windows

### NovaSharp Latest (captured 2025-12-21 19:15:01 -08:00)

**Environment**

- OS: Windows 11 Pro 25H2 (build 26200.7462, 10.0.26200)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.22 (8.0.22, 8.0.2225.52707)
- Approx. RAM: 195,968 MB
- Suite: NovaSharp Benchmarks

**Delta vs MoonSharp baseline**

| Summary                                      | Method              | Parameters                 | Nova Mean | MoonSharp Mean |    Mean Δ | Mean Δ % | Nova Alloc | MoonSharp Alloc |  Alloc Δ | Alloc Δ % |
| -------------------------------------------- | ------------------- | -------------------------- | --------: | -------------: | --------: | -------: | ---------: | --------------: | -------: | --------: |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=CoroutinePipeline |    219 ns |         247 ns |  -28.4 ns |  -11.48% |    1.07 KB |         1.10 KB |  -32.0 B |    -2.84% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=NumericLoops      |    127 ns |         195 ns |  -68.5 ns |  -35.09% |      760 B |           928 B |   -168 B |    -18.1% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=TableMutation     |  3.355 us |       4.205 us |   -851 ns |  -20.23% |    25.2 KB |         30.2 KB | -5.02 KB |   -16.61% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=UserDataInterop   |    233 ns |         295 ns |  -62.3 ns |  -21.09% |    1.32 KB |         1.33 KB |  -8.00 B |    -0.59% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Large           |   1.044 s |         779 ms |   +265 ms |  +34.02% |    3.41 GB |         2.86 GB |  +564 MB |   +19.25% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Medium          |   62.7 ms |        46.7 ms |  +16.0 ms |  +34.38% |     171 MB |          143 MB | +27.3 MB |   +19.07% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Small           |  2.863 ms |       1.693 ms | +1.170 ms |  +69.08% |    2.55 MB |         2.36 MB |  +192 KB |    +7.96% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Tiny            |  2.044 ms |       1.478 ms |   +566 us |  +38.26% |    2.52 MB |         2.33 MB |  +192 KB |    +8.05% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Large           |  2.772 ms |       3.062 ms |   -291 us |   -9.49% |    2.87 MB |         2.80 MB | +69.5 KB |    +2.42% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Medium          |  2.231 ms |       2.161 ms |  +70.5 us |   +3.26% |    2.63 MB |         2.48 MB |  +155 KB |    +6.09% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Small           |  2.449 ms |       1.614 ms |   +835 us |  +51.71% |    2.52 MB |         2.34 MB |  +185 KB |    +7.73% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Tiny            |  2.100 ms |       1.522 ms |   +578 us |  +38.01% |    2.53 MB |         2.33 MB |  +199 KB |    +8.32% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large           |    975 ms |         746 ms |   +228 ms |  +30.55% |    3.41 GB |         2.86 GB |  +564 MB |   +19.27% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium          |   57.2 ms |        35.6 ms |  +21.7 ms |  +61.02% |     168 MB |          141 MB | +27.2 MB |    +19.3% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small           |   10.5 us |       6.778 us | +3.768 us |  +55.58% |    23.8 KB |         18.7 KB | +5.09 KB |    +27.2% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny            |    121 ns |        96.6 ns |  +24.6 ns |  +25.42% |      512 B |           536 B |  -24.0 B |    -4.48% |

#### WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks-20251221-191040

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                   | ScenarioName          |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **CoroutinePipeline** |   **218.8 ns** |  **69.17 ns** |  **45.75 ns** |   **291.8 ns** | **2** | **0.0582** |      **-** |  **1096 B** |
| **'Scenario Execution'** | **NumericLoops**      |   **126.8 ns** |   **7.37 ns** |   **3.86 ns** |   **130.8 ns** | **1** | **0.0403** |      **-** |   **760 B** |
| **'Scenario Execution'** | **TableMutation**     | **3,354.7 ns** | **298.80 ns** | **156.28 ns** | **3,569.3 ns** | **3** | **1.3657** | **0.0992** | **25824 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **233.0 ns** |  **31.64 ns** |  **16.55 ns** |   **258.6 ns** | **2** | **0.0718** |      **-** |  **1352 B** |

#### WallstopStudios.NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251221-191132

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                  | ComplexityName |                   Mean |                 Error |                StdDev |                    P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | -------------- | ---------------------: | --------------------: | --------------------: | ---------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Large**      | **1,044,165,955.6 ns** | **251,256,021.32 ns** | **149,518,428.18 ns** | **1,311,421,240.0 ns** | **5** | **194000.0000** | **1000.0000** |        **-** | **3663576496 B** |
| 'Compile Only'          | Large          |         2,771,886.9 ns |         362,828.77 ns |         215,913.58 ns |         3,121,253.2 ns |     3 |        343.7500 |      332.0313 |     304.6875 |        3008597 B |
| 'Execute Precompiled'   | Large          |       974,520,262.5 ns |     132,361,239.31 ns |      69,227,487.52 ns |     1,088,450,155.0 ns |     5 |     194000.0000 |             - |            - |     3660464640 B |
| **'Compile + Execute'** | **Medium**     |    **62,716,886.0 ns** |  **16,492,248.79 ns** |  **10,908,601.44 ns** |    **78,146,283.0 ns** | **4** |   **9700.0000** |  **900.0000** | **400.0000** |  **179078043 B** |
| 'Compile Only'          | Medium         |         2,231,085.4 ns |         237,952.52 ns |         141,601.73 ns |         2,341,314.1 ns |     3 |        296.8750 |      277.3438 |     269.5313 |        2760198 B |
| 'Execute Precompiled'   | Medium         |        57,244,602.0 ns |      16,396,164.85 ns |      10,845,047.86 ns |        71,429,688.0 ns |     4 |       9300.0000 |             - |            - |      176215808 B |
| **'Compile + Execute'** | **Small**      |     **2,863,283.8 ns** |     **735,796.07 ns** |     **486,683.54 ns** |     **3,289,055.6 ns** | **3** |    **257.8125** |  **246.0938** | **234.3750** |    **2670707 B** |
| 'Compile Only'          | Small          |         2,448,524.9 ns |         755,095.04 ns |         499,448.62 ns |         3,143,569.4 ns |     3 |        253.9063 |      246.0938 |     234.3750 |        2645814 B |
| 'Execute Precompiled'   | Small          |            10,546.0 ns |             498.99 ns |             330.05 ns |            10,958.9 ns |     2 |          1.2817 |             - |            - |          24392 B |
| **'Compile + Execute'** | **Tiny**       |     **2,043,776.3 ns** |     **370,754.49 ns** |     **245,231.14 ns** |     **2,294,221.1 ns** | **3** |    **285.1563** |  **281.2500** | **265.6250** |    **2644339 B** |
| 'Compile Only'          | Tiny           |         2,099,905.6 ns |         559,022.52 ns |         369,758.79 ns |         2,627,606.4 ns |     3 |        320.3125 |      316.4063 |     300.7813 |        2648424 B |
| 'Execute Precompiled'   | Tiny           |               121.2 ns |              25.12 ns |              16.62 ns |               146.6 ns |     1 |          0.0272 |             - |            - |            512 B |

#### WallstopStudios.NovaSharp.Benchmarks.StringPatternBenchmarks-20251221-191348

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                        | ScenarioName         |          Mean |         Error |       StdDev |           P95 |  Rank |        Gen0 |       Gen1 |      Allocated |
| ----------------------------- | -------------------- | ------------: | ------------: | -----------: | ------------: | ----: | ----------: | ---------: | -------------: |
| **'String Pattern Scenario'** | **FormatMultiple**   | **444.36 μs** | **107.34 μs** | **63.88 μs** | **547.59 μs** | **3** | **70.8008** | **0.9766** | **1307.63 KB** |
| **'String Pattern Scenario'** | **GsubSimple**       | **198.73 μs** |  **48.94 μs** | **32.37 μs** | **253.82 μs** | **2** | **23.1934** | **0.2441** |  **428.76 KB** |
| **'String Pattern Scenario'** | **GsubWithCaptures** | **433.54 μs** |  **81.94 μs** | **54.20 μs** | **523.16 μs** | **3** | **41.5039** | **0.4883** |  **770.11 KB** |
| **'String Pattern Scenario'** | **MatchComplex**     | **188.63 μs** |  **53.34 μs** | **35.28 μs** | **237.52 μs** | **2** | **10.9863** |      **-** |  **204.15 KB** |
| **'String Pattern Scenario'** | **MatchSimple**      |  **70.61 μs** |  **16.87 μs** | **11.16 μs** |  **88.80 μs** | **1** |  **9.8877** | **0.0610** |  **182.77 KB** |

To refresh this section, run:

```
dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj
```

Then replace everything under `### NovaSharp Latest` with the new results.

______________________________________________________________________

### MoonSharp Baseline (captured 2025-11-08 16:54:46 -08:00)

_MoonSharp baseline cloned from the NovaSharp measurements executed on 2025-11-08 16:54:46 -08:00._

### MoonSharp.Comparison.LuaPerformanceBenchmarks-20251108-165221

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
\[Host\]     : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
Comparison : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=Comparison  IterationCount=10  LaunchCount=1\
WarmupCount=2

```

| Method                  | Scenario              |             Mean |              P95 |  Rank |         Gen0 |         Gen1 |         Gen2 |     Allocated |
| ----------------------- | --------------------- | ---------------: | ---------------: | ----: | -----------: | -----------: | -----------: | ------------: |
| **'MoonSharp Compile'** | **TowerOfHanoi**      | **1,674.957 μs** | **1,777.867 μs** | **8** | **238.2813** | **232.4219** | **226.5625** | **2468432 B** |
| 'MoonSharp Execute'     | TowerOfHanoi          |     7,175.532 μs |     7,599.155 μs |    10 |    1882.8125 |      15.6250 |            - |    35521179 B |
| 'NLua Compile'          | TowerOfHanoi          |         5.218 μs |         5.540 μs |     1 |       0.0229 |       0.0153 |            - |         560 B |
| 'NLua Execute'          | TowerOfHanoi          |       430.154 μs |       481.635 μs |     6 |            - |            - |            - |          24 B |
| **'MoonSharp Compile'** | **EightQueens**       | **1,852.369 μs** | **2,153.248 μs** | **8** | **259.7656** | **253.9063** | **244.1406** | **2519168 B** |
| 'MoonSharp Execute'     | EightQueens           |       978.524 μs |     1,126.261 μs |     7 |     185.5469 |      13.6719 |            - |     3496441 B |
| 'NLua Compile'          | EightQueens           |        10.805 μs |        11.553 μs |     3 |       0.0610 |       0.0458 |            - |        1184 B |
| 'NLua Execute'          | EightQueens           |        82.167 μs |        87.591 μs |     4 |            - |            - |            - |          24 B |
| **'MoonSharp Compile'** | **CoroutinePingPong** | **1,642.332 μs** | **1,756.526 μs** | **8** | **250.0000** | **240.2344** | **236.3281** | **2491323 B** |
| 'MoonSharp Execute'     | CoroutinePingPong     |     3,240.333 μs |     3,379.492 μs |     9 |     250.0000 |     222.6563 |     222.6563 |     2632137 B |
| 'NLua Compile'          | CoroutinePingPong     |         9.677 μs |        10.476 μs |     2 |       0.0458 |       0.0305 |            - |         920 B |
| 'NLua Execute'          | CoroutinePingPong     |       197.683 μs |       219.349 μs |     5 |            - |            - |            - |          24 B |

### MoonSharp.Benchmarks.RuntimeBenchmarks-20251108-164841

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
\[Host\]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1\
WarmupCount=2

```

| Method                   | Scenario              |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **NumericLoops**      |   **195.3 ns** |  **12.84 ns** |   **8.50 ns** |   **208.9 ns** | **1** | **0.0491** |      **-** |   **928 B** |
| **'Scenario Execution'** | **TableMutation**     | **4,205.5 ns** | **454.08 ns** | **300.34 ns** | **4,562.9 ns** | **4** | **1.6403** | **0.1450** | **30968 B** |
| **'Scenario Execution'** | **CoroutinePipeline** |   **247.2 ns** |  **24.00 ns** |  **15.87 ns** |   **268.8 ns** | **2** | **0.0596** |      **-** |  **1128 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **295.3 ns** |  **14.82 ns** |   **8.82 ns** |   **308.2 ns** | **3** | **0.0720** |      **-** |  **1360 B** |

### MoonSharp.Benchmarks.ScriptLoadingBenchmarks-20251108-164922

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
\[Host\]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1\
WarmupCount=2

```

| Method                  | Complexity |                  Mean |                 Error |                StdDev |                  P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | ---------- | --------------------: | --------------------: | --------------------: | -------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Tiny**   |   **1,478,166.58 ns** |    **244,461.723 ns** |    **161,696.294 ns** |   **1,718,837.4 ns** | **3** |    **255.8594** |  **248.0469** | **244.1406** |    **2447292 B** |
| 'Compile Only'          | Tiny       |       1,521,540.88 ns |        158,137.487 ns |        104,598.157 ns |       1,661,218.0 ns |     3 |        236.3281 |      230.4688 |     224.6094 |        2444896 B |
| 'Execute Precompiled'   | Tiny       |              96.62 ns |              7.261 ns |              4.803 ns |             104.3 ns |     1 |          0.0284 |             - |            - |            536 B |
| **'Compile + Execute'** | **Small**  |   **1,693,482.92 ns** |    **130,447.113 ns** |     **77,626.985 ns** |   **1,803,269.0 ns** | **3** |    **250.0000** |  **242.1875** | **238.2813** |    **2473905 B** |
| 'Compile Only'          | Small      |       1,613,933.16 ns |        291,098.906 ns |        192,543.903 ns |       1,893,643.4 ns |     3 |        265.6250 |      259.7656 |     253.9063 |        2456059 B |
| 'Execute Precompiled'   | Small      |           6,778.39 ns |            298.463 ns |            197.415 ns |           7,041.4 ns |     2 |          1.0147 |             - |            - |          19176 B |
| **'Compile + Execute'** | **Medium** |  **46,672,822.50 ns** |  **5,427,202.299 ns** |  **3,589,758.290 ns** |  **51,668,609.6 ns** | **7** |   **8250.0000** |  **916.6667** | **416.6667** |  **150400357 B** |
| 'Compile Only'          | Medium     |       2,160,586.60 ns |        227,705.364 ns |        150,613.000 ns |       2,389,209.8 ns |     4 |        251.9531 |      248.0469 |     232.4219 |        2601683 B |
| 'Execute Precompiled'   | Medium     |      35,550,519.26 ns |      1,968,523.972 ns |      1,171,437.040 ns |      37,547,669.3 ns |     6 |       7800.0000 |             - |            - |      147707363 B |
| **'Compile + Execute'** | **Large**  | **779,093,000.00 ns** | **66,814,499.935 ns** | **44,193,654.815 ns** | **838,759,330.0 ns** | **8** | **163000.0000** | **1000.0000** |        **-** | **3072062728 B** |
| 'Compile Only'          | Large      |       3,062,434.41 ns |        448,910.023 ns |        296,926.186 ns |       3,403,041.4 ns |     5 |        312.5000 |      292.9688 |     277.3438 |        2937461 B |
| 'Execute Precompiled'   | Large      |     746,448,587.50 ns |     22,981,568.876 ns |     12,019,804.892 ns |     761,896,265.0 ns |     8 |     163000.0000 |             - |            - |     3069038760 B |

To refresh this section, run:

```

dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj

```

Then replace everything under `### NovaSharp Latest` with the new results.

______________________________________________________________________

## Linux

### NovaSharp Latest (captured 2025-12-22 01:34:11 +00:00)

**Environment**

- OS: Debian GNU/Linux 12 (bookworm)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.22 (8.0.22, 8.0.2225.52707)
- Approx. RAM: 96,242 MB
- Suite: NovaSharp Benchmarks

#### WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks-20251222-013322

```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm) (container)
Intel Core Ultra 9 285K 3.69GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  Dry      : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

LaunchCount=1  

```

| Method                   | Job      | IterationCount | RunStrategy   | UnrollFactor | WarmupCount | ScenarioName          |               Mean |     Error |      StdDev |                P95 |  Rank |   Gen0 |   Gen1 |   Allocated |
| ------------------------ | -------- | -------------- | ------------- | ------------ | ----------- | --------------------- | -----------------: | --------: | ----------: | -----------------: | ----: | -----: | -----: | ----------: |
| **'Scenario Execution'** | **Dry**  | **1**          | **ColdStart** | **1**        | **1**       | **CoroutinePipeline** | **7,648,293.0 ns** |    **NA** | **0.00 ns** | **7,648,293.0 ns** | **8** |  **-** |  **-** |  **1096 B** |
| 'Scenario Execution'     | ShortRun | 10             | Default       | 16           | 2           | CoroutinePipeline     |           225.0 ns |  40.88 ns |    27.04 ns |           263.8 ns |     2 | 0.0582 |      - |      1096 B |
| **'Scenario Execution'** | **Dry**  | **1**          | **ColdStart** | **1**        | **1**       | **NumericLoops**      | **5,327,933.0 ns** |    **NA** | **0.00 ns** | **5,327,933.0 ns** | **5** |  **-** |  **-** |   **760 B** |
| 'Scenario Execution'     | ShortRun | 10             | Default       | 16           | 2           | NumericLoops          |           147.4 ns |  25.69 ns |    16.99 ns |           171.7 ns |     1 | 0.0403 |      - |       760 B |
| **'Scenario Execution'** | **Dry**  | **1**          | **ColdStart** | **1**        | **1**       | **TableMutation**     | **7,285,042.0 ns** |    **NA** | **0.00 ns** | **7,285,042.0 ns** | **7** |  **-** |  **-** | **25824 B** |
| 'Scenario Execution'     | ShortRun | 10             | Default       | 16           | 2           | TableMutation         |         4,259.6 ns | 917.91 ns |   607.14 ns |         4,946.5 ns |     4 | 1.3695 | 0.0992 |     25824 B |
| **'Scenario Execution'** | **Dry**  | **1**          | **ColdStart** | **1**        | **1**       | **UserDataInterop**   | **6,433,316.0 ns** |    **NA** | **0.00 ns** | **6,433,316.0 ns** | **6** |  **-** |  **-** |  **1352 B** |
| 'Scenario Execution'     | ShortRun | 10             | Default       | 16           | 2           | UserDataInterop       |           286.3 ns |  50.62 ns |    33.48 ns |           338.5 ns |     3 | 0.0715 |      - |      1352 B |

To refresh this section, run:

```
dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj
```

Then replace everything under `### NovaSharp Latest` with the new results.

______________________________________________________________________

_No MoonSharp baseline recorded yet._

## macOS

_No benchmark data recorded yet._

______________________________________________________________________

```

```
