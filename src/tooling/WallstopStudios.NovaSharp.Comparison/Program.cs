namespace WallstopStudios.NovaSharp.Comparison
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    /// <summary>
    /// Entry point for running NovaSharp external-runtime comparison benchmarks.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Executes the requested BenchmarkDotNet suites and prints a completion summary.
        /// </summary>
        /// <param name="args">Command-line arguments passed to BenchmarkDotNet.</param>
        public static void Main(string[] args)
        {
            if (TryExportScenarios(args))
            {
                return;
            }

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

        private static bool TryExportScenarios(string[] args)
        {
            const string Option = "--export-scenarios";
            const string OptionPrefix = "--export-scenarios=";

            string outputDirectory = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, Option, StringComparison.Ordinal))
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException(
                            "--export-scenarios requires an output directory."
                        );
                    }

                    outputDirectory = args[i + 1];
                    break;
                }

                if (arg.StartsWith(OptionPrefix, StringComparison.Ordinal))
                {
                    outputDirectory = arg.Substring(OptionPrefix.Length);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return false;
            }

            ExportScenarios(outputDirectory);
            return true;
        }

        private static void ExportScenarios(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            int exported = 0;
            foreach (ScriptScenario scenario in BenchmarkScripts.GetScenarios())
            {
                string outputPath = Path.Combine(
                    outputDirectory,
                    string.Concat(BenchmarkScripts.GetScenarioName(scenario), ".lua")
                );
                File.WriteAllText(outputPath, BenchmarkScripts.GetScript(scenario));
                exported++;
            }

            Console.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Exported {0} benchmark scenarios to {1}.",
                    exported,
                    outputDirectory
                )
            );
        }
    }
}
