namespace NovaSharp.Interpreter.Serialization.Json
{
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Tree.Lexer;
    using Tree;

    /// <summary>
    /// Class performing conversions between Tables and Json.
    /// NOTE : the conversions are done respecting json syntax but using Lua constructs. This means mostly that:
    /// 1) Lua string escapes can be accepted while they are not technically valid JSON, and viceversa
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
            StringBuilder sb = new();
            TableToJson(sb, table);
            return sb.ToString();
        }

        /// <summary>
        /// Tables to json.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="table">The table.</param>
        private static void TableToJson(StringBuilder sb, Table table)
        {
            bool first = true;

            if (table.Length == 0)
            {
                sb.Append("{");
                foreach (TablePair pair in table.Pairs)
                {
                    if (pair.Key.Type == DataType.String && IsValueJsonCompatible(pair.Value))
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        ValueToJson(sb, pair.Key);
                        sb.Append(':');
                        ValueToJson(sb, pair.Value);

                        first = false;
                    }
                }
                sb.Append("}");
            }
            else
            {
                sb.Append("[");
                for (int i = 1; i <= table.Length; i++)
                {
                    DynValue value = table.Get(i);
                    if (IsValueJsonCompatible(value))
                    {
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        ValueToJson(sb, value);

                        first = false;
                    }
                }
                sb.Append("]");
            }
        }

        /// <summary>
        /// Converts a generic object to JSON
        /// </summary>
        public static string ObjectToJson(object obj)
        {
            DynValue v = ObjectValueConverter.SerializeObjectToDynValue(
                null,
                obj,
                JsonNull.Create()
            );
            return TableToJson(v.Table);
        }

        private static void ValueToJson(StringBuilder sb, DynValue value)
        {
            switch (value.Type)
            {
                case DataType.Boolean:
                    sb.Append(value.Boolean ? "true" : "false");
                    break;
                case DataType.Number:
                    sb.Append(value.Number.ToString("r"));
                    break;
                case DataType.String:
                    sb.Append(EscapeString(value.String ?? ""));
                    break;
                case DataType.Table:
                    TableToJson(sb, value.Table);
                    break;
                case DataType.Nil:
                case DataType.Void:
                case DataType.UserData:
                default:
                    sb.Append("null");
                    break;
            }
        }

        private static string EscapeString(string s)
        {
            s = s.Replace(@"\", @"\\");
            s = s.Replace(@"/", @"\/");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\f", @"\f");
            s = s.Replace("\b", @"\b");
            s = s.Replace("\n", @"\n");
            s = s.Replace("\r", @"\r");
            s = s.Replace("\t", @"\t");
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

            if (l.Current.type == TokenType.BrkOpenCurly)
            {
                return ParseJsonObject(l, script);
            }
            else if (l.Current.type == TokenType.BrkOpenSquare)
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
            if (l.Current.type != type)
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

            while (l.Current.type != TokenType.BrkCloseSquare)
            {
                DynValue v = ParseJsonValue(l, script);
                t.Append(v);
                l.Next();

                if (l.Current.type == TokenType.Comma)
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

            while (l.Current.type != TokenType.BrkCloseCurly)
            {
                AssertToken(l, TokenType.String);
                string key = l.Current.Text;
                l.Next();
                AssertToken(l, TokenType.Colon);
                l.Next();
                DynValue v = ParseJsonValue(l, script);
                t.Set(key, v);
                l.Next();

                if (l.Current.type == TokenType.Comma)
                {
                    l.Next();
                }
            }

            return t;
        }

        private static DynValue ParseJsonValue(Lexer l, Script script)
        {
            if (l.Current.type == TokenType.BrkOpenCurly)
            {
                Table t = ParseJsonObject(l, script);
                return DynValue.NewTable(t);
            }
            else if (l.Current.type == TokenType.BrkOpenSquare)
            {
                Table t = ParseJsonArray(l, script);
                return DynValue.NewTable(t);
            }
            else if (l.Current.type == TokenType.String)
            {
                return DynValue.NewString(l.Current.Text);
            }
            else if (l.Current.type == TokenType.Number || l.Current.type == TokenType.OpMinusOrSub)
            {
                return ParseJsonNumberValue(l, script);
            }
            else if (l.Current.type == TokenType.True)
            {
                return DynValue.True;
            }
            else if (l.Current.type == TokenType.False)
            {
                return DynValue.False;
            }
            else if (l.Current.type == TokenType.Name && l.Current.Text == "null")
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
            if (l.Current.type == TokenType.OpMinusOrSub)
            {
                // Negative number consists of 2 tokens.
                l.Next();
                negative = true;
            }
            else
            {
                negative = false;
            }
            if (l.Current.type != TokenType.Number)
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
    }
}
