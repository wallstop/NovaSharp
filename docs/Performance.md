# NovaSharp Performance Baselines

This document captures benchmark snapshots across supported operating systems. Each run overwrites the section for the current OS with fresh measurements, hardware notes, and environment details.

## Windows

_Last updated: 2025-11-08 16:54:46 -08:00_

**Environment**
- OS: Microsoft Windows 10.0.26200
- Logical cores: 24
- Runtime: .NET 8.0.21 (8.0.2125.47513)
- Approx. RAM: 195,968 MB
- Suite: Interpreter vs NLua

### NovaSharp.Comparison.LuaPerformanceBenchmarks-20251108-165221

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
| **&#39;NovaSharp Compile&#39;** | **TowerOfHanoi**      | **1,674.957 μs** | **1,777.867 μs** |    **8** |  **238.2813** | **232.4219** | **226.5625** |  **2468432 B** |
| &#39;NovaSharp Execute&#39; | TowerOfHanoi      | 7,175.532 μs | 7,599.155 μs |   10 | 1882.8125 |  15.6250 |        - | 35521179 B |
| &#39;NLua Compile&#39;      | TowerOfHanoi      |     5.218 μs |     5.540 μs |    1 |    0.0229 |   0.0153 |        - |      560 B |
| &#39;NLua Execute&#39;      | TowerOfHanoi      |   430.154 μs |   481.635 μs |    6 |         - |        - |        - |       24 B |
| **&#39;NovaSharp Compile&#39;** | **EightQueens**       | **1,852.369 μs** | **2,153.248 μs** |    **8** |  **259.7656** | **253.9063** | **244.1406** |  **2519168 B** |
| &#39;NovaSharp Execute&#39; | EightQueens       |   978.524 μs | 1,126.261 μs |    7 |  185.5469 |  13.6719 |        - |  3496441 B |
| &#39;NLua Compile&#39;      | EightQueens       |    10.805 μs |    11.553 μs |    3 |    0.0610 |   0.0458 |        - |     1184 B |
| &#39;NLua Execute&#39;      | EightQueens       |    82.167 μs |    87.591 μs |    4 |         - |        - |        - |       24 B |
| **&#39;NovaSharp Compile&#39;** | **CoroutinePingPong** | **1,642.332 μs** | **1,756.526 μs** |    **8** |  **250.0000** | **240.2344** | **236.3281** |  **2491323 B** |
| &#39;NovaSharp Execute&#39; | CoroutinePingPong | 3,240.333 μs | 3,379.492 μs |    9 |  250.0000 | 222.6563 | 222.6563 |  2632137 B |
| &#39;NLua Compile&#39;      | CoroutinePingPong |     9.677 μs |    10.476 μs |    2 |    0.0458 |   0.0305 |        - |      920 B |
| &#39;NLua Execute&#39;      | CoroutinePingPong |   197.683 μs |   219.349 μs |    5 |         - |        - |        - |       24 B |

### NovaSharp.Benchmarks.RuntimeBenchmarks-20251108-164841

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


### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251108-164922

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

### NovaSharp.Benchmarks.RuntimeBenchmarks-20251108-163349

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
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **173.7 ns** |   **8.79 ns** |   **5.81 ns** |   **182.1 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **3,408.4 ns** | **215.25 ns** | **142.37 ns** | **3,618.1 ns** |    **4** | **1.6441** | **0.1488** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **212.1 ns** |   **5.25 ns** |   **3.12 ns** |   **215.4 ns** |    **2** | **0.0598** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **285.9 ns** |   **9.23 ns** |   **6.10 ns** |   **295.0 ns** |    **3** | **0.0720** |      **-** |    **1360 B** |


### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251108-163439

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method                | Complexity | Mean              | Error             | StdDev            | P95               | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |------------------:|------------------:|------------------:|------------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **1,356,833.77 ns** |    **166,430.291 ns** |    **110,083.333 ns** |   **1,467,525.71 ns** |    **3** |    **248.0469** |  **242.1875** | **236.3281** |    **2446449 B** |
| &#39;Compile Only&#39;        | Tiny       |   1,384,870.88 ns |    193,808.043 ns |    128,192.021 ns |   1,531,258.63 ns |    3 |    248.0469 |  240.2344 | 236.3281 |    2445812 B |
| &#39;Execute Precompiled&#39; | Tiny       |          86.08 ns |          2.433 ns |          1.272 ns |          87.65 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **1,473,076.70 ns** |    **232,335.125 ns** |    **153,675.300 ns** |   **1,682,584.49 ns** |    **3** |    **261.7188** |  **255.8594** | **250.0000** |    **2475100 B** |
| &#39;Compile Only&#39;        | Small      |   1,397,566.54 ns |    206,067.592 ns |    122,627.519 ns |   1,573,979.61 ns |    3 |    236.3281 |  232.4219 | 224.6094 |    2453091 B |
| &#39;Execute Precompiled&#39; | Small      |       6,183.95 ns |        108.998 ns |         72.096 ns |       6,292.04 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **41,016,701.82 ns** |  **5,297,432.757 ns** |  **3,503,923.773 ns** |  **45,649,889.55 ns** |    **7** |   **8181.8182** |  **818.1818** | **363.6364** |  **150401378 B** |
| &#39;Compile Only&#39;        | Medium     |   1,817,840.12 ns |    293,991.499 ns |    194,457.173 ns |   2,060,786.55 ns |    4 |    242.1875 |  232.4219 | 222.6563 |    2601363 B |
| &#39;Execute Precompiled&#39; | Medium     |  33,677,739.38 ns |  1,469,138.605 ns |    971,744.224 ns |  34,820,317.50 ns |    6 |   7812.5000 |         - |        - |  147707361 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **704,081,088.89 ns** | **17,920,401.728 ns** | **10,664,143.621 ns** | **717,652,360.00 ns** |    **8** | **163000.0000** | **1000.0000** |        **-** | **3072062728 B** |
| &#39;Compile Only&#39;        | Large      |   2,697,265.78 ns |    342,248.271 ns |    203,666.457 ns |   2,939,429.80 ns |    5 |    308.5938 |  296.8750 | 273.4375 |    2937088 B |
| &#39;Execute Precompiled&#39; | Large      | 689,928,655.56 ns | 28,924,227.120 ns | 17,212,343.609 ns | 714,486,840.00 ns |    8 | 163000.0000 |         - |        - | 3069038760 B |

### NovaSharp.Benchmarks.RuntimeBenchmarks-20251108-115938

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
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **188.7 ns** |   **8.21 ns** |   **5.43 ns** |   **195.4 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **3,931.1 ns** | **189.85 ns** | **125.58 ns** | **4,091.4 ns** |    **4** | **1.6403** | **0.1450** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **230.4 ns** |   **5.14 ns** |   **3.06 ns** |   **234.1 ns** |    **2** | **0.0596** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **280.2 ns** |  **11.47 ns** |   **7.59 ns** |   **291.9 ns** |    **3** | **0.0720** |      **-** |    **1360 B** |


### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251108-120017

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method                | Complexity | Mean              | Error             | StdDev            | P95               | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |------------------:|------------------:|------------------:|------------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **1,430,111.94 ns** |     **73,413.616 ns** |     **38,396.740 ns** |   **1,474,527.62 ns** |    **3** |    **236.3281** |  **230.4688** | **224.6094** |    **2445452 B** |
| &#39;Compile Only&#39;        | Tiny       |   1,432,807.91 ns |    142,384.526 ns |     94,178.548 ns |   1,581,124.56 ns |    3 |    244.1406 |  236.3281 | 232.4219 |    2445294 B |
| &#39;Execute Precompiled&#39; | Tiny       |          86.09 ns |          1.020 ns |          0.534 ns |          86.73 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **1,647,974.92 ns** |    **241,468.399 ns** |    **159,716.395 ns** |   **1,861,875.04 ns** |    **4** |    **250.0000** |  **240.2344** | **238.2813** |    **2473905 B** |
| &#39;Compile Only&#39;        | Small      |   1,587,277.40 ns |    214,111.235 ns |    141,621.325 ns |   1,795,142.76 ns |    4 |    224.6094 |  220.7031 | 212.8906 |    2452063 B |
| &#39;Execute Precompiled&#39; | Small      |       6,251.05 ns |         75.641 ns |         50.032 ns |       6,308.92 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **38,857,248.46 ns** |  **1,860,896.197 ns** |  **1,230,867.615 ns** |  **40,777,934.23 ns** |    **8** |   **8230.7692** |  **846.1538** | **384.6154** |  **150401007 B** |
| &#39;Compile Only&#39;        | Medium     |   2,079,617.53 ns |    451,345.966 ns |    268,588.745 ns |   2,474,714.53 ns |    5 |    242.1875 |  230.4688 | 222.6563 |    2600719 B |
| &#39;Execute Precompiled&#39; | Medium     |  33,718,416.00 ns |  1,328,339.686 ns |    878,614.456 ns |  35,005,120.67 ns |    7 |   7800.0000 |         - |        - |  147707363 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **712,393,040.00 ns** | **22,378,396.024 ns** | **14,801,923.387 ns** | **732,490,345.00 ns** |    **9** | **163000.0000** | **1000.0000** |        **-** | **3072062776 B** |
| &#39;Compile Only&#39;        | Large      |   3,189,098.95 ns |    726,924.237 ns |    480,815.375 ns |   3,633,812.87 ns |    6 |    320.3125 |  312.5000 | 285.1563 |    2937547 B |
| &#39;Execute Precompiled&#39; | Large      | 796,714,790.00 ns | 95,348,054.650 ns | 63,066,834.573 ns | 879,477,975.00 ns |   10 | 163000.0000 |         - |        - | 3069038760 B |

