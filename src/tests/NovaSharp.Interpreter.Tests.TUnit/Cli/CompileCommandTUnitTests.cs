namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

    [PlatformDetectorIsolation]
    public sealed class CompileCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWritesCompiledChunkToDisk()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope sourceScope = TempFileScope.Create(
                namePrefix: "compile_",
                extension: ".lua"
            );
            string sourcePath = sourceScope.FilePath;
            string targetPath = sourcePath + "-compiled";
            using TempFileScope targetScope = TempFileScope.FromExisting(targetPath);
            await File.WriteAllTextAsync(sourcePath, "return 'compiled'").ConfigureAwait(false);

            CompileCommand command = new();
            ShellContext context = CreateShellContext();

            string consoleOutput = string.Empty;
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    consoleScope =>
                    {
                        command.Execute(context, sourcePath);
                        consoleOutput = consoleScope.Writer.ToString();
                        return Task.CompletedTask;
                    },
                    captureError: false
                )
                .ConfigureAwait(false);

            await Assert.That(File.Exists(targetPath)).IsTrue();
            await Assert.That(new FileInfo(targetPath).Length).IsGreaterThan(0);
            await Assert
                .That(consoleOutput)
                .Contains(CliMessages.CompileCommandSuccess(targetPath));
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWhenSourceMissingLogsFailureAndLeavesNoArtefact()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope sourceScope = TempFileScope.Create(
                namePrefix: "compile_",
                extension: ".lua"
            );
            string sourcePath = sourceScope.FilePath;
            string targetPath = sourcePath + "-compiled";
            using TempFileScope targetScope = TempFileScope.FromExisting(targetPath);
            CompileCommand command = new();
            ShellContext context = CreateShellContext();

            command.Execute(context, sourcePath);

            await Assert.That(File.Exists(targetPath)).IsFalse();
            await Assert.That(File.Exists(sourcePath)).IsFalse();
        }
    }
}
