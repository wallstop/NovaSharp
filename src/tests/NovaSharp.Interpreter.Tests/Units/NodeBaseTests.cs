namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class NodeBaseTests
    {
        [Test]
        public void CheckTokenTypeConsumesMatchingToken()
        {
            ScriptLoadingContext context = CreateContext("identifier");

            Token token = TestNode.CallCheckTokenType(context, TokenType.Name);

            Assert.Multiple(() =>
            {
                Assert.That(token.Text, Is.EqualTo("identifier"));
                Assert.That(context.Lexer.Current.Type, Is.EqualTo(TokenType.Eof));
            });
        }

        [Test]
        public void CheckTokenTypeThrowsOnMismatchAndFlagsPrematureTermination()
        {
            ScriptLoadingContext context = CreateContext(string.Empty);

            SyntaxErrorException ex = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.Name)
            );
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Does.Contain("unexpected symbol"));
                Assert.That(ex.IsPrematureStreamTermination, Is.True);
            });
        }

        [Test]
        public void CheckTokenTypeSupportsMultipleExpectedTokens()
        {
            ScriptLoadingContext context = CreateContext("true");

            Token token = TestNode.CallCheckTokenType(context, TokenType.True, TokenType.False);

            Assert.That(token.Type, Is.EqualTo(TokenType.True));
        }

        [Test]
        public void CheckTokenTypeSupportsThreeExpectedTokens()
        {
            ScriptLoadingContext context = CreateContext("elseif");

            Token token = TestNode.CallCheckTokenType(
                context,
                TokenType.If,
                TokenType.Else,
                TokenType.ElseIf
            );

            Assert.That(token.Type, Is.EqualTo(TokenType.ElseIf));
        }

        [Test]
        public void CheckTokenTypeNotNextDoesNotAdvanceLexer()
        {
            ScriptLoadingContext context = CreateContext("name");
            string original = context.Lexer.Current.Text;

            TestNode.CallCheckTokenTypeNotNext(context, TokenType.Name);

            Assert.That(context.Lexer.Current.Text, Is.EqualTo(original));
        }

        [Test]
        public void CheckTokenTypeNotNextThrowsWhenTokenDiffers()
        {
            ScriptLoadingContext context = CreateContext("name");

            Assert.That(
                () => TestNode.CallCheckTokenTypeNotNext(context, TokenType.Number),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void UnexpectedTokenDoesNotMarkPrematureTerminationWhenNotEof()
        {
            ScriptLoadingContext context = CreateContext("identifier");

            SyntaxErrorException ex = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckTokenType(context, TokenType.Number)
            );

            Assert.That(ex.IsPrematureStreamTermination, Is.False);
        }

        [Test]
        public void CheckTokenTypeWithMultipleOptionsThrowsWhenTokenDoesNotMatch()
        {
            ScriptLoadingContext context = CreateContext("end");

            Assert.That(
                () => TestNode.CallCheckTokenType(context, TokenType.True, TokenType.False),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void CheckTokenTypeWithThreeOptionsThrowsWhenTokenDoesNotMatch()
        {
            ScriptLoadingContext context = CreateContext("do");

            Assert.That(
                () =>
                    TestNode.CallCheckTokenType(
                        context,
                        TokenType.If,
                        TokenType.Else,
                        TokenType.ElseIf
                    ),
                Throws.TypeOf<SyntaxErrorException>()
            );
        }

        [Test]
        public void LoadingContextPropertyCanBeQueriedByDerivedType()
        {
            ScriptLoadingContext context = CreateContext("return 1");
            TestNode node = new(context);

            Assert.That(node.GetLoadingContextViaHelper(), Is.SameAs(context));
        }

        [Test]
        public void CheckMatchConsumesClosingToken()
        {
            ScriptLoadingContext context = CreateContext("()");
            Token opening = context.Lexer.Current;
            TestNode.CallCheckTokenType(context, TokenType.BrkOpenRound);

            Token closing = TestNode.CallCheckMatch(context, opening, TokenType.BrkCloseRound, ")");

            Assert.Multiple(() =>
            {
                Assert.That(closing.Type, Is.EqualTo(TokenType.BrkCloseRound));
                Assert.That(context.Lexer.Current.Type, Is.EqualTo(TokenType.Eof));
            });
        }

        [Test]
        public void CheckMatchThrowsForMismatchedClosingToken()
        {
            ScriptLoadingContext context = CreateContext("(");
            Token opening = context.Lexer.Current;
            TestNode.CallCheckTokenType(context, TokenType.BrkOpenRound);

            SyntaxErrorException ex = Assert.Throws<SyntaxErrorException>(() =>
                TestNode.CallCheckMatch(context, opening, TokenType.BrkCloseRound, ")")
            );
            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Does.Contain("')' expected"));
                Assert.That(ex.IsPrematureStreamTermination, Is.True);
            });
        }

        private static ScriptLoadingContext CreateContext(string code)
        {
            Script script = new();
            ScriptLoadingContext context = new(script)
            {
                Lexer = new Lexer(0, code, autoSkipComments: true),
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
