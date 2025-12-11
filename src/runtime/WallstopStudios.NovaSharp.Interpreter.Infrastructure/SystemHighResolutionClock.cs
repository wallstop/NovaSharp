namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Default implementation backed by <see cref="Stopwatch"/> for production use.
    /// </summary>
    public sealed class SystemHighResolutionClock : IHighResolutionClock
    {
        /// <summary>
        /// Singleton instance for production use.
        /// </summary>
        public static readonly SystemHighResolutionClock Instance = new();
        private readonly double _frequencyHz = Stopwatch.Frequency;

        private SystemHighResolutionClock() { }

        /// <inheritdoc />
        public double TimestampFrequency => _frequencyHz;

        /// <inheritdoc />
        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <inheritdoc />
        public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null)
        {
            long stop = endTimestamp ?? GetTimestamp();
            long delta = stop - startTimestamp;
            return (long)(delta * 1000.0 / _frequencyHz);
        }
    }
}
