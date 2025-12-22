namespace WallstopStudios.NovaSharp.Interpreter.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Provides helpers that serialize Lua tables/values into Lua source-friendly string representations.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Serializes a prime (ownerless) table into Lua syntax.
        /// </summary>
        /// <param name="table">Table to serialize (must not be owned by a script).</param>
        /// <param name="prefixReturn">True to emit a leading <c>return</c> statement.</param>
        /// <param name="tabs">Indentation depth used when emitting nested tables.</param>
        /// <returns>A string containing Lua code that recreates the table.</returns>
        public static string Serialize(this Table table, bool prefixReturn = false, int tabs = 0)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (table.OwnerScript != null)
            {
                throw new ScriptRuntimeException("Table is not a prime table.");
            }

            string tabString = new string('\t', tabs);
            using Utf16ValueStringBuilder builder = ZStringBuilder.CreateNested();

            if (prefixReturn)
            {
                builder.Append("return ");
            }

            if (table.Count == 0)
            {
                builder.Append("{}");

                if (tabs == 0)
                {
                    builder.AppendLine();
                }

                return builder.ToString();
            }

            builder.AppendLine("{");

            foreach (TablePair tp in table.GetPairsEnumerator())
            {
                builder.Append(tabString);
                builder.Append('\t');

                string key = IsStringIdentifierValid(tp.Key)
                    ? tp.Key.String
                    : "[" + SerializeValue(tp.Key, tabs + 1) + "]";

                builder.Append(key);
                builder.Append(" = ");
                builder.Append(SerializeValue(tp.Value, tabs + 1));
                builder.Append(',');
                builder.AppendLine();
            }

            builder.Append(tabString);
            builder.Append('}');

            if (tabs == 0)
            {
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static bool IsStringIdentifierValid(DynValue dynValue)
        {
            if (dynValue.Type != DataType.String)
            {
                return false;
            }

            if (dynValue.String.Length == 0)
            {
                return false;
            }

            if (LuaKeywords.All.Contains(dynValue.String))
            {
                return false;
            }

            if (!char.IsLetter(dynValue.String[0]) && (dynValue.String[0] != '_'))
            {
                return false;
            }

            foreach (char c in dynValue.String)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Serializes a primitive value (or prime table) into Lua syntax.
        /// </summary>
        /// <param name="dynValue">Value to serialize.</param>
        /// <param name="tabs">Indentation depth used when emitting nested tables.</param>
        /// <returns>Lua string representing the supplied value.</returns>
        public static string SerializeValue(this DynValue dynValue, int tabs = 0)
        {
            if (dynValue == null)
            {
                throw new ArgumentNullException(nameof(dynValue));
            }

            if (dynValue.Type == DataType.Nil || dynValue.Type == DataType.Void)
            {
                return LuaKeywords.Nil;
            }
            else if (dynValue.Type == DataType.Tuple)
            {
                return dynValue.Tuple.Length > 0
                    ? SerializeValue(dynValue.Tuple[0], tabs)
                    : LuaKeywords.Nil;
            }
            else if (dynValue.Type == DataType.Number)
            {
                return dynValue.Number.ToString("r", CultureInfo.InvariantCulture);
            }
            else if (dynValue.Type == DataType.Boolean)
            {
                return dynValue.Boolean ? LuaKeywords.True : LuaKeywords.False;
            }
            else if (dynValue.Type == DataType.String)
            {
                return EscapeString(dynValue.String ?? "");
            }
            else if (dynValue.Type == DataType.Table && dynValue.Table.OwnerScript == null)
            {
                return Serialize(dynValue.Table, false, tabs);
            }
            else
            {
                throw new ScriptRuntimeException(
                    "Value is not a primitive value or a prime table."
                );
            }
        }

        private static string EscapeString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "\"\"";
            }

            // Fast path: check if any escaping is needed
            bool needsEscape = false;
            foreach (char c in input)
            {
                if (
                    c == '\\'
                    || c == '\n'
                    || c == '\r'
                    || c == '\t'
                    || c == '\a'
                    || c == '\f'
                    || c == '\b'
                    || c == '\v'
                    || c == '"'
                    || c == '\''
                )
                {
                    needsEscape = true;
                    break;
                }
            }

            if (!needsEscape)
            {
                return ZString.Concat("\"", input, "\"");
            }

            // Slow path: build escaped string character by character
            using Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested();
            sb.Append('"');
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append(@"\\");
                        break;
                    case '\n':
                        sb.Append(@"\n");
                        break;
                    case '\r':
                        sb.Append(@"\r");
                        break;
                    case '\t':
                        sb.Append(@"\t");
                        break;
                    case '\a':
                        sb.Append(@"\a");
                        break;
                    case '\f':
                        sb.Append(@"\f");
                        break;
                    case '\b':
                        sb.Append(@"\b");
                        break;
                    case '\v':
                        sb.Append(@"\v");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\'':
                        sb.Append(@"\'");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
