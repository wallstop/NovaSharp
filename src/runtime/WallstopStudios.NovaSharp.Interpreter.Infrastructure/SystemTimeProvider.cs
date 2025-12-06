namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;

    /// <summary>
    /// Production time provider that forwards to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    public sealed class SystemTimeProvider : ITimeProvider
    {
        /// <summary>
        /// Singleton instance for production use.
        /// </summary>
        public static readonly SystemTimeProvider Instance = new();

        private SystemTimeProvider() { }

        /// <inheritdoc />
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
