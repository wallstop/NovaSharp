using BenchmarkDotNet.Running;
using NovaSharp.Benchmarking;

namespace NovaSharp.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        var effectiveArgs = args.Length > 0 ? args : new[] { "--filter", "*" };

        var summaries = BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(effectiveArgs, DefaultConfigFactory.Create());

#if NET8_0_OR_GREATER
        PerformanceReportWriter.Write("NovaSharp Benchmarks", summaries);
#endif
    }
}
