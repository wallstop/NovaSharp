namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
    using TreeLexer = WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    public sealed class NodeBaseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeConsumesMatchingToken()
        {
            ScriptLoadingContext context = CreateContext("identifier");

            Token token = TestNode.CallCheckTokenType(context, TokenType.Name);

            await Assert.That(token.Text).IsEqualTo("identifier").ConfigureAwait(false);
            await Assert
                .That(context.Lexer.Current.Type)
                .IsEqualTo(TokenType.Eof)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeThrowsOnMismatchAndFlagsPrematureTermination()
        {
            ScriptLoadingContext context = CreateContext(string.Empty);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.Name)
            )!;
            await Assert
                .That(exception.Message)
                .Contains("unexpected symbol")
                .ConfigureAwait(false);
            await Assert
                .That(exception.IsPrematureStreamTermination)
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeSupportsMultipleExpectedTokens()
        {
            ScriptLoadingContext context = CreateContext("true");

            Token token = TestNode.CallCheckTokenType(context, TokenType.True, TokenType.False);

            await Assert.That(token.Type).IsEqualTo(TokenType.True).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeSupportsThreeExpectedTokens()
        {
            ScriptLoadingContext context = CreateContext("elseif");

            Token token = TestNode.CallCheckTokenType(
                context,
                TokenType.If,
                TokenType.Else,
                TokenType.ElseIf
            );

            await Assert.That(token.Type).IsEqualTo(TokenType.ElseIf).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeNotNextDoesNotAdvanceLexer()
        {
            ScriptLoadingContext context = CreateContext("name");
            string original = context.Lexer.Current.Text;

            TestNode.CallCheckTokenTypeNotNext(context, TokenType.Name);

            await Assert.That(context.Lexer.Current.Text).IsEqualTo(original).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeNotNextThrowsWhenTokenDiffers()
        {
            ScriptLoadingContext context = CreateContext("name");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenTypeNotNext(context, TokenType.Number)
            )!;
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnexpectedTokenDoesNotMarkPrematureTerminationWhenNotEof()
        {
            ScriptLoadingContext context = CreateContext("identifier");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.Number)
            )!;

            await Assert
                .That(exception.IsPrematureStreamTermination)
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeWithMultipleOptionsThrowsWhenTokenDoesNotMatch()
        {
            ScriptLoadingContext context = CreateContext("end");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.True, TokenType.False)
            )!;
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckTokenTypeWithThreeOptionsThrowsWhenTokenDoesNotMatch()
        {
            ScriptLoadingContext context = CreateContext("do");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.If, TokenType.Else, TokenType.ElseIf)
            )!;
            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadingContextPropertyCanBeQueriedByDerivedType()
        {
            ScriptLoadingContext context = CreateContext("return 1");
            TestNode node = new(context);

            await Assert
                .That(node.GetLoadingContextViaHelper())
                .IsSameReferenceAs(context)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckMatchConsumesClosingToken()
        {
            ScriptLoadingContext context = CreateContext("()");
            Token opening = context.Lexer.Current;
            TestNode.CallCheckTokenType(context, TokenType.BrkOpenRound);

            Token closing = TestNode.CallCheckMatch(context, opening, TokenType.BrkCloseRound, ")");

            await Assert
                .That(closing.Type)
                .IsEqualTo(TokenType.BrkCloseRound)
                .ConfigureAwait(false);
            await Assert
                .That(context.Lexer.Current.Type)
                .IsEqualTo(TokenType.Eof)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CheckMatchThrowsForMismatchedClosingToken()
        {
            ScriptLoadingContext context = CreateContext("(");
            Token opening = context.Lexer.Current;
            TestNode.CallCheckTokenType(context, TokenType.BrkOpenRound);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckMatch(context, opening, TokenType.BrkCloseRound, ")")
            )!;
            await Assert.That(exception.Message).Contains("')' expected").ConfigureAwait(false);
            await Assert
                .That(exception.IsPrematureStreamTermination)
                .IsTrue()
                .ConfigureAwait(false);
        }

        private static ScriptLoadingContext CreateContext(string code)
        {
            Script script = new();
            ScriptLoadingContext context = new(script)
            {
                Lexer = new TreeLexer.Lexer(0, code, autoSkipComments: true),
            };
            context.Lexer.Next();
            return context;
        }

        private sealed class TestNode : NodeBase
        {
            public TestNode(ScriptLoadingContext context)
                : base(context) { }

            public override void Compile(ByteCode bc)
            {
                throw new NotImplementedException();
            }

            public static Token CallCheckTokenType(
                ScriptLoadingContext context,
                TokenType tokenType
            )
            {
                return CheckTokenType(context, tokenType);
            }

            public static Token CallCheckTokenType(
                ScriptLoadingContext context,
                TokenType tokenType1,
                TokenType tokenType2
            )
            {
                return CheckTokenType(context, tokenType1, tokenType2);
            }

            public static Token CallCheckTokenType(
                ScriptLoadingContext context,
                TokenType tokenType1,
                TokenType tokenType2,
                TokenType tokenType3
            )
            {
                return CheckTokenType(context, tokenType1, tokenType2, tokenType3);
            }

            public static void CallCheckTokenTypeNotNext(
                ScriptLoadingContext context,
                TokenType tokenType
            )
            {
                CheckTokenTypeNotNext(context, tokenType);
            }

            public static Token CallCheckMatch(
                ScriptLoadingContext context,
                Token original,
                TokenType expected,
                string expectedText
            )
            {
                return CheckMatch(context, original, expected, expectedText);
            }

            public ScriptLoadingContext GetLoadingContextViaHelper()
            {
                return LoadingContext;
            }
        }
    }
}
