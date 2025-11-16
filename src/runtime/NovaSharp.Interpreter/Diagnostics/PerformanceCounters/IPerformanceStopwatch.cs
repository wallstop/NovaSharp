namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;

    internal interface IPerformanceStopwatch
    {
        public IDisposable Start();
        public PerformanceResult GetResult();
    }
}
