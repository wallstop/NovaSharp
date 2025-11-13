namespace NovaSharp.Interpreter.Diagnostics
{
    using System;

    /// <summary>
    /// Enumeration of unit of measures of the performance counters
    /// </summary>
    public enum PerformanceCounterType
    {
        /// <summary>
        /// Legacy placeholder; prefer an explicit counter type.
        /// </summary>
        [Obsolete("Use a concrete PerformanceCounterType value.", false)]
        Unknown = 0,

        /// <summary>
        /// The performance counter is specified in bytes (of memory)
        /// </summary>
        MemoryBytes = 1,

        /// <summary>
        /// The performance counter is specified in milliseconds
        /// </summary>
        TimeMilliseconds = 2,
    }
}
