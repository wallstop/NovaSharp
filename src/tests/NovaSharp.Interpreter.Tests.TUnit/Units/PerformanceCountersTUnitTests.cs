#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;

    public sealed class PerformanceCountersTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GlobalPerformanceStopwatchAggregatesElapsedTime()
        {
            GlobalPerformanceStopwatch stopwatch = new(PerformanceCounter.Execution);

            using (stopwatch.Start())
            {
                await Task.Delay(1);
            }

            using (stopwatch.Start())
            {
                await Task.Delay(1);
            }

            PerformanceResult result = stopwatch.GetResult();

            await Assert.That(result.Name).IsEqualTo(PerformanceCounter.Execution.ToString());
            await Assert.That(result.Instances).IsEqualTo(2);
            await Assert.That(result.Counter >= 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DummyPerformanceStopwatchReturnsSharedResult()
        {
            DummyPerformanceStopwatch first = DummyPerformanceStopwatch.Instance;
            DummyPerformanceStopwatch second = DummyPerformanceStopwatch.Instance;

            await Assert.That(ReferenceEquals(first, second)).IsTrue();

            using (first.Start())
            {
                // The dummy stopwatch ignores timing, but exercise the path to ensure no exceptions.
            }

            PerformanceResult result = first.GetResult();

            await Assert.That(result.Name).IsEqualTo("::dummy::");
            await Assert.That(result.Global).IsTrue();
            await Assert.That(result.Instances).IsEqualTo(0);
            await Assert.That(result.Counter).IsEqualTo(0);
        }
    }
}
#pragma warning restore CA2007
