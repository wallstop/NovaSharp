namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;
    using Platforms;

    [TestFixture]
    public sealed class IoModuleVirtualizationTests : IDisposable
    {
        private IPlatformAccessor _previousPlatform;
        private InMemoryPlatformAccessor _platform;

        [SetUp]
        public void SetUp()
        {
            _previousPlatform = Script.GlobalOptions.Platform;
            _platform = new InMemoryPlatformAccessor();
            Script.GlobalOptions.Platform = _platform;
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        [Test]
        public void IoOpenWriteThenReadUsesVirtualFileSystem()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("local f = io.open('virtual.txt', 'w'); f:write('hello'); f:close()");

            DynValue result = script.DoString(
                @"
                local f = io.open('virtual.txt', 'r')
                local data = f:read('*a')
                f:close()
                return data
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.String));
                Assert.That(result.String, Is.EqualTo("hello"));
                Assert.That(_platform.ReadAllText("virtual.txt"), Is.EqualTo("hello"));
            });
        }

        [Test]
        public void IoOutputRedirectWritesToVirtualFile()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString(
                @"
                local previous = io.output()
                local redirected = io.output('log.txt')
                io.write('abc')
                io.write('123')
                io.flush()
                io.flush()
                redirected:close()
                -- restore original default
                io.output(previous)
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(_platform.ReadAllText("log.txt"), Is.EqualTo("abc123"));
                Assert.That(_platform.GetStdOutText(), Is.Empty);
            });
        }

        [Test]
        public void OsRemoveDeletesVirtualFile()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("local f = io.open('temp.txt', 'w'); f:write('payload'); f:close()");
            Assert.That(_platform.HasFile("temp.txt"), Is.True);

            DynValue result = script.DoString("return os.remove('temp.txt')");
            Assert.Multiple(() =>
            {
                Assert.That(result.Boolean, Is.True);
                Assert.That(_platform.HasFile("temp.txt"), Is.False);
            });
        }

        [Test]
        public void OsTmpNameGeneratesUniqueVirtualNames()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue firstValue = script.DoString("return os.tmpname()");
            DynValue secondValue = script.DoString("return os.tmpname()");

            Assert.Multiple(() =>
            {
                Assert.That(firstValue.Type, Is.EqualTo(DataType.String));
                Assert.That(secondValue.Type, Is.EqualTo(DataType.String));
            });

            string first = firstValue.String;
            string second = secondValue.String;

            Assert.Multiple(() =>
            {
                Assert.That(first, Does.StartWith("temp_"));
                Assert.That(second, Does.StartWith("temp_"));
                Assert.That(first, Does.EndWith(".lua"));
                Assert.That(second, Does.EndWith(".lua"));
                Assert.That(first, Is.Not.EqualTo(second));
                Assert.That(_platform.HasFile(first), Is.False);
                Assert.That(_platform.HasFile(second), Is.False);
            });
        }

        [Test]
        public void OsRenameMovesVirtualFileContents()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("local f = io.open('old.txt', 'w'); f:write('payload'); f:close()");

            DynValue result = script.DoString("return os.rename('old.txt', 'new.txt')");

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.True);
            Assert.That(_platform.HasFile("old.txt"), Is.False);
            Assert.That(_platform.ReadAllText("new.txt"), Is.EqualTo("payload"));
        }

        [Test]
        public void IoWriteTargetsVirtualStdOut()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString("io.write('first'); io.write('second'); io.flush();");

            Assert.That(_platform.GetStdOutText(), Is.EqualTo("firstsecond"));
            Assert.That(_platform.GetStdErrText(), Is.Empty);
        }

        [Test]
        public void IoStdErrWriteCapturesVirtualStdErr()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString("io.stderr:write('failure'); io.stderr:flush();");

            Assert.That(_platform.GetStdErrText(), Is.EqualTo("failure"));
            Assert.That(_platform.GetStdOutText(), Is.Empty);
        }

        public void Dispose()
        {
            if (_previousPlatform != null)
            {
                Script.GlobalOptions.Platform = _previousPlatform;
                _previousPlatform = null;
            }

            if (_platform != null)
            {
                _platform.Dispose();
                _platform = null;
            }
        }

        private sealed class InMemoryPlatformAccessor : PlatformAccessorBase, IDisposable
        {
            private readonly ConcurrentDictionary<string, byte[]> _files = new(
                StringComparer.OrdinalIgnoreCase
            );

            private readonly MemoryStream _stdin = new();
            private readonly MemoryStream _stdout = new();
            private readonly MemoryStream _stderr = new();

            public override string GetPlatformNamePrefix() => "test";

            public override void DefaultPrint(string content)
            {
                WriteToStandardStream(
                    _stdout,
                    Encoding.UTF8.GetBytes(content + Environment.NewLine)
                );
            }

            public override string GetEnvironmentVariable(string envvarname) => null;

            public override bool IsRunningOnAOT() => false;

            public override Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            )
            {
                string normalizedMode = string.IsNullOrEmpty(mode) ? "r" : mode;
                bool truncate = ModeContains(normalizedMode, 'w');
                bool append = ModeContains(normalizedMode, 'a');
                bool read = ModeContains(normalizedMode, 'r') || ModeContains(normalizedMode, '+');
                bool write = normalizedMode.Any(c => c is 'w' or 'a' or '+');

                if (write)
                {
                    byte[] existing = Array.Empty<byte>();
                    if (!truncate && _files.TryGetValue(filename, out byte[] current))
                    {
                        existing = current;
                    }

                    MemoryStream mem = new();
                    if (append && existing.Length > 0)
                    {
                        mem.Write(existing, 0, existing.Length);
                        mem.Position = mem.Length;
                    }

                    return new TrackingStream(mem, data => _files[filename] = data);
                }

                if (read)
                {
                    if (!_files.TryGetValue(filename, out byte[] data))
                    {
                        throw new FileNotFoundException(
                            $"File '{filename}' not found in virtual FS."
                        );
                    }

                    return new MemoryStream(data, writable: false);
                }

                throw new NotSupportedException($"Unsupported IO mode '{mode}'.");
            }

            public override Stream GetStandardStream(StandardFileType type) =>
                new NonClosingStream(
                    type switch
                    {
                        StandardFileType.StdIn => _stdin,
                        StandardFileType.StdOut => _stdout,
                        StandardFileType.StdErr => _stderr,
                        _ => new MemoryStream(),
                    }
                );

            public override string GetTempFileName() => $"temp_{Guid.NewGuid():N}.lua";

            public override void ExitFast(int exitCode)
            {
                // No-op for tests
            }

            public override bool FileExists(string file) => _files.ContainsKey(file);

            public override CoreModules FilterSupportedCoreModules(CoreModules coreModules) =>
                coreModules;

            public override void DeleteFile(string file)
            {
                _files.TryRemove(file, out _);
            }

            public override void MoveFile(string src, string dst)
            {
                if (_files.TryRemove(src, out byte[] data))
                {
                    _files[dst] = data;
                }
            }

            public override int ExecuteCommand(string cmdline) => 0;

            public void Dispose()
            {
                _stdin.Dispose();
                _stdout.Dispose();
                _stderr.Dispose();
            }

            private static void WriteToStandardStream(MemoryStream stream, byte[] data)
            {
                stream.Position = stream.Length;
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }

            public string ReadAllText(string file) =>
                _files.TryGetValue(file, out byte[] data)
                    ? Encoding.UTF8.GetString(data)
                    : string.Empty;

            public bool HasFile(string file) => _files.ContainsKey(file);

            public string GetStdOutText()
            {
                return Encoding.UTF8.GetString(_stdout.ToArray());
            }

            public string GetStdErrText()
            {
                return Encoding.UTF8.GetString(_stderr.ToArray());
            }

            private static bool ModeContains(string mode, char symbol)
            {
                if (string.IsNullOrEmpty(mode))
                {
                    return false;
                }

                return mode.Contains(symbol, StringComparison.Ordinal);
            }

            private sealed class TrackingStream : Stream
            {
                private readonly MemoryStream _inner;
                private readonly Action<byte[]> _onDispose;

                public TrackingStream(MemoryStream inner, Action<byte[]> onDispose)
                {
                    _inner = inner;
                    _onDispose = onDispose;
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

                public override void Flush() => _inner.Flush();

                public override int Read(byte[] buffer, int offset, int count) =>
                    _inner.Read(buffer, offset, count);

                public override long Seek(long offset, SeekOrigin origin) =>
                    _inner.Seek(offset, origin);

                public override void SetLength(long value) => _inner.SetLength(value);

                public override void Write(byte[] buffer, int offset, int count) =>
                    _inner.Write(buffer, offset, count);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _inner.Flush();
                        _onDispose(_inner.ToArray());
                        _inner.Dispose();
                    }

                    base.Dispose(disposing);
                }
            }

            private sealed class NonClosingStream : Stream
            {
                private readonly MemoryStream _inner;

                public NonClosingStream(MemoryStream inner)
                {
                    _inner = inner;
                    _inner.Position = _inner.Length;
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

                public override void Flush() => _inner.Flush();

                public override int Read(byte[] buffer, int offset, int count) =>
                    _inner.Read(buffer, offset, count);

                public override long Seek(long offset, SeekOrigin origin) =>
                    _inner.Seek(offset, origin);

                public override void SetLength(long value) => _inner.SetLength(value);

                public override void Write(byte[] buffer, int offset, int count) =>
                    _inner.Write(buffer, offset, count);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _inner.Flush();
                    }
                    base.Dispose(disposing);
                }
            }
        }
    }
}
