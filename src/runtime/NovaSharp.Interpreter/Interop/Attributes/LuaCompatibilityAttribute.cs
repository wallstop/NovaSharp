namespace NovaSharp.Interpreter.Interop.Attributes
{
    using System;
    using NovaSharp.Interpreter.Compatibility;

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

        public LuaCompatibilityVersion MinVersion { get; }

        public LuaCompatibilityVersion MaxVersion { get; }

        internal bool IsSupported(LuaCompatibilityVersion version)
        {
            LuaCompatibilityVersion normalizedVersion = Normalize(version);
            LuaCompatibilityVersion normalizedMin = Normalize(MinVersion);
            LuaCompatibilityVersion normalizedMax = Normalize(MaxVersion);

            int value = GetComparableValue(normalizedVersion);
            return value >= GetComparableValue(normalizedMin)
                && value <= GetComparableValue(normalizedMax);
        }

        private static LuaCompatibilityVersion Normalize(LuaCompatibilityVersion version)
        {
            return version == LuaCompatibilityVersion.Latest
                ? LuaCompatibilityVersion.Lua55
                : version;
        }

        private static int GetComparableValue(LuaCompatibilityVersion version)
        {
            return (int)version;
        }
    }
}
