namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Threading;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PerformanceStopwatchTests
    {
        [Test]
        public void StartAndDisposeProducesResult()
        {
            PerformanceStopwatch stopwatch = new(PerformanceCounter.Execution);

            using (stopwatch.Start())
            {
                Thread.Sleep(1);
            }

            PerformanceResult result = stopwatch.GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo(PerformanceCounter.Execution.ToString()));
                Assert.That(result.Type, Is.EqualTo(PerformanceCounterType.TimeMilliseconds));
                Assert.That(result.Instances, Is.EqualTo(1));
                Assert.That(result.Counter, Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void ReentrantStartCountsSingleInstance()
        {
            PerformanceStopwatch stopwatch = new(PerformanceCounter.Compilation);

            using (stopwatch.Start())
            {
                using (stopwatch.Start())
                {
                    Thread.Sleep(1);
                }
            }

            PerformanceResult result = stopwatch.GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.Instances, Is.EqualTo(1));
                Assert.That(result.Counter, Is.GreaterThanOrEqualTo(0));
            });
        }
    }
}
