#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Diagnostics.PerformanceCounters;
    using NovaSharp.Interpreter.Infrastructure;

    public sealed class PerformanceStatisticsTUnitTests
    {
        private const PerformanceCounter ExecutionCounter = PerformanceCounter.Execution;

        private static readonly object SyncRoot = new();

        [global::TUnit.Core.Test]
        public async Task StartStopwatchReturnsNullWhenDisabled()
        {
            IDisposable handle;
            lock (SyncRoot)
            {
                ResetGlobalState();
                PerformanceStatistics stats = new(new FakeClock());
                handle = stats.StartStopwatch(ExecutionCounter);
            }

            await Assert.That(handle).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task EnabledTrueCreatesStopwatchInstances()
        {
            PerformanceResult result;
            lock (SyncRoot)
            {
                ResetGlobalState();
                FakeClock clock = new();
                PerformanceStatistics stats = new(clock) { Enabled = true };

                using (stats.StartStopwatch(ExecutionCounter))
                {
                    clock.AdvanceMilliseconds(25);
                }

                result = stats.GetPerformanceCounterResult(ExecutionCounter);
            }

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Counter).IsEqualTo(25);
            await Assert.That(result.Instances).IsEqualTo(1);
            await Assert.That(result.Name).IsEqualTo(ExecutionCounter.ToString());
            await Assert.That(result.Global).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DisablingClearsStopwatchesAndGlobalState()
        {
            PerformanceResult resultAfterDisable;
            PerformanceResult resultBeforeDisable;
            lock (SyncRoot)
            {
                ResetGlobalState();
                FakeClock clock = new();
                PerformanceStatistics stats = new(clock) { Enabled = true };

                using (stats.StartStopwatch(ExecutionCounter))
                {
                    clock.AdvanceMilliseconds(10);
                }

                resultBeforeDisable = stats.GetPerformanceCounterResult(ExecutionCounter);
                stats.Enabled = false;
                resultAfterDisable = stats.GetPerformanceCounterResult(ExecutionCounter);
            }

            await Assert.That(resultBeforeDisable).IsNotNull();
            await Assert.That(resultAfterDisable).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task StartGlobalStopwatchFeedsNewInstance()
        {
            PerformanceResult result;
            lock (SyncRoot)
            {
                ResetGlobalState();
                FakeClock clock = new();
                PerformanceStatistics.GlobalClock = clock;

                using (PerformanceStatistics.StartGlobalStopwatch(ExecutionCounter))
                {
                    clock.AdvanceMilliseconds(40);
                }

                PerformanceStatistics stats = new(clock) { Enabled = true };
                result = stats.GetPerformanceCounterResult(ExecutionCounter);
            }

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Global).IsTrue();
            await Assert.That(result.Counter).IsEqualTo(40);
            await Assert.That(result.Instances).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task EnabledIgnoresRedundantAssignments()
        {
            PerformanceResult enabledResult;
            PerformanceResult disabledResult;
            lock (SyncRoot)
            {
                ResetGlobalState();
                FakeClock clock = new();
                PerformanceStatistics stats = new(clock);

                stats.Enabled = true;
                stats.Enabled = true;

                using (stats.StartStopwatch(ExecutionCounter))
                {
                    clock.AdvanceMilliseconds(5);
                }

                enabledResult = stats.GetPerformanceCounterResult(ExecutionCounter);

                stats.Enabled = false;
                stats.Enabled = false;

                disabledResult = stats.GetPerformanceCounterResult(ExecutionCounter);
            }

            await Assert.That(enabledResult.Counter).IsEqualTo(5);
            await Assert.That(disabledResult).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task StartGlobalStopwatchAccumulatesAcrossScopes()
        {
            PerformanceResult result;
            lock (SyncRoot)
            {
                ResetGlobalState();
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
                result = stats.GetPerformanceCounterResult(ExecutionCounter);
            }

            await Assert.That(result.Counter).IsEqualTo(25);
            await Assert.That(result.Instances).IsEqualTo(2);
            await Assert.That(result.Global).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetPerformanceLogListsRecordedCounters()
        {
            string log;
            lock (SyncRoot)
            {
                ResetGlobalState();
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

                log = stats.GetPerformanceLog();
            }

            await Assert.That(log).Contains("Execution : 1 times / 4 ms");
            await Assert.That(log).Contains("Compilation : 1 times / 6 ms");
        }

        private static void ResetGlobalState()
        {
            PerformanceStatistics.GlobalClock = SystemHighResolutionClock.Instance;
            PerformanceStatistics.TestHooks.ResetGlobalStopwatches();
        }

        private sealed class FakeClock : IHighResolutionClock
        {
            private long _timestamp;

            public double TimestampFrequency => 1000;

            public long GetTimestamp() => _timestamp;

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
#pragma warning restore CA2007
