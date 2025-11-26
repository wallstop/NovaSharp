namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    [NonParallelizable]
    public sealed class OsSystemModuleTests
    {
        private static readonly string[] BuildCommand = { "build" };
        private static readonly string[] FailCommand = { "fail" };
        private IPlatformAccessor _originalPlatform;
        private StubPlatformAccessor _stub;

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
            if (_originalPlatform != null)
            {
                Script.GlobalOptions.Platform = _originalPlatform;
            }
        }

        [Test]
        public void ExecuteCommandReturnsSuccessTuple()
        {
            _stub.NextExecuteExitCode = 0;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('build')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("exit"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(0));
                Assert.That(_stub.ExecutedCommands, Is.EqualTo(BuildCommand));
            });
        }

        [Test]
        public void ExecuteCommandReturnsNilTupleOnFailure()
        {
            _stub.ExecuteThrows = true;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('fail')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("Command failed"));
                Assert.That(_stub.ExecutedCommands, Is.EqualTo(FailCommand));
            });
        }

        [Test]
        public void ExecuteCommandReturnsNilTupleOnNonZeroExit()
        {
            _stub.NextExecuteExitCode = 7;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('fail')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("exit"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(7));
            });
        }

        [Test]
        public void ExecuteCommandReportsSignalWhenExitCodeNegative()
        {
            _stub.NextExecuteExitCode = -9;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('terminate')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Is.EqualTo("signal"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(9));
            });
        }

        [Test]
        public void ExecuteCommandReportsNotSupportedMessage()
        {
            _stub.ExecuteNotSupported = true;
            Script script = CreateScript();

            DynValue result = script.DoString("return os.execute('build')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("not supported"));
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
        public void GetEnvReturnsStoredValue()
        {
            _stub.Environment["HOME"] = "/tmp/home";
            Script script = CreateScript();

            DynValue value = script.DoString("return os.getenv('HOME')");

            Assert.That(value.String, Is.EqualTo("/tmp/home"));
        }

        [Test]
        public void GetEnvReturnsNilWhenMissing()
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
        public void DateUtcEpochMatchesExpectedFields()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local t = os.date('!*t', 0)
                return t.year, t.month, t.day, t.hour, t.min, t.sec, t.wday, t.yday, t.isdst
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(1970));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(1));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(1));
                Assert.That(tuple.Tuple[3].Number, Is.EqualTo(0));
                Assert.That(tuple.Tuple[4].Number, Is.EqualTo(0));
                Assert.That(tuple.Tuple[5].Number, Is.EqualTo(0));
                Assert.That(tuple.Tuple[6].Number, Is.EqualTo(5));
                Assert.That(tuple.Tuple[7].Number, Is.EqualTo(1));
                Assert.That(tuple.Tuple[8].Boolean, Is.False);
            });
        }

        [Test]
        public void DateUtcFormatMatchesExpectedString()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.date('!%d/%m/%y %H:%M:%S', 0)");
            Assert.That(result.String, Is.EqualTo("01/01/70 00:00:00"));
        }

        [Test]
        public void DateInvalidSpecifierThrows()
        {
            Script script = CreateScript();
            Assert.That(
                () => script.DoString("return os.date('%Ja', 0)"),
                Throws
                    .InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("invalid conversion specifier")
            );
        }

        [Test]
        public void DateLocalFormatMatchesClockPattern()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.date('%H:%M:%S')");
            Assert.That(result.String, Does.Match(@"^\d\d:\d\d:\d\d$"));
        }

        [Test]
        public void DifftimeReturnsDelta()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.difftime(1234, 1200)");
            Assert.That(result.Number, Is.EqualTo(34d));
        }

        [Test]
        public void DifftimeSingleArgumentReturnsValue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.difftime(1234)");
            Assert.That(result.Number, Is.EqualTo(1234d));
        }

        [Test]
        public void TimeReturnsPositiveNumber()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.time()");
            Assert.That(result.Number, Is.GreaterThan(0d));
        }

        [Test]
        public void TimeRoundTripsThroughUtcDate()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local stamp = os.time({
                    year = 2000,
                    month = 1,
                    day = 1,
                    hour = 0,
                    min = 0,
                    sec = 0,
                    isdst = 0,
                })
                local t = os.date('!*t', stamp)
                return stamp, t.year, t.month, t.day, t.hour, t.min, t.sec
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.GreaterThan(0d));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(2000));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(1));
                Assert.That(tuple.Tuple[3].Number, Is.EqualTo(1));
                Assert.That(tuple.Tuple[4].Number, Is.EqualTo(0));
                Assert.That(tuple.Tuple[5].Number, Is.EqualTo(0));
                Assert.That(tuple.Tuple[6].Number, Is.EqualTo(0));
            });
        }

        [Test]
        public void TimeMissingFieldRaisesError()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return os.time({ year = 2000 })
                end)
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("field 'day' missing"));
            });
        }

        [Test]
        public void ClockReturnsMonotonicValues()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return os.clock(), os.clock()");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Number, Is.GreaterThanOrEqualTo(0d));
                Assert.That(tuple.Tuple[1].Number, Is.GreaterThanOrEqualTo(tuple.Tuple[0].Number));
            });
        }

        [Test]
        public void DatePercentOyYieldsTwoDigitYear()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.date('!%Oy', 0)");
            Assert.That(result.String, Is.EqualTo("70"));
        }

        [Test]
        public void TimeWithNilArgumentReturnsTimestamp()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return os.time(nil)");
            Assert.That(result.Number, Is.GreaterThan(0d));
        }

        [Test]
        public void TmpNameReturnsPlatformTempFileName()
        {
            _stub.TempFileName = "stub-temp";
            Script script = CreateScript();

            DynValue result = script.DoString("return os.tmpname()");

            Assert.That(result.String, Is.EqualTo("stub-temp"));
        }

        [Test]
        public void TmpNameReturnsQueuedPlatformValuesThenFallsBack()
        {
            _stub.TempFileSequence.Enqueue("queued-one");
            _stub.TempFileSequence.Enqueue("queued-two");
            _stub.TempFileName = "fallback-temp";

            Script script = CreateScript();
            DynValue tuple = script.DoString("return os.tmpname(), os.tmpname(), os.tmpname()");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("queued-one"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("queued-two"));
                Assert.That(tuple.Tuple[2].String, Is.EqualTo("fallback-temp"));
            });
        }

        [Test]
        public void TmpNamePropagatesPlatformExceptions()
        {
            _stub.TempFileThrows = true;
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("return os.tmpname()"),
                Throws.InstanceOf<IOException>().With.Message.Contains("temp name failure")
            );
        }

        [Test]
        public void SetLocaleReturnsPlaceholderString()
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
            public bool ExecuteNotSupported { get; set; }
            public bool DeleteThrows { get; set; }
            public bool MoveThrows { get; set; }
            public string TempFileName { get; set; } = "temp-file";
            public Queue<string> TempFileSequence { get; } = new();
            public bool TempFileThrows { get; set; }

            public CoreModules FilterSupportedCoreModules(CoreModules coreModules) => coreModules;

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

            public string GetTempFileName()
            {
                if (TempFileThrows)
                {
                    throw new IOException("temp name failure");
                }

                if (TempFileSequence.Count > 0)
                {
                    return TempFileSequence.Dequeue();
                }

                return TempFileName;
            }

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

                if (ExecuteNotSupported)
                {
                    throw new PlatformNotSupportedException("Command execution is not supported.");
                }

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
        public ExitFastException() { }

        public ExitFastException(string message)
            : base(message) { }

        public ExitFastException(string message, Exception innerException)
            : base(message, innerException) { }

        public ExitFastException(int exitCode)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }
}
