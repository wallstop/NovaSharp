namespace NovaSharp.Interpreter.Infrastructure
{
    using System;

    /// <summary>
    /// Production time provider that forwards to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    internal sealed class SystemTimeProvider : ITimeProvider
    {
        internal static readonly SystemTimeProvider Instance = new();

        private SystemTimeProvider() { }

        /// <inheritdoc />
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
