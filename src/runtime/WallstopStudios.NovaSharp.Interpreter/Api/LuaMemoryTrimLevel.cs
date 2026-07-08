namespace NovaSharp
{
    /// <summary>
    /// Describes how aggressively NovaSharp should release retained pool memory.
    /// </summary>
    public enum LuaMemoryTrimLevel
    {
        /// <summary>
        /// Releases entries that have exceeded their idle timeout.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Releases spare entries while preserving configured warm-retain floors.
        /// </summary>
        MemoryPressure = 1,

        /// <summary>
        /// Releases all non-minimum retained entries from NovaSharp-owned shared pools.
        /// </summary>
        Critical = 2,
    }
}
