namespace NovaSharp.Benchmarks
{
    using System;
    using BenchmarkDotNet.Loggers;

    internal sealed class ConsoleLoggerWithoutSummary : ILogger
    {
        private readonly ILogger _inner;

        public ConsoleLoggerWithoutSummary(ILogger inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string Id => _inner.Id;

        public int Priority => _inner.Priority;

        public void Write(LogKind logKind, string text)
        {
            if (ShouldSuppress(logKind, text))
            {
                return;
            }

            _inner.Write(logKind, text);
        }

        public void WriteLine()
        {
            _inner.WriteLine();
        }

        public void WriteLine(LogKind logKind, string text)
        {
            if (ShouldSuppress(logKind, text))
            {
                return;
            }

            _inner.WriteLine(logKind, text);
        }

        public void Flush()
        {
            _inner.Flush();
        }

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
