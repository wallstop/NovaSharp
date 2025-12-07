namespace WallstopStudios.NovaSharp.Interpreter.Interop.Attributes
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;

    /// <summary>
    /// Marks a module method or constant as only available for a subset of Lua compatibility versions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public sealed class LuaCompatibilityAttribute : Attribute
    {
        public LuaCompatibilityAttribute(
            LuaCompatibilityVersion minVersion = LuaCompatibilityVersion.Lua52,
            LuaCompatibilityVersion maxVersion = LuaCompatibilityVersion.Lua55
        )
        {
            MinVersion = minVersion;
            MaxVersion = maxVersion;
        }

        /// <summary>
        /// Gets the minimum Lua compatibility version that exposes the annotated member.
        /// </summary>
        public LuaCompatibilityVersion MinVersion { get; }

        /// <summary>
        /// Gets the maximum Lua compatibility version that exposes the annotated member.
        /// </summary>
        public LuaCompatibilityVersion MaxVersion { get; }

        /// <summary>
        /// Determines whether the annotated member should be surfaced for the specified version.
        /// </summary>
        internal bool IsSupported(LuaCompatibilityVersion version)
        {
            LuaCompatibilityVersion normalizedVersion = LuaVersionDefaults.ResolveForHighest(
                version
            );
            LuaCompatibilityVersion normalizedMin = LuaVersionDefaults.ResolveForHighest(
                MinVersion
            );
            LuaCompatibilityVersion normalizedMax = LuaVersionDefaults.ResolveForHighest(
                MaxVersion
            );

            int value = GetComparableValue(normalizedVersion);
            return value >= GetComparableValue(normalizedMin)
                && value <= GetComparableValue(normalizedMax);
        }

        private static int GetComparableValue(LuaCompatibilityVersion version)
        {
            return (int)version;
        }
    }
}
