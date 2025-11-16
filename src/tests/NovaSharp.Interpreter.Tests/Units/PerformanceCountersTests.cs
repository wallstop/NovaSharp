namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Threading;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PerformanceCountersTests
    {
        [Test]
        public void GlobalPerformanceStopwatchAggregatesElapsedTime()
        {
            GlobalPerformanceStopwatch stopwatch = new(PerformanceCounter.Execution);

            using (stopwatch.Start())
            {
                Thread.Sleep(1);
            }

            using (stopwatch.Start())
            {
                Thread.Sleep(1);
            }

            PerformanceResult result = stopwatch.GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo(PerformanceCounter.Execution.ToString()));
                Assert.That(result.Instances, Is.EqualTo(2));
                Assert.That(result.Counter, Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void DummyPerformanceStopwatchReturnsSharedResult()
        {
            DummyPerformanceStopwatch first = DummyPerformanceStopwatch.Instance;
            DummyPerformanceStopwatch second = DummyPerformanceStopwatch.Instance;

            Assert.That(first, Is.SameAs(second));

            using (first.Start())
            {
                // No operation required; dummy stopwatch ignores timing.
            }

            PerformanceResult result = first.GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("::dummy::"));
                Assert.That(result.Global, Is.True);
                Assert.That(result.Instances, Is.EqualTo(0));
                Assert.That(result.Counter, Is.EqualTo(0));
            });
        }
    }
}
