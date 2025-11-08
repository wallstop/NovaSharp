#if NET8_0_OR_GREATER
using BenchmarkDotNet.Running;
using MoonSharp.Benchmarking;

namespace MoonSharp.PerformanceComparison;

internal static class Program
{
    public static void Main(string[] args)
    {
        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, BenchmarkConfig.Create());

        PerformanceReportWriter.Write("Interpreter vs NLua", summaries);
    }
}
#endif
