namespace WallstopStudios.NovaSharp.Interpreter.Tree.Lexer
{
    using System;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Represents a single token produced by the lexer, including location metadata.
    /// </summary>
    internal readonly struct Token : IEquatable<Token>
    {
        /// <summary>
        /// Gets the identifier of the source chunk that produced the token.
        /// </summary>
        public readonly int sourceId;

        /// <summary>
        /// Gets the starting column (1-based) of the token.
        /// </summary>
        public readonly int fromCol;

        /// <summary>
        /// Gets the ending column (1-based, inclusive) of the token.
        /// </summary>
        public readonly int toCol;

        /// <summary>
        /// Gets the starting line (1-based) of the token.
        /// </summary>
        public readonly int fromLine;

        /// <summary>
        /// Gets the ending line (1-based) of the token.
        /// </summary>
        public readonly int toLine;

        /// <summary>
        /// Gets the previous column recorded by the lexer (useful for exclusive spans).
        /// </summary>
        public readonly int prevCol;

        /// <summary>
        /// Gets the previous line recorded by the lexer (useful for exclusive spans).
        /// </summary>
        public readonly int prevLine;

        /// <summary>
        /// Gets the token classification (identifier, keyword, operator, etc.).
        /// </summary>
        public readonly TokenType type;

        /// <summary>
        /// Gets the textual payload associated with the token.
        /// </summary>
        public readonly string text;

        public Token(
            TokenType tokenType,
            int sourceId,
            int fromLine,
            int fromCol,
            int toLine,
            int toCol,
            int prevLine,
            int prevCol,
            string text = null
        )
        {
            type = tokenType;
            this.sourceId = sourceId;
            this.fromLine = fromLine;
            this.fromCol = fromCol;
            this.toCol = toCol;
            this.toLine = toLine;
            this.prevCol = prevCol;
            this.prevLine = prevLine;
            this.text = text;
        }

        /// <summary>
        /// Creates a new Token with the specified text, preserving all other fields.
        /// </summary>
        /// <param name="text">The new text value.</param>
        /// <returns>A new Token with the updated text.</returns>
        public Token WithText(string text)
        {
            return new Token(
                type,
                sourceId,
                fromLine,
                fromCol,
                toLine,
                toCol,
                prevLine,
                prevCol,
                text
            );
        }

        /// <summary>
        /// Determines whether this token equals another token.
        /// </summary>
        /// <param name="other">The other token to compare.</param>
        /// <returns>true if all fields are equal; otherwise, false.</returns>
        public bool Equals(Token other)
        {
            return sourceId == other.sourceId
                && fromCol == other.fromCol
                && toCol == other.toCol
                && fromLine == other.fromLine
                && toLine == other.toLine
                && prevCol == other.prevCol
                && prevLine == other.prevLine
                && type == other.type
                && string.Equals(text, other.text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether this token equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>true if the object is a Token with equal fields; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is Token other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this token.
        /// </summary>
        /// <returns>A hash code based on all fields.</returns>
        public override int GetHashCode()
        {
            return DataStructs.HashCodeHelper.HashCode(
                    sourceId,
                    fromCol,
                    toCol,
                    fromLine,
                    toLine,
                    prevCol,
                    prevLine,
                    (int)type
                ) ^ (text != null ? DataStructs.HashCodeHelper.HashCode(text) : 0);
        }

        /// <summary>
        /// Determines whether two tokens are equal.
        /// </summary>
        public static bool operator ==(Token left, Token right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two tokens are not equal.
        /// </summary>
        public static bool operator !=(Token left, Token right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Formats the token for debugging by printing its type, range, and text.
        /// </summary>
        /// <returns>Human-friendly representation of the token.</returns>
        public override string ToString()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            string tokenTypeStr = TokenTypeStrings.GetName(type);
            sb.Append(tokenTypeStr);
            int tokenTypePadding = 16 - tokenTypeStr.Length;
            if (tokenTypePadding > 0)
            {
                sb.Append(' ', tokenTypePadding);
            }

            sb.Append("  - ");
            sb.Append(fromLine);
            sb.Append(':');
            sb.Append(fromCol);
            sb.Append('-');
            sb.Append(toLine);
            sb.Append(':');
            sb.Append(toCol);

            // Calculate how much padding we need for location (target: 10 chars)
            int locationLength =
                DigitCount(fromLine)
                + 1
                + DigitCount(fromCol)
                + 1
                + DigitCount(toLine)
                + 1
                + DigitCount(toCol);
            int locationPadding = 10 - locationLength;
            if (locationPadding > 0)
            {
                sb.Append(' ', locationPadding);
            }

            sb.Append(" - '");
            sb.Append(text ?? "");
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
            if (reservedWord == null)
            {
                return null;
            }

            return TryGetReservedTokenType(
                reservedWord,
                0,
                reservedWord.Length,
                out TokenType tokenType,
                out _
            )
                ? tokenType
                : null;
        }

        /// <summary>
        /// Maps a reserved word range to its token type and canonical keyword text, if any.
        /// </summary>
        /// <param name="source">Source string containing the identifier text.</param>
        /// <param name="start">The start offset of the identifier.</param>
        /// <param name="length">The identifier length.</param>
        /// <param name="tokenType">The matched token type.</param>
        /// <param name="text">The canonical keyword text.</param>
        /// <returns>true when the range is a reserved word; otherwise, false.</returns>
        internal static bool TryGetReservedTokenType(
            string source,
            int start,
            int length,
            out TokenType tokenType,
            out string text
        )
        {
            tokenType = default;
            text = null;

            if (source == null || start < 0 || length < 0 || start > source.Length - length)
            {
                return false;
            }

            switch (length)
            {
                case 2:
                    return TryGetTwoCharacterKeyword(source, start, out tokenType, out text);
                case 3:
                    return TryGetThreeCharacterKeyword(source, start, out tokenType, out text);
                case 4:
                    return TryGetFourCharacterKeyword(source, start, out tokenType, out text);
                case 5:
                    return TryGetFiveCharacterKeyword(source, start, out tokenType, out text);
                case 6:
                    return TryGetSixCharacterKeyword(source, start, out tokenType, out text);
                case 8:
                    if (MatchesKeyword(source, start, LuaKeywords.Function))
                    {
                        tokenType = TokenType.Function;
                        text = LuaKeywords.Function;
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryGetTwoCharacterKeyword(
            string source,
            int start,
            out TokenType tokenType,
            out string text
        )
        {
            char first = source[start];
            char second = source[start + 1];
            if (first == 'd' && second == 'o')
            {
                tokenType = TokenType.Do;
                text = LuaKeywords.Do;
                return true;
            }

            if (first == 'i' && second == 'f')
            {
                tokenType = TokenType.If;
                text = LuaKeywords.If;
                return true;
            }

            if (first == 'i' && second == 'n')
            {
                tokenType = TokenType.In;
                text = LuaKeywords.In;
                return true;
            }

            if (first == 'o' && second == 'r')
            {
                tokenType = TokenType.Or;
                text = LuaKeywords.Or;
                return true;
            }

            tokenType = default;
            text = null;
            return false;
        }

        private static bool TryGetThreeCharacterKeyword(
            string source,
            int start,
            out TokenType tokenType,
            out string text
        )
        {
            char first = source[start];
            if (first == 'a' && MatchesKeyword(source, start, LuaKeywords.And))
            {
                tokenType = TokenType.And;
                text = LuaKeywords.And;
                return true;
            }

            if (first == 'e' && MatchesKeyword(source, start, LuaKeywords.End))
            {
                tokenType = TokenType.End;
                text = LuaKeywords.End;
                return true;
            }

            if (first == 'f' && MatchesKeyword(source, start, LuaKeywords.For))
            {
                tokenType = TokenType.For;
                text = LuaKeywords.For;
                return true;
            }

            if (first == 'n' && MatchesKeyword(source, start, LuaKeywords.Nil))
            {
                tokenType = TokenType.Nil;
                text = LuaKeywords.Nil;
                return true;
            }

            if (first == 'n' && MatchesKeyword(source, start, LuaKeywords.Not))
            {
                tokenType = TokenType.Not;
                text = LuaKeywords.Not;
                return true;
            }

            tokenType = default;
            text = null;
            return false;
        }

        private static bool TryGetFourCharacterKeyword(
            string source,
            int start,
            out TokenType tokenType,
            out string text
        )
        {
            char first = source[start];
            if (first == 'e' && MatchesKeyword(source, start, LuaKeywords.Else))
            {
                tokenType = TokenType.Else;
                text = LuaKeywords.Else;
                return true;
            }

            if (first == 'g' && MatchesKeyword(source, start, LuaKeywords.Goto))
            {
                tokenType = TokenType.Goto;
                text = LuaKeywords.Goto;
                return true;
            }

            if (first == 't' && MatchesKeyword(source, start, LuaKeywords.Then))
            {
                tokenType = TokenType.Then;
                text = LuaKeywords.Then;
                return true;
            }

            if (first == 't' && MatchesKeyword(source, start, LuaKeywords.True))
            {
                tokenType = TokenType.True;
                text = LuaKeywords.True;
                return true;
            }

            tokenType = default;
            text = null;
            return false;
        }

        private static bool TryGetFiveCharacterKeyword(
            string source,
            int start,
            out TokenType tokenType,
            out string text
        )
        {
            char first = source[start];
            if (first == 'b' && MatchesKeyword(source, start, LuaKeywords.Break))
            {
                tokenType = TokenType.Break;
                text = LuaKeywords.Break;
                return true;
            }

            if (first == 'f' && MatchesKeyword(source, start, LuaKeywords.False))
            {
                tokenType = TokenType.False;
                text = LuaKeywords.False;
                return true;
            }

            if (first == 'l' && MatchesKeyword(source, start, LuaKeywords.Local))
            {
                tokenType = TokenType.Local;
                text = LuaKeywords.Local;
                return true;
            }

            if (first == 'u' && MatchesKeyword(source, start, LuaKeywords.Until))
            {
                tokenType = TokenType.Until;
                text = LuaKeywords.Until;
                return true;
            }

            if (first == 'w' && MatchesKeyword(source, start, LuaKeywords.While))
            {
                tokenType = TokenType.While;
                text = LuaKeywords.While;
                return true;
            }

            tokenType = default;
            text = null;
            return false;
        }

        private static bool TryGetSixCharacterKeyword(
            string source,
            int start,
            out TokenType tokenType,
            out string text
        )
        {
            char first = source[start];
            if (first == 'e' && MatchesKeyword(source, start, LuaKeywords.ElseIf))
            {
                tokenType = TokenType.ElseIf;
                text = LuaKeywords.ElseIf;
                return true;
            }

            if (first == 'r' && MatchesKeyword(source, start, LuaKeywords.Repeat))
            {
                tokenType = TokenType.Repeat;
                text = LuaKeywords.Repeat;
                return true;
            }

            if (first == 'r' && MatchesKeyword(source, start, LuaKeywords.Return))
            {
                tokenType = TokenType.Return;
                text = LuaKeywords.Return;
                return true;
            }

            tokenType = default;
            text = null;
            return false;
        }

        private static bool MatchesKeyword(string source, int start, string keyword)
        {
            for (int i = 0; i < keyword.Length; i++)
            {
                if (source[start + i] != keyword[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Maps syntax tokens with one canonical source spelling to their shared text.
        /// </summary>
        /// <param name="tokenType">The token type to map.</param>
        /// <param name="text">The fixed source spelling, when one exists.</param>
        /// <returns>true when the token type has exactly one fixed source spelling.</returns>
        internal static bool TryGetFixedSyntaxText(TokenType tokenType, out string text)
        {
            switch (tokenType)
            {
                case TokenType.Eof:
                    text = "<eof>";
                    return true;
                case TokenType.Pipe:
                    text = "|";
                    return true;
                case TokenType.OpEqual:
                    text = "==";
                    return true;
                case TokenType.OpAssignment:
                    text = "=";
                    return true;
                case TokenType.OpLessThan:
                    text = "<";
                    return true;
                case TokenType.OpLessThanEqual:
                    text = "<=";
                    return true;
                case TokenType.OpGreaterThanEqual:
                    text = ">=";
                    return true;
                case TokenType.OpGreaterThan:
                    text = ">";
                    return true;
                case TokenType.OpConcat:
                    text = "..";
                    return true;
                case TokenType.VarArgs:
                    text = "...";
                    return true;
                case TokenType.Dot:
                    text = ".";
                    return true;
                case TokenType.Colon:
                    text = ":";
                    return true;
                case TokenType.DoubleColon:
                    text = "::";
                    return true;
                case TokenType.Comma:
                    text = ",";
                    return true;
                case TokenType.BrkCloseCurly:
                    text = "}";
                    return true;
                case TokenType.BrkOpenCurly:
                    text = "{";
                    return true;
                case TokenType.BrkCloseRound:
                    text = ")";
                    return true;
                case TokenType.BrkOpenRound:
                    text = "(";
                    return true;
                case TokenType.BrkCloseSquare:
                    text = "]";
                    return true;
                case TokenType.BrkOpenSquare:
                    text = "[";
                    return true;
                case TokenType.OpLen:
                    text = "#";
                    return true;
                case TokenType.OpPwr:
                    text = "^";
                    return true;
                case TokenType.OpMod:
                    text = "%";
                    return true;
                case TokenType.OpDiv:
                    text = "/";
                    return true;
                case TokenType.OpMul:
                    text = "*";
                    return true;
                case TokenType.OpMinusOrSub:
                    text = "-";
                    return true;
                case TokenType.OpAdd:
                    text = "+";
                    return true;
                case TokenType.SemiColon:
                    text = ";";
                    return true;
                case TokenType.BrkOpenCurlyShared:
                    text = "${";
                    return true;
                case TokenType.OpDollar:
                    text = "$";
                    return true;
                case TokenType.OpBitNotOrXor:
                    text = "~";
                    return true;
                case TokenType.OpBitAnd:
                    text = "&";
                    return true;
                case TokenType.OpShiftLeft:
                    text = "<<";
                    return true;
                case TokenType.OpShiftRight:
                    text = ">>";
                    return true;
                case TokenType.OpFloorDiv:
                    text = "//";
                    return true;
                default:
                    text = null;
                    return false;
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
            if (type == TokenType.Number)
            {
                return LexerUtils.ParseNumber(this);
            }
            else if (type == TokenType.NumberHex)
            {
                return LexerUtils.ParseHexInteger(this);
            }
            else if (type == TokenType.NumberHexFloat)
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
            if (type == TokenType.NumberHex)
            {
                return LexerUtils.TryParseHexIntegerAsLong(this, out value);
            }
            else if (type == TokenType.Number && !IsFloatLiteralSyntax())
            {
                return long.TryParse(
                    text,
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
            if (type == TokenType.NumberHexFloat)
            {
                return true;
            }
            else if (type == TokenType.NumberHex)
            {
                return false;
            }
            else if (type == TokenType.Number)
            {
                // Check if the text contains a decimal point or exponent indicator
                // Examples: "1.0" -> true, "1e5" -> true, "1" -> false, "123" -> false
                return text.Contains('.', StringComparison.Ordinal)
                    || text.Contains('e', StringComparison.OrdinalIgnoreCase)
                    || text.Contains('E', StringComparison.Ordinal);
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
            switch (type)
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
            return type == TokenType.OpMinusOrSub
                || type == TokenType.Not
                || type == TokenType.OpLen
                || type == TokenType.OpBitNotOrXor;
        }

        /// <summary>
        /// Determines whether the token can be used as a binary operator.
        /// </summary>
        /// <returns><c>true</c> when the token represents a binary operator.</returns>
        public bool IsBinaryOperator()
        {
            switch (type)
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
                sourceId,
                fromCol,
                toCol,
                fromLine,
                toLine,
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
                sourceId,
                fromCol,
                to.toCol,
                fromLine,
                to.toLine,
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
                sourceId,
                fromCol,
                to.prevCol,
                fromLine,
                to.prevLine,
                isStepStop
            );
        }
    }
}
