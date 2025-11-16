namespace NovaSharp.Interpreter.Infrastructure
{
    /// <summary>
    /// Provides access to high-resolution, monotonic timestamps.
    /// </summary>
    public interface IHighResolutionClock
    {
        public long GetTimestamp();

        public double TimestampFrequency { get; }

        public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null);
    }
}
