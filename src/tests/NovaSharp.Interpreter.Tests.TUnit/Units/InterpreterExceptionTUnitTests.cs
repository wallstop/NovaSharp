#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;

    public sealed class InterpreterExceptionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DecorateMessageUsesSourceReferenceWhenAvailable()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/decorate");
            InterpreterException exception = new("boom");

            exception.DecorateMessage(script, location);

            await Assert
                .That(exception.DecoratedMessage)
                .IsEqualTo($"{location.FormatLocation(script)}: boom");
        }

        [global::TUnit.Core.Test]
        public async Task DecorateMessageFallsBackToInstructionPointer()
        {
            Script script = new();
            InterpreterException exception = new("boom");

            exception.DecorateMessage(script, null, ip: 42);

            await Assert.That(exception.DecoratedMessage).IsEqualTo("bytecode:42: boom");
        }

        [global::TUnit.Core.Test]
        public async Task DecorateMessageRespectsDoNotDecorateFlag()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/do not decorate");
            InterpreterException exception = new("boom") { DoNotDecorateMessage = true };

            exception.DecorateMessage(script, location);

            await Assert.That(exception.DecoratedMessage).IsEqualTo("boom");
        }

        [global::TUnit.Core.Test]
        public async Task DecorateMessageDoesNotOverrideExistingDecoration()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/custom");
            InterpreterException exception = new("boom") { DecoratedMessage = "custom" };

            exception.DecorateMessage(script, location);

            await Assert.That(exception.DecoratedMessage).IsEqualTo("custom");
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorThrowsWhenFormatIsNull()
        {
            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
            {
                _ = new TestInterpreterException(format: null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("format");
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorAppliesInvariantFormatting()
        {
            TestInterpreterException exception = new("value {0}", 1.5);
            await Assert.That(exception.Message).IsEqualTo("value 1.5");
        }

        [global::TUnit.Core.Test]
        public async Task FormatConstructorReturnsLiteralWhenNoArgumentsProvided()
        {
            TestInterpreterException exception = new("literal");
            await Assert.That(exception.Message).IsEqualTo("literal");
        }

        [global::TUnit.Core.Test]
        public async Task WrappingConstructorThrowsWhenInnerExceptionNull()
        {
            ArgumentNullException noMessage = ExpectException<ArgumentNullException>(() =>
                TestInterpreterException.Wrap(null)
            );
            await Assert.That(noMessage.ParamName).IsEqualTo("ex");

            ArgumentNullException withMessage = ExpectException<ArgumentNullException>(() =>
                TestInterpreterException.Wrap(null, "override")
            );
            await Assert.That(withMessage.ParamName).IsEqualTo("ex");
        }

        [global::TUnit.Core.Test]
        public async Task WrappingConstructorUsesInnerExceptionDetails()
        {
            InvalidOperationException inner = new("inner");

            TestInterpreterException wrapped = TestInterpreterException.Wrap(inner);

            await Assert.That(wrapped.InnerException).IsSameReferenceAs(inner);
            await Assert.That(wrapped.Message).IsEqualTo("inner");
        }

        [global::TUnit.Core.Test]
        public async Task WrappingConstructorOverridesMessageWhenProvided()
        {
            InvalidOperationException inner = new("inner");

            TestInterpreterException wrapped = TestInterpreterException.Wrap(inner, "override");

            await Assert.That(wrapped.InnerException).IsSameReferenceAs(inner);
            await Assert.That(wrapped.Message).IsEqualTo("override");
        }

        [global::TUnit.Core.Test]
        public async Task AppendCompatibilityContextAppendsProfileOnce()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/profile");
            InterpreterException exception = new("boom");
            exception.DecorateMessage(script, location);

            string profile = script.CompatibilityProfile.DisplayName;

            exception.AppendCompatibilityContext(script);

            await Assert
                .That(exception.DecoratedMessage)
                .EndsWith($"[compatibility: {profile}]");

            string firstAppend = exception.DecoratedMessage ?? string.Empty;

            exception.AppendCompatibilityContext(script);

            await Assert.That(exception.DecoratedMessage).IsEqualTo(firstAppend);
        }

        [global::TUnit.Core.Test]
        public async Task AppendCompatibilityContextSkipsWhenMessageMissingOrScriptNull()
        {
            InterpreterException exception = new("boom");

            exception.AppendCompatibilityContext(null);
            await Assert.That(exception.DecoratedMessage).IsNull();

            Script script = new();
            exception.AppendCompatibilityContext(script);
            await Assert.That(exception.DecoratedMessage).IsNull();

            exception.DecoratedMessage = "ready";
            exception.AppendCompatibilityContext(script);
            await Assert.That(exception.DecoratedMessage).Contains("compatibility");
        }

        private static SourceRef CreateSourceRef(Script script, string chunkName)
        {
            script.DoString("return 1", null, chunkName);
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);
            string line = source.Lines[1];
            return new SourceRef(source.SourceId, 0, line.Length - 1, 1, 1, isStepStop: false);
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }

        private sealed class TestInterpreterException : InterpreterException
        {
            public TestInterpreterException() { }

            public TestInterpreterException(string message)
                : base(message) { }

            public TestInterpreterException(string message, Exception innerException)
                : base(message, innerException) { }

            public TestInterpreterException(string format, params object[] args)
                : base(format, args) { }

            private TestInterpreterException(Exception ex, string message, bool useMessage)
                : base(ex, message) { }

            private TestInterpreterException(Exception ex)
                : base(ex) { }

            public static TestInterpreterException Wrap(Exception ex, string message = null)
            {
                if (message == null)
                {
                    return new TestInterpreterException(ex);
                }

                return new TestInterpreterException(ex, message, useMessage: true);
            }
        }
    }
}
#pragma warning restore CA2007
