namespace WallstopStudios.NovaSharp.Benchmarks
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

    /// <summary>
    /// Builds the canonical BenchmarkDotNet configuration used by all NovaSharp performance suites.
    /// </summary>
    internal static class DefaultConfigFactory
    {
        private static readonly Job ShortRunJob = Job
            .ShortRun.WithWarmupCount(2)
            .WithIterationCount(10)
            .WithId("ShortRun");

        /// <summary>
        /// Creates a new <see cref="IConfig"/> tailored for short NovaSharp interpreter benchmarks.
        /// </summary>
        /// <returns>BenchmarkDotNet configuration with the desired jobs/loggers/columns.</returns>
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

        /// <summary>
        /// Determines whether BenchmarkDotNet should print its summary tables to the console.
        /// </summary>
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

        /// <summary>
        /// Returns <c>true</c> when a known CI environment variable is present.
        /// </summary>
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

        /// <summary>
        /// Simple <see cref="IConfig"/> wrapper that swaps out the logger set while delegating all other behavior.
        /// </summary>
        private sealed class LoggerOverridingConfig : IConfig
        {
            private readonly IConfig _inner;
            private readonly IReadOnlyList<ILogger> _loggers;

            /// <summary>
            /// Initializes a new wrapper that returns <paramref name="loggers"/> for <see cref="GetLoggers"/>.
            /// </summary>
            /// <param name="inner">Inner BenchmarkDotNet config.</param>
            /// <param name="loggers">Logger collection that should replace the inner config's loggers.</param>
            public LoggerOverridingConfig(IConfig inner, IEnumerable<ILogger> loggers)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _loggers = loggers?.ToArray() ?? Array.Empty<ILogger>();
            }

            /// <inheritdoc />
            public IEnumerable<IColumnProvider> GetColumnProviders() => _inner.GetColumnProviders();

            /// <inheritdoc />
            public IEnumerable<ILogger> GetLoggers() => _loggers;

            /// <inheritdoc />
            public IEnumerable<IExporter> GetExporters() => _inner.GetExporters();

            /// <inheritdoc />
            public IEnumerable<IDiagnoser> GetDiagnosers() => _inner.GetDiagnosers();

            /// <inheritdoc />
            public IEnumerable<IAnalyser> GetAnalysers() => _inner.GetAnalysers();

            /// <inheritdoc />
            public IEnumerable<IValidator> GetValidators() => _inner.GetValidators();

            /// <inheritdoc />
            public IEnumerable<HardwareCounter> GetHardwareCounters() =>
                _inner.GetHardwareCounters();

            /// <inheritdoc />
            public IEnumerable<Job> GetJobs() => _inner.GetJobs();

            /// <inheritdoc />
            public IEnumerable<BenchmarkLogicalGroupRule> GetLogicalGroupRules() =>
                _inner.GetLogicalGroupRules();

            /// <inheritdoc />
            public IEnumerable<IFilter> GetFilters() => _inner.GetFilters();

            /// <inheritdoc />
            public IEnumerable<EventProcessor> GetEventProcessors() => _inner.GetEventProcessors();

            /// <inheritdoc />
            public IEnumerable<IColumnHidingRule> GetColumnHidingRules() =>
                _inner.GetColumnHidingRules();

            /// <inheritdoc />
            public SummaryStyle SummaryStyle => _inner.SummaryStyle;

            /// <inheritdoc />
            public ConfigUnionRule UnionRule => _inner.UnionRule;

            /// <inheritdoc />
            public TimeSpan BuildTimeout => _inner.BuildTimeout;

            /// <inheritdoc />
            public string ArtifactsPath => _inner.ArtifactsPath;

            /// <inheritdoc />
            public CultureInfo CultureInfo => _inner.CultureInfo;

            /// <inheritdoc />
            public ConfigOptions Options => _inner.Options;

            /// <inheritdoc />
            public WakeLockType WakeLock => _inner.WakeLock;

            /// <inheritdoc />
            public IReadOnlyList<Conclusion> ConfigAnalysisConclusion =>
                _inner.ConfigAnalysisConclusion;

            /// <inheritdoc />
            public IOrderer Orderer => _inner.Orderer;

            /// <inheritdoc />
            public ICategoryDiscoverer CategoryDiscoverer => _inner.CategoryDiscoverer;
        }
    }
}
