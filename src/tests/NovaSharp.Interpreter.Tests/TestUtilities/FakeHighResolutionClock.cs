namespace NovaSharp.Interpreter.Tests.TestUtilities
{
    using NovaSharp.Interpreter.Infrastructure;

    internal sealed class FakeHighResolutionClock : IHighResolutionClock
    {
        private long _timestamp;
        private readonly double _frequencyHz;

        internal FakeHighResolutionClock(double frequencyHz = 1_000_000)
        {
            _frequencyHz = frequencyHz;
        }

        public double TimestampFrequency => _frequencyHz;

        public long GetTimestamp()
        {
            return _timestamp;
        }

        public long GetElapsedMilliseconds(long startTimestamp, long? endTimestamp = null)
        {
            long stop = endTimestamp ?? _timestamp;
            long delta = stop - startTimestamp;
            return (long)(delta * 1000.0 / _frequencyHz);
        }

        internal void AdvanceMilliseconds(long milliseconds)
        {
            _timestamp += (long)(_frequencyHz * milliseconds / 1000.0);
        }
    }
}
