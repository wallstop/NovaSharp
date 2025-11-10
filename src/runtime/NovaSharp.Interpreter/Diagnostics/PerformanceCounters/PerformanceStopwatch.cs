namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// This class is not *really* IDisposable.. it's just use to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal class PerformanceStopwatch : IDisposable, IPerformanceStopwatch
    {
        private readonly Stopwatch _stopwatch = new();
        private int _count;
        private int _reentrant;
        private readonly PerformanceCounter _counter;

        public PerformanceStopwatch(PerformanceCounter perfcounter)
        {
            _counter = perfcounter;
        }

        public IDisposable Start()
        {
            if (_reentrant == 0)
            {
                _count += 1;
                _stopwatch.Start();
            }

            _reentrant += 1;

            return this;
        }

        public void Dispose()
        {
            _reentrant -= 1;

            if (_reentrant == 0)
            {
                _stopwatch.Stop();
            }
        }

        public PerformanceResult GetResult()
        {
            return new PerformanceResult()
            {
                Type = PerformanceCounterType.TimeMilliseconds,
                Global = false,
                Name = _counter.ToString(),
                Instances = _count,
                Counter = _stopwatch.ElapsedMilliseconds,
            };
        }
    }
}
