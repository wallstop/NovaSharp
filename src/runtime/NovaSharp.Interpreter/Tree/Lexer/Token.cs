namespace NovaSharp.Interpreter.Tree.Lexer
{
    using System;

    /// <summary>
    /// Represents a single token produced by the lexer, including location metadata.
    /// </summary>
    internal class Token
    {
        public int SourceId { get; }
        public int FromCol { get; }
        public int ToCol { get; }
        public int FromLine { get; }
        public int ToLine { get; }
        public int PrevCol { get; }
        public int PrevLine { get; }
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
            string tokenTypeString = (
                Type.ToString() + "                                                      "
            ).Substring(0, 16);

            string location = $"{FromLine}:{FromCol}-{ToLine}:{ToCol}";

            location = (
                location + "                                                      "
            ).Substring(0, 10);

            return $"{tokenTypeString}  - {location} - '{Text ?? ""}'";
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
