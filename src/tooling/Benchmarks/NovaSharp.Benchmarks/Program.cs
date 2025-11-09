using BenchmarkDotNet.Running;
using NovaSharp.Benchmarking;

namespace NovaSharp.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfigFactory.Create());

#if NET8_0_OR_GREATER
        PerformanceReportWriter.Write("NovaSharp Benchmarks", summaries);
#endif
    }
}
