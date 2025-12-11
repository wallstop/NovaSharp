namespace WallstopStudios.NovaSharp.Interpreter.Tree.Lexer
{
    using System;
    using System.Globalization;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
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
            string txt = t.Text;
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
        /// Parses a hexadecimal integer literal (prefixed with <c>0x</c> or <c>0X</c>).
        /// </summary>
        /// <param name="t">Token describing the literal text.</param>
        /// <returns>The parsed integer represented as <see cref="double" /> for DynValue storage.</returns>
        /// <exception cref="SyntaxErrorException">Thrown when the literal is malformed.</exception>
        /// <exception cref="InternalErrorException">
        /// Thrown when the literal does not start with the expected prefix.
        /// </exception>
        public static double ParseHexInteger(Token t)
        {
            string txt = t.Text;
            if ((txt.Length < 2) || (txt[0] != '0' && (char.ToUpperInvariant(txt[1]) != 'X')))
            {
                throw new InternalErrorException(
                    "hex numbers must start with '0x' near '{0}'.",
                    txt
                );
            }

            ReadOnlySpan<char> digits = txt.AsSpan(2);

            if (
                !ulong.TryParse(
                    digits,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ulong res
                )
            )
            {
                throw new SyntaxErrorException(t, "malformed number near '{0}'", txt);
            }

            return (double)res;
        }

        /// <summary>
        /// Attempts to parse a hexadecimal integer token directly as a 64-bit signed integer.
        /// This avoids the precision loss that occurs when parsing large values through double.
        /// </summary>
        /// <param name="t">The token to parse.</param>
        /// <param name="value">When successful, contains the parsed value.</param>
        /// <returns>true if the value fits in a signed 64-bit integer; false otherwise.</returns>
        public static bool TryParseHexIntegerAsLong(Token t, out long value)
        {
            string txt = t.Text;
            if (txt.Length < 2 || txt[0] != '0' || (txt[1] != 'x' && txt[1] != 'X'))
            {
                value = 0;
                return false;
            }

            ReadOnlySpan<char> digits = txt.AsSpan(2);

            // Try to parse as ulong first (handles the full 64-bit range)
            if (
                !ulong.TryParse(
                    digits,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out ulong res
                )
            )
            {
                value = 0;
                return false;
            }

            // Check if it fits in signed long range or can be interpreted as negative via two's complement
            // Values 0x8000000000000000 through 0xFFFFFFFFFFFFFFFF are interpreted as negative
            value = unchecked((long)res);
            return true;
        }

        /// <summary>
        /// Iterates over the hexadecimal digits at the start of <paramref name="s" />, updating the
        /// supplied accumulator and returning the remaining substring.
        /// </summary>
        /// <param name="s">Text starting at the first potential digit.</param>
        /// <param name="d">
        /// Accumulator that receives the parsed value; multiplied by 16 for each processed digit.
        /// </param>
        /// <param name="digits">Outputs how many digits were consumed.</param>
        /// <returns>The substring that begins after the consumed digits.</returns>
        public static string ReadHexProgressive(string s, ref double d, out int digits)
        {
            digits = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (CharIsHexDigit(c))
                {
                    int v = HexDigit2Value(c);
                    d *= 16.0;
                    d += v;
                    ++digits;
                }
                else
                {
                    return s.Substring(i);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Parses Lua's <c>0x...</c> hexadecimal floating-point literals (including <c>p</c> exponents).
        /// </summary>
        /// <param name="t">Token describing the literal text.</param>
        /// <returns>The parsed floating point value.</returns>
        /// <exception cref="SyntaxErrorException">Thrown when formatting rules are violated.</exception>
        /// <exception cref="InternalErrorException">
        /// Thrown when the literal does not start with a hexadecimal prefix.
        /// </exception>
        public static double ParseHexFloat(Token t)
        {
            string s = t.Text;

            try
            {
                if ((s.Length < 2) || (s[0] != '0' && (char.ToUpperInvariant(s[1]) != 'X')))
                {
                    throw new InternalErrorException(
                        "hex float must start with '0x' near '{0}'",
                        s
                    );
                }

                s = s.Substring(2);

                double value = 0.0;
                int dummy,
                    exp = 0;

                s = ReadHexProgressive(s, ref value, out dummy);

                if (s.Length > 0 && s[0] == '.')
                {
                    s = s.Substring(1);
                    s = ReadHexProgressive(s, ref value, out exp);
                }

                exp *= -4;

                if (s.Length > 0 && char.ToUpperInvariant(s[0]) == 'P')
                {
                    if (s.Length == 1)
                    {
                        throw new SyntaxErrorException(t, "invalid hex float format near '{0}'", s);
                    }

                    s = s.Substring(s[1] == '+' ? 2 : 1);

                    int exp1 = int.Parse(s, CultureInfo.InvariantCulture);

                    exp += exp1;
                }

                double result = value * Math.Pow(2, exp);
                return result;
            }
            catch (FormatException)
            {
                throw new SyntaxErrorException(t, "malformed number near '{0}'", s);
            }
        }

        /// <summary>
        /// Converts a single hexadecimal digit into its integer value.
        /// </summary>
        /// <param name="c">Digit to convert.</param>
        /// <returns>Integer value in the range 0-15.</returns>
        /// <exception cref="InternalErrorException">Thrown when the character is not hexadecimal.</exception>
        public static int HexDigit2Value(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            else if (c >= 'A' && c <= 'F')
            {
                return 10 + (c - 'A');
            }
            else if (c >= 'a' && c <= 'f')
            {
                return 10 + (c - 'a');
            }
            else
            {
                throw new InternalErrorException("invalid hex digit near '{0}'", c);
            }
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
