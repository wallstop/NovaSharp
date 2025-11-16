namespace NovaSharp.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using BenchmarkDotNet.Analysers;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.EventProcessors;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Filters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Order;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Validators;

    internal static class DefaultConfigFactory
    {
        private static readonly Job ShortRunJob = Job
            .ShortRun.WithWarmupCount(2)
            .WithIterationCount(10)
            .WithId("ShortRun");

        public static IConfig Create()
        {
            ManualConfig baseConfig = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(ShortRunJob)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddColumn(StatisticColumn.P95, RankColumn.Arabic);

            if (ShouldEmitSummary())
            {
                return baseConfig;
            }

            ILogger filteredLogger = new ConsoleLoggerWithoutSummary(ConsoleLogger.Default);
            return new LoggerOverridingConfig(baseConfig, new[] { filteredLogger });
        }

        private static bool ShouldEmitSummary()
        {
            string overrideValue = Environment.GetEnvironmentVariable(
                "NOVASHARP_BENCHMARK_SUMMARY"
            );
            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                return overrideValue.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || overrideValue.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return IsCiEnvironment();
        }

        private static bool IsCiEnvironment()
        {
            string[] markers =
            {
                "CI",
                "GITHUB_ACTIONS",
                "TF_BUILD",
                "TEAMCITY_VERSION",
                "BUILD_BUILDID",
                "APPVEYOR",
            };

            return markers.Any(static key =>
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))
            );
        }

        private sealed class LoggerOverridingConfig : IConfig
        {
            private readonly IConfig _inner;
            private readonly IReadOnlyList<ILogger> _loggers;

            public LoggerOverridingConfig(IConfig inner, IEnumerable<ILogger> loggers)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _loggers = loggers?.ToArray() ?? Array.Empty<ILogger>();
            }

            public IEnumerable<IColumnProvider> GetColumnProviders() => _inner.GetColumnProviders();

            public IEnumerable<ILogger> GetLoggers() => _loggers;

            public IEnumerable<IExporter> GetExporters() => _inner.GetExporters();

            public IEnumerable<IDiagnoser> GetDiagnosers() => _inner.GetDiagnosers();

            public IEnumerable<IAnalyser> GetAnalysers() => _inner.GetAnalysers();

            public IEnumerable<IValidator> GetValidators() => _inner.GetValidators();

            public IEnumerable<HardwareCounter> GetHardwareCounters() =>
                _inner.GetHardwareCounters();

            public IEnumerable<Job> GetJobs() => _inner.GetJobs();

            public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() =>
                _inner.GetLogicalGroupRules();

            public IEnumerable<IFilter> GetFilters() => _inner.GetFilters();

            public IEnumerable<EventProcessor> GetEventProcessors() => _inner.GetEventProcessors();

            public IEnumerable<IColumnHidingRule> GetColumnHidingRules() =>
                _inner.GetColumnHidingRules();

            public SummaryStyle SummaryStyle => _inner.SummaryStyle;

            public ConfigUnionRule UnionRule => _inner.UnionRule;

            public TimeSpan BuildTimeout => _inner.BuildTimeout;

            public string ArtifactsPath => _inner.ArtifactsPath;

            public CultureInfo CultureInfo => _inner.CultureInfo;

            public ConfigOptions Options => _inner.Options;

            public WakeLockType WakeLock => _inner.WakeLock;

            public IReadOnlyList<Conclusion> ConfigAnalysisConclusion =>
                _inner.ConfigAnalysisConclusion;

            public IOrderer Orderer => _inner.Orderer;

            public ICategoryDiscoverer CategoryDiscoverer => _inner.CategoryDiscoverer;
        }
    }
}
