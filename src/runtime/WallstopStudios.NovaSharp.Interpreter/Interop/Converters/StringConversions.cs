namespace WallstopStudios.NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Provides helpers for mapping Lua strings onto common CLR string-like types.
    /// </summary>
    internal static class StringConversions
    {
        /// <summary>
        /// Identifies the CLR target shape for a string conversion.
        /// </summary>
        internal enum StringSubtype
        {
            [Obsolete("Use a specific StringSubtype.", false)]
            None = 0,
            String = 1,
            StringBuilder = 2,
            Char = 3,
        }

        /// <summary>
        /// Determines which string subtype best matches the requested <paramref name="desiredType"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static StringSubtype GetStringSubtype(Type desiredType)
        {
            if (desiredType == typeof(string))
            {
                return StringSubtype.String;
            }
            else if (desiredType == typeof(StringBuilder))
            {
                return StringSubtype.StringBuilder;
            }
            else if (desiredType == typeof(char))
            {
                return StringSubtype.Char;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Converts the supplied string into the requested CLR shape.
        /// </summary>
        internal static object ConvertString(
            StringSubtype stringSubType,
            string str,
            Type desiredType,
            DataType dataType
        )
        {
            switch (stringSubType)
            {
                case StringSubtype.String:
                    return str;
                case StringSubtype.StringBuilder:
                    return new StringBuilder(str);
                case StringSubtype.Char:
                    if (!string.IsNullOrEmpty(str))
                    {
                        return str[0];
                    }

                    break;
                default:
                    break;
            }

            throw ScriptRuntimeException.ConvertObjectFailed(dataType, desiredType);
        }
    }
}
