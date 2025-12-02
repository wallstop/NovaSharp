namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Tree.Lexer;

    public sealed class TokenTUnitTests
    {
        private static readonly (string Keyword, TokenType Type)[] ReservedKeywordCases = new[]
        {
            ("and", TokenType.And),
            ("break", TokenType.Break),
            ("do", TokenType.Do),
            ("else", TokenType.Else),
            ("elseif", TokenType.ElseIf),
            ("end", TokenType.End),
            ("false", TokenType.False),
            ("for", TokenType.For),
            ("function", TokenType.Function),
            ("goto", TokenType.Goto),
            ("if", TokenType.If),
            ("in", TokenType.In),
            ("local", TokenType.Local),
            ("nil", TokenType.Nil),
            ("not", TokenType.Not),
            ("or", TokenType.Or),
            ("repeat", TokenType.Repeat),
            ("return", TokenType.Return),
            ("then", TokenType.Then),
            ("true", TokenType.True),
            ("until", TokenType.Until),
            ("while", TokenType.While),
        };

        private static readonly TokenType[] EndOfBlockTokens =
        {
            TokenType.Else,
            TokenType.ElseIf,
            TokenType.End,
            TokenType.Until,
            TokenType.Eof,
        };

        private static readonly TokenType[] UnaryOperatorTokens =
        {
            TokenType.OpMinusOrSub,
            TokenType.Not,
            TokenType.OpLen,
        };

        private static readonly TokenType[] BinaryOperatorTokens =
        {
            TokenType.And,
            TokenType.Or,
            TokenType.OpEqual,
            TokenType.OpLessThan,
            TokenType.OpLessThanEqual,
            TokenType.OpGreaterThanEqual,
            TokenType.OpGreaterThan,
            TokenType.OpNotEqual,
            TokenType.OpConcat,
            TokenType.OpPwr,
            TokenType.OpMod,
            TokenType.OpDiv,
            TokenType.OpMul,
            TokenType.OpMinusOrSub,
            TokenType.OpAdd,
        };

        private static readonly (TokenType Type, string Text, double Expected)[] NumericTokenCases =
            new[]
            {
                (TokenType.Number, "42.5", 42.5d),
                (TokenType.NumberHex, "0x1A", 26d),
                (TokenType.NumberHexFloat, "0x1.fp+2", 7.75d),
            };

        [global::TUnit.Core.Test]
        public async Task ToStringIncludesTypeLocationAndText()
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

            await Assert.That(description).StartsWith("Name").ConfigureAwait(false);
            await Assert.That(description).Contains("10:4-10:8").ConfigureAwait(false);
            await Assert.That(description).Contains("'print'").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetReservedTokenTypeRecognizesKeywords()
        {
            foreach ((string keyword, TokenType expected) in ReservedKeywordCases)
            {
                await Assert
                    .That(Token.GetReservedTokenType(keyword))
                    .IsEqualTo(expected)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task GetReservedTokenTypeReturnsNullForUnknownWord()
        {
            await Assert
                .That(Token.GetReservedTokenType("novasharp"))
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetNumberValueParsesSupportedNumericTokens()
        {
            foreach ((TokenType type, string text, double expected) in NumericTokenCases)
            {
                Token token = CreateToken(type, text);
                await Assert.That(token.GetNumberValue()).IsEqualTo(expected).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task GetNumberValueThrowsOnNonNumericTokens()
        {
            Token token = CreateToken(TokenType.String, "\"value\"");

            NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
                token.GetNumberValue()
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsEndOfBlockReturnsTrueForBlockTerminators()
        {
            foreach (TokenType type in EndOfBlockTokens)
            {
                Token token = CreateToken(type);
                await Assert.That(token.IsEndOfBlock()).IsTrue().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task IsEndOfBlockReturnsFalseForRegularTokens()
        {
            Token token = CreateToken(TokenType.Name);
            await Assert.That(token.IsEndOfBlock()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsUnaryOperatorRecognizesSupportedTokens()
        {
            foreach (TokenType type in UnaryOperatorTokens)
            {
                Token token = CreateToken(type);
                await Assert.That(token.IsUnaryOperator()).IsTrue().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task IsUnaryOperatorReturnsFalseForOtherTokens()
        {
            Token token = CreateToken(TokenType.Nil);
            await Assert.That(token.IsUnaryOperator()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IsBinaryOperatorRecognizesSupportedTokens()
        {
            foreach (TokenType type in BinaryOperatorTokens)
            {
                Token token = CreateToken(type);
                await Assert.That(token.IsBinaryOperator()).IsTrue().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task IsBinaryOperatorReturnsFalseForOtherTokens()
        {
            Token token = CreateToken(TokenType.Nil);
            await Assert.That(token.IsBinaryOperator()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetSourceRefUsesTokenLocation()
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

            await Assert.That(sourceRef.SourceIdx).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(sourceRef.FromLine).IsEqualTo(8).ConfigureAwait(false);
            await Assert.That(sourceRef.ToLine).IsEqualTo(9).ConfigureAwait(false);
            await Assert.That(sourceRef.FromChar).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(sourceRef.ToChar).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(sourceRef.IsStepStop).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetSourceRefExtendsToTargetTokenEnd()
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

            await Assert.That(sourceRef.SourceIdx).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(sourceRef.FromChar).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(sourceRef.ToChar).IsEqualTo(9).ConfigureAwait(false);
            await Assert.That(sourceRef.FromLine).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(sourceRef.ToLine).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(sourceRef.IsStepStop).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetSourceRefUpToStopsBeforeTargetPreviousLocation()
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

            await Assert.That(sourceRef.SourceIdx).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(sourceRef.FromChar).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(sourceRef.ToChar).IsEqualTo(15).ConfigureAwait(false);
            await Assert.That(sourceRef.FromLine).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(sourceRef.ToLine).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(sourceRef.IsStepStop).IsFalse().ConfigureAwait(false);
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
