using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace NovaSharp.Benchmarks;

internal static class DefaultConfigFactory
{
    private static readonly Job ShortRunJob = Job
        .ShortRun.WithWarmupCount(2)
        .WithIterationCount(10)
        .WithId("ShortRun");

    public static IConfig Create() =>
        ManualConfig
            .Create(DefaultConfig.Instance)
            .AddJob(ShortRunJob)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.P95, RankColumn.Arabic);
}
