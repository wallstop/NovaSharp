#pragma warning disable CA2007
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

    public sealed class StandardPlatformAccessorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ParseFileAccessHandlesModes()
        {
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("r"))
                .IsEqualTo(FileAccess.Read);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("r+"))
                .IsEqualTo(FileAccess.ReadWrite);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("w"))
                .IsEqualTo(FileAccess.Write);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("w+"))
                .IsEqualTo(FileAccess.ReadWrite);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("rb"))
                .IsEqualTo(FileAccess.Read);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeHandlesAppend()
        {
            await Assert.That(StandardPlatformAccessor.ParseFileMode("r")).IsEqualTo(FileMode.Open);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("w"))
                .IsEqualTo(FileMode.Create);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("a"))
                .IsEqualTo(FileMode.Append);
            await Assert
                .That(StandardPlatformAccessor.ParseFileMode("a+"))
                .IsEqualTo(FileMode.Append);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessFallsBackToReadWrite()
        {
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("a"))
                .IsEqualTo(FileAccess.ReadWrite);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("a+"))
                .IsEqualTo(FileAccess.ReadWrite);
            await Assert
                .That(StandardPlatformAccessor.ParseFileAccess("unknown"))
                .IsEqualTo(FileAccess.ReadWrite);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                StandardPlatformAccessor.ParseFileAccess(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode");
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                StandardPlatformAccessor.ParseFileMode(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode");
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvironmentVariableReflectsEnvironment()
        {
            const string variable = "NOVASHARP_STD_TEST";
            string previous = Environment.GetEnvironmentVariable(variable);
            try
            {
                Environment.SetEnvironmentVariable(variable, "expected");
                StandardPlatformAccessor accessor = new();
                await Assert.That(accessor.GetEnvironmentVariable(variable)).IsEqualTo("expected");
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, previous);
            }
        }

        [global::TUnit.Core.Test]
        public async Task FilterSupportedModulesIsIdentity()
        {
            StandardPlatformAccessor accessor = new();
            CoreModules modules = CoreModules.PresetComplete;
            await Assert.That(accessor.FilterSupportedCoreModules(modules)).IsEqualTo(modules);
        }

        [global::TUnit.Core.Test]
        public async Task PrefixIsStd()
        {
            StandardPlatformAccessor accessor = new();
            await Assert.That(accessor.GetPlatformNamePrefix()).IsEqualTo("std");
        }

        [global::TUnit.Core.Test]
        public async Task OpenFileSupportsReadAndWrite()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);
            StandardPlatformAccessor accessor = new();
            try
            {
                using (Stream writeStream = accessor.OpenFile(script, path, Encoding.UTF8, "w"))
                {
                    using StreamWriter writer = new(writeStream, Encoding.UTF8, leaveOpen: false);
                    await writer.WriteAsync("data");
                }

                using (Stream readStream = accessor.OpenFile(script, path, Encoding.UTF8, "r"))
                {
                    using StreamReader reader = new(readStream, Encoding.UTF8);
                    string contents = await reader.ReadToEndAsync();
                    await Assert.That(contents).IsEqualTo("data");
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task DefaultPrintDoesNotThrow()
        {
            StandardPlatformAccessor accessor = new();
            accessor.DefaultPrint("hello");
            await Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public async Task StandardStreamsAreAvailable()
        {
            StandardPlatformAccessor accessor = new();
            await Assert.That(accessor.GetStandardStream(StandardFileType.StdIn)).IsNotNull();
            await Assert.That(accessor.GetStandardStream(StandardFileType.StdOut)).IsNotNull();
            await Assert.That(accessor.GetStandardStream(StandardFileType.StdErr)).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task UnknownStandardStreamTypeThrows()
        {
            StandardPlatformAccessor accessor = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                accessor.GetStandardStream((StandardFileType)(-1))
            )!;

            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task TempFileIsCreated()
        {
            StandardPlatformAccessor accessor = new();
            string tempFile = accessor.GetTempFileName();
            try
            {
                await Assert.That(File.Exists(tempFile)).IsTrue();
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task FileOperationsReflectFilesystem()
        {
            StandardPlatformAccessor accessor = new();
            string source = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

            try
            {
                await File.WriteAllTextAsync(source, "content");
                await Assert.That(accessor.FileExists(source)).IsTrue();

                accessor.MoveFile(source, destination);
                await Assert.That(accessor.FileExists(source)).IsFalse();
                await Assert.That(accessor.FileExists(destination)).IsTrue();

                accessor.DeleteFile(destination);
                await Assert.That(accessor.FileExists(destination)).IsFalse();
            }
            finally
            {
                if (File.Exists(source))
                {
                    File.Delete(source);
                }

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }
            }
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
            await Assert.That(exitCode).IsEqualTo(0);
        }
    }
}
#pragma warning restore CA2007
