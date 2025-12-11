```

BenchmarkDotNet v0.15.6, Linux Debian GNU/Linux 12 (bookworm) (container)
Intel Core Ultra 9 285K 3.69GHz, 1 CPU, 24 logical and 24 physical cores
.NET SDK 9.0.308
  [Host]   : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  ShortRun : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=10  LaunchCount=1  
WarmupCount=2  

```

| Method                   | ScenarioName          |           Mean |         Error |        StdDev |            P95 |  Rank |       Gen0 |       Gen1 |   Allocated |
| ------------------------ | --------------------- | -------------: | ------------: | ------------: | -------------: | ----: | ---------: | ---------: | ----------: |
| **'Scenario Execution'** | **CoroutinePipeline** |   **277.7 ns** |  **38.70 ns** |  **25.60 ns** |   **310.9 ns** | **2** | **0.0663** |      **-** |  **1248 B** |
| **'Scenario Execution'** | **NumericLoops**      |   **194.9 ns** |  **16.26 ns** |  **10.76 ns** |   **208.7 ns** | **1** | **0.0491** |      **-** |   **928 B** |
| **'Scenario Execution'** | **TableMutation**     | **5,239.8 ns** | **693.16 ns** | **458.49 ns** | **5,764.3 ns** | **4** | **1.6479** | **0.1450** | **31088 B** |
| **'Scenario Execution'** | **UserDataInterop**   |   **374.0 ns** |  **43.33 ns** |  **28.66 ns** |   **417.3 ns** | **3** | **0.0782** |      **-** |  **1480 B** |
