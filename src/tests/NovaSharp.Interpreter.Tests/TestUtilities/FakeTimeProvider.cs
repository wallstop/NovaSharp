namespace NovaSharp.Interpreter.Tests.TestUtilities
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.Infrastructure;

    [SuppressMessage(
        "Performance",
        "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated via linked TUnit projects."
    )]
    internal sealed class FakeTimeProvider : ITimeProvider
    {
        private DateTimeOffset _current;

        internal FakeTimeProvider(DateTimeOffset? start = null)
        {
            _current = start ?? DateTimeOffset.UnixEpoch;
        }

        public DateTimeOffset GetUtcNow()
        {
            return _current;
        }

        internal void Advance(TimeSpan delta)
        {
            _current += delta;
        }
    }
}
