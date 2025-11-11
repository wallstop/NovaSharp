namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
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
    }
}
