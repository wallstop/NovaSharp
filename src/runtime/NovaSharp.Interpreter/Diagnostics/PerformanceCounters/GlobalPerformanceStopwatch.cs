namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;
    using NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// This class is not *really* IDisposable.. it's just used to have a RAII like pattern.
    /// You are free to reuse this instance after calling Dispose.
    /// </summary>
    internal sealed class GlobalPerformanceStopwatch : IPerformanceStopwatch
    {
        private sealed class Scope : IDisposable
        {
            private readonly GlobalPerformanceStopwatch _owner;
            private readonly long _startedAt;

            internal Scope(GlobalPerformanceStopwatch owner)
            {
                _owner = owner;
                _startedAt = owner._clock.GetTimestamp();
            }

            public void Dispose()
            {
                long stop = _owner._clock.GetTimestamp();
                _owner.SignalCompleted(_startedAt, stop);
            }
        }

        private readonly PerformanceCounter _counter;
        private readonly IHighResolutionClock _clock;
        private int _count;
        private long _elapsedMilliseconds;

        public GlobalPerformanceStopwatch(
            PerformanceCounter perfcounter,
            IHighResolutionClock clock = null
        )
        {
            _counter = perfcounter;
            _clock = clock ?? SystemHighResolutionClock.Instance;
        }

        private void SignalCompleted(long startTimestamp, long endTimestamp)
        {
            _elapsedMilliseconds += _clock.GetElapsedMilliseconds(startTimestamp, endTimestamp);
            _count += 1;
        }

        public IDisposable Start()
        {
            return new Scope(this);
        }

        public PerformanceResult GetResult()
        {
            return new PerformanceResult()
            {
                Type = PerformanceCounterType.TimeMilliseconds,
                Global = true,
                Name = _counter.ToString(),
                Instances = _count,
                Counter = _elapsedMilliseconds,
            };
        }
    }
}
