namespace WallstopStudios.NovaSharp.Interpreter.Tree.Lexer
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Represents a single token produced by the lexer, including location metadata.
    /// </summary>
    internal class Token
    {
        /// <summary>
        /// Gets the identifier of the source chunk that produced the token.
        /// </summary>
        public int SourceId { get; }

        /// <summary>
        /// Gets the starting column (1-based) of the token.
        /// </summary>
        public int FromCol { get; }

        /// <summary>
        /// Gets the ending column (1-based, inclusive) of the token.
        /// </summary>
        public int ToCol { get; }

        /// <summary>
        /// Gets the starting line (1-based) of the token.
        /// </summary>
        public int FromLine { get; }

        /// <summary>
        /// Gets the ending line (1-based) of the token.
        /// </summary>
        public int ToLine { get; }

        /// <summary>
        /// Gets the previous column recorded by the lexer (useful for exclusive spans).
        /// </summary>
        public int PrevCol { get; }

        /// <summary>
        /// Gets the previous line recorded by the lexer (useful for exclusive spans).
        /// </summary>
        public int PrevLine { get; }

        /// <summary>
        /// Gets the token classification (identifier, keyword, operator, etc.).
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Gets or sets the textual payload associated with the token.
        /// </summary>
        public string Text { get; set; }

        public Token(
            TokenType tokenType,
            int sourceId,
            int fromLine,
            int fromCol,
            int toLine,
            int toCol,
            int prevLine,
            int prevCol
        )
        {
            Type = tokenType;

            SourceId = sourceId;
            FromLine = fromLine;
            FromCol = fromCol;
            ToCol = toCol;
            ToLine = toLine;
            PrevCol = prevCol;
            PrevLine = prevLine;
        }

        /// <summary>
        /// Formats the token for debugging by printing its type, range, and text.
        /// </summary>
        /// <returns>Human-friendly representation of the token.</returns>
        public override string ToString()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            string tokenTypeStr = Type.ToString();
            sb.Append(tokenTypeStr);
            int tokenTypePadding = 16 - tokenTypeStr.Length;
            if (tokenTypePadding > 0)
            {
                sb.Append(' ', tokenTypePadding);
            }

            sb.Append("  - ");
            sb.Append(FromLine);
            sb.Append(':');
            sb.Append(FromCol);
            sb.Append('-');
            sb.Append(ToLine);
            sb.Append(':');
            sb.Append(ToCol);

            // Calculate how much padding we need for location (target: 10 chars)
            int locationLength =
                DigitCount(FromLine)
                + 1
                + DigitCount(FromCol)
                + 1
                + DigitCount(ToLine)
                + 1
                + DigitCount(ToCol);
            int locationPadding = 10 - locationLength;
            if (locationPadding > 0)
            {
                sb.Append(' ', locationPadding);
            }

            sb.Append(" - '");
            sb.Append(Text ?? "");
            sb.Append('\'');

            return sb.ToString();
        }

        private static int DigitCount(int n)
        {
            if (n == 0)
            {
                return 1;
            }

            int count = 0;
            if (n < 0)
            {
                count = 1;
                n = -n;
            }

            while (n > 0)
            {
                count++;
                n /= 10;
            }
            return count;
        }

        /// <summary>
        /// Maps a reserved word to the corresponding <see cref="TokenType" />, if any.
        /// </summary>
        /// <param name="reservedWord">Identifier text encountered by the lexer.</param>
        /// <returns>The matched token type, or <c>null</c> when the word is not reserved.</returns>
        public static TokenType? GetReservedTokenType(string reservedWord)
        {
            switch (reservedWord)
            {
                case "and":
                    return TokenType.And;
                case "break":
                    return TokenType.Break;
                case "do":
                    return TokenType.Do;
                case "else":
                    return TokenType.Else;
                case "elseif":
                    return TokenType.ElseIf;
                case "end":
                    return TokenType.End;
                case "false":
                    return TokenType.False;
                case "for":
                    return TokenType.For;
                case "function":
                    return TokenType.Function;
                case "goto":
                    return TokenType.Goto;
                case "if":
                    return TokenType.If;
                case "in":
                    return TokenType.In;
                case "local":
                    return TokenType.Local;
                case "nil":
                    return TokenType.Nil;
                case "not":
                    return TokenType.Not;
                case "or":
                    return TokenType.Or;
                case "repeat":
                    return TokenType.Repeat;
                case "return":
                    return TokenType.Return;
                case "then":
                    return TokenType.Then;
                case "true":
                    return TokenType.True;
                case "until":
                    return TokenType.Until;
                case "while":
                    return TokenType.While;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Parses the numeric literal carried by this token.
        /// </summary>
        /// <returns>The literal value as a <see cref="double" />.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the token is not numeric (decimal, hexadecimal integer, or hexadecimal float).
        /// </exception>
        public double GetNumberValue()
        {
            if (Type == TokenType.Number)
            {
                return LexerUtils.ParseNumber(this);
            }
            else if (Type == TokenType.NumberHex)
            {
                return LexerUtils.ParseHexInteger(this);
            }
            else if (Type == TokenType.NumberHexFloat)
            {
                return LexerUtils.ParseHexFloat(this);
            }
            else
            {
                throw new NotSupportedException(
                    "GetNumberValue is supported only on numeric tokens"
                );
            }
        }

        /// <summary>
        /// Attempts to parse the numeric literal as a 64-bit signed integer without intermediate double conversion.
        /// This is required for exact representation of large integers like long.MaxValue which cannot be
        /// exactly represented in IEEE 754 double precision.
        /// </summary>
        /// <param name="value">When successful, contains the parsed integer value.</param>
        /// <returns>true if the token represents an integer in the long.MinValue to long.MaxValue range; false otherwise.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the token is not numeric.
        /// </exception>
        public bool TryGetIntegerValue(out long value)
        {
            if (Type == TokenType.NumberHex)
            {
                return LexerUtils.TryParseHexIntegerAsLong(this, out value);
            }
            else if (Type == TokenType.Number && !IsFloatLiteralSyntax())
            {
                return long.TryParse(
                    Text,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value
                );
            }
            else
            {
                value = 0;
                return false;
            }
        }

        /// <summary>
        /// Determines whether the numeric token uses float syntax (contains decimal point or exponent).
        /// For Lua 5.3+ compliance, literals like "1.0" or "1e5" should be floats,
        /// while literals like "1" or "0x10" should be integers.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the token contains a decimal point or exponent;
        /// <c>false</c> for pure integer syntax (decimal or hex).
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the token is not numeric.
        /// </exception>
        public bool IsFloatLiteralSyntax()
        {
            if (Type == TokenType.NumberHexFloat)
            {
                return true;
            }
            else if (Type == TokenType.NumberHex)
            {
                return false;
            }
            else if (Type == TokenType.Number)
            {
                // Check if the text contains a decimal point or exponent indicator
                // Examples: "1.0" -> true, "1e5" -> true, "1" -> false, "123" -> false
                return Text.Contains('.', StringComparison.Ordinal)
                    || Text.Contains('e', StringComparison.OrdinalIgnoreCase)
                    || Text.Contains('E', StringComparison.Ordinal);
            }
            else
            {
                throw new NotSupportedException(
                    "IsFloatLiteralSyntax is supported only on numeric tokens"
                );
            }
        }

        /// <summary>
        /// Determines whether the token closes the current block during parsing.
        /// </summary>
        /// <returns><c>true</c> when the token is a block terminator.</returns>
        public bool IsEndOfBlock()
        {
            switch (Type)
            {
                case TokenType.Else:
                case TokenType.ElseIf:
                case TokenType.End:
                case TokenType.Until:
                case TokenType.Eof:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the token can be used as a unary operator.
        /// </summary>
        /// <returns><c>true</c> when the token starts a unary operation.</returns>
        public bool IsUnaryOperator()
        {
            return Type == TokenType.OpMinusOrSub
                || Type == TokenType.Not
                || Type == TokenType.OpLen
                || Type == TokenType.OpBitNotOrXor;
        }

        /// <summary>
        /// Determines whether the token can be used as a binary operator.
        /// </summary>
        /// <returns><c>true</c> when the token represents a binary operator.</returns>
        public bool IsBinaryOperator()
        {
            switch (Type)
            {
                case TokenType.And:
                case TokenType.Or:
                case TokenType.OpEqual:
                case TokenType.OpLessThan:
                case TokenType.OpLessThanEqual:
                case TokenType.OpGreaterThanEqual:
                case TokenType.OpGreaterThan:
                case TokenType.OpNotEqual:
                case TokenType.OpConcat:
                case TokenType.OpPwr:
                case TokenType.OpMod:
                case TokenType.OpDiv:
                case TokenType.OpFloorDiv:
                case TokenType.OpMul:
                case TokenType.OpMinusOrSub:
                case TokenType.OpAdd:
                case TokenType.OpBitAnd:
                case TokenType.OpBitNotOrXor:
                case TokenType.OpShiftLeft:
                case TokenType.OpShiftRight:
                case TokenType.Pipe:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Builds a <see cref="NovaSharp.Interpreter.Debugging.SourceRef" /> for this token.
        /// </summary>
        /// <param name="isStepStop">Whether the debugger should pause on the produced span.</param>
        /// <returns>A source reference covering just this token.</returns>
        internal NovaSharp.Interpreter.Debugging.SourceRef GetSourceRef(bool isStepStop = true)
        {
            return new NovaSharp.Interpreter.Debugging.SourceRef(
                SourceId,
                FromCol,
                ToCol,
                FromLine,
                ToLine,
                isStepStop
            );
        }

        /// <summary>
        /// Builds a <see cref="NovaSharp.Interpreter.Debugging.SourceRef" /> spanning the start of
        /// this token through the end of <paramref name="to" />.
        /// </summary>
        /// <param name="to">Token that marks the end of the reference.</param>
        /// <param name="isStepStop">Whether the debugger should pause on the produced span.</param>
        /// <returns>A combined source reference covering the token range.</returns>
        internal NovaSharp.Interpreter.Debugging.SourceRef GetSourceRef(
            Token to,
            bool isStepStop = true
        )
        {
            return new NovaSharp.Interpreter.Debugging.SourceRef(
                SourceId,
                FromCol,
                to.ToCol,
                FromLine,
                to.ToLine,
                isStepStop
            );
        }

        /// <summary>
        /// Builds a <see cref="NovaSharp.Interpreter.Debugging.SourceRef" /> that ends before
        /// <paramref name="to" /> by using its previous column/line information.
        /// </summary>
        /// <param name="to">Token that supplies the exclusive end position.</param>
        /// <param name="isStepStop">Whether the debugger should pause on the produced span.</param>
        /// <returns>A source reference useful for ranges such as <c>function ... end</c>.</returns>
        internal NovaSharp.Interpreter.Debugging.SourceRef GetSourceRefUpTo(
            Token to,
            bool isStepStop = true
        )
        {
            return new NovaSharp.Interpreter.Debugging.SourceRef(
                SourceId,
                FromCol,
                to.PrevCol,
                FromLine,
                to.PrevLine,
                isStepStop
            );
        }
    }
}
