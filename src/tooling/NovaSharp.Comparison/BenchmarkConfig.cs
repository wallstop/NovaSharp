namespace NovaSharp.Comparison
{
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Jobs;

    internal static class BenchmarkConfig
    {
        private static readonly Job _comparisonJob = Job
            .ShortRun.WithWarmupCount(2)
            .WithIterationCount(10)
            .WithId("Comparison");

        public static IConfig Create() =>
            ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(_comparisonJob)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddColumn(StatisticColumn.P95, RankColumn.Arabic);
    }
}
