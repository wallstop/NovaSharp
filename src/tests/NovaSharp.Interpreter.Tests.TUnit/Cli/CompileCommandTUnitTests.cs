namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class CompileCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWritesCompiledChunkToDisk()
        {
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
            ShellContext context = new(new Script());

            string consoleOutput = string.Empty;
            await ConsoleCaptureCoordinator
                .RunAsync(() =>
                {
                    using ConsoleCaptureScope consoleScopeInner = new(captureError: false);
                    command.Execute(context, sourcePath);
                    consoleOutput = consoleScopeInner.Writer.ToString();
                    return Task.CompletedTask;
                })
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
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope sourceScope = TempFileScope.Create(
                namePrefix: "compile_",
                extension: ".lua"
            );
            string sourcePath = sourceScope.FilePath;
            string targetPath = sourcePath + "-compiled";
            using TempFileScope targetScope = TempFileScope.FromExisting(targetPath);
            CompileCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, sourcePath);

            await Assert.That(File.Exists(targetPath)).IsFalse();
            await Assert.That(File.Exists(sourcePath)).IsFalse();
        }
    }
}
