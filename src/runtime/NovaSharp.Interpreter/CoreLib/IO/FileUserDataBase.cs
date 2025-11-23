namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal abstract class FileUserDataBase : RefIdObject
    {
        /// <summary>
        /// Implements <c>file:lines</c> as defined by the Lua 5.4 manual (§6.8 I/O Facilities),
        /// repeatedly invoking <see cref="Read(ScriptExecutionContext, CallbackArguments)"/> with
        /// the supplied arguments until <c>nil</c> is returned.
        /// </summary>
        /// <param name="executionContext">Execution context owning the Lua script.</param>
        /// <param name="args">Optional format arguments (mirroring <c>file:read</c>).</param>
        /// <returns>A tuple whose elements contain each successful read followed by the final <c>nil</c>.</returns>
        public DynValue Lines(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            List<DynValue> readLines = new();

            DynValue readValue = null;

            do
            {
                readValue = Read(executionContext, args);
                readLines.Add(readValue);
            } while (readValue.IsNotNil());

            return DynValue.FromObject(executionContext.GetScript(), readLines.Select(s => s));
        }

        /// <summary>
        /// Implements Lua's <c>file:read</c> contract (§6.8) by returning strings, numbers, or
        /// tuples according to the provided format specifiers or byte counts.
        /// </summary>
        /// <param name="executionContext">Execution context owning the current script.</param>
        /// <param name="args">Lua-style parameters describing byte counts or format specifiers.</param>
        /// <returns>A tuple containing the requested values, or <c>nil</c> when the stream reaches EOF.</returns>
        public DynValue Read(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (args.Count == 0)
            {
                string str = ReadLine();

                if (str == null)
                {
                    return DynValue.Nil;
                }

                str = str.TrimEnd('\n', '\r');
                return DynValue.NewString(str);
            }
            else
            {
                List<DynValue> rets = new();

                for (int i = 0; i < args.Count; i++)
                {
                    DynValue v;

                    if (args[i].Type == DataType.Number)
                    {
                        if (Eof())
                        {
                            return DynValue.Nil;
                        }

                        int howmany = (int)args[i].Number;

                        string str = ReadBuffer(howmany);
                        v = DynValue.NewString(str);
                    }
                    else
                    {
                        string opt = args.AsType(i, "read", DataType.String, false).String;

                        if (Eof())
                        {
                            v = opt.StartsWith("*a", StringComparison.Ordinal)
                                ? DynValue.NewString(string.Empty)
                                : DynValue.Nil;
                        }
                        else if (opt.StartsWith("*n", StringComparison.Ordinal))
                        {
                            double? d = ReadNumber();

                            if (d.HasValue)
                            {
                                v = DynValue.NewNumber(d.Value);
                            }
                            else
                            {
                                v = DynValue.Nil;
                            }
                        }
                        else if (opt.StartsWith("*a", StringComparison.Ordinal))
                        {
                            string str = ReadToEnd();
                            v = DynValue.NewString(str);
                        }
                        else if (opt.StartsWith("*l", StringComparison.Ordinal))
                        {
                            string str = ReadLine();
                            str = str.TrimEnd('\n', '\r');
                            v = DynValue.NewString(str);
                        }
                        else if (opt.StartsWith("*L", StringComparison.Ordinal))
                        {
                            string str = ReadLine();

                            str = str.TrimEnd('\n', '\r');
                            str += "\n";

                            v = DynValue.NewString(str);
                        }
                        else
                        {
                            throw ScriptRuntimeException.BadArgument(i, "read", "invalid option");
                        }
                    }

                    rets.Add(v);
                }

                return DynValue.NewTuple(rets.ToArray());
            }
        }

        /// <summary>
        /// Implements Lua's <c>file:write</c> (§6.8) by writing each provided argument to the
        /// underlying stream using the current encoding.
        /// </summary>
        /// <param name="executionContext">Execution context owning the current script.</param>
        /// <param name="args">Arguments that should be written sequentially to the stream.</param>
        /// <returns>The userdata handle so Lua callers can chain additional writes.</returns>
        public DynValue Write(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            try
            {
                for (int i = 0; i < args.Count; i++)
                {
                    string str = args.AsType(i, "write", DataType.String, false).String;
                    Write(str);
                }

                return UserData.Create(this);
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex) when (IsRecoverableIoException(ex))
            {
                return CreateIoFailure(ex);
            }
        }

        /// <summary>
        /// Implements Lua's <c>file:close</c> (§6.8), returning <c>true</c> on success or the
        /// <c>(nil, message, code)</c> tuple when closing fails with a recoverable IO error.
        /// </summary>
        /// <param name="executionContext">Execution context owning the current script.</param>
        /// <param name="args">Unused Lua arguments; accepted for signature parity.</param>
        /// <returns>A <see cref="DynValue"/> conveying <c>true</c> or the Lua error tuple.</returns>
        public DynValue Close(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            try
            {
                string msg = Close();
                if (msg == null)
                {
                    return DynValue.True;
                }
                else
                {
                    return DynValue.NewTuple(
                        DynValue.Nil,
                        DynValue.NewString(msg),
                        DynValue.NewNumber(-1)
                    );
                }
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex) when (IsRecoverableIoException(ex))
            {
                return CreateIoFailure(ex);
            }
        }

        private double? ReadNumber()
        {
            bool canRewind = SupportsRewind;
            long startPosition = canRewind ? GetCurrentPosition() : 0;
            long lastConsumedPosition = startPosition;

            ConsumeLeadingWhitespace(canRewind, ref lastConsumedPosition);

            StringBuilder literal = new();
            bool hasDigits = false;
            bool hasDecimalPoint = false;
            bool exponentSeen = false;
            bool exponentHasDigits = false;

            bool isHex = false;
            bool hexDigitsSeen = false;
            bool hexExponentSeen = false;
            bool hexExponentHasDigits = false;
            int hexFractionDigitCount = 0;

            while (true)
            {
                int peekValue = PeekRaw();
                if (peekValue == -1)
                {
                    break;
                }

                char current = (char)peekValue;

                if (
                    IsSignAllowed(
                        literal,
                        isHex,
                        exponentSeen,
                        exponentHasDigits,
                        hexExponentSeen,
                        hexExponentHasDigits,
                        current
                    )
                )
                {
                    if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                    {
                        break;
                    }

                    continue;
                }

                if (isHex)
                {
                    if (LexerUtils.CharIsHexDigit(current))
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        hexDigitsSeen = true;

                        if (hasDecimalPoint && !hexExponentSeen)
                        {
                            hexFractionDigitCount++;
                        }

                        if (hexExponentSeen)
                        {
                            hexExponentHasDigits = true;
                        }

                        continue;
                    }

                    if (current == '.' && !hasDecimalPoint && !hexExponentSeen)
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        hasDecimalPoint = true;
                        continue;
                    }

                    if ((current == 'p' || current == 'P') && !hexExponentSeen && hexDigitsSeen)
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        hexExponentSeen = true;
                        hexExponentHasDigits = false;
                        continue;
                    }

                    break;
                }
                else
                {
                    if (char.IsDigit(current))
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        hasDigits = true;

                        if (exponentSeen)
                        {
                            exponentHasDigits = true;
                        }

                        continue;
                    }

                    if ((current == 'x' || current == 'X') && IsValidHexPrefix(literal))
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        isHex = true;
                        hexDigitsSeen = false;
                        hasDecimalPoint = false;
                        hexFractionDigitCount = 0;
                        continue;
                    }

                    if (current == '.' && !hasDecimalPoint && !exponentSeen)
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        hasDecimalPoint = true;
                        continue;
                    }

                    if ((current == 'e' || current == 'E') && !exponentSeen && hasDigits)
                    {
                        if (!TryConsumeChar(ref lastConsumedPosition, canRewind, literal))
                        {
                            break;
                        }

                        exponentSeen = true;
                        exponentHasDigits = false;
                        continue;
                    }
                }

                break;
            }

            string numericText = literal.ToString();

            bool parsedSuccessfully = false;
            double parsedValue = 0;

            if (isHex)
            {
                bool hasValidDigits =
                    hexDigitsSeen || (hasDecimalPoint && hexFractionDigitCount > 0);
                bool isValidLiteral = numericText.Length > 0 && hasValidDigits;

                if (hexExponentSeen && !hexExponentHasDigits)
                {
                    isValidLiteral = false;
                }

                if (isValidLiteral && TryParseHexFloatLiteral(numericText, out parsedValue))
                {
                    parsedSuccessfully = true;
                }
            }
            else
            {
                bool isValidLiteral =
                    numericText.Length > 0
                    && !IsStandaloneSignOrDot(numericText)
                    && hasDigits
                    && !(exponentSeen && !exponentHasDigits);

                if (
                    isValidLiteral
                    && double.TryParse(
                        numericText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsedValue
                    )
                )
                {
                    parsedSuccessfully = true;
                }
            }

            if (!parsedSuccessfully)
            {
                if (canRewind)
                {
                    ResetToPosition(startPosition);
                }

                return null;
            }

            if (canRewind)
            {
                ResetToPosition(lastConsumedPosition);
            }

            return parsedValue;
        }

        private void ConsumeLeadingWhitespace(bool canRewind, ref long lastConsumedPosition)
        {
            while (true)
            {
                int peekValue = PeekRaw();
                if (peekValue == -1)
                {
                    break;
                }

                char peekChar = (char)peekValue;
                if (!char.IsWhiteSpace(peekChar))
                {
                    break;
                }

                string chunk = ReadBuffer(1);
                if (chunk.Length == 0)
                {
                    break;
                }

                if (canRewind)
                {
                    lastConsumedPosition = GetCurrentPosition();
                }
            }
        }

        private bool TryConsumeChar(
            ref long lastConsumedPosition,
            bool canRewind,
            StringBuilder numberBuilder
        )
        {
            string chunk = ReadBuffer(1);
            if (chunk.Length == 0)
            {
                return false;
            }

            numberBuilder.Append(chunk);

            if (canRewind)
            {
                lastConsumedPosition = GetCurrentPosition();
            }

            return true;
        }

        /// <summary>
        /// Determines whether the provided character can legally act as a sign while parsing a Lua
        /// decimal or hexadecimal literal (§3.4.3 in the Lua 5.4 manual).
        /// </summary>
        /// <param name="literal">Characters consumed so far.</param>
        /// <param name="isHex">Indicates whether the literal currently represents a hexadecimal number.</param>
        /// <param name="exponentSeen">Whether a decimal exponent marker (<c>e/E</c>) has been processed.</param>
        /// <param name="exponentHasDigits">Whether digits have been seen after the decimal exponent marker.</param>
        /// <param name="hexExponentSeen">Whether a hexadecimal exponent marker (<c>p/P</c>) has been processed.</param>
        /// <param name="hexExponentHasDigits">Whether digits have been seen after the hexadecimal exponent.</param>
        /// <param name="candidate">The character being evaluated.</param>
        /// <returns><c>true</c> when the candidate can serve as a sign, otherwise <c>false</c>.</returns>
        internal static bool IsSignAllowed(
            StringBuilder literal,
            bool isHex,
            bool exponentSeen,
            bool exponentHasDigits,
            bool hexExponentSeen,
            bool hexExponentHasDigits,
            char candidate
        )
        {
            if (candidate != '+' && candidate != '-')
            {
                return false;
            }

            if (literal.Length == 0)
            {
                return true;
            }

            if (!isHex)
            {
                if (
                    exponentSeen
                    && !exponentHasDigits
                    && literal.Length > 0
                    && (literal[^1] == 'e' || literal[^1] == 'E')
                )
                {
                    return true;
                }
            }
            else if (
                hexExponentSeen
                && !hexExponentHasDigits
                && literal.Length > 0
                && (literal[^1] == 'p' || literal[^1] == 'P')
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the provided text is a solitary sign or decimal point, which Lua treats
        /// as an incomplete numeric literal.
        /// </summary>
        /// <param name="numericText">Candidate text extracted from the stream.</param>
        /// <returns><c>true</c> when the text is a lone <c>'+'</c>, <c>'-'</c>, or <c>'.'</c>.</returns>
        internal static bool IsStandaloneSignOrDot(string numericText)
        {
            if (numericText.Length != 1)
            {
                return false;
            }

            char single = numericText[0];
            return single == '+' || single == '-' || single == '.';
        }

        /// <summary>
        /// Determines whether the characters collected so far constitute a valid hexadecimal prefix
        /// (<c>0x</c> or <c>-0x</c>) per Lua's numeric literal grammar (§3.4.3).
        /// </summary>
        /// <param name="literal">Characters consumed while reading the literal.</param>
        /// <returns><c>true</c> when the literal can legally continue as a hexadecimal value.</returns>
        internal static bool IsValidHexPrefix(StringBuilder literal)
        {
            if (literal.Length == 0)
            {
                return false;
            }

            if (literal.Length == 1 && literal[0] == '0')
            {
                return true;
            }

            if (
                literal.Length == 2
                && (literal[0] == '+' || literal[0] == '-')
                && literal[1] == '0'
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses the supplied hexadecimal floating-point literal according to Lua 5.4 §3.4.3 rules.
        /// </summary>
        /// <param name="literal">Text that should represent a complete hexadecimal literal.</param>
        /// <param name="value">Outputs the computed IEEE 754 double on success.</param>
        /// <returns><c>true</c> when the literal is valid and <paramref name="value"/> contains the parsed number.</returns>
        internal static bool TryParseHexFloatLiteral(string literal, out double value)
        {
            value = 0;

            if (string.IsNullOrEmpty(literal))
            {
                return false;
            }

            int index = 0;
            int sign = 1;

            if (literal[index] == '+' || literal[index] == '-')
            {
                if (literal[index] == '-')
                {
                    sign = -1;
                }

                index++;

                if (index >= literal.Length)
                {
                    return false;
                }
            }

            if (
                index + 1 >= literal.Length
                || literal[index] != '0'
                || (literal[index + 1] != 'x' && literal[index + 1] != 'X')
            )
            {
                return false;
            }

            index += 2;

            double significand = 0;
            bool digitsSeen = false;
            int fractionalDigits = 0;

            while (index < literal.Length && LexerUtils.CharIsHexDigit(literal[index]))
            {
                significand = (significand * 16.0) + TryGetHexDigitValue(literal[index]);
                index++;
                digitsSeen = true;
            }

            if (index < literal.Length && literal[index] == '.')
            {
                index++;

                while (index < literal.Length && LexerUtils.CharIsHexDigit(literal[index]))
                {
                    significand = (significand * 16.0) + TryGetHexDigitValue(literal[index]);
                    index++;
                    digitsSeen = true;
                    fractionalDigits++;
                }
            }

            if (!digitsSeen)
            {
                return false;
            }

            int exponent = -4 * fractionalDigits;

            if (index < literal.Length && (literal[index] == 'p' || literal[index] == 'P'))
            {
                index++;
                if (index >= literal.Length)
                {
                    return false;
                }

                int exponentSign = 1;
                if (literal[index] == '+' || literal[index] == '-')
                {
                    if (literal[index] == '-')
                    {
                        exponentSign = -1;
                    }

                    index++;
                }

                if (index >= literal.Length || !char.IsDigit(literal[index]))
                {
                    return false;
                }

                int exponentValue = 0;
                while (index < literal.Length && char.IsDigit(literal[index]))
                {
                    exponentValue = (exponentValue * 10) + (literal[index] - '0');
                    index++;
                }

                exponent += exponentSign * exponentValue;
            }

            if (index != literal.Length)
            {
                return false;
            }

            double magnitude = significand * Math.Pow(2.0, exponent);
            value = sign * magnitude;
            return true;
        }

        private static int TryGetHexDigitValue(char c)
        {
            return LexerUtils.HexDigit2Value(c);
        }

        private static DynValue CreateIoFailure(Exception exception)
        {
            return DynValue.NewTuple(
                DynValue.Nil,
                DynValue.NewString(exception.Message),
                DynValue.NewNumber(-1)
            );
        }

        private static bool IsRecoverableIoException(Exception exception)
        {
            return exception switch
            {
                IOException => true,
                UnauthorizedAccessException => true,
                ObjectDisposedException => true,
                SecurityException => true,
                NotSupportedException => true,
                InvalidOperationException => true,
                _ => false,
            };
        }

        protected abstract bool Eof();
        protected abstract string ReadLine();
        protected abstract string ReadBuffer(int p);
        protected abstract string ReadToEnd();
        protected abstract char Peek();
        protected abstract int PeekRaw();
        protected abstract void Write(string value);
        protected abstract bool SupportsRewind { get; }
        protected abstract long GetCurrentPosition();
        protected abstract void ResetToPosition(long position);

        protected internal abstract bool IsOpen();
        protected abstract string Close();

        /// <summary>
        /// Flushes buffered content to the underlying stream, mirroring Lua's <c>file:flush</c>.
        /// </summary>
        /// <returns><c>true</c> when the flush succeeds.</returns>
        public abstract bool Flush();

        /// <summary>
        /// Implements Lua's <c>file:seek</c> control, repositioning the file pointer relative to the specified origin.
        /// </summary>
        /// <param name="whence">Lua-style origin indicator (<c>"set"</c>, <c>"cur"</c>, or <c>"end"</c>).</param>
        /// <param name="offset">Byte offset relative to <paramref name="whence"/>.</param>
        /// <returns>The new absolute file position.</returns>
        public abstract long Seek(string whence, long offset = 0);

        /// <summary>
        /// Implements Lua's <c>file:setvbuf</c>, switching between <c>"no"</c>, <c>"full"</c>, or <c>"line"</c> buffering.
        /// </summary>
        /// <param name="mode">Desired Lua buffering mode.</param>
        /// <returns><c>true</c> when the buffer mode is applied successfully.</returns>
        public abstract bool Setvbuf(string mode);

        /// <summary>
        /// Returns the Lua-facing identifier for the userdata so diagnostics match <c>tostring(file)</c>.
        /// </summary>
        /// <returns><c>"file (closed)"</c> when the handle is closed; otherwise the reference id.</returns>
        public override string ToString()
        {
            if (IsOpen())
            {
                return $"file ({ReferenceId:X8})";
            }
            else
            {
                return "file (closed)";
            }
        }
    }
}
