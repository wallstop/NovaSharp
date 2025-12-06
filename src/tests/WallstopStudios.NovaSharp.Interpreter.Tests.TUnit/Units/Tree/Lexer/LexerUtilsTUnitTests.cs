namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Lexer
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    public sealed class LexerUtilsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParseNumberReadsInvariantValue()
        {
            Token token = CreateToken(TokenType.Number, "42.5");

            double value = LexerUtils.ParseNumber(token);

            await Assert.That(value).IsEqualTo(42.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseNumberThrowsOnMalformedInput()
        {
            Token token = CreateToken(TokenType.Number, "12..3");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.ParseNumber(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexIntegerParsesDigits()
        {
            Token token = CreateToken(TokenType.NumberHex, "0x1A");

            double value = LexerUtils.ParseHexInteger(token);

            await Assert.That(value).IsEqualTo(26d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexIntegerThrowsOnInvalidDigits()
        {
            Token token = CreateToken(TokenType.NumberHex, "0x1G");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.ParseHexInteger(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexIntegerThrowsWhenPrefixMissing()
        {
            Token token = CreateToken(TokenType.NumberHex, "12");

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LexerUtils.ParseHexInteger(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexIntegerThrowsWhenTextTooShort()
        {
            Token token = CreateToken(TokenType.NumberHex, "0");

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LexerUtils.ParseHexInteger(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexFloatHandlesFractionAndExponent()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.fp+2");

            double value = LexerUtils.ParseHexFloat(token);

            await Assert.That(value).IsEqualTo(7.75d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexFloatThrowsOnMissingExponentDigits()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.p");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.ParseHexFloat(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexFloatThrowsWhenPrefixMissing()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "1.fp");

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LexerUtils.ParseHexFloat(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexFloatThrowsWhenTextTooShort()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0");

            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LexerUtils.ParseHexFloat(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseHexFloatThrowsWhenExponentIsNotNumeric()
        {
            Token token = CreateToken(TokenType.NumberHexFloat, "0x1.fp+Z");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.ParseHexFloat(token)
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ReadHexProgressiveConsumesDigitsAndReturnsRemainder()
        {
            double value = 0d;

            string remainder = LexerUtils.ReadHexProgressive("1AG", ref value, out int digits);

            await Assert.That(digits).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(value).IsEqualTo(26d).ConfigureAwait(false);
            await Assert.That(remainder).IsEqualTo("G").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HexDigit2ValueThrowsOnInvalidCharacter()
        {
            InternalErrorException exception = Assert.Throws<InternalErrorException>(() =>
                LexerUtils.HexDigit2Value('Z')
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AdjustLuaLongStringDropsLeadingLineBreaks()
        {
            await Assert
                .That(LexerUtils.AdjustLuaLongString("\r\nline"))
                .IsEqualTo("line")
                .ConfigureAwait(false);
            await Assert
                .That(LexerUtils.AdjustLuaLongString("\nline"))
                .IsEqualTo("line")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnescapeLuaStringHandlesNumericHexUnicodeAndZMode()
        {
            Token token = CreateToken(TokenType.String, string.Empty);
            string escaped = "\\083\\x41\\u{1F600}\\z \nworld";

            string result = LexerUtils.UnescapeLuaString(token, escaped);

            await Assert.That(result).IsEqualTo("SA😀world").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnescapeLuaStringThrowsOnInvalidEscape()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.UnescapeLuaString(token, "\\x4G")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnescapeLuaStringThrowsWhenUnicodeOpeningBraceMissing()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.UnescapeLuaString(token, "\\u1234")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnescapeLuaStringThrowsWhenUnicodeClosingBraceMissing()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.UnescapeLuaString(token, "\\u{123456789")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnescapeLuaStringThrowsOnUnfinishedEscape()
        {
            Token token = CreateToken(TokenType.String, string.Empty);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                LexerUtils.UnescapeLuaString(token, "unfinished\\")
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
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
