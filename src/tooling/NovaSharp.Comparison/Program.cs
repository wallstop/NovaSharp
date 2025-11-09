#if NET8_0_OR_GREATER
using BenchmarkDotNet.Running;
using NovaSharp.Benchmarking;

namespace NovaSharp.Comparison;

internal static class Program
{
    public static void Main(string[] args)
    {
        var effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(effectiveArgs, BenchmarkConfig.Create());

        PerformanceReportWriter.Write("Interpreter vs NLua", summaries);
    }
}
#endif
