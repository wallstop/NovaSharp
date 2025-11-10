namespace NovaSharp.Interpreter.Diagnostics
{
    using System;
    using System.Text;
    using PerformanceCounters;

    /// <summary>
    /// A single object of this type exists for every script and gives access to performance statistics.
    /// </summary>
    public class PerformanceStatistics
    {
        private IPerformanceStopwatch[] _stopwatches = new IPerformanceStopwatch[
            (int)PerformanceCounter.LastValue
        ];

        private static IPerformanceStopwatch[] _globalStopwatches = new IPerformanceStopwatch[
            (int)PerformanceCounter.LastValue
        ];

        private bool _enabled;

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
                if (value && !_enabled)
                {
                    if (_globalStopwatches[(int)PerformanceCounter.AdaptersCompilation] == null)
                    {
                        _globalStopwatches[(int)PerformanceCounter.AdaptersCompilation] =
                            new GlobalPerformanceStopwatch(PerformanceCounter.AdaptersCompilation);
                    }

                    for (int i = 0; i < (int)PerformanceCounter.LastValue; i++)
                    {
                        _stopwatches[i] =
                            _globalStopwatches[i]
                            ?? new PerformanceStopwatch((PerformanceCounter)i);
                    }
                }
                else if (!value && _enabled)
                {
                    _stopwatches = new IPerformanceStopwatch[(int)PerformanceCounter.LastValue];
                    _globalStopwatches = new IPerformanceStopwatch[
                        (int)PerformanceCounter.LastValue
                    ];
                }

                _enabled = value;
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
            IPerformanceStopwatch pco = _globalStopwatches[(int)pc];
            return (pco != null) ? pco.Start() : null;
        }

        /// <summary>
        /// Gets a string with a complete performance log.
        /// </summary>
        /// <returns></returns>
        public string GetPerformanceLog()
        {
            StringBuilder sb = new();

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
    }
}
