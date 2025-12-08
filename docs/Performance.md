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

### NovaSharp Latest (captured 2025-12-07 20:02:13 -08:00)

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
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=CoroutinePipeline |    242 ns |         247 ns | -4.914 ns |   -1.99% |    1.13 KB |         1.10 KB |  +32.0 B |    +2.84% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=NumericLoops      |    172 ns |         195 ns |  -23.7 ns |  -12.15% |      824 B |           928 B |   -104 B |   -11.21% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=TableMutation     |  4.529 us |       4.205 us |   +323 ns |   +7.68% |    25.3 KB |         30.2 KB | -4.96 KB |    -16.4% |
| NovaSharp.Benchmarks.RuntimeBenchmarks       | Scenario Execution  | Scenario=UserDataInterop   |    305 ns |         295 ns | +9.440 ns |    +3.2% |    1.38 KB |         1.33 KB |  +56.0 B |    +4.12% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Large           |   1.156 s |         779 ms |   +377 ms |  +48.34% |    3.53 GB |         2.86 GB |  +690 MB |   +23.54% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Medium          |   71.4 ms |        46.7 ms |  +24.8 ms |  +53.04% |     177 MB |          143 MB | +33.4 MB |   +23.31% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Small           |  2.498 ms |       1.693 ms |   +805 us |  +47.53% |    2.55 MB |         2.36 MB |  +192 KB |    +7.94% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute   | Complexity=Tiny            |  2.698 ms |       1.478 ms | +1.219 ms |  +82.49% |    2.52 MB |         2.33 MB |  +190 KB |    +7.94% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Large           |  3.050 ms |       3.062 ms |  -12.4 us |   -0.41% |    2.90 MB |         2.80 MB |  +105 KB |    +3.66% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Medium          |  2.332 ms |       2.161 ms |   +171 us |   +7.94% |    2.64 MB |         2.48 MB |  +162 KB |    +6.36% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Small           |  2.514 ms |       1.614 ms |   +900 us |  +55.77% |    2.52 MB |         2.34 MB |  +182 KB |    +7.59% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only        | Complexity=Tiny            |  2.227 ms |       1.522 ms |   +705 us |  +46.35% |    2.52 MB |         2.33 MB |  +194 KB |    +8.11% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large           |   1.261 s |         746 ms |   +515 ms |  +68.93% |    3.53 GB |         2.86 GB |  +690 MB |   +23.56% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium          |   61.0 ms |        35.6 ms |  +25.5 ms |  +71.64% |     174 MB |          141 MB | +33.3 MB |   +23.61% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small           |   11.3 us |       6.778 us | +4.519 us |  +66.67% |    23.9 KB |         18.7 KB | +5.22 KB |   +27.87% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny            |    159 ns |        96.6 ns |  +62.8 ns |  +65.02% |      576 B |           536 B |  +40.0 B |    +7.46% |

#### WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks-20251207-195859

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.7171)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                   | ScenarioName          |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **CoroutinePipeline** |   **242.3 ns** |  **56.77 ns** |  **37.55 ns** |   **296.3 ns** | **2** | **0.0615** |      **-** |  **1160 B** |
| **'Scenario Execution'** | **NumericLoops**      |   **171.6 ns** |  **32.30 ns** |  **21.36 ns** |   **192.0 ns** | **1** | **0.0436** |      **-** |   **824 B** |
| **'Scenario Execution'** | **TableMutation**     | **4,528.6 ns** | **959.89 ns** | **634.91 ns** | **5,376.8 ns** | **4** | **1.3733** | **0.1030** | **25888 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **304.7 ns** |  **67.10 ns** |  **44.38 ns** |   **377.4 ns** | **3** | **0.0749** |      **-** |  **1416 B** |

