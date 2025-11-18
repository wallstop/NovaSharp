namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;
    using NovaSharp.Interpreter.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class PerformanceStatisticsTests
    {
        private static readonly PerformanceCounter ExecutionCounter = PerformanceCounter.Execution;

        [SetUp]
        public void ResetGlobalState()
        {
            PerformanceStatistics.GlobalClock = SystemHighResolutionClock.Instance;
            FieldInfo globalField = typeof(PerformanceStatistics).GetField(
                "_globalStopwatches",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            globalField.SetValue(
                null,
                new IPerformanceStopwatch[(int)PerformanceCounter.LastValue]
            );
        }

        [Test]
        public void StartStopwatchReturnsNullWhenDisabled()
        {
            PerformanceStatistics stats = new(new FakeClock());

            IDisposable handle = stats.StartStopwatch(ExecutionCounter);

            Assert.That(handle, Is.Null);
        }

        [Test]
        public void EnabledTrueCreatesStopwatchInstances()
        {
            FakeClock clock = new();
            PerformanceStatistics stats = new(clock) { Enabled = true };

            using (stats.StartStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(25);
            }

            PerformanceResult result = stats.GetPerformanceCounterResult(ExecutionCounter);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Counter, Is.EqualTo(25));
                Assert.That(result.Instances, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo(_executionCounter.ToString()));
                Assert.That(result.Global, Is.False);
            });
        }

        [Test]
        public void DisablingClearsStopwatchesAndGlobalState()
        {
            FakeClock clock = new();
            PerformanceStatistics stats = new(clock) { Enabled = true };

            using (stats.StartStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(10);
            }

            Assert.That(stats.GetPerformanceCounterResult(ExecutionCounter), Is.Not.Null);

            stats.Enabled = false;

            Assert.That(stats.GetPerformanceCounterResult(ExecutionCounter), Is.Null);
        }

        [Test]
        public void StartGlobalStopwatchFeedsNewInstance()
        {
            FakeClock clock = new();
            PerformanceStatistics.GlobalClock = clock;

            using (PerformanceStatistics.StartGlobalStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(40);
            }

            PerformanceStatistics stats = new(clock) { Enabled = true };
            PerformanceResult result = stats.GetPerformanceCounterResult(ExecutionCounter);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Global, Is.True);
                Assert.That(result.Counter, Is.EqualTo(40));
                Assert.That(result.Instances, Is.EqualTo(1));
            });
        }

        [Test]
        public void EnabledIgnoresRedundantAssignments()
        {
            FakeClock clock = new();
            PerformanceStatistics stats = new(clock);

            stats.Enabled = true;
            stats.Enabled = true;

            using (stats.StartStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(5);
            }

            PerformanceResult enabledResult = stats.GetPerformanceCounterResult(ExecutionCounter);

            Assert.That(enabledResult.Counter, Is.EqualTo(5));

            stats.Enabled = false;
            stats.Enabled = false;

            Assert.That(stats.GetPerformanceCounterResult(ExecutionCounter), Is.Null);
        }

        [Test]
        public void StartGlobalStopwatchAccumulatesAcrossScopes()
        {
            FakeClock clock = new();
            PerformanceStatistics.GlobalClock = clock;

            using (PerformanceStatistics.StartGlobalStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(10);
            }

            using (PerformanceStatistics.StartGlobalStopwatch(ExecutionCounter))
            {
                clock.AdvanceMilliseconds(15);
            }

            PerformanceStatistics stats = new(clock) { Enabled = true };
            PerformanceResult result = stats.GetPerformanceCounterResult(ExecutionCounter);

            Assert.Multiple(() =>
            {
                Assert.That(result.Counter, Is.EqualTo(25));
                Assert.That(result.Instances, Is.EqualTo(2));
                Assert.That(result.Global, Is.True);
            });
        }

        [Test]
        public void GetPerformanceLogListsRecordedCounters()
        {
            FakeClock clock = new();
            PerformanceStatistics stats = new(clock) { Enabled = true };

            using (stats.StartStopwatch(PerformanceCounter.Execution))
            {
                clock.AdvanceMilliseconds(4);
            }

            using (stats.StartStopwatch(PerformanceCounter.Compilation))
            {
                clock.AdvanceMilliseconds(6);
            }

            string log = stats.GetPerformanceLog();

            Assert.Multiple(() =>
            {
                Assert.That(
                    log,
                    Does.Contain("Execution : 1 times / 4 ms"),
                    "Execution line missing"
                );
                Assert.That(
                    log,
                    Does.Contain("Compilation : 1 times / 6 ms"),
                    "Compilation line missing"
                );
            });
        }

        private sealed class FakeClock : IHighResolutionClock
        {
            public double TimestampFrequency => 1000;

            private long _timestamp;

            public long GetTimestamp()
            {
                return _timestamp;
            }

            public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null)
            {
                long stop = endTimestamp ?? _timestamp;
                return stop - startTimestamp;
            }

            public void AdvanceMilliseconds(long milliseconds)
            {
                _timestamp += milliseconds;
            }
        }
    }
}
