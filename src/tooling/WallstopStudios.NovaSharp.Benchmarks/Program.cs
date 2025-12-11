namespace WallstopStudios.NovaSharp.Benchmarks
{
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

            PerformanceReportWriter.Write("NovaSharp Benchmarks", summaries);
        }
    }
}