#### WallstopStudios.NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251207-195952

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.7171)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                  | ComplexityName |                   Mean |                 Error |                StdDev |                    P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | -------------- | ---------------------: | --------------------: | --------------------: | ---------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Large**      | **1,155,737,890.0 ns** | **186,533,055.87 ns** | **123,380,067.09 ns** | **1,342,886,060.0 ns** | **5** | **201000.0000** | **1000.0000** |        **-** | **3795302832 B** |
| 'Compile Only'          | Large          |         3,050,008.0 ns |         375,210.82 ns |         248,178.73 ns |         3,325,799.9 ns |     3 |        273.4375 |      253.9063 |     230.4688 |        3045018 B |
| 'Execute Precompiled'   | Large          |     1,260,953,530.0 ns |     179,709,961.13 ns |     118,867,012.38 ns |     1,400,809,025.0 ns |     5 |     201000.0000 |             - |            - |     3792151872 B |
| **'Compile + Execute'** | **Medium**     |    **71,427,601.4 ns** |   **8,255,435.04 ns** |   **5,460,459.14 ns** |    **76,621,680.0 ns** | **4** |  **10000.0000** |  **714.2857** | **285.7143** |  **185451274 B** |
| 'Compile Only'          | Medium         |         2,332,062.0 ns |         331,291.40 ns |         219,128.75 ns |         2,532,790.5 ns |     3 |        250.0000 |      242.1875 |     222.6563 |        2767276 B |
| 'Execute Precompiled'   | Medium         |        61,017,873.3 ns |       9,472,155.71 ns |       6,265,244.53 ns |        67,787,740.0 ns |     4 |       9666.6667 |             - |            - |      182581376 B |
| **'Compile + Execute'** | **Small**      |     **2,498,383.1 ns** |     **279,949.58 ns** |     **146,419.04 ns** |     **2,685,365.6 ns** | **3** |    **257.8125** |  **250.0000** | **234.3750** |    **2670429 B** |
| 'Compile Only'          | Small          |         2,513,966.1 ns |         622,062.06 ns |         411,455.54 ns |         3,096,428.5 ns |     3 |        242.1875 |      234.3750 |     222.6563 |        2642507 B |
| 'Execute Precompiled'   | Small          |            11,297.5 ns |             534.84 ns |             353.76 ns |            11,697.9 ns |     2 |          1.2970 |             - |            - |          24520 B |
| **'Compile + Execute'** | **Tiny**       |     **2,697,527.6 ns** |     **460,396.93 ns** |     **304,524.06 ns** |     **3,073,715.4 ns** | **3** |    **277.3438** |  **269.5313** | **257.8125** |    **2641648 B** |
| 'Compile Only'          | Tiny           |         2,226,709.2 ns |         361,198.76 ns |         214,943.59 ns |         2,513,629.2 ns |     3 |        296.8750 |      285.1563 |     277.3438 |        2643150 B |
| 'Execute Precompiled'   | Tiny           |               159.4 ns |              23.77 ns |              15.72 ns |               177.6 ns |     1 |          0.0305 |             - |            - |            576 B |

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

### NovaSharp Latest (captured 2025-12-06 19:34:36 +00:00)

**Environment**

- OS: Debian GNU/Linux 12 (bookworm)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.22 (8.0.22, 8.0.2225.52707)
- Approx. RAM: 96,242 MB
- Suite: NovaSharp Benchmarks

#### WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks-20251206-193121

```

BenchmarkDotNet v0.15.6, Linux Debian GNU/Linux 12 (bookworm) (container)
Intel Core Ultra 9 285K 3.69GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.308
\[Host\]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1\
WarmupCount=2

```

| Method                   | ScenarioName          |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **CoroutinePipeline** |   **277.7 ns** |  **38.70 ns** |  **25.60 ns** |   **310.9 ns** | **2** | **0.0663** |      **-** |  **1248 B** |
| **'Scenario Execution'** | **NumericLoops**      |   **194.9 ns** |  **16.26 ns** |  **10.76 ns** |   **208.7 ns** | **1** | **0.0491** |      **-** |   **928 B** |
| **'Scenario Execution'** | **TableMutation**     | **5,239.8 ns** | **693.16 ns** | **458.49 ns** | **5,764.3 ns** | **4** | **1.6479** | **0.1450** | **31088 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **374.0 ns** |  **43.33 ns** |  **28.66 ns** |   **417.3 ns** | **3** | **0.0782** |      **-** |  **1480 B** |

