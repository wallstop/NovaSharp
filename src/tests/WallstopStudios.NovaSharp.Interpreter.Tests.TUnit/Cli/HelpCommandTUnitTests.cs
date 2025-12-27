namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Cli.Commands;
    using WallstopStudios.NovaSharp.Cli.Commands.Implementations;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

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
        [AllLuaVersions]
        public async Task ExecuteWithoutArgumentsListsRegisteredCommands(
            LuaCompatibilityVersion version
        )
        {
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    async consoleScope =>
                    {
                        HelpCommand command = new();
                        ShellContext context = new(CreateScript(version));

                        command.Execute(context, string.Empty);

                        string output = consoleScope.Writer.ToString();
                        string expectedSummary =
                            context.Script.CompatibilityProfile.GetFeatureSummary();
                        await Assert
                            .That(output)
                            .Contains(CliMessages.HelpCommandCommandListHeading)
                            .ConfigureAwait(false);
                        await Assert
                            .That(output)
                            .Contains($"{CliMessages.HelpCommandCommandPrefix}help")
                            .ConfigureAwait(false);
                        await Assert
                            .That(output)
                            .Contains($"{CliMessages.HelpCommandCommandPrefix}run")
                            .ConfigureAwait(false);
                        await Assert.That(output).Contains(expectedSummary).ConfigureAwait(false);
                        await Assert
                            .That(output)
                            .Contains(CliMessages.HelpCommandCompatibilitySummary)
                            .ConfigureAwait(false);
                    },
                    captureError: false
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ExecuteWithKnownCommandWritesLongHelp(LuaCompatibilityVersion version)
        {
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    async consoleScope =>
                    {
                        HelpCommand command = new();

                        command.Execute(new ShellContext(CreateScript(version)), "run");

                        await Assert
                            .That(consoleScope.Writer.ToString())
                            .Contains(CliMessages.RunCommandLongHelp)
                            .ConfigureAwait(false);
                    },
                    captureError: false
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ExecuteWithUnknownCommandPrintsError(LuaCompatibilityVersion version)
        {
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    async consoleScope =>
                    {
                        HelpCommand command = new();

                        command.Execute(new ShellContext(CreateScript(version)), "garbage");

                        await Assert
                            .That(consoleScope.Writer.ToString())
                            .Contains(CliMessages.HelpCommandCommandNotFound("garbage"))
                            .ConfigureAwait(false);
                    },
                    captureError: false
                )
                .ConfigureAwait(false);
        }
    }
}
