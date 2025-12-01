#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    [PlatformDetectorIsolation]
    public sealed class ShellCommandManagerTUnitTests
    {
        static ShellCommandManagerTUnitTests()
        {
            if (CommandManager.Find("help") == null)
            {
                CommandManager.Initialize();
            }
        }

        [global::TUnit.Core.Test]
        public async Task HelpWithoutArgumentsListsCommands()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: true);
                ShellContext context = new(new Script());

                CommandManager.Execute(context, "help");

                string output = consoleScope.Writer.ToString();
                await Assert.That(output).Contains(CliMessages.HelpCommandPrimaryInstruction);
                await Assert.That(output).Contains(CliMessages.HelpCommandCommandListHeading);
                await Assert
                    .That(output)
                    .Contains(
                        $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.HelpCommandShortHelp}"
                    );
                await Assert
                    .That(output)
                    .Contains(
                        $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.RunCommandShortHelp}"
                    );
            });
        }

        [global::TUnit.Core.Test]
        public async Task HelpTrimsWhitespaceAroundArguments()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: true);
                ShellContext context = new(new Script());

                CommandManager.Execute(context, "   help   run   ");

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.RunCommandLongHelp);
            });
        }

        [global::TUnit.Core.Test]
        public async Task UnknownCommandReportsAnError()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: true);
                ShellContext context = new(new Script());

                CommandManager.Execute(context, "nope");

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.CommandManagerInvalidCommand("nope"));
            });
        }

        [global::TUnit.Core.Test]
        public async Task NullContextThrowsArgumentNullException()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                CommandManager.Execute(null, "help");
            });

            await Assert.That(exception.ParamName).IsEqualTo("context");
        }

        [global::TUnit.Core.Test]
        public async Task NullCommandLineThrowsArgumentNullException()
        {
            ShellContext context = new(new Script());

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                CommandManager.Execute(context, null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("commandLine");
        }

        [global::TUnit.Core.Test]
        public async Task EmptyCommandLineReportsInvalidCommand()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: true);
                ShellContext context = new(new Script());

                CommandManager.Execute(context, "     ");

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.CommandManagerEmptyCommand);
            });
        }
    }
}

#pragma warning restore CA2007
