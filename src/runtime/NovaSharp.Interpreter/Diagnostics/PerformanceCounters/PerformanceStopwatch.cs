namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;
    using NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// This class is not *really* IDisposable.. it's just use to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal sealed class PerformanceStopwatch : IDisposable, IPerformanceStopwatch
    {
        private readonly PerformanceCounter _counter;
        private readonly IHighResolutionClock _clock;
        private int _count;
        private int _reentrant;
        private long? _runningSince;
        private long _elapsedMilliseconds;

        public PerformanceStopwatch(
            PerformanceCounter perfcounter,
            IHighResolutionClock clock = null
        )
        {
            _counter = perfcounter;
            _clock = clock ?? SystemHighResolutionClock.Instance;
        }

        public IDisposable Start()
        {
            if (_reentrant == 0)
            {
                _count += 1;
                _runningSince = _clock.GetTimestamp();
            }

            _reentrant += 1;

            return this;
        }

        public void Dispose()
        {
            _reentrant -= 1;

            if (_reentrant == 0 && _runningSince.HasValue)
            {
                long now = _clock.GetTimestamp();
                _elapsedMilliseconds += _clock.GetElapsedMilliseconds(_runningSince.Value, now);
                _runningSince = null;
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
                Counter = _elapsedMilliseconds,
            };
        }
    }
}
