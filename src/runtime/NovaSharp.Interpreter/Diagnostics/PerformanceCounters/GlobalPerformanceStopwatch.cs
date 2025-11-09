namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// This class is not *really* IDisposable.. it's just use to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal class GlobalPerformanceStopwatch : IPerformanceStopwatch
    {
        private class GlobalPerformanceStopwatchStopwatchObject : IDisposable
        {
            private readonly Stopwatch _stopwatch;
            private readonly GlobalPerformanceStopwatch _parent;

            public GlobalPerformanceStopwatchStopwatchObject(GlobalPerformanceStopwatch parent)
            {
                _parent = parent;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _parent.SignalStopwatchTerminated(_stopwatch);
            }
        }

        private int _count = 0;
        private long _elapsed = 0;
        private readonly PerformanceCounter _counter;

        public GlobalPerformanceStopwatch(PerformanceCounter perfcounter)
        {
            _counter = perfcounter;
        }

        private void SignalStopwatchTerminated(Stopwatch sw)
        {
            _elapsed += sw.ElapsedMilliseconds;
            _count += 1;
        }

        public IDisposable Start()
        {
            return new GlobalPerformanceStopwatchStopwatchObject(this);
        }

        public PerformanceResult GetResult()
        {
            return new PerformanceResult()
            {
                Type = PerformanceCounterType.TimeMilliseconds,
                Global = false,
                Name = _counter.ToString(),
                Instances = _count,
                Counter = _elapsed,
            };
        }
    }
}
