namespace NovaSharp.Interpreter.Infrastructure
{
    /// <summary>
    /// Provides access to high-resolution, monotonic timestamps.
    /// </summary>
    public interface IHighResolutionClock
    {
        /// <summary>
        /// Gets the current timestamp exposed by the underlying high-resolution clock.
        /// </summary>
        public long GetTimestamp();

        /// <summary>
        /// Gets the clock frequency expressed in ticks per second.
        /// </summary>
        public double TimestampFrequency { get; }

        /// <summary>
        /// Computes the elapsed milliseconds between <paramref name="startTimestamp"/> and <paramref name="endTimestamp"/>.
        /// </summary>
        /// <param name="startTimestamp">Timestamp captured at the start of the interval.</param>
        /// <param name="endTimestamp">Optional end timestamp; when omitted the current timestamp is used.</param>
        /// <returns>Elapsed milliseconds as a whole number.</returns>
        public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null);
    }
}
