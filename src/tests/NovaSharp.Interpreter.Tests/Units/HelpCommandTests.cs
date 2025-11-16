namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HelpCommandTests
    {
        private TextWriter _originalOut = null!;
        private StringWriter _writer = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

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
        public void ExecuteWithoutArgumentsListsRegisteredCommands()
        {
            HelpCommand command = new();

            command.Execute(new ShellContext(new Interpreter.Script()), string.Empty);

            string output = _writer.ToString();
            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Commands:"));
                Assert.That(output, Does.Contain("!help"));
                Assert.That(output, Does.Contain("!run"));
            });
        }

        [Test]
        public void ExecuteWithKnownCommandWritesLongHelp()
        {
            HelpCommand command = new();

            command.Execute(new ShellContext(new Interpreter.Script()), "run");

            Assert.That(
                _writer.ToString(),
                Does.Contain("run <filename> - Executes the specified Lua script.")
            );
        }

        [Test]
        public void ExecuteWithUnknownCommandPrintsError()
        {
            HelpCommand command = new();

            command.Execute(new ShellContext(new Interpreter.Script()), "garbage");

            Assert.That(_writer.ToString(), Does.Contain("Command 'garbage' not found."));
        }
    }
}
