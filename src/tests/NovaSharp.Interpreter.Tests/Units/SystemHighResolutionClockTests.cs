namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Diagnostics;
    using System.Threading;
    using NovaSharp.Interpreter.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    public sealed class SystemHighResolutionClockTests
    {
        [Test]
        public void TimestampFrequencyMatchesStopwatch()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;

            Assert.That(clock.TimestampFrequency, Is.EqualTo(Stopwatch.Frequency));
        }

        [Test]
        public void GetTimestampReturnsMonotonicValues()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;

            long first = clock.GetTimestamp();
            Thread.Sleep(1);
            long second = clock.GetTimestamp();

            Assert.That(first, Is.LessThan(second));
        }

        [Test]
        public void GetElapsedMillisecondsUsesEndTimestampWhenProvided()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;
            double freq = clock.TimestampFrequency;
            long start = 10_000;
            long delta = (long)(freq * 1.5); // emulate 1500 ms
            long expected = 1500;

            long elapsed = clock.GetElapsedMilliseconds(start, start + delta);

            Assert.That(elapsed, Is.EqualTo(expected).Within(1));
        }

        [Test]
        public void GetElapsedMillisecondsUsesCurrentTimestampWhenEndMissing()
        {
            SystemHighResolutionClock clock = SystemHighResolutionClock.Instance;
            long start = clock.GetTimestamp();
            Thread.Sleep(5);

            long elapsed = clock.GetElapsedMilliseconds(start);

            Assert.That(elapsed, Is.GreaterThanOrEqualTo(5));
        }
    }
}
