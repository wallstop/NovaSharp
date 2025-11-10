# NovaSharp Performance Baselines

This document tracks benchmark snapshots used for regression analysis. Maintain exactly two sections:

- A frozen MoonSharp baseline (do not overwrite)
- The most recent NovaSharp benchmark run

When capturing new data, replace the entire `### NovaSharp Latest` section instead of appending new content to the top of the file.


## Benchmark Governance

### Workflow
- Restore tools once per clone with `dotnet tool restore`, then build the solution via `dotnet build src/NovaSharp.sln -c Release` to ensure dependencies are hot.
- Run the runtime benchmarks locally with `dotnet run --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj -c Release`. The `PerformanceReportWriter` will refresh the `### NovaSharp Latest` block for the current OS.
- Capture cross-runtime parity by executing `dotnet run --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj -c Release`. This emits the NLua/MoonSharp comparison tables consumed by this document.
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
| Method              | Scenario          | Mean         | P95          | Rank | Gen0      | Gen1     | Gen2     | Allocated  |
|-------------------- |------------------ |-------------:|-------------:|-----:|----------:|---------:|---------:|-----------:|
| **&#39;MoonSharp Compile&#39;** | **TowerOfHanoi**      | **1,674.957 μs** | **1,777.867 μs** |    **8** |  **238.2813** | **232.4219** | **226.5625** |  **2468432 B** |
| &#39;MoonSharp Execute&#39; | TowerOfHanoi      | 7,175.532 μs | 7,599.155 μs |   10 | 1882.8125 |  15.6250 |        - | 35521179 B |
| &#39;NLua Compile&#39;      | TowerOfHanoi      |     5.218 μs |     5.540 μs |    1 |    0.0229 |   0.0153 |        - |      560 B |
| &#39;NLua Execute&#39;      | TowerOfHanoi      |   430.154 μs |   481.635 μs |    6 |         - |        - |        - |       24 B |
| **&#39;MoonSharp Compile&#39;** | **EightQueens**       | **1,852.369 μs** | **2,153.248 μs** |    **8** |  **259.7656** | **253.9063** | **244.1406** |  **2519168 B** |
| &#39;MoonSharp Execute&#39; | EightQueens       |   978.524 μs | 1,126.261 μs |    7 |  185.5469 |  13.6719 |        - |  3496441 B |
| &#39;NLua Compile&#39;      | EightQueens       |    10.805 μs |    11.553 μs |    3 |    0.0610 |   0.0458 |        - |     1184 B |
| &#39;NLua Execute&#39;      | EightQueens       |    82.167 μs |    87.591 μs |    4 |         - |        - |        - |       24 B |
| **&#39;MoonSharp Compile&#39;** | **CoroutinePingPong** | **1,642.332 μs** | **1,756.526 μs** |    **8** |  **250.0000** | **240.2344** | **236.3281** |  **2491323 B** |
| &#39;MoonSharp Execute&#39; | CoroutinePingPong | 3,240.333 μs | 3,379.492 μs |    9 |  250.0000 | 222.6563 | 222.6563 |  2632137 B |
| &#39;NLua Compile&#39;      | CoroutinePingPong |     9.677 μs |    10.476 μs |    2 |    0.0458 |   0.0305 |        - |      920 B |
| &#39;NLua Execute&#39;      | CoroutinePingPong |   197.683 μs |   219.349 μs |    5 |         - |        - |        - |       24 B |


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
| Method               | Scenario          | Mean       | Error     | StdDev    | P95        | Rank | Gen0   | Gen1   | Allocated |
|--------------------- |------------------ |-----------:|----------:|----------:|-----------:|-----:|-------:|-------:|----------:|
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **195.3 ns** |  **12.84 ns** |   **8.50 ns** |   **208.9 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **4,205.5 ns** | **454.08 ns** | **300.34 ns** | **4,562.9 ns** |    **4** | **1.6403** | **0.1450** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **247.2 ns** |  **24.00 ns** |  **15.87 ns** |   **268.8 ns** |    **2** | **0.0596** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **295.3 ns** |  **14.82 ns** |   **8.82 ns** |   **308.2 ns** |    **3** | **0.0720** |      **-** |    **1360 B** |


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
| Method                | Complexity | Mean              | Error             | StdDev            | P95              | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |------------------:|------------------:|------------------:|-----------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **1,478,166.58 ns** |    **244,461.723 ns** |    **161,696.294 ns** |   **1,718,837.4 ns** |    **3** |    **255.8594** |  **248.0469** | **244.1406** |    **2447292 B** |
| &#39;Compile Only&#39;        | Tiny       |   1,521,540.88 ns |    158,137.487 ns |    104,598.157 ns |   1,661,218.0 ns |    3 |    236.3281 |  230.4688 | 224.6094 |    2444896 B |
| &#39;Execute Precompiled&#39; | Tiny       |          96.62 ns |          7.261 ns |          4.803 ns |         104.3 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **1,693,482.92 ns** |    **130,447.113 ns** |     **77,626.985 ns** |   **1,803,269.0 ns** |    **3** |    **250.0000** |  **242.1875** | **238.2813** |    **2473905 B** |
| &#39;Compile Only&#39;        | Small      |   1,613,933.16 ns |    291,098.906 ns |    192,543.903 ns |   1,893,643.4 ns |    3 |    265.6250 |  259.7656 | 253.9063 |    2456059 B |
| &#39;Execute Precompiled&#39; | Small      |       6,778.39 ns |        298.463 ns |        197.415 ns |       7,041.4 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **46,672,822.50 ns** |  **5,427,202.299 ns** |  **3,589,758.290 ns** |  **51,668,609.6 ns** |    **7** |   **8250.0000** |  **916.6667** | **416.6667** |  **150400357 B** |
| &#39;Compile Only&#39;        | Medium     |   2,160,586.60 ns |    227,705.364 ns |    150,613.000 ns |   2,389,209.8 ns |    4 |    251.9531 |  248.0469 | 232.4219 |    2601683 B |
| &#39;Execute Precompiled&#39; | Medium     |  35,550,519.26 ns |  1,968,523.972 ns |  1,171,437.040 ns |  37,547,669.3 ns |    6 |   7800.0000 |         - |        - |  147707363 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **779,093,000.00 ns** | **66,814,499.935 ns** | **44,193,654.815 ns** | **838,759,330.0 ns** |    **8** | **163000.0000** | **1000.0000** |        **-** | **3072062728 B** |
| &#39;Compile Only&#39;        | Large      |   3,062,434.41 ns |    448,910.023 ns |    296,926.186 ns |   3,403,041.4 ns |    5 |    312.5000 |  292.9688 | 277.3438 |    2937461 B |
| &#39;Execute Precompiled&#39; | Large      | 746,448,587.50 ns | 22,981,568.876 ns | 12,019,804.892 ns | 761,896,265.0 ns |    8 | 163000.0000 |         - |        - | 3069038760 B |

