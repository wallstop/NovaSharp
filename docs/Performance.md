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
  - Runtime benchmarks: `dotnet run --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj -c Release`
  - NLua/MoonSharp comparison: `dotnet run --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj -c Release`
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

### NovaSharp Latest (captured 2025-11-24 20:25:30 -08:00)

**Environment**

- OS: Windows 11 Pro 25H2 (build 26200.7171, 10.0.26200)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.22 (8.0.22, 8.0.2225.52707)
- Approx. RAM: 195,968 MB
- Suite: NovaSharp Benchmarks

**Delta vs MoonSharp baseline**

| Summary                                      | Method              | Parameters                 | Nova Mean | MoonSharp Mean |    Mean Δ | Mean Δ % | Nova Alloc | MoonSharp Alloc |  Alloc Δ | Alloc Δ % |
| -------------------------------------------- | ------------------- | -------------------------- | --------: | -------------: | --------: | -------: | ---------: | --------------: | -------: | --------: |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=CoroutinePipeline |    250 ns |         247 ns | +2.833 ns |   +1.15% |    1.22 KB |         1.10 KB |   +120 B |   +10.64% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=NumericLoops      |    216 ns |         195 ns |  +20.3 ns |  +10.41% |      976 B |           928 B |  +48.0 B |    +5.17% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=TableMutation     |  5.568 us |       4.205 us | +1.363 us |   +32.4% |    30.4 KB |         30.2 KB |   +120 B |    +0.39% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=UserDataInterop   |    342 ns |         295 ns |  +46.6 ns |  +15.78% |    1.45 KB |         1.33 KB |   +120 B |    +8.82% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Large           |   1.082 s |         779 ms |   +303 ms |  +38.85% |    3.29 GB |         2.86 GB |  +440 MB |      +15% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Medium          |   64.0 ms |        46.7 ms |  +17.3 ms |  +37.13% |     165 MB |          143 MB | +21.4 MB |   +14.95% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Small           |  2.711 ms |       1.693 ms | +1.017 ms |  +60.08% |    2.57 MB |         2.36 MB |  +217 KB |       +9% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Tiny            |  2.550 ms |       1.478 ms | +1.072 ms |   +72.5% |    2.55 MB |         2.33 MB |  +218 KB |    +9.14% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Large           |  3.499 ms |       3.062 ms |   +437 us |  +14.26% |    3.02 MB |         2.80 MB |  +223 KB |    +7.77% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Medium          |  2.917 ms |       2.161 ms |   +756 us |  +34.99% |    2.70 MB |         2.48 MB |  +224 KB |    +8.82% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Small           |  2.371 ms |       1.614 ms |   +757 us |  +46.93% |    2.56 MB |         2.34 MB |  +218 KB |     +9.1% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Tiny            |  2.418 ms |       1.522 ms |   +896 us |  +58.89% |    2.54 MB |         2.33 MB |  +216 KB |    +9.04% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large           |   1.100 s |         746 ms |   +353 ms |  +47.32% |    3.29 GB |         2.86 GB |  +439 MB |   +15.01% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium          |   53.5 ms |        35.6 ms |  +18.0 ms |  +50.55% |     162 MB |          141 MB | +21.2 MB |   +15.06% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small           |  8.785 us |       6.778 us | +2.007 us |   +29.6% |    21.1 KB |         18.7 KB | +2.35 KB |   +12.56% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny            |    155 ns |        96.6 ns |  +58.1 ns |  +60.16% |      744 B |           536 B |   +208 B |   +38.81% |

