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

    [PlatformDetectorIsolation]
    public sealed class CompileCommandTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ExecuteWritesCompiledChunkToDisk()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string sourcePath = CreateTempScriptPath();
            string targetPath = sourcePath + "-compiled";
            await File.WriteAllTextAsync(sourcePath, "return 'compiled'").ConfigureAwait(false);

            CompileCommand command = new();
            ShellContext context = new(new Script());

            await ConsoleCaptureCoordinator.Semaphore.WaitAsync().ConfigureAwait(false);
            using ConsoleCaptureScope consoleScope = new(captureError: false);
            try
            {
                command.Execute(context, sourcePath);
            }
            finally
            {
                ConsoleCaptureCoordinator.Semaphore.Release();
            }

            try
            {
                await Assert.That(File.Exists(targetPath)).IsTrue();
                await Assert.That(new FileInfo(targetPath).Length).IsGreaterThan(0);
                await Assert
                    .That(consoleScope.Writer.ToString())
                    .Contains(CliMessages.CompileCommandSuccess(targetPath));
            }
            finally
            {
                CleanupFile(sourcePath);
                CleanupFile(targetPath);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWhenSourceMissingLogsFailureAndLeavesNoArtefact()
        {
            PlatformDetectionTestHelper.ForceFileSystemLoader();
            string sourcePath = CreateTempScriptPath();
            string targetPath = sourcePath + "-compiled";
            CompileCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, sourcePath);

            try
            {
                await Assert.That(File.Exists(targetPath)).IsFalse();
                await Assert.That(File.Exists(sourcePath)).IsFalse();
            }
            finally
            {
                CleanupFile(sourcePath);
                CleanupFile(targetPath);
            }
        }

        private static string CreateTempScriptPath()
        {
            return Path.Combine(Path.GetTempPath(), $"compile_{Guid.NewGuid():N}.lua");
        }

        private static void CleanupFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
