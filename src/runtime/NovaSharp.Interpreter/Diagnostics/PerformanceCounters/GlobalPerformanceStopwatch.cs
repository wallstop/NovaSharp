namespace NovaSharp.Interpreter.Diagnostics.PerformanceCounters
{
    using System;
    using NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// Stopwatch that aggregates timings across scripts for a given performance counter.
    /// </summary>
    /// <remarks>
    /// The stopwatch itself is not disposable; consumers call <see cref="Start"/> to obtain a scope
    /// that reports elapsed time when disposed, enabling idiomatic RAII usage.
    /// </remarks>
    internal sealed class GlobalPerformanceStopwatch : IPerformanceStopwatch
    {
        /// <summary>
        /// Disposable scope returned by <see cref="GlobalPerformanceStopwatch.Start"/> to capture elapsed time.
        /// </summary>
        private sealed class Scope : IDisposable
        {
            private readonly GlobalPerformanceStopwatch _owner;
            private readonly long _startedAt;

            internal Scope(GlobalPerformanceStopwatch owner)
            {
                _owner = owner;
                _startedAt = owner._clock.GetTimestamp();
            }

            /// <summary>
            /// Stops the measurement scope and reports the elapsed duration to the owner.
            /// </summary>
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

        /// <summary>
        /// Initializes a stopwatch bound to the specified performance counter.
        /// </summary>
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

        /// <inheritdoc/>
        public IDisposable Start()
        {
            return new Scope(this);
        }

        /// <inheritdoc/>
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
