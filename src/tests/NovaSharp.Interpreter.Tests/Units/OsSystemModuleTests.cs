namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    [NonParallelizable]
    public sealed class OsSystemModuleTests
    {
        private IPlatformAccessor? _originalPlatform;
        private StubPlatformAccessor _stub = null!;

        [SetUp]
        public void SetUp()
        {
            _originalPlatform = Script.GlobalOptions.Platform;
            _stub = new StubPlatformAccessor();
            Script.GlobalOptions.Platform = _stub;
        }

        [TearDown]
        public void TearDown()
        {
            Script.GlobalOptions.Platform = _originalPlatform!;
        }

        [Test]
        public void ExecuteCommandReturnsExitTuple()
        {
            _stub.NextExecuteExitCode = 42;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('build')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("exit"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(42));
                Assert.That(_stub.ExecutedCommands, Is.EqualTo(new[] { "build" }));
            });
        }

        [Test]
        public void ExecuteCommandReturnsNilOnFailure()
        {
            _stub.ExecuteThrows = true;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('fail')");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(_stub.ExecutedCommands, Is.EqualTo(new[] { "fail" }));
            });
        }

        [Test]
        public void ExecuteWithoutArgumentsReturnsTrue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.execute()");

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void ExitInvokesPlatformAndThrowsExitException()
        {
            Script script = CreateScript();

            ExitFastException exception = Assert.Throws<ExitFastException>(() =>
                script.DoString("os.exit(5)")
            )!;
            Assert.That(exception.ExitCode, Is.EqualTo(5));
        }

        [Test]
        public void GetenvReturnsStoredValue()
        {
            _stub.Environment["HOME"] = "/tmp/home";
            Script script = CreateScript();

            DynValue value = script.DoString("return os.getenv('HOME')");

            Assert.That(value.String, Is.EqualTo("/tmp/home"));
        }

        [Test]
        public void GetenvReturnsNilWhenMissing()
        {
            Script script = CreateScript();
            DynValue value = script.DoString("return os.getenv('MISSING')");

            Assert.That(value.IsNil(), Is.True);
        }

        [Test]
        public void RemoveDeletesExistingFile()
        {
            _stub.CreateFile("file.txt");
            Script script = CreateScript();

            DynValue result = script.DoString("return os.remove('file.txt')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Boolean, Is.True);
                Assert.That(_stub.FileExists("file.txt"), Is.False);
            });
        }

        [Test]
        public void RemoveReturnsErrorTupleWhenMissing()
        {
            Script script = CreateScript();

            DynValue result = script.DoString("return os.remove('missing.txt')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("missing.txt"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void RemoveReturnsErrorTupleWhenDeleteThrows()
        {
            _stub.CreateFile("locked.txt");
            _stub.DeleteThrows = true;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.remove('locked.txt')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("locked.txt"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void RenameMovesExistingFile()
        {
            _stub.CreateFile("old.txt");
            Script script = CreateScript();

            DynValue result = script.DoString("return os.rename('old.txt', 'new.txt')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Boolean, Is.True);
                Assert.That(_stub.FileExists("old.txt"), Is.False);
                Assert.That(_stub.FileExists("new.txt"), Is.True);
            });
        }

        [Test]
        public void RenameReturnsTupleWhenSourceMissing()
        {
            Script script = CreateScript();

            DynValue result = script.DoString("return os.rename('nope', 'dest')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("nope"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void RenameReturnsTupleWhenMoveThrows()
        {
            _stub.CreateFile("source");
            _stub.MoveThrows = true;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.rename('source', 'dest')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("source"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void TmpnameReturnsPlatformTempFileName()
        {
            _stub.TempFileName = "stub-temp";
            Script script = CreateScript();

            DynValue result = script.DoString("return os.tmpname()");

            Assert.That(result.String, Is.EqualTo("stub-temp"));
        }

        [Test]
        public void SetlocaleReturnsPlaceholderString()
        {
            Script script = CreateScript();

            DynValue result = script.DoString("return os.setlocale()");

            Assert.That(result.String, Is.EqualTo("n/a"));
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private sealed class StubPlatformAccessor : IPlatformAccessor
        {
            public Dictionary<string, string> Environment { get; } =
                new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);
            public List<string> ExecutedCommands { get; } = new();
            public int NextExecuteExitCode { get; set; }
            public bool ExecuteThrows { get; set; }
            public bool DeleteThrows { get; set; }
            public bool MoveThrows { get; set; }
            public string TempFileName { get; set; } = "temp-file";

            public CoreModules FilterSupportedCoreModules(CoreModules module) => module;

            public string GetEnvironmentVariable(string envvarname) =>
                Environment.TryGetValue(envvarname, out string value) ? value : null;

            public bool IsRunningOnAOT() => false;

            public string GetPlatformName() => "StubPlatform";

            public void DefaultPrint(string content) { }

            public string DefaultInput(string prompt) => string.Empty;

            public Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            ) => new MemoryStream();

            public Stream GetStandardStream(StandardFileType type) => Stream.Null;

            public string GetTempFileName() => TempFileName;

            public void ExitFast(int exitCode) => throw new ExitFastException(exitCode);

            public bool FileExists(string file) => _files.Contains(file);

            public void DeleteFile(string file)
            {
                if (DeleteThrows)
                {
                    throw new IOException($"{file}: cannot delete");
                }

                _files.Remove(file);
            }

            public void MoveFile(string src, string dst)
            {
                if (MoveThrows)
                {
                    throw new IOException($"{src}: cannot move");
                }

                if (!_files.Remove(src))
                {
                    throw new FileNotFoundException("Source not found", src);
                }

                _files.Add(dst);
            }

            public int ExecuteCommand(string cmdline)
            {
                ExecutedCommands.Add(cmdline);

                if (ExecuteThrows)
                {
                    throw new InvalidOperationException("Command failed");
                }

                return NextExecuteExitCode;
            }

            public void CreateFile(string fileName) => _files.Add(fileName);
        }
    }

    public sealed class ExitFastException : Exception
    {
        public ExitFastException(int exitCode)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
}
