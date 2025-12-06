namespace WallstopStudios.NovaSharp.Interpreter.Serialization.Json
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using Cysharp.Text;
    using Tree;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Class performing conversions between Tables and Json.
    /// NOTE : the conversions are done respecting json syntax but using Lua constructs. This means mostly that:
    /// 1) Lua string escapes can be accepted while they are not technically valid JSON, and vice versa
    /// 2) Null values are represented using a static userdata of type JsonNull
    /// 3) Do not use it when input cannot be entirely trusted
    /// </summary>
    public static class JsonTableConverter
    {
        /// <summary>
        /// Converts a table to a json string
        /// </summary>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        public static string TableToJson(this Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            try
            {
                TableToJson(ref sb, table);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>
        /// Tables to json.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="table">The table.</param>
        private static void TableToJson(ref Utf16ValueStringBuilder sb, Table table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            bool first = true;

            if (table.Length == 0)
            {
                sb.Append('{');
                foreach (TablePair pair in table.Pairs)
                {
                    if (pair.Key.Type == DataType.String && IsValueJsonCompatible(pair.Value))
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        ValueToJson(ref sb, pair.Key);
                        sb.Append(':');
                        ValueToJson(ref sb, pair.Value);

                        first = false;
                    }
                }
                sb.Append('}');
            }
            else
            {
                sb.Append('[');
                for (int i = 1; i <= table.Length; i++)
                {
                    DynValue value = table.Get(i);
                    if (IsValueJsonCompatible(value))
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        ValueToJson(ref sb, value);

                        first = false;
                    }
                }
                sb.Append(']');
            }
        }

        /// <summary>
        /// Converts a generic object to JSON, preserving array semantics for collections.
        /// </summary>
        public static string ObjectToJson(object obj)
        {
            Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            try
            {
                ObjectToJsonCore(ref sb, obj);
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
        }

        /// <summary>
        /// Core JSON serialization that preserves collection/array semantics.
        /// </summary>
        private static void ObjectToJsonCore(ref Utf16ValueStringBuilder sb, object obj)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            // Handle primitives and simple types directly
            switch (obj)
            {
                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;
                case string s:
                    sb.Append(EscapeString(s));
                    return;
                case char c:
                    sb.Append(EscapeString(c.ToString()));
                    return;
                case JsonNull:
                    sb.Append("null");
                    return;
            }

            // Handle numeric types
            if (
                obj
                is sbyte
                    or byte
                    or short
                    or ushort
                    or int
                    or uint
                    or long
                    or ulong
                    or float
                    or double
                    or decimal
            )
            {
                sb.Append(
                    Convert
                        .ToDouble(obj, CultureInfo.InvariantCulture)
                        .ToString("r", CultureInfo.InvariantCulture)
                );
                return;
            }

            // Handle enums as numbers
            if (obj is Enum)
            {
                sb.Append(
                    Convert
                        .ToDouble(obj, CultureInfo.InvariantCulture)
                        .ToString("r", CultureInfo.InvariantCulture)
                );
                return;
            }

            // Handle DynValue
            if (obj is DynValue dv)
            {
                ValueToJson(ref sb, dv);
                return;
            }

            // Handle Table directly
            if (obj is Table table)
            {
                TableToJson(ref sb, table);
                return;
            }

            // Handle dictionaries as objects (before IEnumerable check since dictionaries are enumerable)
            if (obj is IDictionary dict)
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry entry in dict)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    sb.Append(EscapeString(entry.Key?.ToString() ?? string.Empty));
                    sb.Append(':');
                    ObjectToJsonCore(ref sb, entry.Value);
                    first = false;
                }
                sb.Append('}');
                return;
            }

            // Handle collections/arrays as JSON arrays (even when empty)
            if (obj is IEnumerable enumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in enumerable)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    ObjectToJsonCore(ref sb, item);
                    first = false;
                }
                sb.Append(']');
                return;
            }

            // Handle other objects by serializing their properties
            sb.Append('{');
            bool firstProp = true;
            Type type = obj.GetType();
            foreach (PropertyInfo pi in Framework.Do.GetProperties(type))
            {
                MethodInfo getter = Framework.Do.GetGetMethod(pi);
                if (getter == null)
                {
                    continue;
                }
                bool isStatic = getter.IsStatic;
                object value = getter.Invoke(isStatic ? null : obj, null);

                if (!firstProp)
                {
                    sb.Append(',');
                }
                sb.Append(EscapeString(pi.Name));
                sb.Append(':');
                ObjectToJsonCore(ref sb, value);
                firstProp = false;
            }
            sb.Append('}');
        }

        private static void ValueToJson(ref Utf16ValueStringBuilder sb, DynValue value)
        {
            switch (value.Type)
            {
                case DataType.Boolean:
                    sb.Append(value.Boolean ? "true" : "false");
                    break;
                case DataType.Number:
                    sb.Append(value.Number.ToString("r", CultureInfo.InvariantCulture));
                    break;
                case DataType.String:
                    sb.Append(EscapeString(value.String));
                    break;
                case DataType.Table:
                    TableToJson(ref sb, value.Table);
                    break;
                case DataType.Nil:
                case DataType.Void:
                case DataType.UserData:
                default:
                    sb.Append("null");
                    break;
            }
        }

        private static string EscapeString(string input)
        {
            string s = input ?? string.Empty;
            s = ReplaceOrdinal(s, @"\", @"\\");
            s = ReplaceOrdinal(s, @"/", @"\/");
            s = ReplaceOrdinal(s, "\"", "\\\"");
            s = ReplaceOrdinal(s, "\f", @"\f");
            s = ReplaceOrdinal(s, "\b", @"\b");
            s = ReplaceOrdinal(s, "\n", @"\n");
            s = ReplaceOrdinal(s, "\r", @"\r");
            s = ReplaceOrdinal(s, "\t", @"\t");
            return "\"" + s + "\"";
        }

        private static bool IsValueJsonCompatible(DynValue value)
        {
            return value.Type == DataType.Boolean
                || value.IsNil()
                || value.Type == DataType.Number
                || value.Type == DataType.String
                || value.Type == DataType.Table
                || (JsonNull.IsJsonNull(value));
        }

        /// <summary>
        /// Converts a json string to a table
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="script">The script to which the table is assigned (null for prime tables).</param>
        /// <returns>A table containing the representation of the given json.</returns>
        public static Table JsonToTable(string json, Script script = null)
        {
            Lexer l = new(0, json, false);

            if (l.Current.Type == TokenType.BrkOpenCurly)
            {
                return ParseJsonObject(l, script);
            }
            else if (l.Current.Type == TokenType.BrkOpenSquare)
            {
                return ParseJsonArray(l, script);
            }
            else
            {
                throw new SyntaxErrorException(
                    l.Current,
                    "Unexpected token : '{0}'",
                    l.Current.Text
                );
            }
        }

        private static void AssertToken(Lexer l, TokenType type)
        {
            if (l.Current.Type != type)
            {
                throw new SyntaxErrorException(
                    l.Current,
                    "Unexpected token : '{0}'",
                    l.Current.Text
                );
            }
        }

        private static Table ParseJsonArray(Lexer l, Script script)
        {
            Table t = new(script);

            l.Next();

            while (l.Current.Type != TokenType.BrkCloseSquare)
            {
                DynValue v = ParseJsonValue(l, script);
                t.Append(v);
                l.Next();

                if (l.Current.Type == TokenType.Comma)
                {
                    l.Next();
                }
            }

            return t;
        }

        private static Table ParseJsonObject(Lexer l, Script script)
        {
            Table t = new(script);

            l.Next();

            while (l.Current.Type != TokenType.BrkCloseCurly)
            {
                AssertToken(l, TokenType.String);
                string key = l.Current.Text;
                l.Next();
                AssertToken(l, TokenType.Colon);
                l.Next();
                DynValue v = ParseJsonValue(l, script);
                t.Set(key, v);
                l.Next();

                if (l.Current.Type == TokenType.Comma)
                {
                    l.Next();
                }
            }

            return t;
        }

        private static DynValue ParseJsonValue(Lexer l, Script script)
        {
            if (l.Current.Type == TokenType.BrkOpenCurly)
            {
                Table t = ParseJsonObject(l, script);
                return DynValue.NewTable(t);
            }
            else if (l.Current.Type == TokenType.BrkOpenSquare)
            {
                Table t = ParseJsonArray(l, script);
                return DynValue.NewTable(t);
            }
            else if (l.Current.Type == TokenType.String)
            {
                return DynValue.NewString(l.Current.Text);
            }
            else if (l.Current.Type == TokenType.Number || l.Current.Type == TokenType.OpMinusOrSub)
            {
                return ParseJsonNumberValue(l, script);
            }
            else if (l.Current.Type == TokenType.True)
            {
                return DynValue.True;
            }
            else if (l.Current.Type == TokenType.False)
            {
                return DynValue.False;
            }
            else if (l.Current.Type == TokenType.Name && l.Current.Text == "null")
            {
                return JsonNull.Create();
            }
            else
            {
                throw new SyntaxErrorException(
                    l.Current,
                    "Unexpected token : '{0}'",
                    l.Current.Text
                );
            }
        }

        private static DynValue ParseJsonNumberValue(Lexer l, Script script)
        {
            bool negative;
            if (l.Current.Type == TokenType.OpMinusOrSub)
            {
                // Negative number consists of 2 tokens.
                l.Next();
                negative = true;
            }
            else
            {
                negative = false;
            }
            if (l.Current.Type != TokenType.Number)
            {
                throw new SyntaxErrorException(
                    l.Current,
                    "Unexpected token : '{0}'",
                    l.Current.Text
                );
            }
            double numberValue = l.Current.GetNumberValue();
            if (negative)
            {
                numberValue = -numberValue;
            }
            return DynValue.NewNumber(numberValue).AsReadOnly();
        }

        private static string ReplaceOrdinal(string text, string oldValue, string newValue)
        {
            return text.Replace(oldValue, newValue, StringComparison.Ordinal);
        }
    }
}