To refresh this section, run:

```
dotnet run -c Release --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj
```

Then replace everything under `### NovaSharp Latest` with the new results.

---

### NovaSharp Latest (captured 2025-11-10 13:18:25 -08:00)

**Environment**
- OS: Windows 11 Pro 25H2 (build 26200.6899, 10.0.26200)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.21 (8.0.21, 8.0.2125.47513)
- Approx. RAM: 195,968 MB
- Suite: NovaSharp Benchmarks

**Delta vs MoonSharp baseline**

| Summary | Method | Parameters | Nova Mean | MoonSharp Mean | Mean Δ | Mean Δ % | Nova Alloc | MoonSharp Alloc | Alloc Δ | Alloc Δ % |
|-------- |------- |----------- |----------:|---------------:|-------:|--------:|-----------:|----------------:|-------:|----------:|
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=CoroutinePipeline | 217 ns | 247 ns | -30.6 ns | -12.36% | 1.10 KB | 1.10 KB | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=NumericLoops | 173 ns | 195 ns | -22.2 ns | -11.38% | 928 B | 928 B | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=TableMutation | 3.615 us | 4.205 us | -591 ns | -14.05% | 30.2 KB | 30.2 KB | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=UserDataInterop | 289 ns | 295 ns | -6.545 ns | -2.22% | 1.33 KB | 1.33 KB | 0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Large | 712 ms | 779 ms | -67.1 ms | -8.61% | 2.86 GB | 2.86 GB | +173 KB | +0.01% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Medium | 51.6 ms | 46.7 ms | +4.976 ms | +10.66% | 144 MB | 143 MB | +170 KB | +0.12% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Small | 2.147 ms | 1.693 ms | +453 us | +26.75% | 2.52 MB | 2.36 MB | +166 KB | +6.86% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Tiny | 2.024 ms | 1.478 ms | +546 us | +36.93% | 2.49 MB | 2.33 MB | +165 KB | +6.89% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Large | 3.034 ms | 3.062 ms | -28.9 us | -0.94% | 2.96 MB | 2.80 MB | +163 KB | +5.69% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Medium | 2.345 ms | 2.161 ms | +184 us | +8.53% | 2.64 MB | 2.48 MB | +165 KB | +6.51% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Small | 2.033 ms | 1.614 ms | +419 us | +25.94% | 2.50 MB | 2.34 MB | +164 KB | +6.84% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Tiny | 2.091 ms | 1.522 ms | +570 us | +37.46% | 2.49 MB | 2.33 MB | +166 KB | +6.95% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large | 704 ms | 746 ms | -42.2 ms | -5.65% | 2.86 GB | 2.86 GB | -736 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium | 33.7 ms | 35.6 ms | -1.833 ms | -5.16% | 141 MB | 141 MB | -27.0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small | 6.620 us | 6.778 us | -159 ns | -2.34% | 18.7 KB | 18.7 KB | 0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny | 85.1 ns | 96.6 ns | -11.6 ns | -11.97% | 536 B | 536 B | 0 B | 0% |

