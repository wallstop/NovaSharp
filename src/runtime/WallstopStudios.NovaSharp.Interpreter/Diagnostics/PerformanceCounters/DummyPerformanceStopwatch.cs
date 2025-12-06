namespace WallstopStudios.NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;

    /// <summary>
    /// No-op stopwatch used when performance counters are disabled.
    /// </summary>
    internal class DummyPerformanceStopwatch : IPerformanceStopwatch, IDisposable
    {
        public static DummyPerformanceStopwatch Instance = new();
        private readonly PerformanceResult _result;

        private DummyPerformanceStopwatch()
        {
            _result = new PerformanceResult()
            {
                Counter = 0,
                Global = true,
                Instances = 0,
                Name = "::dummy::",
                Type = PerformanceCounterType.TimeMilliseconds,
            };
        }

        /// <inheritdoc/>
        public IDisposable Start()
        {
            return this;
        }

        /// <inheritdoc/>
        public PerformanceResult GetResult()
        {
            return _result;
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
