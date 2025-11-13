namespace NovaSharp.Interpreter.Compatibility
{
    using System;

    /// <summary>
    /// Enumerates the supported Lua compatibility targets.
    /// </summary>
    public enum LuaCompatibilityVersion
    {
        /// <summary>
        /// Legacy placeholder; prefer a concrete version.
        /// </summary>
        [Obsolete("Select an explicit Lua compatibility version.", false)]
        Unknown = 0,

        /// <summary>
        /// The most recent NovaSharp compatibility surface (currently aligning with Lua 5.4+ semantics).
        /// </summary>
        Latest = 1,

        /// <summary>
        /// Compatibility targeting Lua 5.5 (preview/future).
        /// </summary>
        Lua55 = 55,

        /// <summary>
        /// Compatibility targeting Lua 5.4 behaviour.
        /// </summary>
        Lua54 = 54,

        /// <summary>
        /// Compatibility targeting Lua 5.3 behaviour.
        /// </summary>
        Lua53 = 53,

        /// <summary>
        /// Compatibility targeting Lua 5.2 behaviour.
        /// </summary>
        Lua52 = 52,
    }
}
