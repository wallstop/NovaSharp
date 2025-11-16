namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PerformanceStatisticsTests
    {
        [Test]
        public void EnabledStatisticsCaptureStopwatchResults()
        {
            FakeHighResolutionClock clock = new();
            IHighResolutionClock originalGlobal = PerformanceStatistics.GlobalClock;
            PerformanceStatistics.GlobalClock = clock;
            Script script = new Script(CoreModules.None);
            script.PerformanceStats = new PerformanceStatistics(clock);

            try
            {
                script.PerformanceStats.Enabled = true;

                using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Compilation))
                {
                    clock.AdvanceMilliseconds(15);
                }

                PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                    PerformanceCounter.Compilation
                );

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result.Name, Is.EqualTo(PerformanceCounter.Compilation.ToString()));
                    Assert.That(result.Instances, Is.EqualTo(1));
                    Assert.That(result.Counter, Is.EqualTo(15));
                });
            }
            finally
            {
                script.PerformanceStats.Enabled = false;
                PerformanceStatistics.GlobalClock = originalGlobal;
            }
        }

        [Test]
        public void DisablingStatisticsClearsStopwatches()
        {
            FakeHighResolutionClock clock = new();
            IHighResolutionClock originalGlobal = PerformanceStatistics.GlobalClock;
            PerformanceStatistics.GlobalClock = clock;
            Script script = new Script(CoreModules.None);
            script.PerformanceStats = new PerformanceStatistics(clock);

            try
            {
                script.PerformanceStats.Enabled = true;

                using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Execution))
                {
                    clock.AdvanceMilliseconds(5);
                }

                script.PerformanceStats.Enabled = false;

                PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                    PerformanceCounter.Execution
                );

                Assert.That(result, Is.Null);
            }
            finally
            {
                script.PerformanceStats.Enabled = false;
                PerformanceStatistics.GlobalClock = originalGlobal;
            }
        }

        [Test]
        public void GlobalStopwatchAggregatesAcrossInstances()
        {
            FakeHighResolutionClock clock = new();
            IHighResolutionClock originalGlobal = PerformanceStatistics.GlobalClock;
            PerformanceStatistics.GlobalClock = clock;
            Script script = new Script(CoreModules.None);
            script.PerformanceStats = new PerformanceStatistics(clock);

            try
            {
                script.PerformanceStats.Enabled = true;

                using (
                    PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
                )
                {
                    clock.AdvanceMilliseconds(20);
                }

                PerformanceResult global = script.PerformanceStats.GetPerformanceCounterResult(
                    PerformanceCounter.AdaptersCompilation
                );

                Assert.Multiple(() =>
                {
                    Assert.That(global, Is.Not.Null);
                    Assert.That(global.Instances, Is.EqualTo(1));
                    Assert.That(global.Counter, Is.EqualTo(20));
                });
            }
            finally
            {
                script.PerformanceStats.Enabled = false;
                PerformanceStatistics.GlobalClock = originalGlobal;
            }
        }

        [Test]
        public void GlobalStopwatchRemainsStableWhenTogglingEnabledFromOtherThreads()
        {
            PerformanceStatistics stats = new();
            stats.Enabled = true;

            try
            {
                Assert.DoesNotThrow(() =>
                {
                    Parallel.Invoke(
                        () =>
                        {
                            for (int i = 0; i < 200; i++)
                            {
                                using (
                                    PerformanceStatistics.StartGlobalStopwatch(
                                        PerformanceCounter.Execution
                                    )
                                )
                                {
                                    Thread.SpinWait(50);
                                }
                            }
                        },
                        () =>
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                stats.Enabled = false;
                                stats.Enabled = true;
                            }
                        }
                    );
                });
            }
            finally
            {
                stats.Enabled = false;
            }
        }
    }
}
