namespace NovaSharp.Interpreter.Tree
{
    using System;

    internal class Token
    {
        public readonly int sourceId;
        public readonly int fromCol,
            toCol,
            fromLine,
            toLine,
            prevCol,
            prevLine;
        public readonly TokenType type;

        public string Text { get; set; }

        public Token(
            TokenType type,
            int sourceId,
            int fromLine,
            int fromCol,
            int toLine,
            int toCol,
            int prevLine,
            int prevCol
        )
        {
            this.type = type;

            this.sourceId = sourceId;
            this.fromLine = fromLine;
            this.fromCol = fromCol;
            this.toCol = toCol;
            this.toLine = toLine;
            this.prevCol = prevCol;
            this.prevLine = prevLine;
        }

        public override string ToString()
        {
            string tokenTypeString = (
                type.ToString() + "                                                      "
            ).Substring(0, 16);

            string location = $"{fromLine}:{fromCol}-{toLine}:{toCol}";

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

        public bool IsUnaryOperator()
        {
            return type == TokenType.OpMinusOrSub
                || type == TokenType.Not
                || type == TokenType.OpLen;
        }

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
                case TokenType.OpMul:
                case TokenType.OpMinusOrSub:
                case TokenType.OpAdd:
                    return true;
                default:
                    return false;
            }
        }

        internal Debugging.SourceRef GetSourceRef(bool isStepStop = true)
        {
            return new Debugging.SourceRef(sourceId, fromCol, toCol, fromLine, toLine, isStepStop);
        }

        internal Debugging.SourceRef GetSourceRef(Token to, bool isStepStop = true)
        {
            return new Debugging.SourceRef(
                sourceId,
                fromCol,
                to.toCol,
                fromLine,
                to.toLine,
                isStepStop
            );
        }

        internal Debugging.SourceRef GetSourceRefUpTo(Token to, bool isStepStop = true)
        {
            return new Debugging.SourceRef(
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
