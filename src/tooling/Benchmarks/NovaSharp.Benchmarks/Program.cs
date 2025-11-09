namespace NovaSharp.Benchmarks
{
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using Benchmarking;

    internal static class Program
    {
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
