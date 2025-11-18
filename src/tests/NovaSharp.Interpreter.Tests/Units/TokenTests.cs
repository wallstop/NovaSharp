namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TokenTests
    {
        private static readonly object[] _reservedKeywordCases =
        {
            new object[] { "and", (int)TokenType.And },
            new object[] { "break", (int)TokenType.Break },
            new object[] { "do", (int)TokenType.Do },
            new object[] { "else", (int)TokenType.Else },
            new object[] { "elseif", (int)TokenType.ElseIf },
            new object[] { "end", (int)TokenType.End },
            new object[] { "false", (int)TokenType.False },
            new object[] { "for", (int)TokenType.For },
            new object[] { "function", (int)TokenType.Function },
            new object[] { "goto", (int)TokenType.Goto },
            new object[] { "if", (int)TokenType.If },
            new object[] { "in", (int)TokenType.In },
            new object[] { "local", (int)TokenType.Local },
            new object[] { "nil", (int)TokenType.Nil },
            new object[] { "not", (int)TokenType.Not },
            new object[] { "or", (int)TokenType.Or },
            new object[] { "repeat", (int)TokenType.Repeat },
            new object[] { "return", (int)TokenType.Return },
            new object[] { "then", (int)TokenType.Then },
            new object[] { "true", (int)TokenType.True },
            new object[] { "until", (int)TokenType.Until },
            new object[] { "while", (int)TokenType.While },
        };

        private static readonly int[] _endOfBlockTokens =
        {
            (int)TokenType.Else,
            (int)TokenType.ElseIf,
            (int)TokenType.End,
            (int)TokenType.Until,
            (int)TokenType.Eof,
        };

        private static readonly int[] _unaryOperatorTokens =
        {
            (int)TokenType.OpMinusOrSub,
            (int)TokenType.Not,
            (int)TokenType.OpLen,
        };

        private static readonly int[] _binaryOperatorTokens =
        {
            (int)TokenType.And,
            (int)TokenType.Or,
            (int)TokenType.OpEqual,
            (int)TokenType.OpLessThan,
            (int)TokenType.OpLessThanEqual,
            (int)TokenType.OpGreaterThanEqual,
            (int)TokenType.OpGreaterThan,
            (int)TokenType.OpNotEqual,
            (int)TokenType.OpConcat,
            (int)TokenType.OpPwr,
            (int)TokenType.OpMod,
            (int)TokenType.OpDiv,
            (int)TokenType.OpMul,
            (int)TokenType.OpMinusOrSub,
            (int)TokenType.OpAdd,
        };

        private static readonly object[] _numericTokenCases =
        {
            new object[] { (int)TokenType.Number, "42.5", 42.5d },
            new object[] { (int)TokenType.NumberHex, "0x1A", 26d },
            new object[] { (int)TokenType.NumberHexFloat, "0x1.fp+2", 7.75d },
        };

        [Test]
        public void ToStringIncludesTypeLocationAndText()
        {
            Token token = CreateToken(
                TokenType.Name,
                "print",
                sourceId: 3,
                fromLine: 10,
                fromCol: 4,
                toLine: 10,
                toCol: 8
            );

            string description = token.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(description, Does.StartWith("Name"));
                Assert.That(description, Does.Contain("10:4-10:8"));
                Assert.That(description, Does.Contain("'print'"));
            });
        }

        [TestCaseSource(nameof(_reservedKeywordCases))]
        public void GetReservedTokenTypeRecognizesKeywords(string keyword, int expectedTokenValue)
        {
            TokenType expected = (TokenType)expectedTokenValue;

            Assert.That(Token.GetReservedTokenType(keyword), Is.EqualTo(expected));
        }

        [Test]
        public void GetReservedTokenTypeReturnsNullForUnknownWord()
        {
            Assert.That(Token.GetReservedTokenType("novasharp"), Is.Null);
        }

        [TestCaseSource(nameof(_numericTokenCases))]
        public void GetNumberValueParsesSupportedNumericTokens(
            int tokenTypeValue,
            string text,
            double expected
        )
        {
            TokenType tokenType = (TokenType)tokenTypeValue;
            Token token = CreateToken(tokenType, text);

            Assert.That(token.GetNumberValue(), Is.EqualTo(expected));
        }

        [Test]
        public void GetNumberValueThrowsOnNonNumericTokens()
        {
            Token token = CreateToken(TokenType.String, "\"value\"");

            Assert.That(() => token.GetNumberValue(), Throws.TypeOf<NotSupportedException>());
        }

        [TestCaseSource(nameof(_endOfBlockTokens))]
        public void IsEndOfBlockReturnsTrueForBlockTerminators(int tokenTypeValue)
        {
            Token token = CreateToken((TokenType)tokenTypeValue);

            Assert.That(token.IsEndOfBlock(), Is.True);
        }

        [Test]
        public void IsEndOfBlockReturnsFalseForRegularTokens()
        {
            Token token = CreateToken(TokenType.Name);

            Assert.That(token.IsEndOfBlock(), Is.False);
        }

        [TestCaseSource(nameof(_unaryOperatorTokens))]
        public void IsUnaryOperatorRecognizesSupportedTokens(int tokenTypeValue)
        {
            Token token = CreateToken((TokenType)tokenTypeValue);

            Assert.That(token.IsUnaryOperator(), Is.True);
        }

        [Test]
        public void IsUnaryOperatorReturnsFalseForOtherTokens()
        {
            Token token = CreateToken(TokenType.Nil);

            Assert.That(token.IsUnaryOperator(), Is.False);
        }

        [TestCaseSource(nameof(_binaryOperatorTokens))]
        public void IsBinaryOperatorRecognizesSupportedTokens(int tokenTypeValue)
        {
            Token token = CreateToken((TokenType)tokenTypeValue);

            Assert.That(token.IsBinaryOperator(), Is.True);
        }

        [Test]
        public void IsBinaryOperatorReturnsFalseForOtherTokens()
        {
            Token token = CreateToken(TokenType.Nil);

            Assert.That(token.IsBinaryOperator(), Is.False);
        }

        [Test]
        public void GetSourceRefUsesTokenLocation()
        {
            Token token = CreateToken(
                TokenType.Number,
                "12",
                sourceId: 5,
                fromLine: 8,
                fromCol: 2,
                toLine: 9,
                toCol: 4
            );

            SourceRef sourceRef = token.GetSourceRef();

            Assert.Multiple(() =>
            {
                Assert.That(sourceRef.SourceIdx, Is.EqualTo(5));
                Assert.That(sourceRef.FromLine, Is.EqualTo(8));
                Assert.That(sourceRef.ToLine, Is.EqualTo(9));
                Assert.That(sourceRef.FromChar, Is.EqualTo(2));
                Assert.That(sourceRef.ToChar, Is.EqualTo(4));
                Assert.That(sourceRef.IsStepStop, Is.True);
            });
        }

        [Test]
        public void GetSourceRefExtendsToTargetTokenEnd()
        {
            Token start = CreateToken(
                TokenType.Name,
                "lo",
                sourceId: 1,
                fromLine: 1,
                fromCol: 5,
                toLine: 1,
                toCol: 6
            );
            Token end = CreateToken(
                TokenType.Name,
                "rem",
                sourceId: 1,
                fromLine: 1,
                fromCol: 7,
                toLine: 1,
                toCol: 9
            );

            SourceRef sourceRef = start.GetSourceRef(end, isStepStop: false);

            Assert.Multiple(() =>
            {
                Assert.That(sourceRef.SourceIdx, Is.EqualTo(1));
                Assert.That(sourceRef.FromChar, Is.EqualTo(5));
                Assert.That(sourceRef.ToChar, Is.EqualTo(9));
                Assert.That(sourceRef.FromLine, Is.EqualTo(1));
                Assert.That(sourceRef.ToLine, Is.EqualTo(1));
                Assert.That(sourceRef.IsStepStop, Is.False);
            });
        }

        [Test]
        public void GetSourceRefUpToStopsBeforeTargetPreviousLocation()
        {
            Token start = CreateToken(
                TokenType.Name,
                "print",
                sourceId: 2,
                fromLine: 4,
                fromCol: 10,
                toLine: 4,
                toCol: 14,
                prevLine: 4,
                prevCol: 9
            );
            Token end = CreateToken(
                TokenType.Name,
                "line",
                sourceId: 2,
                fromLine: 4,
                fromCol: 16,
                toLine: 4,
                toCol: 19,
                prevLine: 4,
                prevCol: 15
            );

            SourceRef sourceRef = start.GetSourceRefUpTo(end, isStepStop: false);

            Assert.Multiple(() =>
            {
                Assert.That(sourceRef.SourceIdx, Is.EqualTo(2));
                Assert.That(sourceRef.FromChar, Is.EqualTo(10));
                Assert.That(sourceRef.ToChar, Is.EqualTo(15));
                Assert.That(sourceRef.FromLine, Is.EqualTo(4));
                Assert.That(sourceRef.ToLine, Is.EqualTo(4));
                Assert.That(sourceRef.IsStepStop, Is.False);
            });
        }

        private static Token CreateToken(
            TokenType type,
            string text = "",
            int sourceId = 7,
            int fromLine = 3,
            int fromCol = 4,
            int toLine = 3,
            int toCol = 6,
            int prevLine = 3,
            int prevCol = 2
        )
        {
            return new Token(type, sourceId, fromLine, fromCol, toLine, toCol, prevLine, prevCol)
            {
                Text = text,
            };
        }
    }
}
