namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    public sealed class OsSystemModuleTUnitTests
    {
        private static readonly string[] BuildCommand = { "build" };
        private static readonly string[] FailCommand = { "fail" };

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandReturnsSuccessTuple()
        {
            StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 0;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("exit");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(0);
            await Assert.That(stub.ExecutedCommands).IsEquivalentTo(BuildCommand);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandReturnsNilTupleOnFailure()
        {
            StubPlatformAccessor stub = new();
            stub.ExecuteThrows = true;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("Command failed");
            await Assert.That(stub.ExecutedCommands).IsEquivalentTo(FailCommand);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandReturnsNilTupleOnNonZeroExit()
        {
            StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = 7;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('fail')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("exit");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandReportsSignalWhenExitCodeNegative()
        {
            StubPlatformAccessor stub = new();
            stub.NextExecuteExitCode = -9;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('terminate')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("signal");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(9);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteCommandReportsNotSupportedMessage()
        {
            StubPlatformAccessor stub = new();
            stub.ExecuteNotSupported = true;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.execute('build')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("not supported");
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteWithoutArgumentsReturnsTrue()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.execute()");

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ExitInvokesPlatformAndThrowsExitException()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            ExitFastException exception = Assert.Throws<ExitFastException>(() =>
            {
                script.DoString("os.exit(5)");
            });

            await Assert.That(exception.ExitCode).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvReturnsStoredValue()
        {
            StubPlatformAccessor stub = new();
            stub.Environment["HOME"] = "/tmp/home";
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue value = script.DoString("return os.getenv('HOME')");

            await Assert.That(value.String).IsEqualTo("/tmp/home");
        }

        [global::TUnit.Core.Test]
        public async Task GetEnvReturnsNilWhenMissing()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue value = script.DoString("return os.getenv('MISSING')");

            await Assert.That(value.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveDeletesExistingFile()
        {
            StubPlatformAccessor stub = new();
            stub.CreateFile("file.txt");
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.remove('file.txt')");

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(stub.FileExists("file.txt")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task RemoveReturnsErrorTupleWhenMissing()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.remove('missing.txt')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("missing.txt");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task RemoveReturnsErrorTupleWhenDeleteThrows()
        {
            StubPlatformAccessor stub = new();
            stub.CreateFile("locked.txt");
            stub.DeleteThrows = true;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.remove('locked.txt')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("locked.txt");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task RenameMovesExistingFile()
        {
            StubPlatformAccessor stub = new();
            stub.CreateFile("old.txt");
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.rename('old.txt', 'new.txt')");

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(stub.FileExists("old.txt")).IsFalse();
            await Assert.That(stub.FileExists("new.txt")).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task RenameReturnsTupleWhenSourceMissing()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.rename('nope', 'dest')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("nope");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task RenameReturnsTupleWhenMoveThrows()
        {
            StubPlatformAccessor stub = new();
            stub.CreateFile("source");
            stub.MoveThrows = true;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.rename('source', 'dest')");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("source");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task DateUtcEpochMatchesExpectedFields()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue tuple = script.DoString(
                @"
                local t = os.date('!*t', 0)
                return t.year, t.month, t.day, t.hour, t.min, t.sec, t.wday, t.yday, t.isdst
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(1970);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(1);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(0);
            await Assert.That(tuple.Tuple[4].Number).IsEqualTo(0);
            await Assert.That(tuple.Tuple[5].Number).IsEqualTo(0);
            await Assert.That(tuple.Tuple[6].Number).IsEqualTo(5);
            await Assert.That(tuple.Tuple[7].Number).IsEqualTo(1);
            await Assert.That(tuple.Tuple[8].Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task DateUtcFormatMatchesExpectedString()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.date('!%d/%m/%y %H:%M:%S', 0)");

            await Assert.That(result.String).IsEqualTo("01/01/70 00:00:00");
        }

        [global::TUnit.Core.Test]
        public async Task DateInvalidSpecifierThrows()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("return os.date('%Ja', 0)");
            });

            await Assert.That(exception.Message).Contains("invalid conversion specifier");
        }

        [global::TUnit.Core.Test]
        public async Task DateLocalFormatMatchesClockPattern()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.date('%H:%M:%S')");
            bool matches = Regex.IsMatch(result.String, @"^\d\d:\d\d:\d\d$");

            await Assert.That(matches).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DifftimeReturnsDelta()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.difftime(1234, 1200)");

            await Assert.That(result.Number).IsEqualTo(34d);
        }

        [global::TUnit.Core.Test]
        public async Task DifftimeSingleArgumentReturnsValue()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.difftime(1234)");

            await Assert.That(result.Number).IsEqualTo(1234d);
        }

        [global::TUnit.Core.Test]
        public async Task TimeReturnsPositiveNumber()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.time()");

            await Assert.That(result.Number).IsGreaterThan(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TimeRoundTripsThroughUtcDate()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
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

            await Assert.That(tuple.Tuple[0].Number).IsGreaterThan(0d);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(2000);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(1);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(1);
            await Assert.That(tuple.Tuple[4].Number).IsEqualTo(0);
            await Assert.That(tuple.Tuple[5].Number).IsEqualTo(0);
            await Assert.That(tuple.Tuple[6].Number).IsEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task TimeMissingFieldRaisesError()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return os.time({ year = 2000 })
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("field 'day' missing");
        }

        [global::TUnit.Core.Test]
        public async Task ClockReturnsMonotonicValues()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            // Collect a larger sample of clock values to verify monotonicity
            const int sampleCount = 1000;
            DynValue result = script.DoString(
                $@"
                local values = {{}}
                for i = 1, {sampleCount} do
                    values[i] = os.clock()
                end
                return table.unpack(values)
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(sampleCount);

            // Verify all values are monotonically non-decreasing
            int violations = 0;
            for (int i = 1; i < sampleCount; i++)
            {
                if (result.Tuple[i].Number < result.Tuple[i - 1].Number)
                {
                    violations++;
                }
            }

            await Assert.That(violations).IsEqualTo(0);

            // Verify the sequence actually advanced
            await Assert.That(result.Tuple[0].Number).IsGreaterThanOrEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task DatePercentOyYieldsTwoDigitYear()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.date('!%Oy', 0)");

            await Assert.That(result.String).IsEqualTo("70");
        }

        [global::TUnit.Core.Test]
        public async Task TimeWithNilArgumentReturnsTimestamp()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue result = script.DoString("return os.time(nil)");

            await Assert.That(result.Number).IsGreaterThan(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TmpNameReturnsPlatformTempFileName()
        {
            StubPlatformAccessor stub = new();
            stub.TempFileName = "stub-temp";
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.tmpname()");

            await Assert.That(result.String).IsEqualTo("stub-temp");
        }

        [global::TUnit.Core.Test]
        public async Task TmpNameReturnsQueuedPlatformValuesThenFallsBack()
        {
            StubPlatformAccessor stub = new();
            stub.TempFileSequence.Enqueue("queued-one");
            stub.TempFileSequence.Enqueue("queued-two");
            stub.TempFileName = "fallback-temp";
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;
            DynValue tuple = script.DoString("return os.tmpname(), os.tmpname(), os.tmpname()");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("queued-one");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("queued-two");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("fallback-temp");
        }

        [global::TUnit.Core.Test]
        public async Task TmpNamePropagatesPlatformExceptions()
        {
            StubPlatformAccessor stub = new();
            stub.TempFileThrows = true;
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            IOException exception = Assert.Throws<IOException>(() =>
            {
                script.DoString("return os.tmpname()");
            });

            await Assert.That(exception.Message).Contains("temp name failure");
        }

        [global::TUnit.Core.Test]
        public async Task SetLocaleReturnsPlaceholderString()
        {
            StubPlatformAccessor stub = new();
            using ScriptContext context = CreateScriptContext(stub);
            Script script = context.Script;

            DynValue result = script.DoString("return os.setlocale()");

            await Assert.That(result.String).IsEqualTo("n/a");
        }

        private static ScriptContext CreateScriptContext(StubPlatformAccessor stub)
        {
            ArgumentNullException.ThrowIfNull(stub);

            ScriptPlatformScope globalScope = ScriptPlatformScope.Override(stub);
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return new ScriptContext(script, globalScope);
        }

        private readonly struct ScriptContext : IDisposable
        {
            private readonly IDisposable _globalScope;

            internal ScriptContext(Script script, IDisposable globalScope)
            {
                ArgumentNullException.ThrowIfNull(script);
                ArgumentNullException.ThrowIfNull(globalScope);
                Script = script;
                _globalScope = globalScope;
            }

            public Script Script { get; }

            public void Dispose()
            {
                _globalScope.Dispose();
            }
        }

        private sealed class StubPlatformAccessor : IPlatformAccessor
        {
            private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, string> Environment { get; } =
                new(StringComparer.OrdinalIgnoreCase);
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

            public string GetEnvironmentVariable(string envvarname)
            {
                Environment.TryGetValue(envvarname, out string value);
                return value;
            }

            public bool IsRunningOnAOT() => false;

            public string GetPlatformName() => "StubPlatform";

            public void DefaultPrint(string content) { }

            public string DefaultInput(string prompt) => string.Empty;

            public Stream OpenFile(Script script, string filename, Encoding encoding, string mode)
            {
                return new MemoryStream();
            }

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

            public void CreateFile(string fileName)
            {
                _files.Add(fileName);
            }
        }

        private sealed class ExitFastException : Exception
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
}
