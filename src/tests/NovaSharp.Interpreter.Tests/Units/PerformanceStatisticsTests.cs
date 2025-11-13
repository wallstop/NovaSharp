namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Threading;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PerformanceStatisticsTests
    {
        [Test]
        public void EnabledStatisticsCaptureStopwatchResults()
        {
            Script script = new Script(default) { Options = { } };

            script.PerformanceStats.Enabled = true;

            using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Compilation))
            {
                Thread.Sleep(1);
            }

            PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Compilation
            );

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo(PerformanceCounter.Compilation.ToString()));
                Assert.That(result.Instances, Is.EqualTo(1));
                Assert.That(result.Counter, Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void DisablingStatisticsClearsStopwatches()
        {
            Script script = new Script(default);
            script.PerformanceStats.Enabled = true;

            using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Execution))
            {
                Thread.Sleep(1);
            }

            script.PerformanceStats.Enabled = false;

            PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GlobalStopwatchAggregatesAcrossInstances()
        {
            Script script = new Script(default);
            script.PerformanceStats.Enabled = true;

            using (
                PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
            )
            {
                Thread.Sleep(1);
            }

            PerformanceResult global = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.AdaptersCompilation
            );

            Assert.That(global, Is.Not.Null);
            Assert.That(global.Instances, Is.EqualTo(1));
        }
    }
}
