#pragma warning disable CA2007
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

            await Assert.That(access).IsEqualTo(expected);
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

            await Assert.That(fileMode).IsEqualTo(expected);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessNormalizesWhitespaceAndCase()
        {
            FileAccess access = DotNetCorePlatformAccessor.ParseFileAccess(" R+B ");

            await Assert.That(access).IsEqualTo(FileAccess.ReadWrite);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeNormalizesWhitespaceAndCase()
        {
            FileMode mode = DotNetCorePlatformAccessor.ParseFileMode(" W + B ");

            await Assert.That(mode).IsEqualTo(FileMode.Truncate);
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileAccessThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DotNetCorePlatformAccessor.ParseFileAccess(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode");
        }

        [global::TUnit.Core.Test]
        public async Task ParseFileModeThrowsWhenModeIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                DotNetCorePlatformAccessor.ParseFileMode(null)
            )!;

            await Assert.That(exception.ParamName).IsEqualTo("mode");
        }

        [global::TUnit.Core.Test]
        public async Task OpenFileRespectsModeAndAccess()
        {
            DotNetCorePlatformAccessor accessor = new();
            string path = Path.GetTempFileName();
            using TempFileScope tempFileScope = TempFileScope.FromExisting(path);

            using Stream stream = accessor.OpenFile(
                script: null,
                filename: tempFileScope.FilePath,
                encoding: Encoding.UTF8,
                mode: "w+"
            );

            await Assert.That(stream.CanRead).IsTrue();
            await Assert.That(stream.CanWrite).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DefaultPrintWritesToConsoleOut()
        {
            DotNetCorePlatformAccessor accessor = new();
            using ConsoleCaptureScope scope = new(captureError: false);

            accessor.DefaultPrint("hello");

            await Assert.That(scope.Writer.ToString().Trim()).IsEqualTo("hello");
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvironmentVariableReturnsSetValue()
        {
            DotNetCorePlatformAccessor accessor = new();
            const string key = "NS_TEST_ENV_VAR";
            string previous = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, "value123");
            try
            {
                string result = accessor.GetEnvironmentVariable(key);
                await Assert.That(result).IsEqualTo("value123");
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, previous);
            }
        }

        [global::TUnit.Core.Test]
        public async Task GetTempFileNameCreatesFile()
        {
            DotNetCorePlatformAccessor accessor = new();
            string path = accessor.GetTempFileName();
            using TempFileScope tempFileScope = TempFileScope.FromExisting(path);

            await Assert.That(File.Exists(tempFileScope.FilePath)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task FileExistsDeleteAndMoveOperateOnFilesystem()
        {
            DotNetCorePlatformAccessor accessor = new();
            using TempDirectoryScope directoryScope = TempDirectoryScope.Create();
            string directory = directoryScope.DirectoryPath;

            string src = Path.Combine(directory, "source.txt");
            string dst = Path.Combine(directory, "dest.txt");

            await File.WriteAllTextAsync(src, "payload");
            await Assert.That(accessor.FileExists(src)).IsTrue();

            accessor.MoveFile(src, dst);
            await Assert.That(File.Exists(dst)).IsTrue();
            await Assert.That(File.Exists(src)).IsFalse();

            accessor.DeleteFile(dst);
            await Assert.That(File.Exists(dst)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public void ExecuteCommandThrowsNotSupported()
        {
            DotNetCorePlatformAccessor accessor = new();

            _ = Assert.Throws<NotSupportedException>(() => accessor.ExecuteCommand("echo test"));
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

            await Assert.That(stream).IsNotNull();
        }
    }
}
#pragma warning restore CA2007
