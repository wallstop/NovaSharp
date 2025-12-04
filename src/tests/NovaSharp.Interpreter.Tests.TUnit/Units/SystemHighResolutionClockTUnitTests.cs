namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Infrastructure;

    public sealed class SystemHighResolutionClockTUnitTests
    {
        private const int MonotonicSampleCount = 100_000;

        [global::TUnit.Core.Test]
        public async Task TimestampFrequencyMatchesStopwatch()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;
            await Assert
                .That(clock.TimestampFrequency)
                .IsEqualTo(Stopwatch.Frequency)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetTimestampReturnsMonotonicValues()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;

            // Collect a large sample of timestamps and verify monotonicity
            long[] timestamps = new long[MonotonicSampleCount];
            for (int i = 0; i < MonotonicSampleCount; i++)
            {
                timestamps[i] = clock.GetTimestamp();
            }

            // Verify all timestamps are monotonically non-decreasing
            int violations = 0;
            for (int i = 1; i < MonotonicSampleCount; i++)
            {
                if (timestamps[i] < timestamps[i - 1])
                {
                    violations++;
                }
            }

            await Assert.That(violations).IsEqualTo(0).ConfigureAwait(false);

            // Also verify the sequence actually advanced (not all the same value)
            await Assert
                .That(timestamps[MonotonicSampleCount - 1])
                .IsGreaterThan(timestamps[0])
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetElapsedMillisecondsUsesEndTimestampWhenProvided()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;
            double freq = clock.TimestampFrequency;
            long start = 10_000;
            long delta = (long)(freq * 1.5); // emulate 1500 ms

            long elapsed = clock.GetElapsedMilliseconds(start, start + delta);

            await Assert.That(elapsed).IsEqualTo(1500).Within(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetElapsedMillisecondsUsesCurrentTimestampWhenEndMissing()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;
            long start = clock.GetTimestamp();

            // Spin until at least 1ms has elapsed to avoid timing flakiness
            // This is more reliable than Task.Delay on overloaded systems
            while (clock.GetElapsedMilliseconds(start) < 1)
            {
                // Busy wait - this ensures the clock actually advances
            }

            long elapsed = clock.GetElapsedMilliseconds(start);

            await Assert.That(elapsed).IsGreaterThanOrEqualTo(1).ConfigureAwait(false);
        }
    }
}
