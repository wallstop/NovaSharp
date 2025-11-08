using BenchmarkDotNet.Running;
using MoonSharp.Benchmarking;

namespace MoonSharp.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfigFactory.Create());

#if NET8_0_OR_GREATER
        PerformanceReportWriter.Write("MoonSharp Benchmarks", summaries);
#endif
    }
}
