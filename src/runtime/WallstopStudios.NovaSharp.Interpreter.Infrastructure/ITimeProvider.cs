namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;

    /// <summary>
    /// Minimal abstraction over time retrieval so runtime surfaces can be unit tested without wall-clock delays.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        public DateTimeOffset GetUtcNow();
    }
}
