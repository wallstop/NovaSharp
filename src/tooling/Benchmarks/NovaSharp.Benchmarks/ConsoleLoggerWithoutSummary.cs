namespace NovaSharp.Benchmarks
{
    using System;
    using BenchmarkDotNet.Loggers;

    /// <summary>
    /// BenchmarkDotNet logger wrapper that suppresses table/summary output so console runs stay concise.
    /// </summary>
    internal sealed class ConsoleLoggerWithoutSummary : ILogger
    {
        private readonly ILogger _inner;

        /// <summary>
        /// Initializes a new instance that forwards non-suppressed messages to <paramref name="inner"/>.
        /// </summary>
        /// <param name="inner">Underlying logger receiving non-summary output.</param>
        public ConsoleLoggerWithoutSummary(ILogger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public string Id => _inner.Id;

        /// <inheritdoc />
        public int Priority => _inner.Priority;

        /// <summary>
        /// Writes a log entry unless it matches the suppressed benchmark summary formats.
        /// </summary>
        /// <param name="logKind">BenchmarkDotNet log kind.</param>
        /// <param name="text">Message text.</param>
        public void Write(LogKind logKind, string text)
        {
            if (ShouldSuppress(logKind, text))
            {
                return;
            }

            _inner.Write(logKind, text);
        }

        /// <inheritdoc />
        public void WriteLine()
        {
            _inner.WriteLine();
        }

        /// <summary>
        /// Writes a log entry followed by a newline unless it matches the suppressed summary formats.
        /// </summary>
        /// <param name="logKind">BenchmarkDotNet log kind.</param>
        /// <param name="text">Message text.</param>
        public void WriteLine(LogKind logKind, string text)
        {
            if (ShouldSuppress(logKind, text))
            {
                return;
            }

            _inner.WriteLine(logKind, text);
        }

        /// <inheritdoc />
        public void Flush()
        {
            _inner.Flush();
        }

        /// <summary>
        /// Returns <c>true</c> when the supplied entry looks like a BenchmarkDotNet summary table row.
        /// </summary>
        /// <param name="logKind">BenchmarkDotNet log kind.</param>
        /// <param name="text">Message text.</param>
        /// <returns><c>true</c> to drop the message; otherwise <c>false</c>.</returns>
        private static bool ShouldSuppress(LogKind logKind, string text)
        {
            if (logKind != LogKind.Result || text == null)
            {
                return false;
            }

            string trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return false;
            }

            if (trimmed.StartsWith('|'))
            {
                return true;
            }

            if (
                trimmed.StartsWith("Method", StringComparison.OrdinalIgnoreCase)
                && trimmed.Contains('|', StringComparison.Ordinal)
            )
            {
                return true;
            }

            if (
                trimmed.StartsWith("Mean", StringComparison.OrdinalIgnoreCase)
                && trimmed.Contains(" = ", StringComparison.Ordinal)
            )
            {
                return true;
            }

            return false;
        }
    }
}
