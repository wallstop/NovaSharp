namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.IO;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;

    [ScriptGlobalOptionsIsolation]
    public sealed class IoModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task OpenReturnsNilTupleWhenFileDoesNotExist()
        {
            Script script = CreateScript();
            string path = EscapePath(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt"));

            DynValue result = script.DoString($"return io.open('{path}', 'r')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("No such file");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberConsumesMultipleLinesFromStdin()
        {
            string stdinContent = "6.0     -3.23   15e12\n4.3     234     1000001\n";
            using MemoryStream stdinStream = new(
                Encoding.UTF8.GetBytes(stdinContent),
                writable: false
            );

            ScriptOptions options = new(Script.DefaultOptions) { Stdin = stdinStream };
            List<string> outputs = new();
            options.DebugPrint = message =>
            {
                outputs.Add(message ?? string.Empty);
            };

            Script script = new(CoreModules.PresetComplete, options);
            string chunk =
                @"
while true do
    local n1, n2, n3 = io.read('*number', '*number', '*number')
    if not n1 then
        break
    end

    print(math.max(n1, n2, n3))
end
";
            script.DoString(chunk);

            await Assert.That(outputs.Count).IsEqualTo(2);
            await Assert.That(outputs[0]).IsEqualTo("15000000000000");
            await Assert.That(outputs[1]).IsEqualTo("1000001");
        }

        [global::TUnit.Core.Test]
        public async Task OpenThrowsForInvalidMode()
        {
            Script script = CreateScript();
            string path = EscapePath(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt"));

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString($"return io.open('{path}', 'z')");
            });

            await Assert.That(exception.Message).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        public async Task TypeReportsClosedFileAfterClose()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue tuple = script.DoString(
                $@"
                local f = io.open('{path}', 'w')
                local openType = io.Type(f)
                f:close()
                return openType, io.Type(f)
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        public async Task InputReadsFromReassignedDefaultStream()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateWithText("first\nsecond\n");
            string path = temp.EscapedPath;

            DynValue read = script.DoString(
                $@"
                io.input('{path}')
                return io.read('*l')
                "
            );

            await Assert.That(read.String).IsEqualTo("first");
        }

        [global::TUnit.Core.Test]
        public async Task OutputWritesToReassignedDefaultStream()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            script.DoString(
                $@"
                local f = io.open('{path}', 'w')
                io.output(f)
                io.write('abc', '123')
                io.output():close()
                "
            );

            string content = await File.ReadAllTextAsync(temp.FilePath).ConfigureAwait(false);
            await Assert.That(content).IsEqualTo("abc123");
        }

        [global::TUnit.Core.Test]
        public async Task TmpFileCreatesWritableFile()
        {
            Script script = CreateScript();

            DynValue typeValue = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp-data')
                return io.Type(f)
                "
            );

            await Assert.That(typeValue.Type).IsEqualTo(DataType.String);
            await Assert.That(typeValue.String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberWithMissingExponentDigitsReturnsNilAndLeavesStreamIntact()
        {
            DynValue tuple = await ReadNumberFromContent("123e").ConfigureAwait(false);

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Nil);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("123e");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberParsesHexLiteralInput()
        {
            DynValue tuple = await ReadNumberFromContent("0x1p2\n").ConfigureAwait(false);

            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(4d);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("\n");
        }

        [global::TUnit.Core.Test]
        public async Task ReadLineWithStarLIncludesTrailingNewline()
        {
            using TempFileScope temp = TempFileScope.CreateWithText(
                "file with text\nsecond line\n"
            );
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local first = f:read('*L')
                    local second = f:read('*L')
                    f:close()
                    return first, second
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file with text\n");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second line\n");
        }

        [global::TUnit.Core.Test]
        public async Task ReadZeroBytesDoesNotAdvanceStream()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdef");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local zero = f:read(0)
                    local chunk = f:read(3)
                    local remainder = f:read('*a')
                    f:close()
                    return zero, chunk, remainder
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo(string.Empty);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("abc");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("def");
        }

        [global::TUnit.Core.Test]
        public async Task ReadMultipleFixedLengthsReturnsExpectedChunks()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdefghijklmnop");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local first, second, third = f:read(4, 4, 4)
                    f:close()
                    return first, second, third
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("abcd");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("efgh");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("ijkl");
        }

        [global::TUnit.Core.Test]
        public async Task ReadOnClosedHandleThrows()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("content");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString(
                    $@"
                        local f = assert(io.open('{escapedPath}', 'r'))
                        f:close()
                        f:read('*l')
                        "
                );
            });

            await Assert.That(exception.Message).Contains("attempt to use a closed file");
        }

        [global::TUnit.Core.Test]
        public async Task SeekInvalidOptionRaisesError()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("content");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString(
                    $@"
                        local f = assert(io.open('{escapedPath}', 'r'))
                        f:seek('bad', 0)
                        "
                );
            });

            await Assert.That(exception.Message).Contains("invalid option 'bad'");
        }

        [global::TUnit.Core.Test]
        public async Task SeekReturnsFileLength()
        {
            const string content = "file with text\n";
            using TempFileScope temp = TempFileScope.CreateWithText(content);
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue result = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local size = f:seek('end', 0)
                    f:close()
                    return size
                    "
            );

            await Assert.That(result.Number).IsEqualTo(content.Length);
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberReturnsInfinityForHugeExponent()
        {
            DynValue tuple = await ReadNumberFromContent("1e400").ConfigureAwait(false);

            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
            await Assert.That(double.IsPositiveInfinity(tuple.Tuple[0].Number)).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberParsesHugeInteger()
        {
            const string literal = "123456789012345678901234567890\n";
            DynValue tuple = await ReadNumberFromContent(literal).ConfigureAwait(false);
            double expected = double.Parse(
                "123456789012345678901234567890",
                CultureInfo.InvariantCulture
            );

            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(expected);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("\n");
        }

        [global::TUnit.Core.Test]
        public async Task StdStreamsAreAccessibleViaProperties()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                "return io.stdin ~= nil, io.stdout ~= nil, io.stderr ~= nil, io.unknown == nil"
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[2].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[3].Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetDefaultFileOverridesStdInStream()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("override\n"));

            IoModule.SetDefaultFile(script, StandardFileType.StdIn, stream);

            DynValue result = script.DoString("return io.read('*l')");

            await Assert.That(result.String).IsEqualTo("override");
        }

        [global::TUnit.Core.Test]
        public async Task SetDefaultFileOverridesStdOutStream()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream();

            IoModule.SetDefaultFile(script, StandardFileType.StdOut, stream);

            script.DoString("io.write('buffered'); io.flush()");

            string content = Encoding.UTF8.GetString(stream.ToArray());
            await Assert.That(content).IsEqualTo("buffered");
        }

        [global::TUnit.Core.Test]
        public async Task SetDefaultFileThrowsWhenScriptNull()
        {
            using MemoryStream stream = new MemoryStream();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                IoModule.SetDefaultFile(null, StandardFileType.StdIn, stream);
            });

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        public async Task LinesWithoutArgumentsReadFromDefaultInput()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("alpha\nbeta\n"));

            IoModule.SetDefaultFile(script, StandardFileType.StdIn, stream);

            DynValue tuple = script.DoString(
                @"
                local results = {}
                for line in io.lines() do
                    table.insert(results, line)
                    if #results == 3 then break end
                end
                return results[1], results[2], results[3]
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("alpha");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("beta");
            await Assert.That(tuple.Tuple[2].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IoReadUsesDefaultInputWhenNoFileProvided()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("alpha\nbeta\n"));

            IoModule.SetDefaultFile(script, StandardFileType.StdIn, stream);

            DynValue tuple = script.DoString(
                @"
                local first = io.read('*l')
                local second = io.read('*l')
                local eof = io.read('*l')
                return first, second, eof
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("alpha");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("beta");
            await Assert.That(tuple.Tuple[2].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task OpenSupportsBinaryEncodingParameter()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateWithBytes(new byte[] { 0x41, 0x42 });
            string path = temp.EscapedPath;

            DynValue result = script.DoString(
                $@"
                local f = assert(io.open('{path}', 'rb', 'binary'))
                local data = f:read('*a')
                f:close()
                return data
                "
            );

            await Assert.That(result.String).IsEqualTo("AB");
        }

        [global::TUnit.Core.Test]
        public async Task OpenThrowsWhenModeEmpty()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString($"return io.open('{path}', \"\")");
            });

            await Assert.That(exception.Message).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        public async Task OpenReturnsErrorWhenEncodingSpecifiedForBinaryMode()
        {
            Script script = CreateScript();
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', 'rb', 'utf-8')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("Can't specify encodings");
        }

        [global::TUnit.Core.Test]
        public async Task TypeReturnsNilForNonUserData()
        {
            Script script = CreateScript();

            DynValue result = script.DoString("return io.Type(123)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TypeReturnsNilForNonUserDataArguments()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.Type(42), io.Type({})");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TypeReturnsNilForNonFileUserData()
        {
            Script script = CreateScript();
            script.Globals["sampleUserData"] = UserData.Create(new SampleUserData());

            DynValue result = script.DoString("return io.Type(sampleUserData)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IoExceptionToLuaMessageThrowsWhenExceptionNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                IoModule.IoExceptionToLuaMessage(null, "file.txt");
            });

            await Assert.That(exception.ParamName).IsEqualTo("ex");
        }

        [global::TUnit.Core.Test]
        public async Task IoExceptionToLuaMessageReturnsExceptionMessageWhenNotFileNotFound()
        {
            const string message = "access denied";
            string result = IoModule.IoExceptionToLuaMessage(
                new UnauthorizedAccessException(message),
                "file.txt"
            );

            await Assert.That(result).IsEqualTo(message);
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberParsesHexVariants()
        {
            (string Literal, double Expected, string Remainder)[] cases =
            {
                ("0x1p1\n", 2d, "\n"),
                ("0x1.8p1\n", 3d, "\n"),
                ("0X.Cp+2\n", 3d, "\n"),
                ("-0x1p1\n", -2d, "\n"),
                ("+0xAp-1\n", 5d, "\n"),
            };

            foreach ((string literal, double expected, string remainder) in cases)
            {
                DynValue tuple = await ReadNumberFromContent(literal).ConfigureAwait(false);

                await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
                await Assert.That(tuple.Tuple[0].Number).IsEqualTo(expected);
                await Assert.That(tuple.Tuple[1].String).IsEqualTo(remainder);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CloseClosesExplicitFileHandle()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    local result = io.close(f)
                    return result, io.Type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        public async Task CloseWithoutParameterUsesCurrentOutput()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    local closed = io.close()
                    return closed, io.Type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        public async Task FlushReturnsTrueForCurrentOutput()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue result = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    io.write('buffered')
                    local ok = io.flush()
                    io.output():close()
                    return ok
                    "
            );

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task InputReturnsCurrentFileWhenNoArguments()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("data");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    io.input(f)
                    local current = io.input()
                    return io.Type(current), io.Type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        public async Task LinesIteratesOverFileContent()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("alpha\nbeta\ngamma\n");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local iter = io.lines('{escapedPath}')
                    return iter(), iter(), iter(), iter()
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("alpha");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("beta");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("gamma");
            await Assert.That(tuple.Tuple[3].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task LinesRaisesUsefulMessageWhenFileMissing()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function() return io.lines('missing-file.txt') end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("No such file");
        }

        [global::TUnit.Core.Test]
        public async Task CloseStdErrReturnsErrorTuple()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.close(io.stderr)");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).Contains("standard file");
        }

        [global::TUnit.Core.Test]
        public async Task StdErrMethodCloseReturnsErrorTuple()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.stderr:close()");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).Contains("standard file");
        }

        [global::TUnit.Core.Test]
        public async Task StdErrFlushReturnsTrue()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return io.stderr:flush()");
            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TypeReportsClosedFileState()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abc");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local before = io.type(f)
                    f:close()
                    local after = io.type(f)
                    return before, after
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        public async Task SetBufferingModesReturnTrue()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    local noop = f:setvbuf('no')
                    local full = f:setvbuf('full', 128)
                    local line = f:setvbuf('line', 64)
                    f:close()
                    return noop, full, line
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[2].Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task WriteReturnsHandleAndClosedHandleWriteThrows()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    local returned = f:write('payload')
                    f:close()
                    local ok, err = pcall(function() f:write('more') end)
                    return returned == f, ok, err
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[2].String).Contains("attempt to use a closed file");
        }

        [global::TUnit.Core.Test]
        public async Task OpenReturnsErrorTupleForUnknownEncoding()
        {
            string escapedPath = EscapePath(
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt")
            );

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                local file, message = io.open('{escapedPath}', 'w', 'does-not-exist')
                return file, message
                "
            );

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).Contains("does-not-exist");
        }

        [global::TUnit.Core.Test]
        public async Task OpenSupportsExplicitEncoding()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            script.DoString(
                $@"
                local f = assert(io.open('{escapedPath}', 'w', 'utf-16'))
                f:write('hello')
                f:close()
                "
            );

            string content = await File.ReadAllTextAsync(temp.FilePath).ConfigureAwait(false);
            await Assert.That(content).Contains("hello");
        }

        [global::TUnit.Core.Test]
        public async Task OpenRejectsEncodingWhenBinaryModeSpecified()
        {
            string escapedPath = EscapePath(
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt")
            );

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                local ok, res1, res2 = pcall(function()
                    return io.open('{escapedPath}', 'wb', 'utf-8')
                end)
                return ok, res1, res2
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[2].String).Contains("Can't specify encodings");
        }

        [global::TUnit.Core.Test]
        public async Task TmpFileCreatesWritableStream()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp data')
                f:seek('set')
                local t_open = io.Type(f)
                f:close()
                local t_closed = io.Type(f)
                return t_open, t_closed
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        public async Task OpenFileInvokesPlatformAccessorAndStillWritesToDisk()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.FilePath;
            string escapedPath = temp.EscapedPath;
            RecordingPlatformAccessor accessor = new(Script.GlobalOptions.Platform);

            using (new PlatformScope(accessor))
            {
                Script script = CreateScript();
                script.DoString(
                    $@"
                local f = assert(io.open('{escapedPath}', 'w'))
                f:write('hooked payload')
                f:close()
                "
                );
            }

            await Assert.That(accessor.OpenCalls.Count).IsGreaterThan(0);
            await Assert.That(accessor.OpenCalls[0].FileName).IsEqualTo(path);
            await Assert.That(accessor.GetCapturedFileContent(path)).IsEqualTo("hooked payload");
            string diskContents = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            await Assert.That(diskContents).IsEqualTo("hooked payload");
        }

        [global::TUnit.Core.Test]
        public async Task StdOutWritesHonorCustomScriptOptionStream()
        {
            MemoryStream capture = new();
            ScriptOptions options = new ScriptOptions()
            {
                Stdout = new UndisposableStream(capture),
            };

            Script script = new Script(options);
            script.DoString("io.write('brokered output'); io.flush()");

            capture.Position = 0;
            using StreamReader reader = new(
                capture,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true
            );
            string buffered = await reader.ReadToEndAsync().ConfigureAwait(false);
            await Assert.That(buffered).Contains("brokered output");
        }

        [global::TUnit.Core.Test]
        public async Task OutputCanBeRedirectedToCustomFile()
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            script.DoString(
                $@"
                    local original = io.output()
                    local temp = assert(io.open('{escapedPath}', 'w'))
                    io.output(temp)
                    io.write('hello world')
                    io.flush()
                    io.output(original)
                    temp:close()
                    "
            );

            string contents = await File.ReadAllTextAsync(temp.FilePath).ConfigureAwait(false);
            await Assert.That(contents).IsEqualTo("hello world");
        }

        [global::TUnit.Core.Test]
        public async Task PopenIsUnsupportedAndProvidesErrorMessage()
        {
            Script script = CreateScript();
            DynValue typeValue = script.DoString("return type(io.popen)");
            await Assert.That(typeValue.String).IsEqualTo("function");

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function() return io.popen('echo hello') end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("io.popen is not supported");
        }

        [global::TUnit.Core.Test]
        public async Task LinesMethodIteratesOverHandle()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("first\nsecond\nthird\n");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local out = {{}}
                    for line in f:lines() do
                        out[#out + 1] = line
                    end
                    return out[1], out[2], out[3], io.Type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("first");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("third");
            await Assert.That(tuple.Tuple[3].String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        public async Task LinesMethodSupportsReadOptions()
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdef");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local chunks = {{}}
                    for chunk in f:lines(2) do
                        chunks[#chunks + 1] = chunk
                        if #chunks == 3 then break end
                    end
                    f:close()
                    return chunks[1], chunks[2], chunks[3]
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("ab");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("cd");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("ef");
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static string EscapePath(string path) =>
            path.Replace("\\", "\\\\", StringComparison.Ordinal);

        private static void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static async Task<DynValue> ReadNumberFromContent(string content)
        {
            using TempFileScope temp = TempFileScope.CreateWithText(content);
            string escapedPath = temp.EscapedPath;
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                $@"
                local f = assert(io.open('{escapedPath}', 'r'))
                io.input(f)
                local number = io.read('*n')
                local remainder = io.read('*a')
                f:close()
                return number, remainder
                "
            );
            return tuple;
        }

        private sealed class SampleUserData { }

        private sealed class PlatformScope : IDisposable
        {
            private readonly IDisposable _scope;
            private readonly IPlatformAccessor _previous;

            internal PlatformScope(IPlatformAccessor replacement)
            {
                _scope = Script.BeginGlobalOptionsScope();
                _previous = Script.GlobalOptions.Platform;
                Script.GlobalOptions.Platform = replacement;
            }

            public void Dispose()
            {
                Script.GlobalOptions.Platform = _previous;
                _scope.Dispose();
            }
        }

        private sealed class RecordingPlatformAccessor : IPlatformAccessor
        {
            private readonly IPlatformAccessor _inner;
            private readonly List<(string FileName, string Mode)> _openCalls = new();
            private readonly Dictionary<string, string> _writtenContent = new(
                StringComparer.OrdinalIgnoreCase
            );

            internal RecordingPlatformAccessor(IPlatformAccessor inner)
            {
                _inner = inner;
            }

            public List<(string FileName, string Mode)> OpenCalls => _openCalls;

            public string GetCapturedFileContent(string path) =>
                _writtenContent.TryGetValue(path, out string content) ? content : string.Empty;

            public CoreModules FilterSupportedCoreModules(CoreModules coreModules) =>
                _inner.FilterSupportedCoreModules(coreModules);

            public string GetEnvironmentVariable(string envvarname) =>
                _inner.GetEnvironmentVariable(envvarname);

            public bool IsRunningOnAOT() => _inner.IsRunningOnAOT();

            public string GetPlatformName() => _inner.GetPlatformName();

            public void DefaultPrint(string content) => _inner.DefaultPrint(content);

            public string DefaultInput(string prompt) => _inner.DefaultInput(prompt);

            public Stream OpenFile(Script script, string filename, Encoding encoding, string mode)
            {
                _openCalls.Add((filename, mode ?? string.Empty));

                Stream innerStream = _inner.OpenFile(script, filename, encoding, mode);

                if (!innerStream.CanWrite)
                {
                    return innerStream;
                }

                return new RecordingStream(innerStream, data => _writtenContent[filename] = data);
            }

            public Stream GetStandardStream(StandardFileType type) =>
                _inner.GetStandardStream(type);

            public string GetTempFileName() => _inner.GetTempFileName();

            public void ExitFast(int exitCode) => _inner.ExitFast(exitCode);

            public bool FileExists(string file) => _inner.FileExists(file);

            public void DeleteFile(string file) => _inner.DeleteFile(file);

            public void MoveFile(string src, string dst) => _inner.MoveFile(src, dst);

            public int ExecuteCommand(string cmdline) => _inner.ExecuteCommand(cmdline);

            private sealed class RecordingStream : Stream
            {
                private readonly Stream _inner;
                private readonly Action<string> _onFlush;
                private readonly MemoryStream _buffer = new();

                internal RecordingStream(Stream inner, Action<string> onFlush)
                {
                    _inner = inner;
                    _onFlush = onFlush;
                }

                public override bool CanRead => _inner.CanRead;
                public override bool CanSeek => _inner.CanSeek;
                public override bool CanWrite => _inner.CanWrite;
                public override long Length => _inner.Length;

                public override long Position
                {
                    get => _inner.Position;
                    set => _inner.Position = value;
                }

                public override void Flush()
                {
                    _inner.Flush();
                    _onFlush(Encoding.UTF8.GetString(_buffer.ToArray()));
                }

                public override int Read(byte[] buffer, int offset, int count) =>
                    _inner.Read(buffer, offset, count);

                public override long Seek(long offset, SeekOrigin origin) =>
                    _inner.Seek(offset, origin);

                public override void SetLength(long value) => _inner.SetLength(value);

                public override void Write(byte[] buffer, int offset, int count)
                {
                    _inner.Write(buffer, offset, count);
                    _buffer.Write(buffer, offset, count);
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _buffer.Dispose();
                        _inner.Dispose();
                    }

                    base.Dispose(disposing);
                }
            }
        }

        private sealed class TempFileScope : IDisposable
        {
            private TempFileScope(string path)
            {
                FilePath = path;
            }

            internal string FilePath { get; }

            internal string EscapedPath => EscapePath(FilePath);

            public static TempFileScope CreateEmpty() => CreateWithText(string.Empty);

            public static TempFileScope CreateWithText(string content, Encoding encoding = null)
            {
                string path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"{Guid.NewGuid():N}.txt"
                );
                Encoding writerEncoding =
                    encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                File.WriteAllText(path, content ?? string.Empty, writerEncoding);
                return new TempFileScope(path);
            }

            public static TempFileScope CreateWithBytes(byte[] data)
            {
                string path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"{Guid.NewGuid():N}.bin"
                );
                File.WriteAllBytes(path, data ?? Array.Empty<byte>());
                return new TempFileScope(path);
            }

            public void Dispose()
            {
                DeleteFileIfExists(FilePath);
            }
        }
    }
}
