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
    public sealed class HelpCommandTests
    {
        private ConsoleCaptureScope _consoleScope = null!;

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
            _consoleScope.Dispose();
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
                Assert.That(output, Does.Contain("Commands:"));
                Assert.That(output, Does.Contain("!help"));
                Assert.That(output, Does.Contain("!run"));
                Assert.That(
                    output,
                    Does.Contain($"Active compatibility profile: {expectedSummary}")
                );
            });
        }

        [Test]
        public void ExecuteWithKnownCommandWritesLongHelp()
        {
            HelpCommand command = new();

            command.Execute(new ShellContext(CreateScript(LuaCompatibilityVersion.Lua54)), "run");

            Assert.That(
                _consoleScope.Writer.ToString(),
                Does.Contain("run <filename> - Executes the specified Lua script.")
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
                Does.Contain("Command 'garbage' not found.")
            );
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };

            return new Script(options);
        }
    }
}
