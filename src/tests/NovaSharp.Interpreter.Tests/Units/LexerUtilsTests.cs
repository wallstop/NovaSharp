namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LexerUtilsTests
    {
        [Test]
        public void ParseNumberReadsInvariantValue()
        {
            Token token = CreateToken(TokenType.Number, "42.5");

            double value = LexerUtils.ParseNumber(token);

            Assert.That(value, Is.EqualTo(42.5d));
        }

        [Test]
        public void ParseNumberThrowsOnMalformedInput()
        {
            Token token = CreateToken(TokenType.Number, "12..3");

            Assert.That(() => LexerUtils.ParseNumber(token), Throws.TypeOf<SyntaxErrorException>());
        }

        [Test]
        public void ParseHexIntegerParsesDigits()
        {
            Token token = CreateToken(TokenType.NumberHex, "0x1A");

            double value = LexerUtils.ParseHexInteger(token);

            Assert.That(value, Is.EqualTo(26d));
        }

        [Test]
        public void ParseHexIntegerThrowsOnInvalidDigits()
        {
            Token token = CreateToken(TokenType.NumberHex, "0x1G");

            Assert.That(
                () => LexerUtils.ParseHexInteger(token),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void ParseHexIntegerThrowsWhenPrefixMissing()
        {
            Token token = CreateToken(TokenType.NumberHex, "12");

            Assert.That(
                () => LexerUtils.ParseHexInteger(token),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void ParseHexIntegerThrowsWhenTextTooShort()
        {
            Token token = CreateToken(TokenType.NumberHex, "0");

            Assert.That(
                () => LexerUtils.ParseHexInteger(token),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void ParseHexFloatHandlesFractionAndExponent()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.fp+2");

            double value = LexerUtils.ParseHexFloat(token);

            // (1 + 15/16) * 2^2 = 7.75
            Assert.That(value, Is.EqualTo(7.75d));
        }

        [Test]
        public void ParseHexFloatThrowsOnMissingExponentDigits()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.p");

            Assert.That(
                () => LexerUtils.ParseHexFloat(token),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void ParseHexFloatThrowsWhenPrefixMissing()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "1.fp");

            Assert.That(
                () => LexerUtils.ParseHexFloat(token),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void ParseHexFloatThrowsWhenTextTooShort()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0");

            Assert.That(
                () => LexerUtils.ParseHexFloat(token),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void ParseHexFloatThrowsWhenExponentIsNotNumeric()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.fp+Z");

            Assert.That(
                () => LexerUtils.ParseHexFloat(token),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void ReadHexProgressiveConsumesDigitsAndReturnsRemainder()
        {
            double value = 0d;

            string remainder = LexerUtils.ReadHexProgressive("1AG", ref value, out int digits);

            Assert.Multiple(() =>
            {
                Assert.That(digits, Is.EqualTo(2));
                Assert.That(value, Is.EqualTo(26d));
                Assert.That(remainder, Is.EqualTo("G"));
            });
        }

        [Test]
        public void HexDigit2ValueThrowsOnInvalidCharacter()
        {
            Assert.That(
                () => LexerUtils.HexDigit2Value('Z'),
                Throws.TypeOf<InternalErrorException>()
            );
        }

        [Test]
        public void AdjustLuaLongStringDropsLeadingLineBreaks()
        {
            Assert.That(LexerUtils.AdjustLuaLongString("\r\nline"), Is.EqualTo("line"));
            Assert.That(LexerUtils.AdjustLuaLongString("\nline"), Is.EqualTo("line"));
        }

        [Test]
        public void UnescapeLuaStringHandlesNumericHexUnicodeAndZMode()
        {
            Token token = CreateToken(TokenType.String, string.Empty);
            string escaped = "\\083\\x41\\u{1F600}\\z \nworld";

            string result = LexerUtils.UnescapeLuaString(token, escaped);

            // \083 -> 'S', \x41 -> 'A', \u{1F600} -> 😀, \z removes whitespace before "world".
            Assert.That(result, Is.EqualTo("SA😀world"));
        }

        [Test]
        public void UnescapeLuaStringThrowsOnInvalidEscape()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            Assert.That(
                () => LexerUtils.UnescapeLuaString(token, "\\x4G"),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void UnescapeLuaStringThrowsWhenUnicodeOpeningBraceMissing()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            Assert.That(
                () => LexerUtils.UnescapeLuaString(token, "\\u1234"),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void UnescapeLuaStringThrowsWhenUnicodeClosingBraceMissing()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            Assert.That(
                () => LexerUtils.UnescapeLuaString(token, "\\u{123456789"),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void UnescapeLuaStringThrowsOnUnfinishedEscape()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            Assert.That(
                () => LexerUtils.UnescapeLuaString(token, "unfinished\\"),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        private static Token CreateToken(TokenType type, string text)
        {
            return new Token(
                type,
                sourceId: 0,
                fromLine: 1,
                fromCol: 1,
                toLine: 1,
                toCol: text.Length,
                prevLine: 1,
                prevCol: 0
            )
            {
                Text = text,
            };
        }
    }
}
