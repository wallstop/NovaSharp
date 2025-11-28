namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [UserDataIsolation]
    [ScriptGlobalOptionsIsolation]
    [PlatformDetectorIsolation]
    public sealed class StreamFileUserDataBaseTests
    {
        private static readonly string[] ExpectedWriteSequence = { "A", "B" };
        private bool? _previousUnityDetectionOverride;

        [SetUp]
        public void RegisterUserData()
        {
            _previousUnityDetectionOverride =
                PlatformAutoDetector.TestHooks.GetUnityDetectionOverride();
            ForceDesktopPlatform();
            UserData.RegisterType<TestStreamFileUserData>();
        }

        [TearDown]
        public void RestoreUnityDetectionOverride()
        {
            PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(
                _previousUnityDetectionOverride
            );
        }

        [Test]
        public void WriteAppendsTextAndReturnsSelf()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.UserData));
                Assert.That(result.UserData.Object, Is.SameAs(file));
                Assert.That(file.Writes, Is.EquivalentTo(ExpectedWriteSequence));
                Assert.That(file.GetContent().EndsWith("AB", StringComparison.Ordinal), Is.True);
            });
        }

        [Test]
        public void WritereturnsTupleWhenExceptionOccurs()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnWrite = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString(
                @"
                local f = file
                return f:write('boom')
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("write failure"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void WriteRethrowsScriptRuntimeException()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true)
            {
                ThrowScriptRuntimeOnWrite = true,
            };

            script.Globals["file"] = UserData.Create(file);

            Assert.That(
                () => script.DoString("return file:write('boom')"),
                Throws
                    .InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("script write failure")
            );
        }

        [Test]
        public void CloseReturnsTupleWithMessage()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { CloseMessage = "already closed" };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:close()");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("already closed"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void CloseReturnsTupleWhenExceptionIsThrown()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { ThrowOnClose = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:close()");

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("close failure"));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(-1));
            });
        }

        [Test]
        public void CloseRethrowsScriptRuntimeException()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed") { ThrowScriptRuntimeOnClose = true };

            script.Globals["file"] = UserData.Create(file);

            Assert.That(
                () => script.DoString("return file:close()"),
                Throws
                    .InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("script close failure")
            );
        }

        [Test]
        public void FlushReturnsTrueWhenWriterPresent()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:flush()");

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void FlushPropagatesExceptionThroughPcall()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnFlush = true };

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return file:flush()")
            );
            Assert.That(ex.Message, Does.Contain("flush failure"));
        }

        [Test]
        public void FlushWrapsNonScriptExceptions()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true);
            file.TriggerFlushFailure();

            script.Globals["file"] = UserData.Create(file);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return file:flush()")
            );
            Assert.That(ex.Message, Does.Contain("flush failure"));
        }

        [Test]
        public void IoFlushUsesDefaultOutputAndPropagatesException()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnFlush = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue ioTable = script.Globals.Get("io");
            Assert.That(ioTable.IsNil(), Is.False, "IO module is unavailable.");
            Assert.That(ioTable.Table.Get("output").IsNil(), Is.False, "io.output is unavailable.");
            Assert.That(ioTable.Table.Get("flush").IsNil(), Is.False, "io.flush is unavailable.");
            IoModule.SetDefaultFile(script, StandardFileType.StdOut, file);

            DynValue pcall = script.Globals.Get("pcall");
            DynValue flush = ioTable.Table.Get("flush");
            DynValue tuple = script.Call(pcall, flush);

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("flush failure"));
            });
        }

        [Test]
        public void SeekSupportsDifferentOrigins()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(2));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("c"));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(3));
                Assert.That(tuple.Tuple[3].Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void SeekRejectsInvalidWhence()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("invalid option 'bogus'"));
            });
        }

        [Test]
        public void SeekWrapsNonScriptExceptions()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("seek failure"));
            });
        }

        [Test]
        public void SeekPropagatesExceptionThroughPcall()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("seek failure"));
            });
        }

        [Test]
        public void SetvbufWrapsNonScriptExceptions()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("setvbuf failure"));
            });
        }

        [Test]
        public void SetvbufAdjustsAutoFlush()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Boolean, Is.True);
                Assert.That(file.WriterAutoFlush, Is.False);
            });
        }

        [Test]
        public void ClosedFileRejectsFurtherReads()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("attempt to use a closed file"));
            });
        }

        [Test]
        public void EofReturnsFalseWhenReaderMissing()
        {
            TestStreamFileUserData file = new(
                "seed",
                allowWrite: true,
                autoFlush: true,
                allowRead: false
            );
            Assert.That(file.CallEof(), Is.False);
        }

        [Test]
        public void PeekReturnsNextCharacter()
        {
            TestStreamFileUserData file = new("peek");
            Assert.That(file.CallPeek(), Is.EqualTo('p'));
        }

        [Test]
        public void SetvbufPropagatesExceptionThroughPcall()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("setvbuf failure"));
            });
        }

        [Test]
        public void ReadSupportsLineAndBlockModes()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("first\nsecond");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read(), file:read('*a')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("first"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("second"));
            });
        }

        [Test]
        public void ReadSupportsNumericAndAllModes()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(1234));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("AB"));
                Assert.That(tuple.Tuple[2].String, Is.EqualTo("CDE"));
            });
        }

        [Test]
        public void ReadParsesNumbersWithLeadingDecimal()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(0.75d));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("rest"));
            });
        }

        [Test]
        public void ReadParsesNumbersWithExponent()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(0.25d));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("next"));
            });
        }

        [Test]
        public void ReadParsesNumbersWhenStreamCannotRewind()
        {
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

            Assert.That(result.Number, Is.EqualTo(99));
        }

        [Test]
        public void ReadParsesHexFloatLiteralWithFraction()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(3.875d).Within(1e-12));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("tail"));
            });
        }

        [Test]
        public void ReadParsesHexFloatLiteralWithoutFraction()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(16d));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("done"));
            });
        }

        [Test]
        public void ReadReturnsNilForInvalidHexFloatExponent()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Does.StartWith("0x1p remainder"));
            });
        }

        [Test]
        public void ReadParsesHexFloatLiteralWithSignedPrefix()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(-4d));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("rest"));
            });
        }

        [Test]
        public void ReadParsesHexFloatLiteralWithSignedExponent()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(Math.Pow(2, -4)).Within(1e-12));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("tail"));
            });
        }

        [Test]
        public void ReadReturnsNilWhenHexPrefixStartsWithX()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("x12");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("x12"));
            });
        }

        [Test]
        public void ReadReturnsNilWhenHexPrefixLacksZeroAfterSign()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("+x12");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("+x12"));
            });
        }

        [Test]
        public void ReadParsesHexFloatLiteralWithPositiveExponentSign()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(16d));
                Assert.That(tuple.Tuple[1].String.TrimStart(), Does.StartWith("remainder"));
            });
        }

        [Test]
        public void ReadReturnsNilWhenHexLiteralHasNoDigitsAfterPrefix()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x rest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("0x rest"));
            });
        }

        [Test]
        public void ReadParsesHexLiteralAndLeavesTrailingCharacters()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("0x1p0garbage");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*a')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(1d));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("garbage"));
            });
        }

        [Test]
        public void ReadParsesNumbersWithLeadingWhitespace()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(42d));
                Assert.That(tuple.Tuple[1].String, Is.Not.Null);
            });
        }

        [Test]
        public void ReadReturnsNilWhenOnlySignEncountered()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Does.StartWith("+ remainder"));
            });
        }

        [Test]
        public void ReadNumberReturnsNilWhenReaderCannotConsumeChar()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("7\nnext") { ForceReadBufferFailureCount = 1 };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n'), file:read('*l')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("7"));
                Assert.That(file.ForcedReadBufferFailuresTriggered, Is.EqualTo(1));
            });
        }

        [Test]
        public void ReadNumberStopsWhenLeadingSignCannotBeConsumed()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("+42"));
                Assert.That(file.ForcedReadBufferFailuresTriggered, Is.EqualTo(1));
            });
        }

        [Test]
        public void ReadLineHandlesMixedNewlines()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("first"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("second"));
                Assert.That(tuple.Tuple[2].String, Is.EqualTo("third"));
                Assert.That(tuple.Tuple[3].String, Is.EqualTo("last"));
            });
        }

        [Test]
        public void ReadUppercaseLineKeepsTrailingNewLine()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("first\n"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("second\n"));
            });
        }

        [Test]
        public void ReadToEndAfterLineReadsReturnsRemainingContent()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("line1"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("line2\nline3"));
            });
        }

        [Test]
        public void ReadAllReturnsEmptyStringWhenAlreadyAtEof()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new(string.Empty);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*a')");

            Assert.That(result.String, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ReadReturnsNilWhenEofAndModeIsNotAll()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new(string.Empty);

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*l')");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ReadNumberReturnsNilWithoutConsumingNonNumericData()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("abc"));
            });
        }

        [Test]
        public void ReadParsesNumbersWithLeadingPlus()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("+42");

            script.Globals["file"] = UserData.Create(file);

            DynValue result = script.DoString("return file:read('*n')");
            Assert.That(result.Number, Is.EqualTo(42d));
        }

        [Test]
        public void ReadNumberHandlesExponentWithSignAndBuffersRemainder()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("   -12.5e-3\nrest");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                "local n = file:read('*n'); return n, file:read('*l'), file:read('*l')"
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(-12.5e-3).Within(1e-12));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo(string.Empty));
                Assert.That(tuple.Tuple[2].String, Is.EqualTo("rest"));
            });
        }

        [Test]
        public void ReadNumberReturnsNilForStandaloneSignAndRewinds()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("+\nvalue");

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString("return file:read('*n', '*l')");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("+"));
            });
        }

        [Test]
        public void ReadNumericCountReturnsNilWhenEofReached()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("hello");

            script.Globals["file"] = UserData.Create(file);
            script.DoString("file:read('*a')");

            DynValue result = script.DoString("return file:read(4)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ReadThrowsOnUnknownOption()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("payload");

            script.Globals["file"] = UserData.Create(file);

            Assert.That(
                () => script.DoString("file:read('*z')"),
                Throws.InstanceOf<ScriptRuntimeException>().With.Message.Contains("invalid option")
            );
        }

        [Test]
        public void ReadReturnsEmptyStringAtEofWithAOption()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("hi"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo(""));
            });
        }

        [Test]
        public void ReadThrowsOnInvalidOption()
        {
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("invalid option"));
            });
        }

        [Test]
        public void LinesEnumeratorTerminatesAtNil()
        {
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

            Assert.That(result.String, Is.EqualTo("one,two"));
        }

        [Test]
        public void ToStringTracksOpenAndClosedState()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("contents");

            script.Globals["file"] = UserData.Create(file);

            string beforeClose = file.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(beforeClose, Does.StartWith("file ("));
                Assert.That(beforeClose, Does.Not.Contain("closed"));
            });

            script.DoString("file:close()");

            string afterClose = file.ToString();
            Assert.That(afterClose, Is.EqualTo("file (closed)"));
        }

        [Test]
        public void HexPrefixValidationRejectsEmptyBuilder()
        {
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder());
            Assert.That(result, Is.False);
        }

        [Test]
        public void HexPrefixValidationAcceptsSignedZero()
        {
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder("-0"));
            Assert.That(result, Is.True);
        }

        [Test]
        public void HexPrefixValidationRejectsNonZeroAfterSign()
        {
            bool result = FileUserDataBase.IsValidHexPrefix(new StringBuilder("+1"));
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryParseHexFloatLiteralFailsOnEmptyString()
        {
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral(string.Empty, out double value);
            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.False);
                Assert.That(value, Is.EqualTo(0d));
            });
        }

        [Test]
        public void TryParseHexFloatLiteralFailsWhenNoDigitsAfterPrefix()
        {
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x", out double value);
            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.False);
                Assert.That(value, Is.EqualTo(0d));
            });
        }

        [Test]
        public void TryParseHexFloatLiteralFailsWhenExponentDigitsMissing()
        {
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p", out double value);
            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.False);
                Assert.That(value, Is.EqualTo(0d));
            });
        }

        [Test]
        public void TryParseHexFloatLiteralFailsWhenTrailingCharactersRemain()
        {
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p0junk", out double value);
            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.False);
                Assert.That(value, Is.EqualTo(0d));
            });
        }

        [Test]
        public void TryParseHexFloatLiteralParsesPositiveExponentSign()
        {
            bool parsed = FileUserDataBase.TryParseHexFloatLiteral("0x1p+1", out double value);
            Assert.Multiple(() =>
            {
                Assert.That(parsed, Is.True);
                Assert.That(value, Is.EqualTo(2d));
            });
        }

        [Test]
        public void SignAllowancePermitsDecimalExponentSign()
        {
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("1e"),
                isHex: false,
                exponentSeen: true,
                exponentHasDigits: false,
                hexExponentSeen: false,
                hexExponentHasDigits: false,
                candidate: '+'
            );

            Assert.That(allowed, Is.True);
        }

        [Test]
        public void SignAllowancePermitsHexExponentSign()
        {
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("0xp"),
                isHex: true,
                exponentSeen: false,
                exponentHasDigits: false,
                hexExponentSeen: true,
                hexExponentHasDigits: false,
                candidate: '-'
            );

            Assert.That(allowed, Is.True);
        }

        [Test]
        public void SignAllowanceRejectsUnexpectedSign()
        {
            bool allowed = FileUserDataBase.IsSignAllowed(
                new StringBuilder("12"),
                isHex: false,
                exponentSeen: false,
                exponentHasDigits: false,
                hexExponentSeen: false,
                hexExponentHasDigits: false,
                candidate: '+'
            );

            Assert.That(allowed, Is.False);
        }

        [Test]
        public void StandaloneSignOrDotRejectsLongerStrings()
        {
            bool result = FileUserDataBase.IsStandaloneSignOrDot("++");
            Assert.That(result, Is.False);
        }

        [Test]
        public void StandaloneSignOrDotAcceptsDot()
        {
            bool result = FileUserDataBase.IsStandaloneSignOrDot(".");
            Assert.That(result, Is.True);
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static void ForceDesktopPlatform()
        {
            Script.GlobalOptions.Platform = new DotNetCorePlatformAccessor();
            PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(false);
            PlatformAutoDetector.TestHooks.SetFlags(
                isRunningOnUnity: false,
                isUnityNative: false,
                isUnityIl2Cpp: false
            );
            PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(true);
        }

        private sealed class TestStreamFileUserData : StreamFileUserDataBase
        {
            private readonly Encoding _encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false
            );
            private int _remainingForcedReadFailures;
            private int _forcedReadBufferFailuresTriggered;

            internal TestStreamFileUserData(
                string initialContent,
                bool allowWrite = true,
                bool autoFlush = true,
                bool allowRead = true,
                bool allowSeek = true
            )
            {
                FaultyMemoryStream stream = null;
                StreamReader reader = null;
                FaultyStreamWriter writer = null;

                try
                {
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

                    Initialize(stream, reader, writer);
                    stream = null;
                    reader = null;
                    writer = null;
                }
                finally
                {
                    stream?.Dispose();
                    reader?.Dispose();
                    writer?.Dispose();
                }
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
    }
}
