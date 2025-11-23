namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DotNetCorePlatformAccessorTests
    {
        private DotNetCorePlatformAccessor _accessor = null!;
        private TextWriter _originalOut = null!;

        [SetUp]
        public void SetUp()
        {
            _accessor = new DotNetCorePlatformAccessor();
            _originalOut = Console.Out;
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
        }

        [TestCase("rb", FileAccess.Read)]
        [TestCase("r+b", FileAccess.ReadWrite)]
        [TestCase("w", FileAccess.Write)]
        [TestCase("w+b", FileAccess.ReadWrite)]
        [TestCase("invalid", FileAccess.ReadWrite)]
        public void ParseFileAccessRecognisesModes(string mode, FileAccess expected)
        {
            FileAccess access = DotNetCorePlatformAccessor.ParseFileAccess(mode);
            Assert.That(access, Is.EqualTo(expected));
        }

        [TestCase("rb", FileMode.Open)]
        [TestCase("r+b", FileMode.OpenOrCreate)]
        [TestCase("w", FileMode.Create)]
        [TestCase("w+b", FileMode.Truncate)]
        [TestCase("invalid", FileMode.Append)]
        public void ParseFileModeRecognisesModes(string mode, FileMode expected)
        {
            FileMode fileMode = DotNetCorePlatformAccessor.ParseFileMode(mode);
            Assert.That(fileMode, Is.EqualTo(expected));
        }

        [Test]
        public void ParseFileAccessNormalizesWhitespaceAndCase()
        {
            FileAccess access = DotNetCorePlatformAccessor.ParseFileAccess(" R+B ");
            Assert.That(access, Is.EqualTo(FileAccess.ReadWrite));
        }

        [Test]
        public void ParseFileModeNormalizesWhitespaceAndCase()
        {
            FileMode mode = DotNetCorePlatformAccessor.ParseFileMode(" W + B ");
            Assert.That(mode, Is.EqualTo(FileMode.Truncate));
        }

        [Test]
        public void ParseFileAccessThrowsWhenModeIsNull()
        {
            Assert.That(
                () => DotNetCorePlatformAccessor.ParseFileAccess(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mode")
            );
        }

        [Test]
        public void ParseFileModeThrowsWhenModeIsNull()
        {
            Assert.That(
                () => DotNetCorePlatformAccessor.ParseFileMode(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mode")
            );
        }

        [Test]
        public void OpenFileRespectsModeAndAccess()
        {
            string path = Path.GetTempFileName();
            try
            {
                using Stream stream = _accessor.OpenFile(
                    script: null!,
                    filename: path,
                    encoding: Encoding.UTF8,
                    mode: "w+"
                );

                Assert.Multiple(() =>
                {
                    Assert.That(stream.CanRead, Is.True);
                    Assert.That(stream.CanWrite, Is.True);
                });
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void DefaultPrintWritesToConsoleOut()
        {
            using StringWriter capture = new();
            Console.SetOut(capture);

            _accessor.DefaultPrint("hello");

            Assert.That(capture.ToString().Trim(), Is.EqualTo("hello"));
        }

        [Test]
        public void GetEnvironmentVariableReturnsSetValue()
        {
            const string key = "NS_TEST_ENV_VAR";
            Environment.SetEnvironmentVariable(key, "value123");

            string result = _accessor.GetEnvironmentVariable(key);

            Assert.That(result, Is.EqualTo("value123"));
        }

        [Test]
        public void GetTempFileNameCreatesFile()
        {
            string path = _accessor.GetTempFileName();
            try
            {
                Assert.That(File.Exists(path), Is.True);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void FileExistsDeleteAndMoveOperateOnFilesystem()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            string src = Path.Combine(directory, "source.txt");
            string dst = Path.Combine(directory, "dest.txt");

            try
            {
                File.WriteAllText(src, "payload");
                Assert.That(_accessor.FileExists(src), Is.True);

                _accessor.MoveFile(src, dst);
                Assert.Multiple(() =>
                {
                    Assert.That(File.Exists(dst), Is.True);
                    Assert.That(File.Exists(src), Is.False);
                });

                _accessor.DeleteFile(dst);
                Assert.That(File.Exists(dst), Is.False);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void ExecuteCommandThrowsNotSupported()
        {
            Assert.That(
                () => _accessor.ExecuteCommand("echo test"),
                Throws.TypeOf<NotSupportedException>()
            );
        }

        [TestCase(StandardFileType.StdIn)]
        [TestCase(StandardFileType.StdOut)]
        [TestCase(StandardFileType.StdErr)]
        public void GetStandardStreamReturnsLiveStream(StandardFileType type)
        {
            using Stream stream = _accessor.GetStandardStream(type);
            Assert.That(stream, Is.Not.Null);
        }
    }
}
