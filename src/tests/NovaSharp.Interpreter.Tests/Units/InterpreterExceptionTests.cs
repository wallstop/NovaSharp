namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InterpreterExceptionTests
    {
        [Test]
        public void DecorateMessageUsesSourceReferenceWhenAvailable()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/decorate");
            InterpreterException exception = new("boom");

            exception.DecorateMessage(script, location);

            Assert.That(
                exception.DecoratedMessage,
                Is.EqualTo($"{location.FormatLocation(script)}: boom")
            );
        }

        [Test]
        public void DecorateMessageFallsBackToInstructionPointer()
        {
            Script script = new();
            InterpreterException exception = new("boom");

            exception.DecorateMessage(script, null, ip: 42);

            Assert.That(exception.DecoratedMessage, Is.EqualTo("bytecode:42: boom"));
        }

        [Test]
        public void DecorateMessageRespectsDoNotDecorateFlag()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/do-not");
            InterpreterException exception = new("boom") { DoNotDecorateMessage = true };

            exception.DecorateMessage(script, location);

            Assert.That(exception.DecoratedMessage, Is.EqualTo("boom"));
        }

        [Test]
        public void DecorateMessageDoesNotOverrideExistingDecoration()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/custom");
            InterpreterException exception = new("boom") { DecoratedMessage = "custom" };

            exception.DecorateMessage(script, location);

            Assert.That(exception.DecoratedMessage, Is.EqualTo("custom"));
        }

        [Test]
        public void FormatConstructorThrowsWhenFormatIsNull()
        {
            Assert.That(
                () => new TestInterpreterException(format: null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("format")
            );
        }

        [Test]
        public void FormatConstructorAppliesInvariantFormatting()
        {
            TestInterpreterException exception = new("value {0}", 1.5);

            Assert.That(exception.Message, Is.EqualTo("value 1.5"));
        }

        [Test]
        public void FormatConstructorReturnsLiteralWhenNoArgumentsProvided()
        {
            TestInterpreterException exception = new("literal");

            Assert.That(exception.Message, Is.EqualTo("literal"));
        }

        [Test]
        public void WrappingConstructorThrowsWhenInnerExceptionNull()
        {
            Assert.That(
                () => TestInterpreterException.Wrap(null),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("ex")
            );

            Assert.That(
                () => TestInterpreterException.Wrap(null, "override"),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("ex")
            );
        }

        [Test]
        public void WrappingConstructorUsesInnerExceptionDetails()
        {
            InvalidOperationException inner = new("inner");

            TestInterpreterException wrapped = TestInterpreterException.Wrap(inner);

            Assert.Multiple(() =>
            {
                Assert.That(wrapped.InnerException, Is.SameAs(inner));
                Assert.That(wrapped.Message, Is.EqualTo("inner"));
            });
        }

        [Test]
        public void WrappingConstructorOverridesMessageWhenProvided()
        {
            InvalidOperationException inner = new("inner");

            TestInterpreterException wrapped = TestInterpreterException.Wrap(inner, "override");

            Assert.Multiple(() =>
            {
                Assert.That(wrapped.InnerException, Is.SameAs(inner));
                Assert.That(wrapped.Message, Is.EqualTo("override"));
            });
        }

        [Test]
        public void AppendCompatibilityContextAppendsProfileOnce()
        {
            Script script = new();
            SourceRef location = CreateSourceRef(script, "units/interpreter/profile");
            InterpreterException exception = new("boom");
            exception.DecorateMessage(script, location);

            string profile = script.CompatibilityProfile.DisplayName;

            exception.AppendCompatibilityContext(script);

            Assert.That(exception.DecoratedMessage, Does.EndWith($"[compatibility: {profile}]"));

            string firstAppend = exception.DecoratedMessage;

            exception.AppendCompatibilityContext(script);

            Assert.That(exception.DecoratedMessage, Is.EqualTo(firstAppend));
        }

        [Test]
        public void AppendCompatibilityContextSkipsWhenMessageMissingOrScriptNull()
        {
            InterpreterException exception = new("boom");

            exception.AppendCompatibilityContext(null);
            Assert.That(exception.DecoratedMessage, Is.Null);

            Script script = new();
            exception.AppendCompatibilityContext(script);
            Assert.That(exception.DecoratedMessage, Is.Null);

            exception.DecoratedMessage = "ready";
            exception.AppendCompatibilityContext(script);
            Assert.That(exception.DecoratedMessage, Does.Contain("compatibility"));
        }

        private static SourceRef CreateSourceRef(Script script, string chunkName)
        {
            script.DoString("return 1", null, chunkName);
            SourceCode source = script.GetSourceCode(script.SourceCodeCount - 1);
            string line = source.Lines[1];
            return new SourceRef(source.SourceId, 0, line.Length - 1, 1, 1, isStepStop: false);
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
