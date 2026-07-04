namespace NovaSharp
{
    using System;

    /// <summary>
    /// Provides deterministic time to a facade engine.
    /// </summary>
    public interface ILuaTimeProvider
    {
        /// <summary>
        /// Gets the current UTC time.
        /// </summary>
        public DateTimeOffset GetUtcNow();
    }
}
