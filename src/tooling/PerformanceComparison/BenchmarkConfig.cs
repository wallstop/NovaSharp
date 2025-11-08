#if NET8_0_OR_GREATER
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace MoonSharp.PerformanceComparison;

internal static class BenchmarkConfig
{
    private static readonly Job ComparisonJob = Job
        .ShortRun.WithWarmupCount(2)
        .WithIterationCount(10)
        .WithId("Comparison");

    public static IConfig Create() =>
        ManualConfig
            .Create(DefaultConfig.Instance)
            .AddJob(ComparisonJob)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.P95);
}
#endif
