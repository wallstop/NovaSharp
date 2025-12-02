namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Infrastructure;

    public sealed class SystemHighResolutionClockTUnitTests
    {
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

            long first = clock.GetTimestamp();
            await Task.Delay(1).ConfigureAwait(false);
            long second = clock.GetTimestamp();

            await Assert.That(first).IsLessThan(second).ConfigureAwait(false);
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
            const int DelayMilliseconds = 5;
            long start = clock.GetTimestamp();
            await Task.Delay(DelayMilliseconds).ConfigureAwait(false);

            long elapsed = clock.GetElapsedMilliseconds(start);

            int expectedMinimum = DelayMilliseconds - 2;
            await Assert
                .That(elapsed)
                .IsGreaterThanOrEqualTo(expectedMinimum)
                .ConfigureAwait(false);
        }
    }
}
