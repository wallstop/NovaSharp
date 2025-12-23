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

### NovaSharp Latest (captured 2025-12-22 17:37:40 -08:00)

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
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=CoroutinePipeline |    229 ns |         247 ns |  -18.7 ns |   -7.55% |    1.17 KB |         1.10 KB |  +72.0 B |    +6.38% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=NumericLoops      |    148 ns |         195 ns |  -46.9 ns |  -24.01% |      864 B |           928 B |  -64.0 B |     -6.9% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=TableMutation     |  3.217 us |       4.205 us |   -989 ns |  -23.51% |    25.3 KB |         30.2 KB | -4.92 KB |   -16.27% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=UserDataInterop   |    260 ns |         295 ns |  -35.4 ns |  -11.98% |    1.42 KB |         1.33 KB |  +96.0 B |    +7.06% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Large           |   1.059 s |         779 ms |   +279 ms |  +35.87% |    3.75 GB |         2.86 GB |  +909 MB |   +31.02% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Medium          |   64.2 ms |        46.7 ms |  +17.5 ms |  +37.46% |     187 MB |          143 MB | +44.0 MB |   +30.66% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Small           |  2.787 ms |       1.693 ms | +1.093 ms |  +64.55% |    2.55 MB |         2.36 MB |  +192 KB |    +7.94% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Tiny            |  1.994 ms |       1.478 ms |   +516 us |  +34.92% |    2.52 MB |         2.33 MB |  +186 KB |    +7.77% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Large           |  2.640 ms |       3.062 ms |   -422 us |  -13.78% |    2.84 MB |         2.80 MB | +40.6 KB |    +1.41% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Medium          |  2.224 ms |       2.161 ms |  +63.9 us |   +2.96% |    2.62 MB |         2.48 MB |  +141 KB |    +5.54% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Small           |  2.208 ms |       1.614 ms |   +594 us |  +36.81% |    2.52 MB |         2.34 MB |  +184 KB |    +7.66% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Tiny            |  1.929 ms |       1.522 ms |   +407 us |  +26.75% |    2.52 MB |         2.33 MB |  +189 KB |    +7.94% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large           |   1.040 s |         746 ms |   +294 ms |  +39.35% |    3.75 GB |         2.86 GB |  +909 MB |   +31.05% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium          |   48.7 ms |        35.6 ms |  +13.1 ms |  +36.96% |     185 MB |          141 MB | +43.8 MB |   +31.11% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small           |   10.6 us |       6.778 us | +3.867 us |  +57.05% |    28.0 KB |         18.7 KB | +9.26 KB |   +49.44% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny            |    142 ns |        96.6 ns |  +45.5 ns |  +47.11% |      616 B |           536 B |  +80.0 B |   +14.93% |

#### WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks-20251222-173338

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                   | ScenarioName          |           Mean |         Error |       StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | -----------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **CoroutinePipeline** |   **228.5 ns** |  **28.14 ns** | **18.61 ns** |   **252.9 ns** | **2** | **0.0637** |      **-** |  **1200 B** |
| **'Scenario Execution'** | **NumericLoops**      |   **148.4 ns** |  **11.94 ns** |  **7.89 ns** |   **158.3 ns** | **1** | **0.0458** |      **-** |   **864 B** |
| **'Scenario Execution'** | **TableMutation**     | **3,216.7 ns** | **137.55 ns** | **71.94 ns** | **3,323.2 ns** | **3** | **1.3771** | **0.1030** | **25928 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **259.9 ns** |  **12.72 ns** |  **6.65 ns** |   **270.4 ns** | **2** | **0.0772** |      **-** |  **1456 B** |