### NovaSharp.Benchmarks.RuntimeBenchmarks-20251108-115419

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
| **&#39;Scenario Execution&#39;** | **NumericLoops**      |   **179.7 ns** |  **10.21 ns** |   **6.75 ns** |   **188.0 ns** |    **1** | **0.0491** |      **-** |     **928 B** |
| **&#39;Scenario Execution&#39;** | **TableMutation**     | **3,914.7 ns** | **596.86 ns** | **394.79 ns** | **4,523.8 ns** |    **4** | **1.6441** | **0.1488** |   **30968 B** |
| **&#39;Scenario Execution&#39;** | **CoroutinePipeline** |   **259.0 ns** |  **17.46 ns** |  **11.55 ns** |   **274.8 ns** |    **2** | **0.0596** |      **-** |    **1128 B** |
| **&#39;Scenario Execution&#39;** | **UserDataInterop**   |   **285.9 ns** |  **14.15 ns** |   **9.36 ns** |   **299.5 ns** |    **3** | **0.0720** |      **-** |    **1360 B** |


### NovaSharp.Benchmarks.ScriptLoadingBenchmarks-20251108-115506

```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.6899)
Unknown processor
.NET SDK 9.0.306
  [Host]   : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```
| Method                | Complexity | Mean              | Error            | StdDev            | P95              | Rank | Gen0        | Gen1      | Gen2     | Allocated    |
|---------------------- |----------- |------------------:|-----------------:|------------------:|-----------------:|-----:|------------:|----------:|---------:|-------------:|
| **&#39;Compile + Execute&#39;**   | **Tiny**       |   **1,544,405.75 ns** |    **158,492.67 ns** |     **94,316.445 ns** |   **1,670,517.0 ns** |    **3** |    **242.1875** |  **236.3281** | **230.4688** |    **2445661 B** |
| &#39;Compile Only&#39;        | Tiny       |   1,461,924.02 ns |    278,121.58 ns |    165,505.690 ns |   1,641,199.0 ns |    3 |    238.2813 |  230.4688 | 226.5625 |    2444829 B |
| &#39;Execute Precompiled&#39; | Tiny       |          95.73 ns |         11.27 ns |          7.455 ns |         105.9 ns |    1 |      0.0284 |         - |        - |        536 B |
| **&#39;Compile + Execute&#39;**   | **Small**      |   **1,771,863.03 ns** |    **241,245.17 ns** |    **159,568.740 ns** |   **1,993,875.6 ns** |    **4** |    **244.1406** |  **238.2813** | **232.4219** |    **2473354 B** |
| &#39;Compile Only&#39;        | Small      |   1,503,565.49 ns |    425,457.14 ns |    253,182.722 ns |   1,897,897.1 ns |    3 |    257.8125 |  250.0000 | 246.0938 |    2455181 B |
| &#39;Execute Precompiled&#39; | Small      |       6,684.02 ns |        505.15 ns |        334.124 ns |       7,207.8 ns |    2 |      1.0147 |         - |        - |      19176 B |
| **&#39;Compile + Execute&#39;**   | **Medium**     |  **41,347,306.92 ns** |  **3,839,761.86 ns** |  **2,539,764.729 ns** |  **44,928,414.2 ns** |    **7** |   **8230.7692** |  **846.1538** | **384.6154** |  **150400498 B** |
| &#39;Compile Only&#39;        | Medium     |   1,856,976.33 ns |    475,487.11 ns |    314,505.284 ns |   2,310,110.3 ns |    4 |    242.1875 |  234.3750 | 222.6563 |    2601007 B |
| &#39;Execute Precompiled&#39; | Medium     |  34,350,835.56 ns |    532,695.62 ns |    316,998.619 ns |  34,842,962.7 ns |    6 |   7800.0000 |         - |        - |  147707363 B |
| **&#39;Compile + Execute&#39;**   | **Large**      | **726,181,355.56 ns** | **20,773,073.66 ns** | **12,361,722.931 ns** | **743,942,760.0 ns** |    **8** | **163000.0000** | **1000.0000** |        **-** | **3072062776 B** |
| &#39;Compile Only&#39;        | Large      |   2,735,090.31 ns |    596,680.45 ns |    394,667.174 ns |   3,287,124.6 ns |    5 |    304.6875 |  296.8750 | 269.5313 |    2935829 B |
| &#39;Execute Precompiled&#39; | Large      | 769,199,820.00 ns | 77,809,677.95 ns | 51,466,284.295 ns | 855,090,095.0 ns |    9 | 163000.0000 |         - |        - | 3069038760 B |


## Linux

_No benchmark data recorded yet._

## macOS

_No benchmark data recorded yet._