#### WallstopStudios.NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251206-193204

```

BenchmarkDotNet v0.15.6, Linux Debian GNU/Linux 12 (bookworm) (container)
Intel Core Ultra 9 285K 3.69GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.308
\[Host\]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1\
WarmupCount=2

```

| Method                  | ComplexityName |                   Mean |                 Error |               StdDev |                    P95 |  Rank |            Gen0 |          Gen1 |         Gen2 |        Allocated |
| ----------------------- | -------------- | ---------------------: | --------------------: | -------------------: | ---------------------: | ----: | --------------: | ------------: | -----------: | ---------------: |
| **'Compile + Execute'** | **Large**      | **1,024,153,311.3 ns** | **110,176,057.59 ns** | **72,874,640.45 ns** | **1,137,543,655.1 ns** | **6** | **187000.0000** | **1000.0000** |        **-** | **3532982768 B** |
| 'Compile Only'          | Large          |         1,953,213.6 ns |         223,703.34 ns |        147,965.91 ns |         2,132,841.5 ns |     4 |        253.9063 |      242.1875 |     203.1250 |        3179074 B |
| 'Execute Precompiled'   | Large          |       987,036,668.7 ns |     111,311,952.49 ns |     73,625,964.59 ns |     1,093,436,941.3 ns |     6 |     187000.0000 |             - |            - |     3529692336 B |
| **'Compile + Execute'** | **Medium**     |    **54,029,001.0 ns** |  **11,986,145.24 ns** |  **7,928,092.94 ns** |    **65,512,399.5 ns** | **5** |   **9400.0000** |  **900.0000** | **400.0000** |  **172909012 B** |
| 'Compile Only'          | Medium         |         1,247,361.4 ns |         325,784.59 ns |        215,486.34 ns |         1,560,943.0 ns |     3 |        179.6875 |      175.7813 |     148.4375 |        2838276 B |
| 'Execute Precompiled'   | Medium         |        50,039,455.4 ns |       4,931,683.66 ns |      3,262,003.39 ns |        54,027,675.1 ns |     5 |       9000.0000 |             - |            - |      169958320 B |
| **'Compile + Execute'** | **Small**      |     **1,276,020.7 ns** |     **175,983.10 ns** |    **104,724.72 ns** |     **1,427,881.3 ns** | **3** |    **251.9531** |  **246.0938** | **226.5625** |    **2716566 B** |
| 'Compile Only'          | Small          |         1,356,709.0 ns |         178,000.80 ns |        105,925.42 ns |         1,521,229.7 ns |     3 |        255.8594 |      253.9063 |     232.4219 |        2695629 B |
| 'Execute Precompiled'   | Small          |             8,386.4 ns |             619.95 ns |            410.06 ns |             9,025.3 ns |     2 |          1.1292 |             - |            - |          21536 B |
| **'Compile + Execute'** | **Tiny**       |     **1,384,707.5 ns** |     **251,218.18 ns** |    **166,165.27 ns** |     **1,645,590.7 ns** | **3** |    **248.0469** |  **236.3281** | **224.6094** |    **2686812 B** |
| 'Compile Only'          | Tiny           |         1,375,096.3 ns |         236,234.63 ns |        156,254.58 ns |         1,607,305.6 ns |     3 |        269.5313 |      257.8125 |     246.0938 |        2688411 B |
| 'Execute Precompiled'   | Tiny           |               148.0 ns |              26.74 ns |             17.69 ns |               172.7 ns |     1 |          0.0370 |             - |            - |            696 B |

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
