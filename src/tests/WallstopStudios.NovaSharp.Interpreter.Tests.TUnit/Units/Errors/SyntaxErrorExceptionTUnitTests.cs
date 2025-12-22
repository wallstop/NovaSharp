namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Errors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    public sealed class SyntaxErrorExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RethrowWrapsExceptionWhenGlobalOptionEnabled(
            LuaCompatibilityVersion version
        )
        {
            using ScriptGlobalOptionsScope globalScope = ScriptGlobalOptionsScope.Override(
                options => options.RethrowExceptionNested = true
            );
            Script script = new(version);

            SyntaxErrorException captured = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("function broken(")
            )!;

            SyntaxErrorException nested = Assert.Throws<SyntaxErrorException>(() =>
                captured.Rethrow()
            )!;

            await Assert.That(nested).IsNotSameReferenceAs(captured).ConfigureAwait(false);
            await Assert
                .That(nested.DecoratedMessage)
                .IsEqualTo(captured.DecoratedMessage)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RethrowDoesNothingWhenGlobalOptionDisabled()
        {
            using ScriptGlobalOptionsScope globalScope = ScriptGlobalOptionsScope.Override(
                options => options.RethrowExceptionNested = false
            );
            SyntaxErrorException exception = new();

            exception.Rethrow();
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RethrowClonesTokenMetadata()
        {
            using ScriptGlobalOptionsScope globalScope = ScriptGlobalOptionsScope.Override(
                options => options.RethrowExceptionNested = true
            );
            Token token = CreateToken();
            SyntaxErrorException exception = new(token, "unexpected token");

            SyntaxErrorException nested = Assert.Throws<SyntaxErrorException>(() =>
                exception.Rethrow()
            )!;

            await Assert.That(nested).IsNotSameReferenceAs(exception).ConfigureAwait(false);
            await Assert.That(nested.Token).IsEqualTo(token).ConfigureAwait(false);
            await Assert
                .That(nested.InnerException)
                .IsSameReferenceAs(exception)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TokenConstructorDecoratesMessage()
        {
            Script script = CreateScriptWithNamedSource("decorated", out int sourceId);
            Token token = CreateToken(sourceId);
            SyntaxErrorException exception = new(token, "unexpected '{0}'", token.text);

            exception.DecorateMessage(script);

            await Assert.That(exception.Token).IsEqualTo(token).ConfigureAwait(false);
            await Assert
                .That(exception.DecoratedMessage)
                .StartsWith("decorated:")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SourceRefConstructorsDecorateMessage()
        {
            Script script = CreateScriptWithNamedSource("chunk", out int sourceId);
            SourceRef location = new(sourceId, 1, 2, 1, 3, false);

            SyntaxErrorException formatted = new(script, location, "issue {0}", 123);
            SyntaxErrorException plain = new(script, location, "plain issue");

            await Assert
                .That(formatted.DecoratedMessage)
                .StartsWith("chunk:")
                .ConfigureAwait(false);
            await Assert.That(plain.DecoratedMessage).StartsWith("chunk:").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PrematureStreamTerminationFlagRoundTrips()
        {
            SyntaxErrorException exception = new() { IsPrematureStreamTermination = true };

            await Assert
                .That(exception.IsPrematureStreamTermination)
                .IsTrue()
                .ConfigureAwait(false);
            exception.IsPrematureStreamTermination = false;
            await Assert
                .That(exception.IsPrematureStreamTermination)
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicExpressionExceptionPrefixesMessages()
        {
            DynamicExpressionException formatted = new("value {0}", 42);
            DynamicExpressionException plain = new("plain text");
            Exception inner = new InvalidOperationException("boom");
            DynamicExpressionException nested = new("inner", inner);

            await Assert
                .That(formatted.Message)
                .IsEqualTo("<dynamic>: value 42")
                .ConfigureAwait(false);
            await Assert
                .That(plain.Message)
                .IsEqualTo("<dynamic>: plain text")
                .ConfigureAwait(false);
            await Assert.That(nested.Message).IsEqualTo("<dynamic>: inner").ConfigureAwait(false);
            await Assert.That(nested.InnerException).IsSameReferenceAs(inner).ConfigureAwait(false);
            await Assert
                .That(new DynamicExpressionException().Message)
                .IsEqualTo("<dynamic>: dynamic expression error")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MessageOnlyConstructorStoresMessage()
        {
            SyntaxErrorException exception = new("parse error");
            await Assert.That(exception.Message).IsEqualTo("parse error").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MessageAndInnerConstructorPreservesInnerException()
        {
            InvalidOperationException inner = new("inner");

            SyntaxErrorException exception = new("outer", inner);

            await Assert.That(exception.Message).IsEqualTo("outer").ConfigureAwait(false);
            await Assert
                .That(exception.InnerException)
                .IsSameReferenceAs(inner)
                .ConfigureAwait(false);
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
            return new Token(TokenType.Name, sourceId, 1, 1, 1, 3, 1, 0, "value");
        }
    }
}
