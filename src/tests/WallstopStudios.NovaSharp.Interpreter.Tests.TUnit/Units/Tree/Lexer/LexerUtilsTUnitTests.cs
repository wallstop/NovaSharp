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
                prevCol: 0,
                text: text
            );
        }
    }
}