#### WallstopStudios.NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251222-173428

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                  | ComplexityName |                   Mean |                Error |               StdDev |                    P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | -------------- | ---------------------: | -------------------: | -------------------: | ---------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Large**      | **1,058,555,860.0 ns** | **73,488,994.96 ns** | **48,608,420.02 ns** | **1,128,845,355.0 ns** | **6** | **213000.0000** | **1000.0000** |        **-** | **4025104640 B** |
| 'Compile Only'          | Large          |         2,640,340.0 ns |        369,599.52 ns |        244,467.20 ns |         3,026,098.7 ns |     3 |        277.3438 |      265.6250 |     238.2813 |        2979009 B |
| 'Execute Precompiled'   | Large          |     1,040,209,566.7 ns |     48,103,348.70 ns |     28,625,531.24 ns |     1,071,196,040.0 ns |     6 |     213000.0000 |     1000.0000 |            - |     4022008544 B |
| **'Compile + Execute'** | **Medium**     |    **64,155,826.7 ns** |  **4,751,846.60 ns** |  **3,143,052.31 ns** |    **67,977,623.9 ns** | **5** |  **10555.5556** |  **777.7778** | **333.3333** |  **196512777 B** |
| 'Compile Only'          | Medium         |         2,224,455.4 ns |        425,350.72 ns |        281,343.17 ns |         2,612,660.0 ns |     3 |        269.5313 |      265.6250 |     242.1875 |        2745786 B |
| 'Execute Precompiled'   | Medium         |        48,690,814.0 ns |      1,787,717.52 ns |      1,182,464.45 ns |        50,084,259.5 ns |     4 |      10200.0000 |             - |            - |      193654704 B |
| **'Compile + Execute'** | **Small**      |     **2,786,589.2 ns** |    **514,122.86 ns** |    **340,060.44 ns** |     **3,141,490.6 ns** | **3** |    **265.6250** |  **257.8125** | **242.1875** |    **2670271 B** |
| 'Compile Only'          | Small          |         2,208,033.4 ns |        535,268.08 ns |        354,046.70 ns |         2,769,414.7 ns |     3 |        285.1563 |      273.4375 |     265.6250 |        2644198 B |
| 'Execute Precompiled'   | Small          |            10,645.3 ns |            263.15 ns |            137.63 ns |            10,760.4 ns |     2 |          1.5106 |             - |            - |          28656 B |
| **'Compile + Execute'** | **Tiny**       |     **1,994,395.1 ns** |    **295,347.12 ns** |    **175,756.33 ns** |     **2,184,813.7 ns** | **3** |    **277.3438** |  **273.4375** | **257.8125** |    **2637517 B** |
| 'Compile Only'          | Tiny           |         1,928,556.6 ns |        342,023.93 ns |        203,532.96 ns |         2,137,215.4 ns |     3 |        285.1563 |      273.4375 |     265.6250 |        2638901 B |
| 'Execute Precompiled'   | Tiny           |               142.1 ns |             10.62 ns |              6.32 ns |               149.3 ns |     1 |          0.0327 |             - |            - |            616 B |

#### WallstopStudios.NovaSharp.Benchmarks.StringPatternBenchmarks-20251222-173640

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.101
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                        | ScenarioName         |          Mean |         Error |        StdDev |           P95 |  Rank |        Gen0 |       Gen1 |      Allocated |
| ----------------------------- | -------------------- | ------------: | ------------: | ------------: | ------------: | ----: | ----------: | ---------: | -------------: |
| **'String Pattern Scenario'** | **FormatMultiple**   | **444.06 μs** | **35.638 μs** | **21.208 μs** | **466.11 μs** | **3** | **71.2891** | **0.9766** | **1314.93 KB** |
| **'String Pattern Scenario'** | **GsubSimple**       | **186.10 μs** |  **8.813 μs** |  **5.244 μs** | **193.66 μs** | **2** | **23.6816** | **0.2441** |  **436.05 KB** |
| **'String Pattern Scenario'** | **GsubWithCaptures** | **420.62 μs** | **48.397 μs** | **32.012 μs** | **472.25 μs** | **3** | **41.9922** | **0.4883** |  **777.41 KB** |
| **'String Pattern Scenario'** | **MatchComplex**     | **163.84 μs** |  **1.220 μs** |  **0.638 μs** | **164.56 μs** | **2** | **11.4746** |      **-** |  **211.45 KB** |
| **'String Pattern Scenario'** | **MatchSimple**      |  **64.79 μs** |  **4.353 μs** |  **2.879 μs** |  **68.75 μs** | **1** | **10.2539** |      **-** |  **190.06 KB** |

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