#### NovaSharp.Benchmarks.RuntimeBenchmarks-20251124-202224

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.7171)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                   | Scenario              |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **NumericLoops**      |   **215.6 ns** |  **22.19 ns** |  **14.68 ns** |   **234.4 ns** | **1** | **0.0517** |      **-** |   **976 B** |
| **'Scenario Execution'** | **TableMutation**     | **5,568.1 ns** | **860.09 ns** | **568.90 ns** | **6,318.8 ns** | **3** | **1.6479** | **0.1450** | **31088 B** |
| **'Scenario Execution'** | **CoroutinePipeline** |   **250.0 ns** |  **14.76 ns** |   **9.76 ns** |   **263.8 ns** | **1** | **0.0663** |      **-** |  **1248 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **341.9 ns** |  **76.10 ns** |  **50.34 ns** |   **410.3 ns** | **2** | **0.0782** |      **-** |  **1480 B** |

#### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251124-202310

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.7171)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                  | Complexity |                   Mean |                Error |               StdDev |                    P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | ---------- | ---------------------: | -------------------: | -------------------: | ---------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Tiny**   |     **2,549,858.3 ns** |    **405,842.71 ns** |    **268,439.83 ns** |     **2,879,066.7 ns** | **3** |    **261.7188** |  **246.0938** | **238.2813** |    **2670933 B** |
| 'Compile Only'          | Tiny       |         2,417,579.2 ns |        488,740.94 ns |        323,271.87 ns |         2,916,277.6 ns |     3 |        222.6563 |      210.9375 |     199.2188 |        2665885 B |
| 'Execute Precompiled'   | Tiny       |               154.7 ns |              8.05 ns |              5.32 ns |               160.8 ns |     1 |          0.0393 |             - |            - |            744 B |
| **'Compile + Execute'** | **Small**  |     **2,710,978.8 ns** |    **543,449.71 ns** |    **359,458.33 ns** |     **3,127,618.2 ns** | **3** |    **222.6563** |  **210.9375** | **199.2188** |    **2696505 B** |
| 'Compile Only'          | Small      |         2,371,346.4 ns |        443,072.35 ns |        293,064.93 ns |         2,780,601.6 ns |     3 |        265.6250 |      250.0000 |     242.1875 |        2679562 B |
| 'Execute Precompiled'   | Small      |             8,784.9 ns |            913.45 ns |            604.19 ns |             9,683.5 ns |     2 |          1.1444 |             - |            - |          21584 B |
| **'Compile + Execute'** | **Medium** |    **64,002,946.2 ns** |  **7,719,261.45 ns** |  **5,105,813.51 ns** |    **69,299,736.2 ns** | **6** |   **9375.0000** |  **875.0000** | **375.0000** |  **172884481 B** |
| 'Compile Only'          | Medium     |         2,916,540.5 ns |        553,616.11 ns |        366,182.78 ns |         3,405,673.9 ns |     3 |        292.9688 |      277.3438 |     261.7188 |        2831067 B |
| 'Execute Precompiled'   | Medium     |        53,521,065.0 ns |      5,445,809.45 ns |      3,602,065.77 ns |        58,444,787.5 ns |     5 |       9000.0000 |             - |            - |      169958368 B |
| **'Compile + Execute'** | **Large**  | **1,081,750,877.8 ns** | **93,090,441.23 ns** | **55,396,628.42 ns** | **1,137,697,280.0 ns** | **7** | **187000.0000** | **1000.0000** |        **-** | **3532958960 B** |
| 'Compile Only'          | Large      |         3,499,225.0 ns |        351,376.83 ns |        209,098.71 ns |         3,782,692.3 ns |     4 |        296.8750 |      285.1563 |     250.0000 |        3165561 B |
| 'Execute Precompiled'   | Large      |     1,099,652,310.0 ns |    180,432,492.05 ns |    119,344,921.85 ns |     1,286,907,595.0 ns |     7 |     187000.0000 |             - |            - |     3529692384 B |

To refresh this section, run:

```
dotnet run -c Release --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj
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
  [Host]     : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  Comparison : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=Comparison  IterationCount=10  LaunchCount=1  
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
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1  
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
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1  
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
dotnet run -c Release --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj
```

Then replace everything under `### NovaSharp Latest` with the new results.

______________________________________________________________________

## Linux

_No benchmark data recorded yet._

## macOS

_No benchmark data recorded yet._

______________________________________________________________________
