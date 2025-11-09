# NovaSharp Performance Baselines

This document tracks benchmark snapshots used for regression analysis. Maintain exactly two sections:

- A frozen MoonSharp baseline (do not overwrite)
- The most recent NovaSharp benchmark run

When capturing new data, replace the entire `### NovaSharp Latest` section instead of appending new content to the top of the file.

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

### NovaSharp Latest (captured 2025-11-09 08:29:07 -08:00)

**Environment**
- OS: Windows 11 (build 26200, 10.0.26200)
- CPU: Intel(R) Core(TM) Ultra 9 285K
- Logical cores: 24
- Runtime: .NET 8.0.21 (8.0.21, 8.0.2125.47513)
- Approx. RAM: 195,968 MB
- Suite: NovaSharp Benchmarks

**Delta vs MoonSharp baseline**

| Summary | Method | Parameters | Nova Mean | MoonSharp Mean | Mean Δ | Mean Δ % | Nova Alloc | MoonSharp Alloc | Alloc Δ | Alloc Δ % |
|-------- |------- |----------- |----------:|---------------:|-------:|--------:|-----------:|----------------:|-------:|----------:|
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=CoroutinePipeline | 224 ns | 247 ns | -23.3 ns | -9.41% | 1.10 KB | 1.10 KB | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=NumericLoops | 189 ns | 195 ns | -6.154 ns | -3.15% | 928 B | 928 B | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=TableMutation | 3.434 us | 4.205 us | -772 ns | -18.36% | 30.2 KB | 30.2 KB | 0 B | 0% |
| NovaSharp.Benchmarks.RuntimeBenchmarks | Scenario Execution | Scenario=UserDataInterop | 372 ns | 295 ns | +76.8 ns | +26.02% | 1.33 KB | 1.33 KB | 0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Large | 763 ms | 779 ms | -16.4 ms | -2.1% | 2.86 GB | 2.86 GB | +170 KB | +0.01% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Medium | 44.8 ms | 46.7 ms | -1.863 ms | -3.99% | 144 MB | 143 MB | +167 KB | +0.11% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Small | 2.149 ms | 1.693 ms | +456 us | +26.9% | 2.52 MB | 2.36 MB | +167 KB | +6.92% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile + Execute | Complexity=Tiny | 2.112 ms | 1.478 ms | +634 us | +42.9% | 2.49 MB | 2.33 MB | +165 KB | +6.88% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Large | 3.106 ms | 3.062 ms | +43.8 us | +1.43% | 2.96 MB | 2.80 MB | +160 KB | +5.59% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Medium | 2.420 ms | 2.161 ms | +260 us | +12.02% | 2.64 MB | 2.48 MB | +164 KB | +6.44% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Small | 2.170 ms | 1.614 ms | +556 us | +34.45% | 2.50 MB | 2.34 MB | +162 KB | +6.76% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Compile Only | Complexity=Tiny | 1.901 ms | 1.522 ms | +379 us | +24.92% | 2.49 MB | 2.33 MB | +166 KB | +6.97% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Large | 774 ms | 746 ms | +27.2 ms | +3.64% | 2.86 GB | 2.86 GB | -736 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Medium | 38.1 ms | 35.6 ms | +2.593 ms | +7.29% | 141 MB | 141 MB | -27.0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Small | 6.329 us | 6.778 us | -450 ns | -6.64% | 18.7 KB | 18.7 KB | 0 B | 0% |
| NovaSharp.Benchmarks.ScriptLoadingBenchmarks | Execute Precompiled | Complexity=Tiny | 114 ns | 96.6 ns | +17.1 ns | +17.65% | 536 B | 536 B | 0 B | 0% |

#### NovaSharp.Benchmarks.RuntimeBenchmarks-20251109-082550

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3 [AttachedDebugger]
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method               | Scenario          | Mean       | Error     | StdDev    | P95        | Rank | Gen0   | Gen1   | Allocated |
|--------------------- |------------------ |-----------:|----------:|----------:|-----------:|-----:|-------:|-------:|----------:|
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **189.1 ns** |   **8.62 ns** |   **5.70 ns** |   **197.1 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **3,433.6 ns** | **184.83 ns** | **109.99 ns** | **3,569.5 ns** |    **3** | **1.6441** | **0.1488** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **223.9 ns** |  **14.61 ns** |   **8.69 ns** |   **236.7 ns** |    **1** | **0.0598** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **372.1 ns** |   **5.87 ns** |   **3.88 ns** |   **376.7 ns** |    **2** | **0.0720** |      **-** |    **1360 B** |


#### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251109-082646

```

BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
Intel Core Ultra 9 285K 3.70GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3 [AttachedDebugger]
  ShortRun : .NET 8.0.21 (8.0.21, 8.0.2125.47513), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method                | Complexity | Mean             | Error            | StdDev           | P95              | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |-----------------:|-----------------:|-----------------:|-----------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **2,112,261.2 ns** |    **609,916.63 ns** |    **403,422.09 ns** |   **2,691,984.0 ns** |    **3** |    **277.3438** |  **265.6250** | **257.8125** |    **2615759 B** |
| &#39;Compile Only&#39;        | Tiny       |   1,900,660.2 ns |    181,859.80 ns |    120,289.00 ns |   2,077,572.0 ns |    3 |    279.2969 |  271.4844 | 259.7656 |    2615236 B |
| &#39;Execute Precompiled&#39; | Tiny       |         113.7 ns |          1.53 ns |          1.01 ns |         114.9 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **2,149,060.1 ns** |    **324,207.90 ns** |    **214,443.45 ns** |   **2,446,801.7 ns** |    **3** |    **296.8750** |  **281.2500** | **277.3438** |    **2645005 B** |
| &#39;Compile Only&#39;        | Small      |   2,169,980.2 ns |    348,389.79 ns |    230,438.28 ns |   2,482,924.2 ns |    3 |    257.8125 |  242.1875 | 238.2813 |    2621967 B |
| &#39;Execute Precompiled&#39; | Small      |       6,328.5 ns |        188.43 ns |        124.63 ns |       6,507.5 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **44,810,127.5 ns** |  **4,045,662.54 ns** |  **2,675,955.28 ns** |  **49,115,832.9 ns** |    **5** |   **8250.0000** |  **916.6667** | **416.6667** |  **150570924 B** |
| &#39;Compile Only&#39;        | Medium     |   2,420,181.7 ns |    352,942.73 ns |    233,449.76 ns |   2,734,453.3 ns |    3 |    257.8125 |  246.0938 | 230.4688 |    2769239 B |
| &#39;Execute Precompiled&#39; | Medium     |  38,143,778.6 ns |  3,813,747.03 ns |  2,522,557.53 ns |  41,722,963.6 ns |    5 |   7785.7143 |         - |        - |  147707336 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **762,696,322.2 ns** | **44,902,013.71 ns** | **26,720,468.14 ns** | **801,588,020.0 ns** |    **6** | **163000.0000** | **1000.0000** |        **-** | **3072237288 B** |
| &#39;Compile Only&#39;        | Large      |   3,106,277.2 ns |    524,727.54 ns |    274,442.65 ns |   3,284,157.3 ns |    4 |    289.0625 |  281.2500 | 246.0938 |    3101626 B |
| &#39;Execute Precompiled&#39; | Large      | 773,626,910.0 ns | 71,910,676.15 ns | 47,564,459.86 ns | 841,699,800.0 ns |    6 | 163000.0000 |         - |        - | 3069038024 B |

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


