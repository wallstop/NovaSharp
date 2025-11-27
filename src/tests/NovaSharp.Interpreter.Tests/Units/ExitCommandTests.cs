namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ExitCommandTests : IDisposable
    {
        private ConsoleCaptureScope _consoleScope;

        [SetUp]
        public void SetUp()
        {
            _consoleScope = new ConsoleCaptureScope(captureError: false);
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        [Test]
        public void NameReturnsExit()
        {
            ExitCommand command = new();

            Assert.That(command.Name, Is.EqualTo("exit"));
        }

        [Test]
        public void DisplayShortHelpWritesDescription()
        {
            ExitCommand command = new();

            command.DisplayShortHelp();

            Assert.That(
                _consoleScope.Writer.ToString(),
                Does.Contain(CliMessages.ExitCommandShortHelp)
            );
        }

        [Test]
        public void DisplayLongHelpWritesDescription()
        {
            ExitCommand command = new();

            command.DisplayLongHelp();

            Assert.That(
                _consoleScope.Writer.ToString(),
                Does.Contain(CliMessages.ExitCommandLongHelp)
            );
        }

        [Test]
        public void ExecuteMarksContextForExit()
        {
            ExitCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(context.IsExitRequested, Is.True);
                Assert.That(context.ExitCode, Is.EqualTo(0));
            });
        }

        [Test]
        public void ExecuteThrowsWhenContextIsNull()
        {
            ExitCommand command = new();

            Assert.That(() => command.Execute(null, string.Empty), Throws.ArgumentNullException);
        }

        public void Dispose()
        {
            if (_consoleScope != null)
            {
                _consoleScope.Dispose();
                _consoleScope = null;
            }
        }
    }
}
