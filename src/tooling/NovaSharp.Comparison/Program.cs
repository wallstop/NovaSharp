#if NET8_0_OR_GREATER
using BenchmarkDotNet.Running;
using NovaSharp.Benchmarking;

namespace NovaSharp.Comparison;

using BenchmarkDotNet.Reports;

internal static class Program
{
    public static void Main(string[] args)
    {
        string[] effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

        IEnumerable<Summary> summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(effectiveArgs, BenchmarkConfig.Create());

        PerformanceReportWriter.Write("Interpreter vs NLua", summaries);
    }
}
#endif
