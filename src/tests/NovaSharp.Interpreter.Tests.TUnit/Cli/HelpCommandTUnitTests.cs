namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    [PlatformDetectorIsolation]
    public sealed class HelpCommandTUnitTests
    {
        static HelpCommandTUnitTests()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsListsRegisteredCommands()
        {
            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                HelpCommand command = new();
                ShellContext context = new(CreateScript(LuaCompatibilityVersion.Lua53));

                command.Execute(context, string.Empty);

                string output = consoleScope.Writer.ToString();
                string expectedSummary = context.Script.CompatibilityProfile.GetFeatureSummary();
                await Assert.That(output).Contains(CliMessages.HelpCommandCommandListHeading);
                await Assert.That(output).Contains($"{CliMessages.HelpCommandCommandPrefix}help");
                await Assert.That(output).Contains($"{CliMessages.HelpCommandCommandPrefix}run");
                await Assert.That(output).Contains(expectedSummary);
                await Assert.That(output).Contains(CliMessages.HelpCommandCompatibilitySummary);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithKnownCommandWritesLongHelp()
        {
            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                HelpCommand command = new();

                command.Execute(
                    new ShellContext(CreateScript(LuaCompatibilityVersion.Lua54)),
                    "run"
                );

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.RunCommandLongHelp);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithUnknownCommandPrintsError()
        {
            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                HelpCommand command = new();

                command.Execute(
                    new ShellContext(CreateScript(LuaCompatibilityVersion.Lua52)),
                    "garbage"
                );

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.HelpCommandCommandNotFound("garbage"));
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };
            return new Script(options);
        }
    }
}
