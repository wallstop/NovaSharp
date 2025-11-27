namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Tests.Utilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HelpCommandTests : IDisposable
    {
        private ConsoleCaptureScope _consoleScope;

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
            _consoleScope = new ConsoleCaptureScope(captureError: false);
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        [Test]
        public void ExecuteWithoutArgumentsListsRegisteredCommands()
        {
            HelpCommand command = new();

            ShellContext context = new(CreateScript(LuaCompatibilityVersion.Lua53));
            command.Execute(context, string.Empty);

            string output = _consoleScope.Writer.ToString();
            string expectedSummary = context.Script.CompatibilityProfile.GetFeatureSummary();
            Assert.Multiple(() =>
            {
                Assert.That(output, Does.Contain(CliMessages.HelpCommandCommandListHeading));
                Assert.That(output, Does.Contain($"{CliMessages.HelpCommandCommandPrefix}help"));
                Assert.That(output, Does.Contain($"{CliMessages.HelpCommandCommandPrefix}run"));
                Assert.That(
                    output,
                    Does.Contain(CliMessages.ProgramActiveProfile(expectedSummary))
                );
                Assert.That(output, Does.Contain(CliMessages.HelpCommandCompatibilitySummary));
            });
        }

        [Test]
        public void ExecuteWithKnownCommandWritesLongHelp()
        {
            HelpCommand command = new();

            command.Execute(new ShellContext(CreateScript(LuaCompatibilityVersion.Lua54)), "run");

            Assert.That(
                _consoleScope.Writer.ToString(),
                Does.Contain(CliMessages.RunCommandLongHelp)
            );
        }

        [Test]
        public void ExecuteWithUnknownCommandPrintsError()
        {
            HelpCommand command = new();

            command.Execute(
                new ShellContext(CreateScript(LuaCompatibilityVersion.Lua52)),
                "garbage"
            );

            Assert.That(
                _consoleScope.Writer.ToString(),
                Does.Contain(CliMessages.HelpCommandCommandNotFound("garbage"))
            );
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };

            return new Script(options);
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
