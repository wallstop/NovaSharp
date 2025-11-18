namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    internal sealed class TokenTests
    {
        [Test]
        public void ToStringIncludesPaddedTypeLocationAndText()
        {
            Token token = CreateToken(
                TokenType.Name,
                text: "name",
                fromLine: 4,
                fromCol: 2,
                toLine: 4,
                toCol: 6
            );

            string rendered = token.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(rendered, Does.Contain("Name"));
                Assert.That(rendered, Does.Contain("4:2-4:6"));
                Assert.That(rendered, Does.EndWith("'name'"));
            });
        }

        [TestCase("and", TokenType.And)]
        [TestCase("function", TokenType.Function)]
        [TestCase("until", TokenType.Until)]
        public void GetReservedTokenTypeMatchesLuaKeywords(string keyword, TokenType expected)
        {
            TokenType? result = Token.GetReservedTokenType(keyword);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GetReservedTokenTypeReturnsNullForUnknownWord()
        {
            Assert.That(Token.GetReservedTokenType("novasharp"), Is.Null);
        }

        [Test]
        public void GetNumberValueParsesDecimalHexAndHexFloat()
        {
            Token decimalToken = CreateToken(TokenType.Number, text: "12.5");
            Token hexToken = CreateToken(TokenType.NumberHex, text: "0x10");
            Token hexFloatToken = CreateToken(TokenType.NumberHexFloat, text: "0x1p+2");

            Assert.Multiple(() =>
            {
                Assert.That(decimalToken.GetNumberValue(), Is.EqualTo(12.5d));
                Assert.That(hexToken.GetNumberValue(), Is.EqualTo(16d));
                Assert.That(hexFloatToken.GetNumberValue(), Is.EqualTo(4d));
            });
        }

        [Test]
        public void GetNumberValueThrowsWhenTokenNotNumeric()
        {
            Token token = CreateToken(TokenType.Name, text: "foo");

            Assert.That(
                () => token.GetNumberValue(),
                Throws.TypeOf<System.NotSupportedException>().With.Message.Contains("numeric")
            );
        }

        [Test]
        public void IsEndOfBlockRecognizesTerminatingTokens()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CreateToken(TokenType.End).IsEndOfBlock(), Is.True);
                Assert.That(CreateToken(TokenType.Else).IsEndOfBlock(), Is.True);
                Assert.That(CreateToken(TokenType.Break).IsEndOfBlock(), Is.False);
            });
        }

        [Test]
        public void IsUnaryOperatorMatchesNotMinusAndLength()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CreateToken(TokenType.Not).IsUnaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.OpMinusOrSub).IsUnaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.OpLen).IsUnaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.And).IsUnaryOperator(), Is.False);
            });
        }

        [Test]
        public void IsBinaryOperatorRecognizesArithmeticTokens()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CreateToken(TokenType.OpAdd).IsBinaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.OpEqual).IsBinaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.OpConcat).IsBinaryOperator(), Is.True);
                Assert.That(CreateToken(TokenType.Not).IsBinaryOperator(), Is.False);
            });
        }

        [Test]
        public void GetSourceRefUsesTokenCoordinates()
        {
            Token token = CreateToken(TokenType.Name, fromLine: 3, fromCol: 1, toLine: 3, toCol: 5);

            SourceRef source = token.GetSourceRef(isStepStop: false);

            Assert.Multiple(() =>
            {
                Assert.That(source.SourceIdx, Is.EqualTo(7));
                Assert.That(source.FromLine, Is.EqualTo(3));
                Assert.That(source.FromChar, Is.EqualTo(1));
                Assert.That(source.ToLine, Is.EqualTo(3));
                Assert.That(source.ToChar, Is.EqualTo(5));
                Assert.That(source.IsStepStop, Is.False);
            });
        }

        [Test]
        public void GetSourceRefUpToUsesPreviousCoordinates()
        {
            Token from = CreateToken(
                TokenType.Name,
                fromLine: 5,
                fromCol: 2,
                toLine: 5,
                toCol: 4,
                prevLine: 4,
                prevCol: 8
            );
            Token to = CreateToken(
                TokenType.Name,
                fromLine: 5,
                fromCol: 5,
                toLine: 5,
                toCol: 9,
                prevLine: 5,
                prevCol: 6
            );

            SourceRef source = from.GetSourceRefUpTo(to, isStepStop: true);

            Assert.Multiple(() =>
            {
                Assert.That(source.FromChar, Is.EqualTo(2));
                Assert.That(source.ToChar, Is.EqualTo(6));
                Assert.That(source.FromLine, Is.EqualTo(5));
                Assert.That(source.ToLine, Is.EqualTo(5));
                Assert.That(source.IsStepStop, Is.True);
            });
        }

        private static Token CreateToken(
            TokenType type = TokenType.Name,
            string text = null,
            int sourceId = 7,
            int fromLine = 2,
            int fromCol = 3,
            int toLine = 2,
            int toCol = 5,
            int prevLine = 1,
            int prevCol = 1
        )
        {
            Token token = new(type, sourceId, fromLine, fromCol, toLine, toCol, prevLine, prevCol)
            {
                Text = text,
            };
            return token;
        }
    }
}
