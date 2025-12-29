# Skill: Profile and Debug Performance

**When to use**: When investigating performance issues, optimizing hot paths, verifying zero-allocation code, or measuring the impact of changes.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (performance patterns), [memory-allocation-traps](memory-allocation-traps.md) (hidden allocations), [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (migration patterns)

______________________________________________________________________

## 🔴 Profiling First Principle

**Never optimize without measuring first.** Intuition about performance is often wrong. Always:

1. **Profile** to identify actual bottlenecks
1. **Measure** baseline performance
1. **Optimize** the identified hot spots
1. **Verify** improvement with measurement
1. **Document** what changed and why

______________________________________________________________________

## Allocation Testing in Code

### Verify Zero Allocation (TUnit Pattern)

```csharp
[Test]
[AllLuaVersions]
public async Task MethodShouldNotAllocateInSteadyState(LuaCompatibilityVersion version)
{
    Script script = new Script(version);
    
    // Warm up - first call may allocate (JIT, caches, etc.)
    script.DoString("return 1 + 1");

    // Measure steady state
    long before = GC.GetAllocatedBytesForCurrentThread();

    for (int i = 0; i < 1000; i++)
    {
        script.DoString("return 1 + 1");
    }

    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;

    await Assert.That(allocated).IsEqualTo(0)
        .WithMessage($"Method allocated {allocated} bytes over 1000 calls")
        .ConfigureAwait(false);
}
```

### Allocation Test with Budget

For operations where some allocation is acceptable but must stay bounded:

```csharp
[Test]
[AllLuaVersions]
public async Task PatternMatchingShouldStayWithinBudget(LuaCompatibilityVersion version)
{
    const long AllocationBudgetBytes = 8192;  // 8KB budget
    
    Script script = new Script(version);
    string code = "return string.match('hello world', '%w+')";
    
    // Warm up
    for (int warmup = 0; warmup < 5; warmup++)
    {
        script.DoString(code);
    }

    // Measure
    long before = GC.GetAllocatedBytesForCurrentThread();
    
    script.DoString(code);

    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;

    await Assert.That(allocated).IsLessThanOrEqualTo(AllocationBudgetBytes)
        .WithMessage($"Pattern matching allocated {allocated} bytes, budget is {AllocationBudgetBytes}")
        .ConfigureAwait(false);
}
```

### GCAssert Helper Class

Consider creating a test utility for consistent allocation testing:

```csharp
public static class GCAssert
{
    public static long MeasureAllocatedBytes(
        Action action,
        int warmupIterations = 5,
        int measuredIterations = 10)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // Warmup phase
        for (int i = 0; i < warmupIterations; i++)
        {
            action();
        }

        // Measurement phase
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < measuredIterations; i++)
        {
            action();
        }

        long after = GC.GetAllocatedBytesForCurrentThread();
        long delta = after - before;
        return delta < 0 ? 0 : delta;
    }

    public static void DoesNotAllocate(
        Action action,
        int warmupIterations = 5,
        int measuredIterations = 10,
        long toleranceBytes = 0)
    {
        long allocated = MeasureAllocatedBytes(action, warmupIterations, measuredIterations);
        
        if (allocated > toleranceBytes)
        {
            throw new AssertionException(
                $"Action allocated {allocated} bytes (tolerance: {toleranceBytes})");
        }
    }
}
```

______________________________________________________________________

## BenchmarkDotNet Usage

For rigorous performance measurement, use BenchmarkDotNet:

### Basic Benchmark

```csharp
[MemoryDiagnoser]  // Track allocations
[SimpleJob(RuntimeMoniker.Net80)]
public class DynValueBenchmarks
{
    private Script _script;
    
    [GlobalSetup]
    public void Setup()
    {
        _script = new Script(LuaCompatibilityVersion.Lua54);
    }
    
    [Benchmark(Baseline = true)]
    public DynValue SimpleExpression()
    {
        return _script.DoString("return 1 + 1");
    }
    
    [Benchmark]
    public DynValue FunctionCall()
    {
        return _script.DoString("return math.floor(3.7)");
    }
}
```

### Running Benchmarks

```bash
# Run benchmarks in Release mode
dotnet run -c Release --project src/benchmarks/NovaSharp.Benchmarks

# With specific filter
dotnet run -c Release -- --filter *DynValue*

# Export results
dotnet run -c Release -- --exporters json html
```

### Interpreting Results

| Metric    | What It Means                      | Target          |
| --------- | ---------------------------------- | --------------- |
| Mean      | Average execution time             | Lower is better |
| Allocated | Bytes allocated per operation      | 0 for hot paths |
| Gen 0/1/2 | GC collections per 1000 operations | 0 for hot paths |
| StdDev    | Variance in results                | Low = stable    |

______________________________________________________________________

## Identifying Hot Paths

### What Qualifies as a Hot Path

| Code Location          | Frequency        | Optimization Priority |
| ---------------------- | ---------------- | --------------------- |
| VM execution loop      | Millions/second  | ★★★★★ Critical        |
| Opcode handlers        | Millions/second  | ★★★★★ Critical        |
| DynValue operations    | Very frequent    | ★★★★★ Critical        |
| Table get/set          | Very frequent    | ★★★★☆ High            |
| Function call dispatch | Per call         | ★★★★☆ High            |
| String operations      | Per string op    | ★★★☆☆ Medium          |
| Script compilation     | Once per script  | ★★☆☆☆ Low             |
| Script setup/teardown  | Once per session | ★☆☆☆☆ Low             |

### Profiler-Guided Optimization

1. **Profile** the actual usage scenario
1. **Sort by Self Time** to find expensive methods
1. **Check allocations** for GC pressure hotspots
1. **Focus on top 10%** — usually 90% of the performance impact

______________________________________________________________________

## Common Performance Pitfalls

### Symptoms and Causes

| Symptom                  | Likely Cause                | Investigation                                  |
| ------------------------ | --------------------------- | ---------------------------------------------- |
| Slow script execution    | Hot path allocations        | Profile with allocation tracking               |
| GC spikes                | Large allocations in loops  | Check `GC.GetAllocatedBytesForCurrentThread()` |
| Memory grows over time   | Memory leak / pool misuse   | Compare memory snapshots                       |
| Inconsistent performance | GC collection timing        | Check allocation patterns                      |
| Slow first execution     | JIT compilation, cold cache | Profile startup separately                     |

### GC Spike Diagnosis

1. Look for high allocation methods
1. Check frames before spikes for `GC.Collect` calls
1. Common culprits:
   - LINQ in hot paths
   - String concatenation
   - `new List<T>()` in loops
   - Closures/lambdas capturing variables

______________________________________________________________________

## Benchmark Patterns

### Micro-Benchmark Pattern

```csharp
[Test]
public void CompareImplementations()
{
    const int iterations = 100000;
    Script script = new Script(LuaCompatibilityVersion.Lua54);
    
    // Warmup
    for (int i = 0; i < 100; i++)
    {
        ImplementationA(script);
        ImplementationB(script);
    }

    // Measure A
    Stopwatch swA = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        ImplementationA(script);
    }
    swA.Stop();

    // Measure B
    Stopwatch swB = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        ImplementationB(script);
    }
    swB.Stop();

    Console.WriteLine($"A: {swA.ElapsedMilliseconds}ms, B: {swB.ElapsedMilliseconds}ms");
    Console.WriteLine($"B is {(float)swA.ElapsedMilliseconds / swB.ElapsedMilliseconds:F2}x faster");
}
```

### Loop Comparison Reference

Typical performance for common patterns:

| Pattern              | Relative Time | Allocations |
| -------------------- | ------------- | ----------- |
| `for` over array     | 1.0x          | 0B          |
| `for` over List      | 1.8x          | 0B          |
| `foreach` over array | 1.0x          | 0B          |
| `foreach` over List  | 3.4x          | 24B (Mono)  |
| LINQ `.Sum()`        | 7.7x          | 24B+        |

______________________________________________________________________

## Automated Performance Regression Tests

### Performance Test Pattern

```csharp
[Test]
public async Task ProcessItemsUnderBudget(LuaCompatibilityVersion version)
{
    const double MaxMsPerCall = 0.5;
    const int iterations = 1000;
    
    Script script = new Script(version);
    
    // Warmup
    for (int i = 0; i < 10; i++)
    {
        script.DoString("return 1");
    }
    
    Stopwatch sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        script.DoString("return 1");
    }
    sw.Stop();
    
    double msPerCall = sw.ElapsedMilliseconds / (double)iterations;

    await Assert.That(msPerCall).IsLessThan(MaxMsPerCall)
        .WithMessage($"DoString took {msPerCall}ms per call, budget is {MaxMsPerCall}ms")
        .ConfigureAwait(false);
}
```

______________________________________________________________________

## Profiling Tools

### .NET Tools

| Tool              | Purpose                          | Command                   |
| ----------------- | -------------------------------- | ------------------------- |
| `dotnet-trace`    | Collect performance traces       | `dotnet trace collect`    |
| `dotnet-counters` | Real-time performance monitoring | `dotnet counters monitor` |
| `dotnet-dump`     | Memory dump analysis             | `dotnet dump collect`     |
| BenchmarkDotNet   | Rigorous micro-benchmarks        | NuGet package             |
| PerfView          | Deep performance analysis        | Standalone tool           |

### dotnet-trace Quick Start

```bash
# Install (once)
dotnet tool install -g dotnet-trace

# Collect trace for running process
dotnet trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime

# Collect trace while running
dotnet trace collect -- dotnet run --project MyApp.csproj

# Convert to speedscope format for viewing
dotnet trace convert trace.nettrace --format speedscope
```

### dotnet-counters Quick Start

```bash
# Install (once)
dotnet tool install -g dotnet-counters

# Monitor GC and threading
dotnet counters monitor --process-id <PID> \
    --counters System.Runtime[gc-heap-size,gen-0-gc-count,gen-1-gc-count,gen-2-gc-count]
```

______________________________________________________________________

## Optimization Checklist

### Before Optimizing

- [ ] Have you profiled the actual scenario?
- [ ] Is this actually a hot path?
- [ ] What's the current allocation/time cost?
- [ ] What's the acceptable budget?
- [ ] Have you verified with multiple Lua versions?

### After Optimizing

- [ ] Did you verify improvement with profiler/benchmark?
- [ ] Did you add regression tests?
- [ ] Did you verify correctness still matches Lua spec?
- [ ] Did you document the optimization reason?
- [ ] Is the code still maintainable?
- [ ] Did you run pre-commit validation?

______________________________________________________________________

## Resources

- [high-performance-csharp](high-performance-csharp.md) — Performance patterns
- [memory-allocation-traps](memory-allocation-traps.md) — Hidden allocation sources
- [performance-audit](performance-audit.md) — Code review checklist
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Migration patterns
- [correctness-then-performance](correctness-then-performance.md) — Priority hierarchy
