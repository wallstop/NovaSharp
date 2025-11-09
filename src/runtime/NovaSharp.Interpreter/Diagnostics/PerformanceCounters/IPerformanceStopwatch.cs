using System;

namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    internal interface IPerformanceStopwatch
    {
        IDisposable Start();
        PerformanceResult GetResult();
    }
}
