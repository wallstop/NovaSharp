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
                await Task.Delay(1).ConfigureAwait(false);
            }

            using (stopwatch.Start())
            {
                await Task.Delay(1).ConfigureAwait(false);
            }

            PerformanceResult result = stopwatch.GetResult();

            await Assert
                .That(result.Name)
                .IsEqualTo(PerformanceCounter.Execution.ToString())
                .ConfigureAwait(false);
            await Assert.That(result.Instances).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Counter >= 0).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DummyPerformanceStopwatchReturnsSharedResult()
        {
            DummyPerformanceStopwatch first = DummyPerformanceStopwatch.Instance;
            DummyPerformanceStopwatch second = DummyPerformanceStopwatch.Instance;

            await Assert.That(ReferenceEquals(first, second)).IsTrue().ConfigureAwait(false);

            using (first.Start())
            {
                // The dummy stopwatch ignores timing, but exercise the path to ensure no exceptions.
            }

            PerformanceResult result = first.GetResult();

            await Assert.That(result.Name).IsEqualTo("::dummy::").ConfigureAwait(false);
            await Assert.That(result.Global).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Instances).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(result.Counter).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
