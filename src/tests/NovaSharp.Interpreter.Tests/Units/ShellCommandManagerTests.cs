namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ShellCommandManagerTests : IDisposable
    {
        private ConsoleCaptureScope _consoleScope = null!;
        private ShellContext _context = null!;

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
            _context = new ShellContext(new Script());

            _consoleScope = new ConsoleCaptureScope(captureError: true);
        }

        [TearDown]
        public void TearDown()
        {
            _consoleScope.Dispose();
        }

        public void Dispose()
        {
            _consoleScope?.Dispose();
        }

        [Test]
        public void HelpWithoutArgumentsListsCommands()
        {
            string output = Execute("help");

            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain("Type Lua code to execute Lua code"));
                Assert.That(output, Does.Contain("Commands:"));
                Assert.That(
                    output,
                    Does.Contain("!help [command] - gets the list of possible commands")
                );
                Assert.That(
                    output,
                    Does.Contain("!run <filename> - Executes the specified Lua script")
                );
            });
        }

        [Test]
        public void HelpTrimsWhitespaceAroundArguments()
        {
            string output = Execute("   help   run   ");

            Assert.That(
                output,
                Does.Contain("run <filename> - Executes the specified Lua script.")
            );
        }

        [Test]
        public void UnknownCommandReportsAnError()
        {
            string output = Execute("nope");

            Assert.That(output, Does.Contain("Invalid command 'nope'."));
        }

        [Test]
        public void NullContextThrowsArgumentNullException()
        {
            Assert.That(
                () => CommandManager.Execute(null!, "help"),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("context")
            );
        }

        [Test]
        public void NullCommandLineThrowsArgumentNullException()
        {
            Assert.That(
                () => CommandManager.Execute(_context, null!),
                Throws
                    .ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName))
                    .EqualTo("commandLine")
            );
        }

        [Test]
        public void EmptyCommandLineReportsInvalidCommand()
        {
            string output = Execute("     ");

            Assert.That(output, Does.Contain("Invalid command ''."));
        }

        private string Execute(string commandLine)
        {
            CommandManager.Execute(_context, commandLine);
            return _consoleScope.Writer.ToString();
        }
    }
}
