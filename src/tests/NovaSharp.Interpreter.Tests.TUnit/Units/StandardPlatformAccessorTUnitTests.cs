namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class StandardPlatformAccessorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParseFileAccessHandlesModes()
        {
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("r"))
                .IsEqualTo(FileAccess.Read)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("r+"))
                .IsEqualTo(FileAccess.ReadWrite)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("w"))
                .IsEqualTo(FileAccess.Write)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("w+"))
                .IsEqualTo(FileAccess.ReadWrite)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("rb"))
                .IsEqualTo(FileAccess.Read)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeHandlesAppend()
        {
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("r"))
                .IsEqualTo(FileMode.Open)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("w"))
                .IsEqualTo(FileMode.Create)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("a"))
                .IsEqualTo(FileMode.Append)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("a+"))
                .IsEqualTo(FileMode.Append)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessFallsBackToReadWrite()
        {
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("a"))
                .IsEqualTo(FileAccess.ReadWrite)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("a+"))
                .IsEqualTo(FileAccess.ReadWrite)
                .ConfigureAwait(false);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("unknown"))
                .IsEqualTo(FileAccess.ReadWrite)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                StandardPlatformAccessor.ParseFileAccess(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                StandardPlatformAccessor.ParseFileMode(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvironmentVariableReflectsEnvironment()
        {
            const string variable = "NOVASHARP_STD_TEST";
            using EnvironmentVariableScope variableScope = EnvironmentVariableScope.Override(
                variable,
                "expected"
            );

            StandardPlatformAccessor accessor = new();
            await Assert
                .That(accessor.GetEnvironmentVariable(variable))
                .IsEqualTo("expected")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FilterSupportedModulesIsIdentity()
        {
            StandardPlatformAccessor accessor = new();
            CoreModules modules = CoreModules.PresetComplete;
            await Assert
                .That(accessor.FilterSupportedCoreModules(modules))
                .IsEqualTo(modules)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PrefixIsStd()
        {
            StandardPlatformAccessor accessor = new();
            await Assert
                .That(accessor.GetPlatformNamePrefix())
                .IsEqualTo("std")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OpenFileSupportsReadAndWrite()
        {
            using TempFileScope fileScope = TempFileScope.Create(extension: ".txt");
            string path = fileScope.FilePath;
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            StandardPlatformAccessor accessor = new();

            using (Stream writeStream = accessor.OpenFile(script, path, Encoding.UTF8, "w"))
            {
                using StreamWriter writer = new(writeStream, Encoding.UTF8, leaveOpen: false);
                await writer.WriteAsync("data").ConfigureAwait(false);
            }

            using (Stream readStream = accessor.OpenFile(script, path, Encoding.UTF8, "r"))
            {
                using StreamReader reader = new(readStream, Encoding.UTF8);
                string contents = await reader.ReadToEndAsync().ConfigureAwait(false);
                await Assert.That(contents).IsEqualTo("data").ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task DefaultPrintDoesNotThrow()
        {
            StandardPlatformAccessor accessor = new();
            accessor.DefaultPrint("hello");
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StandardStreamsAreAvailable()
        {
            StandardPlatformAccessor accessor = new();
            await Assert
                .That(accessor.GetStandardStream(StandardFileType.StdIn))
                .IsNotNull()
                .ConfigureAwait(false);
            await Assert
                .That(accessor.GetStandardStream(StandardFileType.StdOut))
                .IsNotNull()
                .ConfigureAwait(false);
            await Assert
                .That(accessor.GetStandardStream(StandardFileType.StdErr))
                .IsNotNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UnknownStandardStreamTypeThrows()
        {
            StandardPlatformAccessor accessor = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                accessor.GetStandardStream((StandardFileType)(-1))
            )!;

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TempFileIsCreated()
        {
            StandardPlatformAccessor accessor = new();
            string tempFile = accessor.GetTempFileName();
            using TempFileScope tempFileScope = TempFileScope.FromExisting(tempFile);
            await Assert.That(File.Exists(tempFile)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FileOperationsReflectFilesystem()
        {
            StandardPlatformAccessor accessor = new();
            using TempFileScope sourceScope = TempFileScope.Create(extension: ".txt");
            using TempFileScope destinationScope = TempFileScope.Create(extension: ".txt");
            string source = sourceScope.FilePath;
            string destination = destinationScope.FilePath;

            await File.WriteAllTextAsync(source, "content").ConfigureAwait(false);
            await Assert.That(accessor.FileExists(source)).IsTrue().ConfigureAwait(false);

            accessor.MoveFile(source, destination);
            await Assert.That(accessor.FileExists(source)).IsFalse().ConfigureAwait(false);
            await Assert.That(accessor.FileExists(destination)).IsTrue().ConfigureAwait(false);

            accessor.DeleteFile(destination);
            await Assert.That(accessor.FileExists(destination)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandRunsShellWhenSupported()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            StandardPlatformAccessor accessor = new();
            int exitCode = accessor.ExecuteCommand("ver");
            await Assert.That(exitCode).IsEqualTo(0).ConfigureAwait(false);
        }
    }
}
