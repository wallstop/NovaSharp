namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StandardPlatformAccessorTests
    {
        [Test]
        public void ParseFileAccessHandlesModes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("r"),
                    Is.EqualTo(FileAccess.Read)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("r+"),
                    Is.EqualTo(FileAccess.ReadWrite)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("w"),
                    Is.EqualTo(FileAccess.Write)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("w+"),
                    Is.EqualTo(FileAccess.ReadWrite)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("rb"),
                    Is.EqualTo(FileAccess.Read)
                );
            });
        }

        [Test]
        public void ParseFileModeHandlesAppend()
        {
            Assert.Multiple(() =>
            {
                Assert.That(StandardPlatformAccessor.ParseFileMode("r"), Is.EqualTo(FileMode.Open));
                Assert.That(
                    StandardPlatformAccessor.ParseFileMode("w"),
                    Is.EqualTo(FileMode.Create)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileMode("a"),
                    Is.EqualTo(FileMode.Append)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileMode("a+"),
                    Is.EqualTo(FileMode.Append)
                );
            });
        }

        [Test]
        public void ParseFileAccessFallsBackToReadWrite()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("a"),
                    Is.EqualTo(FileAccess.ReadWrite)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("a+"),
                    Is.EqualTo(FileAccess.ReadWrite)
                );
                Assert.That(
                    StandardPlatformAccessor.ParseFileAccess("unknown"),
                    Is.EqualTo(FileAccess.ReadWrite)
                );
            });
        }

        [Test]
        public void GetEnvironmentVariableReflectsEnvironment()
        {
            const string variable = "NOVASHARP_STD_TEST";
            Environment.SetEnvironmentVariable(variable, "expected");
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            Assert.That(accessor.GetEnvironmentVariable(variable), Is.EqualTo("expected"));
        }

        [Test]
        public void FilterSupportedModulesIsIdentity()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            CoreModules modules = CoreModules.PresetComplete;
            Assert.That(accessor.FilterSupportedCoreModules(modules), Is.EqualTo(modules));
        }

        [Test]
        public void PrefixIsStd()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            Assert.That(accessor.GetPlatformNamePrefix(), Is.EqualTo("std"));
        }

        [Test]
        public void OpenFileSupportsReadAndWrite()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            Script script = new Script(TestCoreModules.BasicGlobals);
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            try
            {
                using (Stream writeStream = accessor.OpenFile(script, path, Encoding.UTF8, "w"))
                using (
                    StreamWriter writer = new StreamWriter(
                        writeStream,
                        Encoding.UTF8,
                        leaveOpen: false
                    )
                )
                {
                    writer.Write("data");
                }

                using (Stream readStream = accessor.OpenFile(script, path, Encoding.UTF8, "r"))
                using (StreamReader reader = new StreamReader(readStream, Encoding.UTF8))
                {
                    Assert.That(reader.ReadToEnd(), Is.EqualTo("data"));
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

        [Test]
        public void DefaultPrintWritesToConsoleOut()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            TextWriter original = Console.Out;
            using StringWriter capture = new StringWriter();

            try
            {
                Console.SetOut(capture);
                accessor.DefaultPrint("hello");
                Console.Out.Flush();
                Assert.That(capture.ToString().Trim(), Is.EqualTo("hello"));
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Test]
        public void StandardStreamsAreAvailable()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();

            Assert.Multiple(() =>
            {
                Assert.That(accessor.GetStandardStream(StandardFileType.StdIn), Is.Not.Null);
                Assert.That(accessor.GetStandardStream(StandardFileType.StdOut), Is.Not.Null);
                Assert.That(accessor.GetStandardStream(StandardFileType.StdErr), Is.Not.Null);
            });
        }

        [Test]
        public void UnknownStandardStreamTypeThrows()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();

            Assert.Throws<ArgumentException>(() =>
                accessor.GetStandardStream((StandardFileType)(-1))
            );
        }

        [Test]
        public void TempFileIsCreated()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            string tempFile = accessor.GetTempFileName();
            Assert.That(File.Exists(tempFile), Is.True);
            File.Delete(tempFile);
        }

        [Test]
        public void FileOperationsReflectFilesystem()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            string source = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

            try
            {
                File.WriteAllText(source, "content");
                Assert.That(accessor.FileExists(source), Is.True);

                accessor.MoveFile(source, destination);
                Assert.That(accessor.FileExists(source), Is.False);
                Assert.That(accessor.FileExists(destination), Is.True);

                accessor.DeleteFile(destination);
                Assert.That(accessor.FileExists(destination), Is.False);
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

        [Test]
        public void ExecuteCommandRunsShell()
        {
            StandardPlatformAccessor accessor = new StandardPlatformAccessor();
            if (OperatingSystem.IsWindows())
            {
                int exitCode = accessor.ExecuteCommand("ver");
                Assert.That(exitCode, Is.EqualTo(0));
            }
            else
            {
                Assert.Ignore("Command execution test only runs on Windows.");
            }
        }
    }
}
