namespace WallstopStudios.NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;

    /// <summary>
    /// Abstraction over stopwatch implementations so both global and per-script counters share the same API.
    /// </summary>
    internal interface IPerformanceStopwatch
    {
        /// <summary>
        /// Starts (or re-enters) the stopwatch and returns a disposable scope for RAII usage.
        /// </summary>
        public IDisposable Start();

        /// <summary>
        /// Retrieves the aggregated result captured by this stopwatch.
        /// </summary>
        public PerformanceResult GetResult();
    }
}
