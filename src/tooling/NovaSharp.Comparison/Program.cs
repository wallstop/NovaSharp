namespace NovaSharp.Comparison
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    internal static class Program
    {
        public static void Main(string[] args)
        {
            string[] effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

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
