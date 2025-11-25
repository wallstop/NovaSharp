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
                Assert.That(output, Does.Contain(CliMessages.HelpCommandPrimaryInstruction));
                Assert.That(output, Does.Contain(CliMessages.HelpCommandCommandListHeading));
                Assert.That(
                    output,
                    Does.Contain(
                        $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.HelpCommandShortHelp}"
                    )
                );
                Assert.That(
                    output,
                    Does.Contain(
                        $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.RunCommandShortHelp}"
                    )
                );
            });
        }

        [Test]
        public void HelpTrimsWhitespaceAroundArguments()
        {
            string output = Execute("   help   run   ");

            Assert.That(output, Does.Contain(CliMessages.RunCommandLongHelp));
        }

        [Test]
        public void UnknownCommandReportsAnError()
        {
            string output = Execute("nope");

            Assert.That(output, Does.Contain(CliMessages.CommandManagerInvalidCommand("nope")));
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

            Assert.That(output, Does.Contain(CliMessages.CommandManagerEmptyCommand));
        }

        private string Execute(string commandLine)
        {
            CommandManager.Execute(_context, commandLine);
            return _consoleScope.Writer.ToString();
        }
    }
}
