namespace NovaSharp.Interpreter.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    ///
    /// </summary>
    public static class SerializationExtensions
    {
        private static readonly HashSet<string> Luakeywords = new()
        {
            "and",
            "break",
            "do",
            "else",
            "elseif",
            "end",
            "false",
            "for",
            "function",
            "goto",
            "if",
            "in",
            "local",
            "nil",
            "not",
            "or",
            "repeat",
            "return",
            "then",
            "true",
            "until",
            "while",
        };

        public static string Serialize(this Table table, bool prefixReturn = false, int tabs = 0)
        {
            if (table.OwnerScript != null)
            {
                throw new ScriptRuntimeException("Table is not a prime table.");
            }

            string tabString = new string('\t', tabs);
            StringBuilder builder = new StringBuilder();

            if (prefixReturn)
            {
                builder.Append("return ");
            }

            if (!table.Values.Any())
            {
                builder.Append("{}");

                if (tabs == 0)
                {
                    builder.AppendLine();
                }

                return builder.ToString();
            }

            builder.AppendLine("{");

            foreach (TablePair tp in table.Pairs)
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

            if (Luakeywords.Contains(dynValue.String))
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

        public static string SerializeValue(this DynValue dynValue, int tabs = 0)
        {
            if (dynValue.Type == DataType.Nil || dynValue.Type == DataType.Void)
            {
                return "nil";
            }
            else if (dynValue.Type == DataType.Tuple)
            {
                return (dynValue.Tuple.Any() ? SerializeValue(dynValue.Tuple[0], tabs) : "nil");
            }
            else if (dynValue.Type == DataType.Number)
            {
                return dynValue.Number.ToString("r", CultureInfo.InvariantCulture);
            }
            else if (dynValue.Type == DataType.Boolean)
            {
                return dynValue.Boolean ? "true" : "false";
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
            string s = input ?? string.Empty;
            s = ReplaceOrdinal(s, @"\", @"\\");
            s = ReplaceOrdinal(s, "\n", @"\n");
            s = ReplaceOrdinal(s, "\r", @"\r");
            s = ReplaceOrdinal(s, "\t", @"\t");
            s = ReplaceOrdinal(s, "\a", @"\a");
            s = ReplaceOrdinal(s, "\f", @"\f");
            s = ReplaceOrdinal(s, "\b", @"\b");
            s = ReplaceOrdinal(s, "\v", @"\v");
            s = ReplaceOrdinal(s, "\"", "\\\"");
            s = ReplaceOrdinal(s, "\'", @"\'");
            return "\"" + s + "\"";
        }

        private static string ReplaceOrdinal(string text, string oldValue, string newValue)
        {
            return text.Replace(oldValue, newValue, StringComparison.Ordinal);
        }
    }
}
