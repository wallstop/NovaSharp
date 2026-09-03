namespace WallstopStudios.NovaSharp.Interpreter.Tree.Lexer
{
    using System;
    using System.Globalization;
    using global::NovaSharp;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Helper routines shared by the lexer and token helpers for parsing Lua literals.
    /// </summary>
    internal static class LexerUtils
    {
        /// <summary>
        /// Parses a decimal number token using invariant-culture rules.
        /// </summary>
        /// <param name="t">Token describing the literal text.</param>
        /// <returns>The parsed floating-point value.</returns>
        /// <exception cref="SyntaxErrorException">Thrown when the literal is malformed.</exception>
        public static double ParseNumber(Token t)
        {
            string txt = t.text;
            if (
                !double.TryParse(
                    txt,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double res
                )
            )
            {
                throw new SyntaxErrorException(t, "malformed number near '{0}'", txt);
            }

            return res;
        }

        /// <summary>
        /// Determines whether the supplied character is a decimal digit.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> when the character is between <c>0</c> and <c>9</c>.</returns>
        public static bool CharIsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Determines whether the supplied character is hexadecimal (0-9, a-f, A-F).
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> when the character is hexadecimal.</returns>
        public static bool CharIsHexDigit(char c)
        {
            return CharIsDigit(c)
                || c == 'a'
                || c == 'b'
                || c == 'c'
                || c == 'd'
                || c == 'e'
                || c == 'f'
                || c == 'A'
                || c == 'B'
                || c == 'C'
                || c == 'D'
                || c == 'E'
                || c == 'F';
        }

        /// <summary>
        /// Removes the optional first newline from Lua long strings to match Section 3.1 lexical rules.
        /// </summary>
        /// <param name="str">String literal payload.</param>
        /// <returns>The normalized string.</returns>
        public static string AdjustLuaLongString(string str)
        {
            if (str.StartsWith("\r\n", StringComparison.Ordinal))
            {
                str = str.Substring(2);
            }
            else if (str.Length > 0 && str[0] == '\n')
            {
                str = str.Substring(1);
            }

            return str;
        }

        /// <summary>
        /// Expands Lua escape sequences inside short strings, including hexadecimal, decimal, and
        /// <c>\u{...}</c> escapes plus the <c>\z</c> whitespace trimming directive.
        /// </summary>
        /// <param name="token">Token describing the string (used for diagnostics).</param>
        /// <param name="str">Raw string literal text.</param>
        /// <returns>The decoded string.</returns>
        /// <exception cref="SyntaxErrorException">
        /// Thrown when an escape sequence is invalid or incomplete.
        /// </exception>
        public static string UnescapeLuaString(Token token, string str)
        {
            if (!Framework.Do.StringContainsChar(str, '\\'))
            {
                return str;
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested();

            bool escape = false;
            bool hex = false;
            int unicodeState = 0;
            string hexprefix = "";
            string val = "";
            bool zmode = false;

            foreach (char c in str)
            {
                redo:
                if (escape)
                {
                    if (val.Length == 0 && !hex && unicodeState == 0)
                    {
                        if (c == 'a')
                        {
                            sb.Append('\a');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '\r') { } // this makes \\r\n -> \\n
                        else if (c == '\n')
                        {
                            sb.Append('\n');
                            escape = false;
                        }
                        else if (c == 'b')
                        {
                            sb.Append('\b');
                            escape = false;
                        }
                        else if (c == 'f')
                        {
                            sb.Append('\f');
                            escape = false;
                        }
                        else if (c == 'n')
                        {
                            sb.Append('\n');
                            escape = false;
                        }
                        else if (c == 'r')
                        {
                            sb.Append('\r');
                            escape = false;
                        }
                        else if (c == 't')
                        {
                            sb.Append('\t');
                            escape = false;
                        }
                        else if (c == 'v')
                        {
                            sb.Append('\v');
                            escape = false;
                        }
                        else if (c == '\\')
                        {
                            sb.Append('\\');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '"')
                        {
                            sb.Append('\"');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '\'')
                        {
                            sb.Append('\'');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '[')
                        {
                            sb.Append('[');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == ']')
                        {
                            sb.Append(']');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == '/')
                        {
                            sb.Append('/');
                            escape = false;
                            zmode = false;
                        }
                        else if (c == 'x')
                        {
                            hex = true;
                        }
                        else if (c == 'u')
                        {
                            unicodeState = 1;
                        }
                        else if (c == 'z')
                        {
                            zmode = true;
                            escape = false;
                        }
                        else if (CharIsDigit(c))
                        {
                            val = val + c;
                        }
                        else
                        {
                            throw new SyntaxErrorException(
                                token,
                                "invalid escape sequence near '\\{0}'",
                                c
                            );
                        }
                    }
                    else
                    {
                        if (unicodeState == 1)
                        {
                            if (c != '{')
                            {
                                throw new SyntaxErrorException(token, "'{' expected near '\\u'");
                            }

                            unicodeState = 2;
                        }
                        else if (unicodeState == 2)
                        {
                            if (c == '}')
                            {
                                int i = int.Parse(
                                    val,
                                    NumberStyles.HexNumber,
                                    CultureInfo.InvariantCulture
                                );
                                sb.Append(ConvertUtf32ToChar(i));
                                unicodeState = 0;
                                val = string.Empty;
                                escape = false;
                            }
                            else if (val.Length >= 8)
                            {
                                throw new SyntaxErrorException(
                                    token,
                                    "'}' missing, or unicode code point too large after '\\u'"
                                );
                            }
                            else
                            {
                                val += c;
                            }
                        }
                        else if (hex)
                        {
                            if (CharIsHexDigit(c))
                            {
                                val += c;
                                if (val.Length == 2)
                                {
                                    int i = int.Parse(
                                        val,
                                        NumberStyles.HexNumber,
                                        CultureInfo.InvariantCulture
                                    );
                                    sb.Append(ConvertUtf32ToChar(i));
                                    zmode = false;
                                    escape = false;
                                }
                            }
                            else
                            {
                                throw new SyntaxErrorException(
                                    token,
                                    "hexadecimal digit expected near '\\{0}{1}{2}'",
                                    hexprefix,
                                    val,
                                    c
                                );
                            }
                        }
                        else if (val.Length > 0)
                        {
                            if (CharIsDigit(c))
                            {
                                val = val + c;
                            }

                            if (val.Length == 3 || !CharIsDigit(c))
                            {
                                int i = int.Parse(val, CultureInfo.InvariantCulture);

                                if (i > 255)
                                {
                                    throw new SyntaxErrorException(
                                        token,
                                        "decimal escape too large near '\\{0}'",
                                        val
                                    );
                                }

                                sb.Append(ConvertUtf32ToChar(i));

                                zmode = false;
                                escape = false;

                                if (!CharIsDigit(c))
                                {
                                    goto redo;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        escape = true;
                        hex = false;
                        val = "";
                    }
                    else
                    {
                        if (!zmode || !char.IsWhiteSpace(c))
                        {
                            sb.Append(c);
                            zmode = false;
                        }
                    }
                }
            }

            if (escape && !hex && val.Length > 0)
            {
                int i = int.Parse(val, CultureInfo.InvariantCulture);
                sb.Append(ConvertUtf32ToChar(i));
                escape = false;
            }

            if (escape)
            {
                throw new SyntaxErrorException(
                    token,
                    "unfinished string near '\"{0}\"'",
                    sb.ToString()
                );
            }

            return sb.ToString();
        }

        private static string ConvertUtf32ToChar(int i)
        {
#if PCL || ENABLE_DOTNET
            return ((char)i).ToString();
#else
            return char.ConvertFromUtf32(i);
#endif
        }
    }
}
