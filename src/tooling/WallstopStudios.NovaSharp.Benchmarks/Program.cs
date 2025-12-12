namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    /// <summary>
    /// Entry point for running the NovaSharp BenchmarkDotNet suites locally or in CI.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Bootstraps BenchmarkDotNet and triggers the performance report update.
        /// </summary>
        /// <param name="args">Command-line arguments passed to BenchmarkDotNet.</param>
        public static void Main(string[] args)
        {
            string[] effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

            IEnumerable<Summary> summaries = BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(effectiveArgs, DefaultConfigFactory.Create());

            // Skip Performance.md update in CI environments to avoid git conflicts
            // during branch operations (e.g., github-action-benchmark switching to gh-pages).
            // Set NOVASHARP_SKIP_PERFORMANCE_DOC=1 to disable locally as well.
            string skipEnv = Environment.GetEnvironmentVariable("NOVASHARP_SKIP_PERFORMANCE_DOC");
            string ciEnv = Environment.GetEnvironmentVariable("CI");
            bool skipPerformanceDoc =
                string.Equals(skipEnv, "1", StringComparison.Ordinal)
                || string.Equals(skipEnv, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ciEnv, "true", StringComparison.OrdinalIgnoreCase);

            if (!skipPerformanceDoc)
            {
                PerformanceReportWriter.Write("NovaSharp Benchmarks", summaries);
            }
        }
    }
}
