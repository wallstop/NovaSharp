namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using Commands.Implementations;
    using NovaSharp.Interpreter;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ExitCommandTests
    {
        private TextWriter _originalOut = null!;
        private StringWriter _writer = null!;

        [SetUp]
        public void SetUp()
        {
            _writer = new StringWriter();
            _originalOut = Console.Out;
            Console.SetOut(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            _writer.Dispose();
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

            Assert.That(_writer.ToString(), Does.Contain("exit - Exits the interpreter"));
        }

        [Test]
        public void DisplayLongHelpWritesDescription()
        {
            ExitCommand command = new();

            command.DisplayLongHelp();

            Assert.That(_writer.ToString(), Does.Contain("exit - Exits the interpreter"));
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

            Assert.That(() => command.Execute(null!, string.Empty), Throws.ArgumentNullException);
        }
    }
}
