namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;

    [ScriptGlobalOptionsIsolation]
    public sealed class IoModuleVirtualizationTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IoOpenWriteThenReadUsesVirtualFileSystem()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString("local f = io.open('virtual.txt', 'w'); f:write('hello'); f:close()");

            DynValue result = script.DoString(
                @"
                local f = io.open('virtual.txt', 'r')
                local data = f:read('*a')
                f:close()
                return data
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("hello");
            await Assert.That(platform.ReadAllText("virtual.txt")).IsEqualTo("hello");
        }

        [global::TUnit.Core.Test]
        public async Task IoOutputRedirectWritesToVirtualFile()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString(
                @"
                local previous = io.output()
                local redirected = io.output('log.txt')
                io.write('abc')
                io.write('123')
                io.flush()
                io.flush()
                redirected:close()
                io.output(previous)
            "
            );

            await Assert.That(platform.ReadAllText("log.txt")).IsEqualTo("abc123");
            await Assert.That(platform.GetStdOutText()).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task OsRemoveDeletesVirtualFile()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString("local f = io.open('temp.txt', 'w'); f:write('payload'); f:close()");
            await Assert.That(platform.HasFile("temp.txt")).IsTrue();

            DynValue result = script.DoString("return os.remove('temp.txt')");

            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(platform.HasFile("temp.txt")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task OsTmpNameGeneratesUniqueVirtualNames()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            DynValue firstValue = script.DoString("return os.tmpname()");
            DynValue secondValue = script.DoString("return os.tmpname()");

            await Assert.That(firstValue.Type).IsEqualTo(DataType.String);
            await Assert.That(secondValue.Type).IsEqualTo(DataType.String);

            string first = firstValue.String;
            string second = secondValue.String;

            await Assert.That(first.StartsWith("temp_", StringComparison.Ordinal)).IsTrue();
            await Assert.That(second.StartsWith("temp_", StringComparison.Ordinal)).IsTrue();
            await Assert.That(first.EndsWith(".lua", StringComparison.Ordinal)).IsTrue();
            await Assert.That(second.EndsWith(".lua", StringComparison.Ordinal)).IsTrue();
            await Assert.That(first).IsNotEqualTo(second);
            await Assert.That(platform.HasFile(first)).IsFalse();
            await Assert.That(platform.HasFile(second)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task OsRenameMovesVirtualFileContents()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString("local f = io.open('old.txt', 'w'); f:write('payload'); f:close()");

            DynValue result = script.DoString("return os.rename('old.txt', 'new.txt')");

            await Assert.That(result.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(result.Boolean).IsTrue();
            await Assert.That(platform.HasFile("old.txt")).IsFalse();
            await Assert.That(platform.ReadAllText("new.txt")).IsEqualTo("payload");
        }

        [global::TUnit.Core.Test]
        public async Task IoWriteTargetsVirtualStdOut()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString("io.write('first'); io.write('second'); io.flush();");

            await Assert.That(platform.GetStdOutText()).IsEqualTo("firstsecond");
            await Assert.That(platform.GetStdErrText()).IsEqualTo(string.Empty);
        }

        [global::TUnit.Core.Test]
        public async Task IoStdErrWriteCapturesVirtualStdErr()
        {
            using VirtualIoContext context = CreateVirtualIoContext();
            Script script = context.Script;
            InMemoryPlatformAccessor platform = context.Platform;

            script.DoString("io.stderr:write('failure'); io.stderr:flush();");

            await Assert.That(platform.GetStdErrText()).IsEqualTo("failure");
            await Assert.That(platform.GetStdOutText()).IsEqualTo(string.Empty);
        }

        private static VirtualIoContext CreateVirtualIoContext() => new();

        private sealed class VirtualIoContext : IDisposable
        {
            private readonly IDisposable _globalScope;

            internal VirtualIoContext()
            {
                _globalScope = Script.BeginGlobalOptionsScope();
                Platform = new InMemoryPlatformAccessor();
                Script.GlobalOptions.Platform = Platform;
                Script = new Script(CoreModules.PresetComplete);
            }

            public Script Script { get; }

            public InMemoryPlatformAccessor Platform { get; }

            public void Dispose()
            {
                Platform.Dispose();
                _globalScope.Dispose();
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
                // no-op for tests
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

            public string GetStdOutText() => Encoding.UTF8.GetString(_stdout.ToArray());

            public string GetStdErrText() => Encoding.UTF8.GetString(_stderr.ToArray());

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
