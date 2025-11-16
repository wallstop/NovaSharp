namespace NovaSharp.Interpreter.Infrastructure
{
    using System;
    using System.Diagnostics;

    internal sealed class SystemHighResolutionClock : IHighResolutionClock
    {
        internal static readonly SystemHighResolutionClock Instance = new();
        private readonly double _frequencyHz = Stopwatch.Frequency;

        private SystemHighResolutionClock() { }

        public double TimestampFrequency => _frequencyHz;

        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null)
        {
            long stop = endTimestamp ?? GetTimestamp();
            long delta = stop - startTimestamp;
            return (long)(delta * 1000.0 / _frequencyHz);
        }
    }
}
