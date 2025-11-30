#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Tree.Lexer;

    [ScriptGlobalOptionsIsolation]
    public sealed class SyntaxErrorExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RethrowWrapsExceptionWhenGlobalOptionEnabled()
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

                await Assert.That(nested).IsNotSameReferenceAs(captured);
                await Assert.That(nested.DecoratedMessage).IsEqualTo(captured.DecoratedMessage);
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task RethrowDoesNothingWhenGlobalOptionDisabled()
        {
            bool original = Script.GlobalOptions.RethrowExceptionNested;
            try
            {
                Script.GlobalOptions.RethrowExceptionNested = false;
                SyntaxErrorException exception = new();

                exception.Rethrow();
                await Task.CompletedTask;
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task RethrowClonesTokenMetadata()
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

                await Assert.That(nested).IsNotSameReferenceAs(exception);
                await Assert.That(nested.Token).IsSameReferenceAs(token);
                await Assert.That(nested.InnerException).IsSameReferenceAs(exception);
            }
            finally
            {
                Script.GlobalOptions.RethrowExceptionNested = original;
            }
        }

        [global::TUnit.Core.Test]
        public async Task TokenConstructorDecoratesMessage()
        {
            Script script = CreateScriptWithNamedSource("decorated", out int sourceId);
            Token token = CreateToken(sourceId);
            SyntaxErrorException exception = new(token, "unexpected '{0}'", token.Text);

            exception.DecorateMessage(script);

            await Assert.That(exception.Token).IsSameReferenceAs(token);
            await Assert.That(exception.DecoratedMessage).StartsWith("decorated:");
        }

        [global::TUnit.Core.Test]
        public async Task SourceRefConstructorsDecorateMessage()
        {
            Script script = CreateScriptWithNamedSource("chunk", out int sourceId);
            SourceRef location = new(sourceId, 1, 2, 1, 3, false);

            SyntaxErrorException formatted = new(script, location, "issue {0}", 123);
            SyntaxErrorException plain = new(script, location, "plain issue");

            await Assert.That(formatted.DecoratedMessage).StartsWith("chunk:");
            await Assert.That(plain.DecoratedMessage).StartsWith("chunk:");
        }

        [global::TUnit.Core.Test]
        public async Task PrematureStreamTerminationFlagRoundTrips()
        {
            SyntaxErrorException exception = new() { IsPrematureStreamTermination = true };

            await Assert.That(exception.IsPrematureStreamTermination).IsTrue();
            exception.IsPrematureStreamTermination = false;
            await Assert.That(exception.IsPrematureStreamTermination).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionExceptionPrefixesMessages()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            DynamicExpressionException plain = new("plain text");
            Exception inner = new InvalidOperationException("boom");
            DynamicExpressionException nested = new("inner", inner);

            await Assert.That(formatted.Message).IsEqualTo("<dynamic>: value 42");
            await Assert.That(plain.Message).IsEqualTo("<dynamic>: plain text");
            await Assert.That(nested.Message).IsEqualTo("<dynamic>: inner");
            await Assert.That(nested.InnerException).IsSameReferenceAs(inner);
            await Assert
                .That(new DynamicExpressionException().Message)
                .IsEqualTo("<dynamic>: dynamic expression error");
        }

        [global::TUnit.Core.Test]
        public async Task MessageOnlyConstructorStoresMessage()
        {
            SyntaxErrorException exception = new("parse error");
            await Assert.That(exception.Message).IsEqualTo("parse error");
        }

        [global::TUnit.Core.Test]
        public async Task MessageAndInnerConstructorPreservesInnerException()
        {
            InvalidOperationException inner = new("inner");

            SyntaxErrorException exception = new("outer", inner);

            await Assert.That(exception.Message).IsEqualTo("outer");
            await Assert.That(exception.InnerException).IsSameReferenceAs(inner);
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
            return new Token(TokenType.Name, sourceId, 1, 1, 1, 3, 1, 0) { Text = "value" };
        }
    }
}
#pragma warning restore CA2007
