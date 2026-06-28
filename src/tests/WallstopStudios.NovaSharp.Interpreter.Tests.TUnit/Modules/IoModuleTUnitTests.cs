namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.IO;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure.IO;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [ScriptGlobalOptionsIsolation]
    public sealed class IoModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenReturnsNilTupleWhenFileDoesNotExist(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            using TempFileScope missingFileScope = TempFileScope.Create(extension: ".txt");
            string path = missingFileScope.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', 'r')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("No such file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberConsumesMultipleLinesFromStdin(LuaCompatibilityVersion version)
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

            Script script = new(CoreModulePresets.Complete, options);
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

        // Lua 5.2+ throws for invalid mode
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenThrowsForInvalidModeInLua52Plus(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);
            using TempFileScope missingFileScope = TempFileScope.Create(extension: ".txt");
            string path = missingFileScope.EscapedPath;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString($"return io.open('{path}', 'z')");
            });

            await Assert.That(exception.Message).Contains("invalid mode");
        }

        // Lua 5.1 returns (nil, error_message) for invalid mode
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenReturnsErrorTupleForInvalidModeInLua51(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua51);
            using TempFileScope tempScope = TempFileScope.Create(extension: ".txt");
            string path = tempScope.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', 'z')");

            await Assert.That(result.Tuple.Length).IsGreaterThan(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("invalid mode")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeReportsClosedFileAfterClose(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue tuple = script.DoString(
                $@"
                local f = io.open('{path}', 'w')
                local openType = io.type(f)
                f:close()
                return openType, io.type(f)
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task InputReadsFromReassignedDefaultStream(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OutputWritesToReassignedDefaultStream(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TmpFileCreatesWritableFile(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue typeValue = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp-data')
                return io.type(f)
                "
            );

            await Assert.That(typeValue.Type).IsEqualTo(DataType.String);
            await Assert.That(typeValue.String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberWithMissingExponentDigitsReturnsNilAndLeavesStreamIntact(
            LuaCompatibilityVersion version
        )
        {
            DynValue tuple = await ReadNumberFromContent("123e").ConfigureAwait(false);

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Nil);
            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("123e");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberParsesHexLiteralInput(LuaCompatibilityVersion version)
        {
            DynValue tuple = await ReadNumberFromContent("0x1p2\n").ConfigureAwait(false);

            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(4d);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("\n");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IoStdinExposesFileUserDataHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue stdinHandle = script.DoString("return io.stdin");

            await Assert.That(stdinHandle.Type).IsEqualTo(DataType.UserData);
            await Assert.That(stdinHandle.UserData.Object).IsTypeOf<FileUserDataBase>();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NumericIndexOnFileHandleReturnsNil(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return io.stdin[1]");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadLineWithStarLIncludesTrailingNewline(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText(
                "file with text\nsecond line\n"
            );
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadZeroBytesDoesNotAdvanceStream(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdef");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadMultipleFixedLengthsReturnsExpectedChunks(
            LuaCompatibilityVersion version
        )
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdefghijklmnop");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadOnClosedHandleThrows(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("content");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SeekInvalidOptionRaisesError(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("content");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SeekReturnsFileLength(LuaCompatibilityVersion version)
        {
            const string content = "file with text\n";
            using TempFileScope temp = TempFileScope.CreateWithText(content);
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberReturnsInfinityForHugeExponent(LuaCompatibilityVersion version)
        {
            DynValue tuple = await ReadNumberFromContent("1e400").ConfigureAwait(false);

            await Assert.That(tuple.Tuple[0].Type).IsEqualTo(DataType.Number);
            await Assert.That(double.IsPositiveInfinity(tuple.Tuple[0].Number)).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberParsesHugeInteger(LuaCompatibilityVersion version)
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StdStreamsAreAccessibleViaProperties(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue tuple = script.DoString(
                "return io.stdin ~= nil, io.stdout ~= nil, io.stderr ~= nil, io.unknown == nil"
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[2].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[3].Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetDefaultFileOverridesStdInStream(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("override\n"));

            IoModule.SetDefaultFile(script, StandardFileType.StdIn, stream);

            DynValue result = script.DoString("return io.read('*l')");

            await Assert.That(result.String).IsEqualTo("override");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetDefaultFileOverridesStdOutStream(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            using MemoryStream stream = new MemoryStream();

            IoModule.SetDefaultFile(script, StandardFileType.StdOut, stream);

            script.DoString("io.write('buffered'); io.flush()");

            string content = Encoding.UTF8.GetString(stream.ToArray());
            await Assert.That(content).IsEqualTo("buffered");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetDefaultFileThrowsWhenScriptNull(LuaCompatibilityVersion version)
        {
            using MemoryStream stream = new MemoryStream();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                IoModule.SetDefaultFile(null, StandardFileType.StdIn, stream);
            });

            await Assert.That(exception.ParamName).IsEqualTo("script");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LinesWithoutArgumentsReadFromDefaultInput(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IoReadUsesDefaultInputWhenNoFileProvided(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenSupportsBinaryEncodingParameter(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenThrowsWhenModeEmptyLua52Plus(LuaCompatibilityVersion version)
        {
            // Lua 5.2+ throws "bad argument #2 to 'open' (invalid mode)" for empty mode
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString($"return io.open('{path}', \"\")");
            });

            await Assert.That(exception.Message).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OpenReturnsNilWhenModeEmptyLua51(LuaCompatibilityVersion version)
        {
            // Lua 5.1 returns (nil, error_message) for empty mode instead of throwing
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', \"\")");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenThrowsForInvalidModeCharacterLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.2+ throws "bad argument #2 to 'open' (invalid mode)" for invalid mode characters
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString($"return io.open('{path}', 'x')");
            });

            await Assert.That(exception.Message).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task OpenReturnsNilForInvalidModeCharacterLua51(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.1 returns (nil, error_message) for invalid mode characters
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', 'x')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("invalid mode");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenReturnsErrorWhenEncodingSpecifiedForBinaryMode(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.EscapedPath;

            DynValue result = script.DoString($"return io.open('{path}', 'rb', 'utf-8')");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("Can't specify encodings");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeReturnsNilForNonUserData(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return io.type(123)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeReturnsNilForNonUserDataArguments(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString("return io.type(42), io.type({})");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeReturnsNilForNonFileUserData(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            script.Globals["sampleUserData"] = UserData.Create(new SampleUserData());

            DynValue result = script.DoString("return io.type(sampleUserData)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IoExceptionToLuaMessageThrowsWhenExceptionNull(
            LuaCompatibilityVersion version
        )
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                IoModule.IoExceptionToLuaMessage(null, "file.txt");
            });

            await Assert.That(exception.ParamName).IsEqualTo("ex");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IoExceptionToLuaMessageReturnsExceptionMessageWhenNotFileNotFound(
            LuaCompatibilityVersion version
        )
        {
            const string message = "access denied";
            string result = IoModule.IoExceptionToLuaMessage(
                new UnauthorizedAccessException(message),
                "file.txt"
            );

            await Assert.That(result).IsEqualTo(message);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ReadNumberParsesHexVariants(LuaCompatibilityVersion version)
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseClosesExplicitFileHandle(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    local result = io.close(f)
                    return result, io.type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseWithoutParameterUsesCurrentOutput(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    local closed = io.close()
                    return closed, io.type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FlushReturnsTrueForCurrentOutput(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task InputReturnsCurrentFileWhenNoArguments(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("data");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    io.input(f)
                    local current = io.input()
                    return io.type(current), io.type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LinesIteratesOverFileContent(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("alpha\nbeta\ngamma\n");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LinesRaisesUsefulMessageWhenFileMissing(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseStdErrReturnsErrorTuple(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString("return io.close(io.stderr)");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).Contains("standard file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StdErrMethodCloseReturnsErrorTuple(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString("return io.stderr:close()");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).Contains("standard file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StdErrFlushReturnsTrue(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue result = script.DoString("return io.stderr:flush()");
            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TypeReportsClosedFileState(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abc");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SetBufferingModesReturnTrue(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WriteReturnsHandleAndClosedHandleWriteThrows(
            LuaCompatibilityVersion version
        )
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenReturnsErrorTupleForUnknownEncoding(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.Create(extension: ".txt");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenSupportsExplicitEncoding(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenRejectsEncodingWhenBinaryModeSpecified(
            LuaCompatibilityVersion version
        )
        {
            using TempFileScope temp = TempFileScope.Create(extension: ".txt");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TmpFileCreatesWritableStream(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp data')
                f:seek('set')
                local t_open = io.type(f)
                f:close()
                local t_closed = io.type(f)
                return t_open, t_closed
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("file");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("closed file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OpenFileInvokesPlatformAccessorAndStillWritesToDisk(
            LuaCompatibilityVersion version
        )
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string path = temp.FilePath;
            string escapedPath = temp.EscapedPath;
            RecordingPlatformAccessor accessor = new(Script.GlobalOptions.Platform);

            using (ScriptPlatformScope platformScope = ScriptPlatformScope.Override(accessor))
            {
                Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StdOutWritesHonorCustomScriptOptionStream(LuaCompatibilityVersion version)
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OutputCanBeRedirectedToCustomFile(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateEmpty();
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PopenIsUnsupportedAndProvidesErrorMessage(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LinesMethodIteratesOverHandle(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("first\nsecond\nthird\n");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
            DynValue tuple = script.DoString(
                $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    local out = {{}}
                    for line in f:lines() do
                        out[#out + 1] = line
                    end
                    return out[1], out[2], out[3], io.type(f)
                    "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("first");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("third");
            await Assert.That(tuple.Tuple[3].String).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LinesMethodSupportsReadOptions(LuaCompatibilityVersion version)
        {
            using TempFileScope temp = TempFileScope.CreateWithText("abcdef");
            string escapedPath = temp.EscapedPath;

            Script script = CreateScriptWithVersion(version);
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
            Script script = new Script(CoreModulePresets.Complete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            Script script = new Script(CoreModulePresets.Complete, options);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static string EscapePath(string path) =>
            path.Replace("\\", "\\\\", StringComparison.Ordinal);

        private static async Task<DynValue> ReadNumberFromContent(string content)
        {
            await Task.CompletedTask.ConfigureAwait(false);
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
    }
}
