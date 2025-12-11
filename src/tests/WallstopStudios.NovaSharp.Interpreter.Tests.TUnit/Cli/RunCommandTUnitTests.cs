namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Cli.Commands.Implementations;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

    public sealed class RunCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsShowsSyntax()
        {
            RunCommand command = new();
            ShellContext context = CreateShellContext();

            await WithConsoleAsync(async console =>
                {
                    command.Execute(context, string.Empty);

                    string output = console.Writer.ToString();
                    await Assert
                        .That(output)
                        .Contains(CliMessages.RunCommandSyntax)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteRunsScriptAndLogsCompatibilitySummary()
        {
            using TempFileScope scriptScope = TempFileScope.Create(
                namePrefix: "run_",
                extension: ".lua"
            );
            string scriptPath = scriptScope.FilePath;
            await File.WriteAllTextAsync(scriptPath, "result = 42").ConfigureAwait(false);

            RunCommand command = new();
            ShellContext context = CreateShellContext();

            await WithConsoleAsync(async console =>
                {
                    command.Execute(context, scriptPath);

                    string output = console.Writer.ToString();
                    string expectedSummary =
                        $"[compatibility] Running '{Path.GetFullPath(scriptPath)}' with {context.Script.CompatibilityProfile.GetFeatureSummary()}";
                    await Assert.That(output).Contains(expectedSummary).ConfigureAwait(false);
                })
                .ConfigureAwait(false);

            DynValue result = context.Script.Globals.Get("result");
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        private static Task WithConsoleAsync(Func<ConsoleRedirectionScope, Task> action)
        {
            return ConsoleTestUtilities.WithConsoleRedirectionAsync(action);
        }
    }
}
