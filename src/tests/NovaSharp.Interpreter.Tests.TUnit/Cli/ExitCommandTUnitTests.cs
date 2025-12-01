#pragma warning disable CA2007

namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;

    [PlatformDetectorIsolation]
    public sealed class ExitCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task NameReturnsExit()
        {
            ExitCommand command = new();

            await Assert.That(command.Name).IsEqualTo("exit");
        }

        [global::TUnit.Core.Test]
        public async Task DisplayShortHelpWritesDescription()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                ExitCommand command = new();

                command.DisplayShortHelp();

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.ExitCommandShortHelp);
            });
        }

        [global::TUnit.Core.Test]
        public async Task DisplayLongHelpWritesDescription()
        {
            await ConsoleCaptureCoordinator.RunAsync(async () =>
            {
                using ConsoleCaptureScope consoleScope = new(captureError: false);
                ExitCommand command = new();

                command.DisplayLongHelp();

                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.ExitCommandLongHelp);
            });
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteMarksContextForExit()
        {
            ExitCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, string.Empty);

            await Assert.That(context.IsExitRequested).IsTrue();
            await Assert.That(context.ExitCode).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteThrowsWhenContextIsNull()
        {
            ExitCommand command = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                command.Execute(null, string.Empty);
            });

            await Assert.That(exception.ParamName).IsEqualTo("context");
        }
    }
}

#pragma warning restore CA2007
