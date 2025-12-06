#if PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET))
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Diagnostics
{
    /// <summary>
    /// Minimal Stopwatch shim used on platforms that lack <see cref="System.Diagnostics.Stopwatch"/>.
    /// It tracks elapsed time using <see cref="DateTime.UtcNow"/> so performance counters keep
    /// working inside PCL/Unity builds.
    /// </summary>
    internal class Stopwatch
    {
        private DateTime startTime;

        private DateTime stopTime;

        /// <summary>
        /// Records the current timestamp so a subsequent <see cref="Stop"/> call can report elapsed
        /// time.
        /// </summary>
        public void Start()
        {
            startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Captures the current timestamp and freezes the stopwatch.
        /// </summary>
        public void Stop()
        {
            stopTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates and starts a new stopwatch in a single call.
        /// </summary>
        /// <returns>The running stopwatch instance.</returns>
        public static Stopwatch StartNew()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            return sw;
        }

        /// <summary>
        /// Gets the elapsed time in milliseconds between the last <see cref="Start"/> and
        /// <see cref="Stop"/> calls.
        /// </summary>
        public long ElapsedMilliseconds
        {
            get { return (long)((stopTime - startTime).TotalMilliseconds); }
        }
    }
}
#endif