#### NovaSharp.Benchmarks.RuntimeBenchmarks-20251110-131524

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method               | Scenario          | Mean       | Error     | StdDev    | P95        | Rank | Gen0   | Gen1   | Allocated |
|--------------------- |------------------ |-----------:|----------:|----------:|-----------:|-----:|-------:|-------:|----------:|
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **173.1 ns** |   **4.67 ns** |   **3.09 ns** |   **177.3 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **3,614.7 ns** | **474.48 ns** | **313.84 ns** | **4,067.2 ns** |    **4** | **1.6441** | **0.1488** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **216.6 ns** |   **8.48 ns** |   **5.04 ns** |   **221.2 ns** |    **2** | **0.0598** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **288.8 ns** |  **17.67 ns** |  **11.69 ns** |   **305.4 ns** |    **3** | **0.0720** |      **-** |    **1360 B** |


#### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251110-131616

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method                | Complexity | Mean              | Error             | StdDev            | P95               | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |------------------:|------------------:|------------------:|------------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **2,024,113.91 ns** |    **420,379.519 ns** |    **250,161.109 ns** |   **2,218,665.51 ns** |    **3** |    **257.8125** |  **246.0938** | **238.2813** |    **2615807 B** |
| &#39;Compile Only&#39;        | Tiny       |   2,091,462.42 ns |    373,756.906 ns |    247,217.052 ns |   2,470,280.84 ns |    3 |    250.0000 |  238.2813 | 230.4688 |    2614726 B |
| &#39;Execute Precompiled&#39; | Tiny       |          85.05 ns |          1.895 ns |          1.254 ns |          87.00 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **2,146,558.83 ns** |    **282,021.848 ns** |    **186,539.991 ns** |   **2,397,917.58 ns** |    **3** |    **257.8125** |  **250.0000** | **238.2813** |    **2643610 B** |
| &#39;Compile Only&#39;        | Small      |   2,032,524.02 ns |    294,823.583 ns |    154,198.435 ns |   2,239,031.88 ns |    3 |    261.7188 |  257.8125 | 242.1875 |    2623982 B |
| &#39;Execute Precompiled&#39; | Small      |       6,619.62 ns |        323.762 ns |        214.148 ns |       6,892.82 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **51,648,795.00 ns** |  **6,284,174.160 ns** |  **4,156,592.117 ns** |  **54,875,033.00 ns** |    **6** |   **8200.0000** |  **900.0000** | **400.0000** |  **150574250 B** |
| &#39;Compile Only&#39;        | Medium     |   2,344,792.40 ns |    260,559.605 ns |    155,054.841 ns |   2,581,472.50 ns |    3 |    261.7188 |  250.0000 | 234.3750 |    2770950 B |
| &#39;Execute Precompiled&#39; | Medium     |  33,717,176.00 ns |    707,177.440 ns |    467,754.091 ns |  34,391,814.33 ns |    5 |   7800.0000 |         - |        - |  147707336 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **712,018,266.67 ns** | **15,494,690.987 ns** |  **9,220,642.074 ns** | **726,772,920.00 ns** |    **7** | **163000.0000** | **1000.0000** |        **-** | **3072239824 B** |
| &#39;Compile Only&#39;        | Large      |   3,033,511.60 ns |    405,626.682 ns |    268,296.936 ns |   3,380,997.05 ns |    4 |    300.7813 |  277.3438 | 253.9063 |    3104471 B |
| &#39;Execute Precompiled&#39; | Large      | 704,252,144.44 ns | 19,412,735.082 ns | 11,552,207.270 ns | 721,282,040.00 ns |    7 | 163000.0000 |         - |        - | 3069038024 B |

To refresh this section, run:

```
dotnet run -c Release --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj
dotnet run -c Release --framework net8.0 --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj
```

Then replace everything under `### NovaSharp Latest` with the new results.

## Linux

_No benchmark data recorded yet._











## macOS

_No benchmark data recorded yet._




---



