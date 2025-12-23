namespace WallstopStudios.NovaSharp.Interpreter.Compatibility
{
    using System;
    using Cysharp.Text;

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

            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
            sb.Append(profile.DisplayName);
            sb.Append(" (bitwise ");
            sb.Append(FormatFlag(profile.SupportsBitwiseOperators));
            sb.Append(", bit32 ");
            sb.Append(FormatFlag(profile.SupportsBit32Library));
            sb.Append(", utf8 ");
            sb.Append(FormatFlag(profile.SupportsUtf8Library));
            sb.Append(", table.move ");
            sb.Append(FormatFlag(profile.SupportsTableMove));
            sb.Append(", <const> ");
            sb.Append(FormatFlag(profile.SupportsConstLocals));
            sb.Append(", <close> ");
            sb.Append(FormatFlag(profile.SupportsToBeClosedVariables));
            sb.Append(", warn ");
            sb.Append(FormatFlag(profile.SupportsWarnFunction));
            sb.Append(')');
            return sb.ToString();
        }

        private static string FormatFlag(bool value)
        {
            return value ? "on" : "off";
        }
    }
}
