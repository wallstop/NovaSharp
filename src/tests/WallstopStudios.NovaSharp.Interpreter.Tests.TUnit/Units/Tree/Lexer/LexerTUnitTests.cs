namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Lexer
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    public sealed class LexerTUnitTests
    {
        private static readonly (string Keyword, TokenType Type)[] ReservedKeywordCases = new[]
        {
            (LuaKeywords.And, TokenType.And),
            (LuaKeywords.Break, TokenType.Break),
            (LuaKeywords.Do, TokenType.Do),
            (LuaKeywords.Else, TokenType.Else),
            (LuaKeywords.ElseIf, TokenType.ElseIf),
            (LuaKeywords.End, TokenType.End),
            (LuaKeywords.False, TokenType.False),
            (LuaKeywords.For, TokenType.For),
            (LuaKeywords.Function, TokenType.Function),
            (LuaKeywords.Goto, TokenType.Goto),
            (LuaKeywords.If, TokenType.If),
            (LuaKeywords.In, TokenType.In),
            (LuaKeywords.Local, TokenType.Local),
            (LuaKeywords.Nil, TokenType.Nil),
            (LuaKeywords.Not, TokenType.Not),
            (LuaKeywords.Or, TokenType.Or),
            (LuaKeywords.Repeat, TokenType.Repeat),
            (LuaKeywords.Return, TokenType.Return),
            (LuaKeywords.Then, TokenType.Then),
            (LuaKeywords.True, TokenType.True),
            (LuaKeywords.Until, TokenType.Until),
            (LuaKeywords.While, TokenType.While),
        };

        [global::TUnit.Core.Test]
        public async Task KeywordTokensUseCanonicalText()
        {
            for (int i = 0; i < ReservedKeywordCases.Length; i++)
            {
                (string keyword, TokenType expectedType) = ReservedKeywordCases[i];
                Lexer lexer = new(sourceId: 0, keyword, autoSkipComments: true);

                Token token = lexer.Current;

                await Assert.That(token.type).IsEqualTo(expectedType).ConfigureAwait(false);
                await Assert.That(token.text).IsSameReferenceAs(keyword).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ReservedKeywordCasesStayAlignedWithLuaKeywordsSet()
        {
            await Assert
                .That(LuaKeywords.All.Count)
                .IsEqualTo(ReservedKeywordCases.Length)
                .ConfigureAwait(false);

            for (int i = 0; i < ReservedKeywordCases.Length; i++)
            {
                (string keyword, _) = ReservedKeywordCases[i];
                await Assert.That(LuaKeywords.All.Contains(keyword)).IsTrue().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task KeywordPrefixesRemainNamesWhenIdentifierContinues()
        {
            string[] names = { "ifx", "if1", "if_", "_if", "endx", "TRUE", "If", "if" + '\u00e9' };

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                Lexer lexer = new(sourceId: 0, name, autoSkipComments: true);

                Token token = lexer.Current;

                await Assert.That(token.type).IsEqualTo(TokenType.Name).ConfigureAwait(false);
                await Assert.That(token.text).IsEqualTo(name).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task KeywordHeavyChunkParsesAfterRangeBasedKeywordClassification()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local total = 0
                local values = { true, false, nil }

                local function adjust(value)
                    if value and not false then
                        return 3
                    elseif value or false then
                        return 2
                    else
                        return 1
                    end
                end

                for _, value in ipairs(values) do
                    total = total + adjust(value)
                end

                repeat
                    total = total + 1
                until total > 8

                while total < 10 do
                    total = total + 1
                end

                return total
            "
            );

            await Assert.That(result.Number).IsEqualTo(10d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task KeywordLexingAvoidsIdentifierStringAllocation()
        {
            const int tokenCount = 256;
            const int iterations = 64;
            string keywordSource = BuildRepeatedTokenSource(LuaKeywords.Local, tokenCount);
            string identifierSource = BuildRepeatedTokenSource("value", tokenCount);

            MeasureLexingAllocations(keywordSource, tokenCount, iterations: 2);
            MeasureLexingAllocations(identifierSource, tokenCount, iterations: 2);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long keywordAllocated = MeasureLexingAllocations(keywordSource, tokenCount, iterations);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long identifierAllocated = MeasureLexingAllocations(
                identifierSource,
                tokenCount,
                iterations
            );

            long savedBytesPerKeyword =
                (identifierAllocated - keywordAllocated) / (tokenCount * iterations);

            await Assert.That(savedBytesPerKeyword).IsGreaterThan(16).ConfigureAwait(false);
        }

        private static string BuildRepeatedTokenSource(string tokenText, int tokenCount)
        {
            StringBuilder builder = new((tokenText.Length + 1) * tokenCount);
            for (int i = 0; i < tokenCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(tokenText);
            }

            return builder.ToString();
        }

        private static long MeasureLexingAllocations(
            string source,
            int expectedTokenCount,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                Lexer lexer = new(sourceId: 0, source, autoSkipComments: true);
                int tokenCount = 0;
                while (true)
                {
                    Token token = lexer.Current;
                    if (token.type == TokenType.Eof)
                    {
                        break;
                    }

                    tokenCount++;
                    lexer.Next();
                }

                if (tokenCount != expectedTokenCount)
                {
                    throw new InvalidOperationException("Lexer allocation probe token mismatch.");
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
