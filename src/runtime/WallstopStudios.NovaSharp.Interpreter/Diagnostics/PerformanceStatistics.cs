namespace WallstopStudios.NovaSharp.Interpreter.Diagnostics
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Diagnostics.PerformanceCounters;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;

    /// <summary>
    /// A single object of this type exists for every script and gives access to performance statistics.
    /// </summary>
    public class PerformanceStatistics
    {
        private IPerformanceStopwatch[] _stopwatches = new IPerformanceStopwatch[
            (int)PerformanceCounter.LastValue
        ];

        private static IPerformanceStopwatch[] GlobalStopwatches = new IPerformanceStopwatch[
            (int)PerformanceCounter.LastValue
        ];
        private static readonly object GlobalSyncRoot = new();

        private readonly IHighResolutionClock _clock;
        private readonly object _syncRoot = new();

        /// <summary>
        /// Gets or sets the high-resolution clock shared by every global stopwatch instance.
        /// </summary>
        /// <remarks>
        /// Tests replace this clock with a deterministic fake so global performance counters can be asserted
        /// without depending on wall-clock time. Production code should leave it at the default.
        /// </remarks>
        internal static IHighResolutionClock GlobalClock { get; set; } =
            SystemHighResolutionClock.Instance;

        private bool _enabled;

        internal PerformanceStatistics(IHighResolutionClock clock = null)
        {
            _clock = clock ?? SystemHighResolutionClock.Instance;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this collection of performance stats is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                lock (_syncRoot)
                {
                    if (value && !_enabled)
                    {
                        EnsureGlobalStopwatch(PerformanceCounter.AdaptersCompilation);

                        lock (GlobalSyncRoot)
                        {
                            for (int i = 0; i < (int)PerformanceCounter.LastValue; i++)
                            {
                                _stopwatches[i] =
                                    GlobalStopwatches[i]
                                    ?? new PerformanceStopwatch((PerformanceCounter)i, _clock);
                            }
                        }
                    }
                    else if (!value && _enabled)
                    {
                        _stopwatches = new IPerformanceStopwatch[(int)PerformanceCounter.LastValue];

                        lock (GlobalSyncRoot)
                        {
                            GlobalStopwatches = new IPerformanceStopwatch[
                                (int)PerformanceCounter.LastValue
                            ];
                        }
                    }

                    _enabled = value;
                }
            }
        }

        /// <summary>
        /// Gets the result of the specified performance counter .
        /// </summary>
        /// <param name="pc">The PerformanceCounter.</param>
        /// <returns></returns>
        public PerformanceResult GetPerformanceCounterResult(PerformanceCounter pc)
        {
            IPerformanceStopwatch pco = _stopwatches[(int)pc];
            return (pco != null) ? pco.GetResult() : null;
        }

        /// <summary>
        /// Starts a stopwatch.
        /// </summary>
        /// <returns></returns>
        internal IDisposable StartStopwatch(PerformanceCounter pc)
        {
            IPerformanceStopwatch pco = _stopwatches[(int)pc];
            return (pco != null) ? pco.Start() : null;
        }

        /// <summary>
        /// Starts a stopwatch.
        /// </summary>
        /// <returns></returns>
        internal static IDisposable StartGlobalStopwatch(PerformanceCounter pc)
        {
            lock (GlobalSyncRoot)
            {
                if (GlobalStopwatches[(int)pc] == null)
                {
                    GlobalStopwatches[(int)pc] = new GlobalPerformanceStopwatch(pc, GlobalClock);
                }

                return GlobalStopwatches[(int)pc].Start();
            }
        }

        /// <summary>
        /// Gets a string with a complete performance log.
        /// </summary>
        /// <returns></returns>
        public string GetPerformanceLog()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = 0; i < (int)PerformanceCounter.LastValue; i++)
            {
                PerformanceResult res = GetPerformanceCounterResult((PerformanceCounter)i);
                if (res != null)
                {
                    sb.AppendLine(res.ToString());
                }
            }

            return sb.ToString();
        }

        private static void EnsureGlobalStopwatch(PerformanceCounter counter)
        {
            lock (GlobalSyncRoot)
            {
                if (GlobalStopwatches[(int)counter] == null)
                {
                    GlobalStopwatches[(int)counter] = new GlobalPerformanceStopwatch(
                        counter,
                        GlobalClock
                    );
                }
            }
        }

        /// <summary>
        /// Provides helpers that allow tests to interrogate and reset the global stopwatch cache.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Clears the global stopwatch cache so tests can assert cold-start behaviour.
            /// </summary>
            public static void ResetGlobalStopwatches()
            {
                lock (GlobalSyncRoot)
                {
                    GlobalStopwatches = new IPerformanceStopwatch[
                        (int)PerformanceCounter.LastValue
                    ];
                }
            }
        }
    }
}
