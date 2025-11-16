namespace NovaSharp.Interpreter.Infrastructure
{
    using System;

    internal sealed class SystemTimeProvider : ITimeProvider
    {
        internal static readonly SystemTimeProvider Instance = new();

        private SystemTimeProvider() { }

        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
