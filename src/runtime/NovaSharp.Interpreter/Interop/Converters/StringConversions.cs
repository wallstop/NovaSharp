namespace NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    internal static class StringConversions
    {
        internal enum StringSubtype
        {
            [Obsolete("Use a specific StringSubtype.", false)]
            None = 0,
            String = 1,
            StringBuilder = 2,
            Char = 3,
        }

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
