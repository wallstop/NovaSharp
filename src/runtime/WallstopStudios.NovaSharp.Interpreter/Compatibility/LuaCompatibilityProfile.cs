namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    using System;

    /// <summary>
    /// Describes language/library behaviors that change across Lua compatibility versions.
    /// </summary>
    public sealed class LuaCompatibilityProfile
    {
        private static readonly LuaCompatibilityProfile Lua51Profile = new(
            LuaCompatibilityVersion.Lua51,
            supportsBitwiseOperators: false,
            supportsBit32Library: false,
            supportsUtf8Library: false,
            supportsTableMove: false,
            supportsToBeClosedVariables: false,
            supportsConstLocals: false,
            supportsWarnFunction: false
        );

        private static readonly LuaCompatibilityProfile Lua52Profile = new(
            LuaCompatibilityVersion.Lua52,
            supportsBitwiseOperators: false,
            supportsBit32Library: true,
            supportsUtf8Library: false,
            supportsTableMove: false,
            supportsToBeClosedVariables: false,
            supportsConstLocals: false,
            supportsWarnFunction: false
        );

        private static readonly LuaCompatibilityProfile Lua53Profile = new(
            LuaCompatibilityVersion.Lua53,
            supportsBitwiseOperators: true,
            supportsBit32Library: false,
            supportsUtf8Library: true,
            supportsTableMove: true,
            supportsToBeClosedVariables: false,
            supportsConstLocals: false,
            supportsWarnFunction: false
        );

        private static readonly LuaCompatibilityProfile Lua54Profile = new(
            LuaCompatibilityVersion.Lua54,
            supportsBitwiseOperators: true,
            supportsBit32Library: false,
            supportsUtf8Library: true,
            supportsTableMove: true,
            supportsToBeClosedVariables: true,
            supportsConstLocals: true,
            supportsWarnFunction: true
        );

        private static readonly LuaCompatibilityProfile Lua55Profile = new(
            LuaCompatibilityVersion.Lua55,
            supportsBitwiseOperators: true,
            supportsBit32Library: false,
            supportsUtf8Library: true,
            supportsTableMove: true,
            supportsToBeClosedVariables: true,
            supportsConstLocals: true,
            supportsWarnFunction: true
        );

        private LuaCompatibilityProfile(
            LuaCompatibilityVersion version,
            bool supportsBitwiseOperators,
            bool supportsBit32Library,
            bool supportsUtf8Library,
            bool supportsTableMove,
            bool supportsToBeClosedVariables,
            bool supportsConstLocals,
            bool supportsWarnFunction
        )
        {
            Version = version;
            SupportsBitwiseOperators = supportsBitwiseOperators;
            SupportsBit32Library = supportsBit32Library;
            SupportsUtf8Library = supportsUtf8Library;
            SupportsTableMove = supportsTableMove;
            SupportsToBeClosedVariables = supportsToBeClosedVariables;
            SupportsConstLocals = supportsConstLocals;
            SupportsWarnFunction = supportsWarnFunction;
        }

        /// <summary>
        /// Gets the compatibility version represented by this profile.
        /// </summary>
        public LuaCompatibilityVersion Version { get; }

        /// <summary>
        /// Gets a value indicating whether the profile exposes Lua 5.3+ bitwise operators.
        /// </summary>
        public bool SupportsBitwiseOperators { get; }

        /// <summary>
        /// Gets a value indicating whether the legacy <c>bit32</c> library is exposed.
        /// </summary>
        public bool SupportsBit32Library { get; }

        /// <summary>
        /// Gets a value indicating whether the profile exposes the Lua 5.3+ <c>utf8</c> library.
        /// </summary>
        public bool SupportsUtf8Library { get; }

        /// <summary>
        /// Gets a value indicating whether the profile expects <c>table.move</c> (introduced in Lua 5.3).
        /// </summary>
        public bool SupportsTableMove { get; }

        /// <summary>
        /// Gets a value indicating whether <c>local &lt;close&gt;</c> variables are available (Lua 5.4+).
        /// </summary>
        public bool SupportsToBeClosedVariables { get; }

        /// <summary>
        /// Gets a value indicating whether <c>local &lt;const&gt;</c> declarations are available (Lua 5.4+).
        /// </summary>
        public bool SupportsConstLocals { get; }

        /// <summary>
        /// Gets a value indicating whether the built-in <c>warn</c> function is available (Lua 5.4+).
        /// </summary>
        public bool SupportsWarnFunction { get; }

        /// <summary>
        /// Gets the human-readable display name for the active compatibility profile (e.g., "Lua 5.3").
        /// </summary>
        public string DisplayName => GetDisplayName(Version);

        /// <summary>
        /// Returns the profile associated with the specified compatibility version.
        /// </summary>
        public static LuaCompatibilityProfile ForVersion(LuaCompatibilityVersion version)
        {
            return version switch
            {
                LuaCompatibilityVersion.Lua51 => Lua51Profile,
                LuaCompatibilityVersion.Lua52 => Lua52Profile,
                LuaCompatibilityVersion.Lua53 => Lua53Profile,
                LuaCompatibilityVersion.Lua54 => Lua54Profile,
                LuaCompatibilityVersion.Lua55 => Lua55Profile,
                LuaCompatibilityVersion.Latest => Lua54Profile,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(version),
                    version,
                    "Unsupported compatibility version."
                ),
            };
        }

        /// <summary>
        /// Returns the human-readable label for the supplied compatibility version. Internal so both
        /// runtime and tooling can keep their banners/tests in sync without duplicating formatting.
        /// </summary>
        /// <param name="version">The compatibility version to convert.</param>
        /// <returns>A friendly display name such as <c>Lua 5.3</c>.</returns>
        internal static string GetDisplayName(LuaCompatibilityVersion version)
        {
            return version switch
            {
                LuaCompatibilityVersion.Lua51 => "Lua 5.1",
                LuaCompatibilityVersion.Lua52 => "Lua 5.2",
                LuaCompatibilityVersion.Lua53 => "Lua 5.3",
                LuaCompatibilityVersion.Lua54 => "Lua 5.4",
                LuaCompatibilityVersion.Lua55 => "Lua 5.5",
                LuaCompatibilityVersion.Latest => "Lua Latest",
                _ => version.ToString(),
            };
        }
    }
}
