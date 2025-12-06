namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    [ScriptGlobalOptionsIsolation]
    [PlatformDetectorIsolation]
    public sealed class StreamFileUserDataBaseTUnitTests
    {
        private static readonly string[] ExpectedWriteSequence = { "A", "B" };

        [global::TUnit.Core.Test]
        public async Task WriteAppendsTextAndReturnsSelf()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed\n");

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString(
                @"
                local f = file
                f:seek('end')
                return f:write('A', 'B')
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.UserData);
            await Assert.That(result.UserData.Object).IsEqualTo(file);
            await Assert.That(file.Writes).IsEquivalentTo(ExpectedWriteSequence);
            await Assert.That(file.GetContent().EndsWith("AB", StringComparison.Ordinal)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task WritereturnsTupleWhenExceptionOccurs()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnWrite = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString(
                @"
                local f = file
                return f:write('boom')
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("write failure");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task WriteRethrowsScriptRuntimeException()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true)
            {
                ThrowScriptRuntimeOnWrite = true,
            };

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("return file:write('boom')");
            });

            await Assert.That(exception.Message).Contains("script write failure");
        }

        [global::TUnit.Core.Test]
        public async Task CloseReturnsTupleWithMessage()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { CloseMessage = "already closed" };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:close()");

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("already closed");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task CloseReturnsTupleWhenExceptionIsThrown()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { ThrowOnClose = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:close()");

            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].String).Contains("close failure");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task CloseRethrowsScriptRuntimeException()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { ThrowScriptRuntimeOnClose = true };

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException ex = AssertScriptRuntimeException(() =>
                script.DoString("return file:close()")
            );
            await Assert.That(ex.Message).Contains("script close failure");
        }

        [global::TUnit.Core.Test]
        public async Task FlushReturnsTrueWhenWriterPresent()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:flush()");

            await Assert.That(result.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task FlushPropagatesExceptionThroughPcall()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnFlush = true };

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException ex = AssertScriptRuntimeException(() =>
                script.DoString("return file:flush()")
            );
            await Assert.That(ex.Message).Contains("flush failure");
        }

        [global::TUnit.Core.Test]
        public async Task FlushWrapsNonScriptExceptions()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true);
            file.TriggerFlushFailure();

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return file:flush()")
            );
            await Assert.That(ex.Message).Contains("flush failure");
        }

        [global::TUnit.Core.Test]
        public async Task IoFlushUsesDefaultOutputAndPropagatesException()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnFlush = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue ioTable = script.Globals.Get("io");
            await Assert.That(ioTable.IsNil()).IsFalse();
            await Assert.That(ioTable.Table.Get("output").IsNil()).IsFalse();
            await Assert.That(ioTable.Table.Get("flush").IsNil()).IsFalse();
            IoModule.SetDefaultFile(script, StandardFileType.StdOut, file);

            DynValue pcall = script.Globals.Get("pcall");
            DynValue flush = ioTable.Table.Get("flush");
            DynValue tuple = script.Call(pcall, flush);

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("flush failure");
        }

        [global::TUnit.Core.Test]
        public async Task SeekSupportsDifferentOrigins()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("abcdef", allowWrite: false);

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local setPos = f:seek('set', 2)
                local char = f:read(1)
                local cur = f:seek()
                local fromEnd = f:seek('end', -1)
                return setPos, char, cur, fromEnd
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(2);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("c");
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(3);
            await Assert.That(tuple.Tuple[3].Number).IsEqualTo(5);
        }

        [global::TUnit.Core.Test]
        public async Task SeekRejectsInvalidWhence()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:seek('bogus', 0)
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("invalid option 'bogus'");
        }

        [global::TUnit.Core.Test]
        public async Task SeekWrapsNonScriptExceptions()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed");
            file.TriggerSeekFailure();

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:seek('set', 0)
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("seek failure");
        }

        [global::TUnit.Core.Test]
        public async Task SeekPropagatesExceptionThroughPcall()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: false) { ThrowOnSeek = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:seek('set', 1)
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("seek failure");
        }

        [global::TUnit.Core.Test]
        public async Task SetvbufWrapsNonScriptExceptions()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true, autoFlush: false);
            script.Globals["file"] = UserData.Create(file);
            script.DoString("file:write('buffer')");
            file.TriggerStreamWriteFailure();

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:setvbuf('line')
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("setvbuf failure");
        }

        [global::TUnit.Core.Test]
        public async Task SetvbufAdjustsAutoFlush()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true, autoFlush: false);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString(
                @"
                local f = file
                local first = f:setvbuf('line')
                local second = f:setvbuf('full')
                return first, second
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(file.WriterAutoFlush).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ClosedFileRejectsFurtherReads()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("line");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                f:close()
                local ok, err = pcall(function()
                    return f:read()
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("attempt to use a closed file");
        }

        [global::TUnit.Core.Test]
        public async Task EofReturnsFalseWhenReaderMissing()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new(
                "seed",
                allowWrite: true,
                autoFlush: true,
                allowRead: false
            );
            await Assert.That(file.CallEof()).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task PeekReturnsNextCharacter()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new("peek");
            await Assert.That(file.CallPeek()).IsEqualTo('p');
        }

        [global::TUnit.Core.Test]
        public async Task SetvbufPropagatesExceptionThroughPcall()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true, autoFlush: false)
            {
                ThrowOnSetvbuf = true,
            };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:setvbuf('line')
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("setvbuf failure");
        }

        [global::TUnit.Core.Test]
        public async Task ReadSupportsLineAndBlockModes()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("first\nsecond");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read(), file:read('*a')");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("first");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second");
        }

        [global::TUnit.Core.Test]
        public async Task ReadSupportsNumericAndAllModes()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("1234\nABCDE");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local num = f:read('*n')
                f:seek('set', 5)
                local rest = f:read(2)
                local all = f:read('*a')
                return num, rest, all
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(1234);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("AB");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("CDE");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesNumbersWithLeadingDecimal()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new(".75 rest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local num = f:read('*n')
                local remainder = f:read('*a')
                return num, remainder
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(0.75d);
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("rest");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesNumbersWithExponent()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("2.5e-1 next");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local num = f:read('*n')
                local tail = f:read('*a')
                return num, tail
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(0.25d);
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("next");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesNumbersWhenStreamCannotRewind()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new(
                "99",
                allowWrite: true,
                autoFlush: true,
                allowRead: true,
                allowSeek: false
            );

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*n')");

            await Assert.That(result.Number).IsEqualTo(99);
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexFloatLiteralWithFraction()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1.fp1 tail");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
                "
            );

            await Assert.That(System.Math.Abs(tuple.Tuple[0].Number - 3.875d) <= 1e-12).IsTrue();
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("tail");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexFloatLiteralWithoutFraction()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x10p0 done");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(16d);
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("done");
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilForInvalidHexFloatExponent()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1p remainder");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
                "
            );

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).StartsWith("0x1p remainder");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexFloatLiteralWithSignedPrefix()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("-0x2p1 rest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(-4d);
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("rest");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexFloatLiteralWithSignedExponent()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1p-4 tail");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
                "
            );

            await Assert
                .That(System.Math.Abs(tuple.Tuple[0].Number - System.Math.Pow(2, -4)) <= 1e-12)
                .IsTrue();
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("tail");
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilWhenHexPrefixStartsWithX()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("x12");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("x12");
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilWhenHexPrefixLacksZeroAfterSign()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("+x12");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("+x12");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexFloatLiteralWithPositiveExponentSign()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1p+4 remainder");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(16d);
            await Assert.That(tuple.Tuple[1].String.TrimStart()).StartsWith("remainder");
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilWhenHexLiteralHasNoDigitsAfterPrefix()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x rest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("0x rest");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesHexLiteralAndLeavesTrailingCharacters()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1p0garbage");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(1d);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("garbage");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesNumbersWithLeadingWhitespace()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("   42\n");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(42d);
            await Assert.That(tuple.Tuple[1].String).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilWhenOnlySignEncountered()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("+ remainder");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
                "
            );

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).StartsWith("+ remainder");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberReturnsNilWhenReaderCannotConsumeChar()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("7\nnext") { ForceReadBufferFailureCount = 1 };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*l')");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("7");
            await Assert.That(file.ForcedReadBufferFailuresTriggered).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberStopsWhenLeadingSignCannotBeConsumed()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("+42") { ForceReadBufferFailureCount = 1 };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
                "
            );

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("+42");
            await Assert.That(file.ForcedReadBufferFailuresTriggered).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ReadLineHandlesMixedNewlines()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("first\r\nsecond\rthird\nlast");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local a = f:read('*l')
                local b = f:read('*l')
                local c = f:read('*l')
                local d = f:read('*l')
                return a, b, c, d
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("first");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second");
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("third");
            await Assert.That(tuple.Tuple[3].String).IsEqualTo("last");
        }

        [global::TUnit.Core.Test]
        public async Task ReadUppercaseLineKeepsTrailingNewLine()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("first\nsecond\n");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local a = f:read('*L')
                local b = f:read('*L')
                return a, b
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("first\n");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("second\n");
        }

        [global::TUnit.Core.Test]
        public async Task ReadToEndAfterLineReadsReturnsRemainingContent()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("line1\nline2\nline3");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local first = f:read('*l')
                local remainder = f:read('*a')
                return first, remainder
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("line1");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("line2\nline3");
        }

        [global::TUnit.Core.Test]
        public async Task ReadAllReturnsEmptyStringWhenAlreadyAtEof()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new(string.Empty);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*a')");

            await Assert.That(result.String).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsNilWhenEofAndModeIsNotAll()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new(string.Empty);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*l')");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberReturnsNilWithoutConsumingNonNumericData()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("abc123");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local first = f:read('*n')
                local remainder = f:read(3)
                return first, remainder
                "
            );

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("abc");
        }

        [global::TUnit.Core.Test]
        public async Task ReadParsesNumbersWithLeadingPlus()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("+42");

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*n')");
            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberHandlesExponentWithSignAndBuffersRemainder()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("   -12.5e-3\nrest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                "local n = file:read('*n'); return n, file:read('*l'), file:read('*l')"
            );

            await Assert.That(System.Math.Abs(tuple.Tuple[0].Number - -12.5e-3) <= 1e-12).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo(string.Empty);
            await Assert.That(tuple.Tuple[2].String).IsEqualTo("rest");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumberReturnsNilForStandaloneSignAndRewinds()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("+\nvalue");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n', '*l')");

            await Assert.That(tuple.Tuple[0].IsNil()).IsTrue();
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("+");
        }

        [global::TUnit.Core.Test]
        public async Task ReadNumericCountReturnsNilWhenEofReached()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("hello");

            script.Globals["file"] = UserData.Create(file);
            script.DoString("file:read('*a')");

            DynValue result = script.DoString("return file:read(4)");

            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ReadThrowsOnUnknownOption()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("payload");

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("file:read('*z')");
            });

            await Assert.That(exception.Message).Contains("invalid option");
        }

        [global::TUnit.Core.Test]
        public async Task ReadReturnsEmptyStringAtEofWithAOption()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("hi");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                local first = f:read('*a')
                local second = f:read('*a')
                return first, second
                "
            );

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("hi");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("");
        }

        [global::TUnit.Core.Test]
        public async Task ReadThrowsOnInvalidOption()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:read('*z')
                end)
                return ok, err
                "
            );

            await Assert.That(tuple.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(tuple.Tuple[0].Boolean).IsFalse();
            await Assert.That(tuple.Tuple[1].String).Contains("invalid option");
        }

        [global::TUnit.Core.Test]
        public async Task LinesEnumeratorTerminatesAtNil()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("one\ntwo\n");

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString(
                @"
                local f = file
                local output = {}
                for line in f:lines() do
                    table.insert(output, line)
                end
                return table.concat(output, ',')
                "
            );

            await Assert.That(result.String).IsEqualTo("one,two");
        }

        [global::TUnit.Core.Test]
        public async Task ToStringTracksOpenAndClosedState()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("contents");

            script.Globals["file"] = UserData.Create(file);

            string beforeClose = file.ToString();

            await Assert.That(beforeClose).StartsWith("file (");
            await Assert.That(beforeClose).DoesNotContain("closed");

            script.DoString("file:close()");

            string afterClose = file.ToString();
            await Assert.That(afterClose).IsEqualTo("file (closed)");
        }

        [global::TUnit.Core.Test]
        public async Task HexPrefixValidationRejectsEmptyBuilder()
        {
            using TestScope scope = InitializeTest();
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder());
            await Assert.That(result).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task HexPrefixValidationAcceptsSignedZero()
        {
            using TestScope scope = InitializeTest();
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder("-0"));
            await Assert.That(result).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task HexPrefixValidationRejectsNonZeroAfterSign()
        {
            using TestScope scope = InitializeTest();
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder("+1"));
            await Assert.That(result).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task TryParseHexFloatLiteralFailsOnEmptyString()
        {
            using TestScope scope = InitializeTest();
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral(string.Empty, out double value);
            await Assert.That(parsed).IsFalse();
            await Assert.That(value).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseHexFloatLiteralFailsWhenNoDigitsAfterPrefix()
        {
            using TestScope scope = InitializeTest();
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x", out double value);
            await Assert.That(parsed).IsFalse();
            await Assert.That(value).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseHexFloatLiteralFailsWhenExponentDigitsMissing()
        {
            using TestScope scope = InitializeTest();
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p", out double value);
            await Assert.That(parsed).IsFalse();
            await Assert.That(value).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseHexFloatLiteralFailsWhenTrailingCharactersRemain()
        {
            using TestScope scope = InitializeTest();
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p0junk", out double value);
            await Assert.That(parsed).IsFalse();
            await Assert.That(value).IsEqualTo(0d);
        }

        [global::TUnit.Core.Test]
        public async Task TryParseHexFloatLiteralParsesPositiveExponentSign()
        {
            using TestScope scope = InitializeTest();
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p+1", out double value);
            await Assert.That(parsed).IsTrue();
            await Assert.That(value).IsEqualTo(2d);
        }

        [global::TUnit.Core.Test]
        public async Task SignAllowancePermitsDecimalExponentSign()
        {
            using TestScope scope = InitializeTest();
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("1e"),
                isHex: false,
                exponentSeen: true,
                exponentHasDigits: false,
                hexExponentSeen: false,
                hexExponentHasDigits: false,
                candidate: '+'
            );

            await Assert.That(allowed).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SignAllowancePermitsHexExponentSign()
        {
            using TestScope scope = InitializeTest();
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("0xp"),
                isHex: true,
                exponentSeen: false,
                exponentHasDigits: false,
                hexExponentSeen: true,
                hexExponentHasDigits: false,
                candidate: '-'
            );

            await Assert.That(allowed).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SignAllowanceRejectsUnexpectedSign()
        {
            using TestScope scope = InitializeTest();
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("12"),
                isHex: false,
                exponentSeen: false,
                exponentHasDigits: false,
                hexExponentSeen: false,
                hexExponentHasDigits: false,
                candidate: '+'
            );

            await Assert.That(allowed).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task StandaloneSignOrDotRejectsLongerStrings()
        {
            using TestScope scope = InitializeTest();
            bool result = FileUserDataBase.IsStandaloneSignOrDot("++");
            await Assert.That(result).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task StandaloneSignOrDotAcceptsDot()
        {
            using TestScope scope = InitializeTest();
            bool result = FileUserDataBase.IsStandaloneSignOrDot(".");
            await Assert.That(result).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task BinaryModePreservesCrlfSequence()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("first\r\nsecond", isBinaryMode: true);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*a')");

            await Assert.That(result.String).IsEqualTo("first\r\nsecond");
        }

        [global::TUnit.Core.Test]
        public async Task BinaryModeReadBufferReturnsRawBytes()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("AB\r\nCD", isBinaryMode: true);

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read(4), file:read('*a')");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("AB\r\n");
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("CD");
        }

        [global::TUnit.Core.Test]
        public async Task PeekRawReturnsNegativeOneWhenReaderIsNull()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new(
                "seed",
                allowWrite: true,
                autoFlush: true,
                allowRead: false
            );

            int peek = file.CallPeekRaw();

            await Assert.That(peek).IsEqualTo(-1);
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeReadChunkHandlesPendingCrFollowedByLf()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new("\nabc");
            file.SetPendingCarriageReturn(true);

            // Call NormalizeReadChunk directly to test the pending CR handling
            string result = file.NormalizeReadChunk("\nabc");

            // On Windows, pending CR + LF in chunk should normalize to just LF
            // On Unix, pending CR is emitted before the chunk
            bool isWindows = Environment.NewLine == "\r\n";
            if (isWindows)
            {
                await Assert.That(result.Length > 0 && result[0] == '\n').IsTrue();
            }
            else
            {
                // On Unix, pending CR is prepended to the chunk
                await Assert.That(result.Length > 0 && result[0] == '\r').IsTrue();
            }
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeReadChunkHandlesPendingCrNotFollowedByLf()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new("abc");
            file.SetPendingCarriageReturn(true);

            // Call NormalizeReadChunk directly with content not starting with LF
            string result = file.NormalizeReadChunk("abc");

            // On Unix (or Windows when chunk doesn't start with LF), pending CR is emitted
            await Assert.That(result.Length > 0 && result[0] == '\r').IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task NormalizeReadChunkEmitsPendingCrAtEof()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new(string.Empty);
            file.SetPendingCarriageReturn(true);

            string normalized = file.CallNormalizeEmptyChunk();

            // On Windows (\r\n newline), pending CR at EOF should be returned
            // On Unix, pending CR is returned regardless
            await Assert.That(normalized == "\r" || normalized.Length == 0).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SupportsRewindReturnsFalseWhenStreamIsNull()
        {
            using TestScope scope = InitializeTest();
            TestStreamFileUserData file = new("seed");
            file.NullifyStream();

            bool supportsRewind = file.CallSupportsRewind();

            await Assert.That(supportsRewind).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task SeekWithCurWhenceUsesCurrentPosition()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new("0123456789");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                local f = file
                f:seek('set', 3)
                local fromCur = f:seek('cur', 2)
                local char = f:read(1)
                return fromCur, char
                "
            );

            await Assert.That(tuple.Tuple[0].Number).IsEqualTo(5);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("5");
        }

        [global::TUnit.Core.Test]
        public async Task WriteDoesNotConvertNewlineInBinaryMode()
        {
            using TestScope scope = InitializeTest();
            Script script = CreateScript();
            TestStreamFileUserData file = new(string.Empty, allowWrite: true, isBinaryMode: true);

            script.Globals["file"] = UserData.Create(file);

            script.DoString("file:write('line1\\nline2')");

            string content = file.GetContent();
            await Assert.That(content).IsEqualTo("line1\nline2");
        }

        private static TestScope InitializeTest()
        {
            UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<TestStreamFileUserData>(ensureUnregistered: true);
            registrationScope.RegisterType<TestStreamFileUserData>();
            PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.ForceDesktopPlatform();
            return new TestScope(platformScope, registrationScope);
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static ScriptRuntimeException AssertScriptRuntimeException(Action action)
        {
            return Assert.Throws<ScriptRuntimeException>(() => ExecuteAndUnwrap(action));
        }

        private static void ExecuteAndUnwrap(Action action)
        {
            try
            {
                action();
                throw new InvalidOperationException(
                    "Expected ScriptRuntimeException, but no exception was thrown."
                );
            }
            catch (Exception ex)
            {
                ScriptRuntimeException scriptException = ExtractScriptRuntimeException(ex);
                if (scriptException != null)
                {
                    throw scriptException;
                }

                throw;
            }
        }

        private static ScriptRuntimeException ExtractScriptRuntimeException(Exception exception)
        {
            while (
                exception is TargetInvocationException invocationException
                && invocationException.InnerException != null
            )
            {
                exception = invocationException.InnerException;
            }

            return exception as ScriptRuntimeException;
        }

        private sealed class TestStreamFileUserData : StreamFileUserDataBase
        {
            private readonly Encoding _encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false
            );
            private int _remainingForcedReadFailures;
            private int _forcedReadBufferFailuresTriggered;

            [SuppressMessage(
                "Reliability",
                "CA2000:Dispose objects before losing scope",
                Justification = "StreamFileUserDataBase owns the created stream and reader/writer instances."
            )]
            internal TestStreamFileUserData(
                string initialContent,
                bool allowWrite = true,
                bool autoFlush = true,
                bool allowRead = true,
                bool allowSeek = true,
                bool isBinaryMode = false
            )
            {
                FaultyMemoryStream stream = null;
                StreamReader reader = null;
                FaultyStreamWriter writer = null;

                using DeferredActionScope cleanupScope = DeferredActionScope.Run(() =>
                {
                    stream?.Dispose();
                    reader?.Dispose();
                    writer?.Dispose();
                });

                stream = new FaultyMemoryStream();
                stream.AllowSeek = allowSeek;

                if (!string.IsNullOrEmpty(initialContent))
                {
                    using StreamWriter seed = new StreamWriter(
                        stream,
                        _encoding,
                        bufferSize: 1024,
                        leaveOpen: true
                    );
                    seed.Write(initialContent);
                    seed.Flush();
                }

                stream.Position = 0;

                if (allowRead)
                {
                    reader = new StreamReader(
                        stream,
                        _encoding,
                        detectEncodingFromByteOrderMarks: false,
                        bufferSize: 1024,
                        leaveOpen: true
                    );
                }

                if (allowWrite)
                {
                    writer = new FaultyStreamWriter(
                        stream,
                        _encoding,
                        bufferSize: 1024,
                        leaveOpen: true
                    )
                    {
                        AutoFlush = autoFlush,
                    };
                }

                Initialize(stream, reader, writer, isBinaryMode);
                cleanupScope.Suppress();
            }

            internal List<string> Writes { get; } = new();

            internal bool ThrowOnWrite { get; set; }

            internal bool ThrowScriptRuntimeOnWrite { get; set; }

            internal bool ThrowOnClose { get; set; }

            internal bool ThrowScriptRuntimeOnClose { get; set; }

            internal bool ThrowOnFlush { get; set; }

            internal bool ThrowOnSeek { get; set; }

            internal bool ThrowOnSetvbuf { get; set; }

            internal string CloseMessage { get; set; }

            internal int ForceReadBufferFailureCount
            {
                get => _remainingForcedReadFailures;
                set => _remainingForcedReadFailures = value;
            }

            internal int ForcedReadBufferFailuresTriggered => _forcedReadBufferFailuresTriggered;

            internal bool WriterAutoFlush =>
                StreamWriterInstance != null && StreamWriterInstance.AutoFlush;

            internal string GetContent()
            {
                FaultyMemoryStream stream = InnerStream;
                long position = stream.Position;
                stream.Position = 0;

                using StreamReader snapshot = new StreamReader(
                    stream,
                    _encoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true
                );

                string text = snapshot.ReadToEnd();
                stream.Position = position;
                return text;
            }

            protected override void Write(string value)
            {
                if (ThrowOnWrite)
                {
                    throw new InvalidOperationException("write failure");
                }

                if (ThrowScriptRuntimeOnWrite)
                {
                    throw new ScriptRuntimeException("script write failure");
                }

                Writes.Add(value);
                base.Write(value);
            }

            protected override string ReadBuffer(int p)
            {
                if (_remainingForcedReadFailures > 0 && p == 1)
                {
                    _remainingForcedReadFailures--;
                    _forcedReadBufferFailuresTriggered++;
                    return string.Empty;
                }

                return base.ReadBuffer(p);
            }

            protected override string Close()
            {
                if (ThrowOnClose)
                {
                    throw new IOException("close failure");
                }

                if (ThrowScriptRuntimeOnClose)
                {
                    throw new ScriptRuntimeException("script close failure");
                }

                string message = CloseMessage;
                string baseResult = base.Close();
                return string.IsNullOrEmpty(message) ? baseResult : message;
            }

            internal bool CallEof()
            {
                return Eof();
            }

            internal char CallPeek()
            {
                return Peek();
            }

            public override bool Flush()
            {
                if (ThrowOnFlush)
                {
                    throw new ScriptRuntimeException("flush failure");
                }

                return base.Flush();
            }

            public override long Seek(string whence, long offset = 0)
            {
                if (ThrowOnSeek)
                {
                    throw new ScriptRuntimeException("seek failure");
                }

                return base.Seek(whence, offset);
            }

            public override bool Setvbuf(string mode)
            {
                if (ThrowOnSetvbuf)
                {
                    throw new ScriptRuntimeException("setvbuf failure");
                }

                return base.Setvbuf(mode);
            }

            internal void TriggerFlushFailure()
            {
                FaultyStreamWriter writer =
                    InnerWriter ?? throw new InvalidOperationException("Writer is not available.");
                writer.ThrowOnFlush = true;
            }

            internal void TriggerSeekFailure()
            {
                InnerStream.ThrowOnSeek = true;
            }

            internal void ReplaceWriterWithDisposedInstance()
            {
                FaultyStreamWriter disposed = new FaultyStreamWriter(
                    InnerStream,
                    _encoding,
                    bufferSize: 1024,
                    leaveOpen: true
                );
                disposed.Dispose();
                StreamWriterInstance = disposed;
            }

            internal void DisposeUnderlyingStream()
            {
                InnerStream.Dispose();
            }

            internal void TriggerStreamWriteFailure()
            {
                InnerStream.ThrowOnWrite = true;
            }

            internal int CallPeekRaw()
            {
                return PeekRaw();
            }

            internal void SetPendingCarriageReturn(bool value)
            {
                _pendingCarriageReturnOnRead = value;
            }

            internal string CallNormalizeEmptyChunk()
            {
                // Call NormalizeReadChunk with an empty string to test the pending CR at EOF path
                return NormalizeReadChunk(string.Empty);
            }

            internal void NullifyStream()
            {
                _streamInstance = null;
            }

            internal bool CallSupportsRewind()
            {
                return SupportsRewind;
            }

            private FaultyMemoryStream InnerStream => (FaultyMemoryStream)StreamInstance;

            private FaultyStreamWriter InnerWriter => StreamWriterInstance as FaultyStreamWriter;
        }

        private sealed class FaultyMemoryStream : MemoryStream
        {
            internal bool AllowSeek { get; set; } = true;
            internal bool ThrowOnSeek { get; set; }
            internal bool ThrowOnWrite { get; set; }

            public override bool CanSeek => AllowSeek && base.CanSeek;

            public override long Seek(long offset, SeekOrigin loc)
            {
                if (ThrowOnSeek)
                {
                    ThrowOnSeek = false;
                    throw new IOException("seek failure");
                }

                return base.Seek(offset, loc);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (ThrowOnWrite)
                {
                    ThrowOnWrite = false;
                    throw new IOException("setvbuf failure");
                }

                base.Write(buffer, offset, count);
            }
        }

        private sealed class FaultyStreamWriter : StreamWriter
        {
            internal FaultyStreamWriter(
                Stream stream,
                Encoding encoding,
                int bufferSize,
                bool leaveOpen
            )
                : base(stream, encoding, bufferSize, leaveOpen) { }

            internal bool ThrowOnFlush { get; set; }

            public override void Flush()
            {
                if (ThrowOnFlush)
                {
                    ThrowOnFlush = false;
                    throw new IOException("flush failure");
                }

                base.Flush();
            }
        }

        private sealed class TestScope : IDisposable
        {
            private readonly PlatformDetectorOverrideScope _platformScope;
            private readonly UserDataRegistrationScope _registrationScope;

            internal TestScope(
                PlatformDetectorOverrideScope platformScope,
                UserDataRegistrationScope registrationScope
            )
            {
                _platformScope = platformScope;
                _registrationScope = registrationScope;
            }

            public void Dispose()
            {
                _registrationScope?.Dispose();
                _platformScope?.Dispose();
            }
        }
    }
}
