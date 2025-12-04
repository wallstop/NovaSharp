namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class DotNetCorePlatformAccessorTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("rb", FileAccess.Read)]
        [global::TUnit.Core.Arguments("r+b", FileAccess.ReadWrite)]
        [global::TUnit.Core.Arguments("w", FileAccess.Write)]
        [global::TUnit.Core.Arguments("w+b", FileAccess.ReadWrite)]
        [global::TUnit.Core.Arguments("invalid", FileAccess.ReadWrite)]
        public async Task ParseFileAccessRecognisesModes(string mode, FileAccess expected)
        {
            FileAccess access = DotNetCorePlatformAccessor.ParseFileAccess(mode);

            await Assert.That(access).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments("rb", FileMode.Open)]
        [global::TUnit.Core.Arguments("r+b", FileMode.OpenOrCreate)]
        [global::TUnit.Core.Arguments("w", FileMode.Create)]
        [global::TUnit.Core.Arguments("w+b", FileMode.Truncate)]
        [global::TUnit.Core.Arguments("invalid", FileMode.Append)]
        public async Task ParseFileModeRecognisesModes(string mode, FileMode expected)
        {
            FileMode fileMode = DotNetCorePlatformAccessor.ParseFileMode(mode);

            await Assert.That(fileMode).IsEqualTo(expected).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessNormalizesWhitespaceAndCase()
        {
            FileAccess access = DotNetCorePlatformAccessor.ParseFileAccess(" R+B ");

            await Assert.That(access).IsEqualTo(FileAccess.ReadWrite).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeNormalizesWhitespaceAndCase()
        {
            FileMode mode = DotNetCorePlatformAccessor.ParseFileMode(" W + B ");

            await Assert.That(mode).IsEqualTo(FileMode.Truncate).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DotNetCorePlatformAccessor.ParseFileAccess(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DotNetCorePlatformAccessor.ParseFileMode(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OpenFileRespectsModeAndAccess()
        {
            DotNetCorePlatformAccessor accessor = new();
            using TempFileScope tempFileScope = TempFileScope.Create(createFile: true);

            using Stream stream = accessor.OpenFile(
                script: null,
                filename: tempFileScope.FilePath,
                encoding: Encoding.UTF8,
                mode: "w+"
            );

            await Assert.That(stream.CanRead).IsTrue().ConfigureAwait(false);
            await Assert.That(stream.CanWrite).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultPrintWritesToConsoleOut()
        {
            DotNetCorePlatformAccessor accessor = new();
            string output = string.Empty;
            await ConsoleTestUtilities
                .WithConsoleCaptureAsync(
                    consoleScope =>
                    {
                        accessor.DefaultPrint("hello");
                        output = consoleScope.Writer.ToString().Trim();
                        return Task.CompletedTask;
                    },
                    captureError: false
                )
                .ConfigureAwait(false);

            await Assert.That(output).IsEqualTo("hello").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvironmentVariableReturnsSetValue()
        {
            DotNetCorePlatformAccessor accessor = new();
            const string key = "NS_TEST_ENV_VAR";
            using EnvironmentVariableScope environmentScope = EnvironmentVariableScope.Override(
                key,
                "value123"
            );

            string result = accessor.GetEnvironmentVariable(key);
            await Assert.That(result).IsEqualTo("value123").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetTempFileNameCreatesFile()
        {
            DotNetCorePlatformAccessor accessor = new();
            string path = accessor.GetTempFileName();
            using TempFileScope tempFileScope = TempFileScope.FromExisting(path);

            await Assert.That(File.Exists(tempFileScope.FilePath)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FileExistsDeleteAndMoveOperateOnFilesystem()
        {
            DotNetCorePlatformAccessor accessor = new();
            using TempDirectoryScope directoryScope = TempDirectoryScope.Create();
            string directory = directoryScope.DirectoryPath;

            string src = Path.Combine(directory, "source.txt");
            string dst = Path.Combine(directory, "dest.txt");

            await File.WriteAllTextAsync(src, "payload").ConfigureAwait(false);
            await Assert.That(accessor.FileExists(src)).IsTrue().ConfigureAwait(false);

            accessor.MoveFile(src, dst);
            await Assert.That(File.Exists(dst)).IsTrue().ConfigureAwait(false);
            await Assert.That(File.Exists(src)).IsFalse().ConfigureAwait(false);

            accessor.DeleteFile(dst);
            await Assert.That(File.Exists(dst)).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandRunsShellCommand()
        {
            DotNetCorePlatformAccessor accessor = new();

            int success = accessor.ExecuteCommand("echo dotnet-core");
            await Assert.That(success).IsEqualTo(0).ConfigureAwait(false);

            int failure = accessor.ExecuteCommand("__totally_invalid_command__");
            await Assert.That(failure).IsNotEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments((int)StandardFileType.StdIn)]
        [global::TUnit.Core.Arguments((int)StandardFileType.StdOut)]
        [global::TUnit.Core.Arguments((int)StandardFileType.StdErr)]
        public async Task GetStandardStreamReturnsLiveStream(int rawType)
        {
            DotNetCorePlatformAccessor accessor = new();
            StandardFileType type = (StandardFileType)rawType;

            using Stream stream = accessor.GetStandardStream(type);

            await Assert.That(stream).IsNotNull().ConfigureAwait(false);
        }
    }
}
