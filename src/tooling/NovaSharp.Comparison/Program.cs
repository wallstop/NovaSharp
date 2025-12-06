namespace NovaSharp.Comparison
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    /// <summary>
    /// Entry point for running NovaSharp vs. NLua comparison benchmarks.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Executes the requested BenchmarkDotNet suites and prints a completion summary.
        /// </summary>
        /// <param name="args">Command-line arguments passed to BenchmarkDotNet.</param>
        public static void Main(string[] args)
        {
            string[] effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

            // Touch the benchmark type so analyzer CA1812 can see a real instantiation outside BDN reflection.
            using LuaPerformanceBenchmarks analyzerAnchor = new LuaPerformanceBenchmarks();

            IEnumerable<Summary> summaries = BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(effectiveArgs, BenchmarkConfig.Create());

            // Comparison benchmarks are run manually; print a reminder instead of
            // attempting to emit a shared performance report (NovaSharp.Benchmarks
            // owns that infrastructure).
            int completedSummaries = summaries.Count();
            string completionMessage = string.Format(
                CultureInfo.InvariantCulture,
                "NovaSharp.Comparison completed {0} summaries.",
                completedSummaries
            );
            Console.WriteLine(completionMessage);
        }
    }
}
