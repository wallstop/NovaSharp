namespace NovaSharp.Interpreter.Tests.TestUtilities
{
    using System;
    using NovaSharp.Interpreter.Infrastructure;

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
