namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    using System;

    /// <summary>
    /// Extension helpers for <see cref="LuaCompatibilityProfile"/>.
    /// </summary>
    public static class LuaCompatibilityProfileExtensions
    {
        /// <summary>
        /// Returns a concise string describing the profile's feature toggles (bitwise operators, bit32, utf8, table.move, &lt;const&gt;, &lt;close&gt;, warn).
        /// </summary>
        public static string GetFeatureSummary(this LuaCompatibilityProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return $"{profile.DisplayName} (bitwise {FormatFlag(profile.SupportsBitwiseOperators)}, bit32 {FormatFlag(profile.SupportsBit32Library)}, utf8 {FormatFlag(profile.SupportsUtf8Library)}, table.move {FormatFlag(profile.SupportsTableMove)}, <const> {FormatFlag(profile.SupportsConstLocals)}, <close> {FormatFlag(profile.SupportsToBeClosedVariables)}, warn {FormatFlag(profile.SupportsWarnFunction)})";
        }

        private static string FormatFlag(bool value)
        {
            return value ? "on" : "off";
        }
    }
}
