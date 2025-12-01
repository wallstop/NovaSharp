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
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: true);
                    ShellContext context = new(new Script());

                    CommandManager.Execute(context, "help");

                    string output = consoleScope.Writer.ToString();
                    await Assert
                        .That(output)
                        .Contains(CliMessages.HelpCommandPrimaryInstruction)
                        .ConfigureAwait(false);
                    await Assert
                        .That(output)
                        .Contains(CliMessages.HelpCommandCommandListHeading)
                        .ConfigureAwait(false);
                    await Assert
                        .That(output)
                        .Contains(
                            $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.HelpCommandShortHelp}"
                        )
                        .ConfigureAwait(false);
                    await Assert
                        .That(output)
                        .Contains(
                            $"{CliMessages.HelpCommandCommandPrefix}{CliMessages.RunCommandShortHelp}"
                        )
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HelpTrimsWhitespaceAroundArguments()
        {
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: true);
                    ShellContext context = new(new Script());

                    CommandManager.Execute(context, "   help   run   ");

                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(CliMessages.RunCommandLongHelp)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnknownCommandReportsAnError()
        {
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: true);
                    ShellContext context = new(new Script());

                    CommandManager.Execute(context, "nope");

                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(CliMessages.CommandManagerInvalidCommand("nope"))
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NullContextThrowsArgumentNullException()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                CommandManager.Execute(null, "help");
            });

            await Assert.That(exception.ParamName).IsEqualTo("context").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NullCommandLineThrowsArgumentNullException()
        {
            ShellContext context = new(new Script());

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                CommandManager.Execute(context, null);
            });

            await Assert.That(exception.ParamName).IsEqualTo("commandLine").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task EmptyCommandLineReportsInvalidCommand()
        {
            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: true);
                    ShellContext context = new(new Script());

                    CommandManager.Execute(context, "     ");

                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(CliMessages.CommandManagerEmptyCommand)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }
    }
}
