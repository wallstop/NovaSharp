namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InternalErrorExceptionTests
    {
        [Test]
        public void ParameterlessConstructorUsesDefaultMessage()
        {
            InternalErrorException exception = new();

            Assert.That(exception.Message, Is.EqualTo("Internal error"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void MessageConstructorNormalizesWhitespace(string message)
        {
            InternalErrorException exception = new(message);

            Assert.That(exception.Message, Is.EqualTo("Internal error"));
        }

        [Test]
        public void MessageConstructorKeepsProvidedText()
        {
            InternalErrorException exception = new("Failure detected");

            Assert.That(exception.Message, Is.EqualTo("Failure detected"));
        }

        [Test]
        public void FormatConstructorBuildsMessageFromArguments()
        {
            InternalErrorException exception = new InternalErrorException(
                "Value {0} is invalid",
                10
            );

            Assert.That(exception.Message, Is.EqualTo("Value 10 is invalid"));
        }

        [Test]
        public void FormatConstructorDefaultsWhenFormatIsMissing()
        {
            InternalErrorException exception = new InternalErrorException(null, (object[])null);

            Assert.That(exception.Message, Is.EqualTo("Internal error"));
        }

        [Test]
        public void FormatConstructorHandlesNullArgumentsArray()
        {
            InternalErrorException exception = new InternalErrorException(
                "Message without args",
                (object[])null
            );

            Assert.That(exception.Message, Is.EqualTo("Message without args"));
        }

        [Test]
        public void InnerExceptionConstructorPreservesInnerException()
        {
            Exception inner = new InvalidOperationException("boom");

            InternalErrorException exception = new("wrap", inner);

            Assert.That(exception.InnerException, Is.SameAs(inner));
        }
    }
}
