namespace NovaSharp.Interpreter.Tree.Lexer
{
    using System;

    internal class Token
    {
        public readonly int SourceId;
        public readonly int FromCol,
            ToCol,
            FromLine,
            ToLine,
            PrevCol,
            PrevLine;
        public readonly TokenType Type;

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

        public bool IsUnaryOperator()
        {
            return Type == TokenType.OpMinusOrSub
                || Type == TokenType.Not
                || Type == TokenType.OpLen
                || Type == TokenType.OpBitNotOrXor;
        }

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

        internal Debugging.SourceRef GetSourceRef(bool isStepStop = true)
        {
            return new Debugging.SourceRef(SourceId, FromCol, ToCol, FromLine, ToLine, isStepStop);
        }

        internal Debugging.SourceRef GetSourceRef(Token to, bool isStepStop = true)
        {
            return new Debugging.SourceRef(
                SourceId,
                FromCol,
                to.ToCol,
                FromLine,
                to.ToLine,
                isStepStop
            );
        }

        internal Debugging.SourceRef GetSourceRefUpTo(Token to, bool isStepStop = true)
        {
            return new Debugging.SourceRef(
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
