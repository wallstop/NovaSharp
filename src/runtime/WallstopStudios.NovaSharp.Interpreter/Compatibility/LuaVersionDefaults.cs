namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    /// <summary>
    /// Provides centralized constants and helpers for <see cref="LuaCompatibilityVersion"/> resolution.
    /// This class ensures consistent handling of <see cref="LuaCompatibilityVersion.Latest"/> across the codebase.
    /// </summary>
    /// <remarks>
    /// When NovaSharp adopts a new Lua version as the default, only <see cref="CurrentDefault"/> needs updating.
    /// All code using <see cref="Resolve"/> will automatically pick up the change.
    /// </remarks>
    public static class LuaVersionDefaults
    {
        /// <summary>
        /// The concrete Lua version that <see cref="LuaCompatibilityVersion.Latest"/> resolves to.
        /// Currently maps to Lua 5.4, the latest stable release of the Lua language.
        /// </summary>
        /// <remarks>
        /// Update this single constant when adopting a new Lua version as the default.
        /// All version-dependent logic should use <see cref="Resolve"/> instead of hardcoding version mappings.
        /// </remarks>
        public const LuaCompatibilityVersion CurrentDefault = LuaCompatibilityVersion.Lua54;

        /// <summary>
        /// The highest Lua version supported by NovaSharp for forward-compatibility checks.
        /// This may differ from <see cref="CurrentDefault"/> when experimental support exists
        /// for upcoming Lua versions.
        /// </summary>
        public const LuaCompatibilityVersion HighestSupported = LuaCompatibilityVersion.Lua55;

        /// <summary>
        /// Resolves <see cref="LuaCompatibilityVersion.Latest"/> to <see cref="CurrentDefault"/>,
        /// returning all other versions unchanged.
        /// </summary>
        /// <param name="version">The version to resolve.</param>
        /// <returns>
        /// <see cref="CurrentDefault"/> if <paramref name="version"/> is <see cref="LuaCompatibilityVersion.Latest"/>;
        /// otherwise, <paramref name="version"/> unchanged.
        /// </returns>
        public static LuaCompatibilityVersion Resolve(LuaCompatibilityVersion version)
        {
            return version == LuaCompatibilityVersion.Latest ? CurrentDefault : version;
        }

        /// <summary>
        /// Resolves <see cref="LuaCompatibilityVersion.Latest"/> to <see cref="HighestSupported"/>,
        /// returning all other versions unchanged. Use this for forward-compatibility checks
        /// where experimental version support should be included.
        /// </summary>
        /// <param name="version">The version to resolve.</param>
        /// <returns>
        /// <see cref="HighestSupported"/> if <paramref name="version"/> is <see cref="LuaCompatibilityVersion.Latest"/>;
        /// otherwise, <paramref name="version"/> unchanged.
        /// </returns>
        public static LuaCompatibilityVersion ResolveForHighest(LuaCompatibilityVersion version)
        {
            return version == LuaCompatibilityVersion.Latest ? HighestSupported : version;
        }
    }
}
