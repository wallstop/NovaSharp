namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    [NonParallelizable]
    public sealed class StreamFileUserDataBaseTests
    {
        [OneTimeSetUp]
        public void RegisterUserData()
        {
            UserData.RegisterType<TestStreamFileUserData>();
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
                Assert.That(file.Writes, Is.EquivalentTo(new[] { "A", "B" }));
                Assert.That(file.GetContent().EndsWith("AB"));
            });
        }

        [Test]
        public void WriteReturnsTupleWhenExceptionOccurs()
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
            });
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

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function()
                    return file:flush()
                end)
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("flush failure"));
            });
        }

        [Test]
        public void IoFlushUsesDefaultOutputAndPropagatesException()
        {
            Script script = CreateScript();
            TestStreamFileUserData file = new("seed", allowWrite: true) { ThrowOnFlush = true };

            script.Globals["file"] = UserData.Create(file);

            DynValue tuple = script.DoString(
                @"
                io.output(file)
                local ok, err = pcall(function()
                    return io.flush()
                end)
                return ok, err
                "
            );

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

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private sealed class TestStreamFileUserData : StreamFileUserDataBase
        {
            private readonly MemoryStream _innerStream;
            private readonly StreamReader _innerReader;
            private StreamWriter _innerWriter;
            private readonly Encoding _encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false
            );

            internal TestStreamFileUserData(
                string initialContent,
                bool allowWrite = true,
                bool autoFlush = true
            )
            {
                _innerStream = new MemoryStream();

                if (!string.IsNullOrEmpty(initialContent))
                {
                    using StreamWriter seed = new StreamWriter(
                        _innerStream,
                        _encoding,
                        bufferSize: 1024,
                        leaveOpen: true
                    );
                    seed.Write(initialContent);
                    seed.Flush();
                }

                _innerStream.Position = 0;

                _innerReader = new StreamReader(
                    _innerStream,
                    _encoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true
                );

                if (allowWrite)
                {
                    _innerWriter = new StreamWriter(
                        _innerStream,
                        _encoding,
                        bufferSize: 1024,
                        leaveOpen: true
                    )
                    {
                        AutoFlush = autoFlush,
                    };
                }

                Initialize(_innerStream, _innerReader, allowWrite ? _innerWriter : null);
            }

            internal List<string> Writes { get; } = new();

            internal bool ThrowOnWrite { get; set; }

            internal bool ThrowOnClose { get; set; }

            internal bool ThrowOnFlush { get; set; }

            internal bool ThrowOnSeek { get; set; }

            internal bool ThrowOnSetvbuf { get; set; }

            internal string CloseMessage { get; set; }

            internal bool WriterAutoFlush => _innerWriter != null && _innerWriter.AutoFlush;

            internal string GetContent()
            {
                long position = _innerStream.Position;
                _innerStream.Position = 0;

                StreamReader snapshot = new StreamReader(
                    _innerStream,
                    _encoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true
                );

                string text = snapshot.ReadToEnd();
                _innerStream.Position = position;
                return text;
            }

            protected override void Write(string value)
            {
                if (ThrowOnWrite)
                {
                    throw new InvalidOperationException("write failure");
                }

                Writes.Add(value);
                base.Write(value);
            }

            protected override string Close()
            {
                if (ThrowOnClose)
                {
                    throw new IOException("close failure");
                }

                string message = CloseMessage;
                string baseResult = base.Close();
                return string.IsNullOrEmpty(message) ? baseResult : message;
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
        }
    }
}
