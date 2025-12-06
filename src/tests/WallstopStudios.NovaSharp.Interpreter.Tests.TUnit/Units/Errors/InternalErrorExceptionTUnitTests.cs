namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Errors
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    public sealed class InternalErrorExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParameterlessConstructorUsesDefaultMessage()
        {
            InternalErrorException exception = new();
            await Assert.That(exception.Message).IsEqualTo("Internal error").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(null)]
        [global::TUnit.Core.Arguments("")]
        [global::TUnit.Core.Arguments("   ")]
        public async Task MessageConstructorNormalizesWhitespace(string message)
        {
            InternalErrorException exception = new(message);
            await Assert.That(exception.Message).IsEqualTo("Internal error").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MessageConstructorKeepsProvidedText()
        {
            InternalErrorException exception = new("Failure detected");
            await Assert
                .That(exception.Message)
                .IsEqualTo("Failure detected")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorBuildsMessageFromArguments()
        {
            InternalErrorException exception = new InternalErrorException(
                "Value {0} is invalid",
                10
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("Value 10 is invalid")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorDefaultsWhenFormatIsMissing()
        {
            InternalErrorException exception = new InternalErrorException(null, (object[])null);
            await Assert.That(exception.Message).IsEqualTo("Internal error").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorHandlesNullArgumentsArray()
        {
            InternalErrorException exception = new InternalErrorException(
                "Message without args",
                (object[])null
            );

            await Assert
                .That(exception.Message)
                .IsEqualTo("Message without args")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InnerExceptionConstructorPreservesInnerException()
        {
            Exception inner = new InvalidOperationException("boom");

            InternalErrorException exception = new("wrap", inner);

            await Assert
                .That(exception.InnerException)
                .IsSameReferenceAs(inner)
                .ConfigureAwait(false);
        }
    }
}
