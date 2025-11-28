namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [ScriptGlobalOptionsIsolation]
    public sealed class SyntaxErrorExceptionTests
    {
        [Test]
        public void RethrowWrapsExceptionWhenGlobalOptionEnabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            try
            {
                Script.GlobalOptions.RethrowExceptionNested = true;
                Script script = new();

                SyntaxErrorException captured = Assert.Throws<SyntaxErrorException>(() =>
                    script.DoString("function broken(")
                )!;

                SyntaxErrorException nested = Assert.Throws<SyntaxErrorException>(() =>
                    captured.Rethrow()
                )!;

                Assert.Multiple(() =>
                {
                    Assert.That(nested, Is.Not.SameAs(captured));
                    Assert.That(nested.DecoratedMessage, Is.EqualTo(captured.DecoratedMessage));
                });
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [Test]
        public void RethrowDoesNothingWhenGlobalOptionDisabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            try
            {
                Script.GlobalOptions.RethrowExceptionNested = false;
                SyntaxErrorException exception = new();

                Assert.DoesNotThrow(() => exception.Rethrow());
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [Test]
        public void RethrowClonesTokenMetadata()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            try
            {
                Script.GlobalOptions.RethrowExceptionNested = true;
                Token token = CreateToken();
                SyntaxErrorException exception = new(token, "unexpected token");

                SyntaxErrorException nested = Assert.Throws<SyntaxErrorException>(() =>
                    exception.Rethrow()
                )!;

                Assert.Multiple(() =>
                {
                    Assert.That(nested, Is.Not.SameAs(exception));
                    Assert.That(nested.Token, Is.SameAs(token));
                    Assert.That(nested.InnerException, Is.SameAs(exception));
                });
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [Test]
        public void TokenConstructorDecoratesMessage()
        {
            Script script = CreateScriptWithNamedSource("decorated", out int sourceId);
            Token token = CreateToken(sourceId);
            SyntaxErrorException exception = new(token, "unexpected '{0}'", token.Text);

            exception.DecorateMessage(script);

            Assert.Multiple(() =>
            {
                Assert.That(exception.Token, Is.SameAs(token));
                Assert.That(exception.DecoratedMessage, Does.StartWith("decorated:"));
            });
        }

        [Test]
        public void SourceRefConstructorsDecorateMessage()
        {
            Script script = CreateScriptWithNamedSource("chunk", out int sourceId);
            SourceRef location = new(sourceId, 1, 2, 1, 3, false);

            SyntaxErrorException formatted = new(script, location, "issue {0}", 123);
            SyntaxErrorException plain = new(script, location, "plain issue");

            Assert.Multiple(() =>
            {
                Assert.That(formatted.DecoratedMessage, Does.StartWith("chunk:"));
                Assert.That(plain.DecoratedMessage, Does.StartWith("chunk:"));
            });
        }

        [Test]
        public void PrematureStreamTerminationFlagRoundTrips()
        {
            SyntaxErrorException exception = new() { IsPrematureStreamTermination = true };

            Assert.That(exception.IsPrematureStreamTermination, Is.True);

            exception.IsPrematureStreamTermination = false;
            Assert.That(exception.IsPrematureStreamTermination, Is.False);
        }

        [Test]
        public void DynamicExpressionExceptionPrefixesMessages()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            DynamicExpressionException plain = new("plain text");
            Exception inner = new InvalidOperationException("boom");
            DynamicExpressionException nested = new("inner", inner);

            Assert.Multiple(() =>
            {
                Assert.That(formatted.Message, Is.EqualTo("<dynamic>: value 42"));
                Assert.That(plain.Message, Is.EqualTo("<dynamic>: plain text"));
                Assert.That(nested.Message, Is.EqualTo("<dynamic>: inner"));
                Assert.That(nested.InnerException, Is.SameAs(inner));
                Assert.That(
                    new DynamicExpressionException().Message,
                    Is.EqualTo("<dynamic>: dynamic expression error")
                );
            });
        }

        [Test]
        public void MessageOnlyConstructorStoresMessage()
        {
            SyntaxErrorException exception = new("parse error");

            Assert.That(exception.Message, Is.EqualTo("parse error"));
        }

        [Test]
        public void MessageAndInnerConstructorPreservesInnerException()
        {
            InvalidOperationException inner = new("inner");

            SyntaxErrorException exception = new("outer", inner);

            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Is.EqualTo("outer"));
                Assert.That(exception.InnerException, Is.SameAs(inner));
            });
        }

        private static Script CreateScriptWithNamedSource(string chunkName)
        {
            return CreateScriptWithNamedSource(chunkName, out _);
        }

        private static Script CreateScriptWithNamedSource(string chunkName, out int sourceId)
        {
            Script script = new();
            script.Options.UseLuaErrorLocations = true;
            script.LoadString("return 1", null, chunkName);
            sourceId = script.SourceCodeCount - 1;
            return script;
        }

        private static Token CreateToken(int sourceId = 0)
        {
            Token token = new(TokenType.Name, sourceId, 1, 1, 1, 3, 1, 0) { Text = "value" };
            return token;
        }
    }
}
