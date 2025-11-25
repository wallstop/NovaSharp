namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.IO;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class IoModuleTests
    {
        [Test]
        public void OpenReturnsNilTupleWhenFileDoesNotExist()
        {
            Script script = CreateScript();
            string path = EscapePath(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt"));

            DynValue result = script.DoString($"return io.open('{path}', 'r')");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].IsNil(), Is.True);
                Assert.That(result.Tuple[1].String, Does.Contain("No such file"));
            });
        }

        [Test]
        public void OpenThrowsForInvalidMode()
        {
            Script script = CreateScript();
            string path = EscapePath(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt"));

            Assert.That(
                () => script.DoString($"return io.open('{path}', 'z')"),
                Throws.InstanceOf<ScriptRuntimeException>().With.Message.Contains("invalid mode")
            );
        }

        [Test]
        public void TypeReportsClosedFileAfterClose()
        {
            Script script = CreateScript();
            string temp = Path.GetTempFileName();
            string path = EscapePath(temp);

            try
            {
                DynValue tuple = script.DoString(
                    $@"
                local f = io.open('{path}', 'w')
                local openType = io.Type(f)
                f:close()
                return openType, io.Type(f)
                "
                );

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].String, Is.EqualTo("file"));
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("closed file"));
                });
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void InputReadsFromReassignedDefaultStream()
        {
            Script script = CreateScript();
            string temp = Path.GetTempFileName();
            File.WriteAllText(temp, "first\nsecond\n");
            string path = EscapePath(temp);

            try
            {
                DynValue read = script.DoString(
                    $@"
                io.input('{path}')
                return io.read('*l')
                "
                );

                Assert.That(read.String, Is.EqualTo("first"));
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void OutputWritesToReassignedDefaultStream()
        {
            Script script = CreateScript();
            string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string path = EscapePath(temp);

            try
            {
                script.DoString(
                    $@"
                local f = io.open('{path}', 'w')
                io.output(f)
                io.write('abc', '123')
                io.output():close()
                "
                );

                string content = File.ReadAllText(temp);
                Assert.That(content, Is.EqualTo("abc123"));
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }

        [Test]
        public void TmpFileCreatesWritableFile()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp-data')
                return io.Type(f)
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Type, Is.EqualTo(DataType.String));
                Assert.That(tuple.String, Is.EqualTo("file"));
            });
        }

        [Test]
        public void ReadNumberWithMissingExponentDigitsReturnsNilAndLeavesStreamIntact()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            try
            {
                File.WriteAllText(path, "123e");
                string escapedPath = EscapePath(path);
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

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Type, Is.EqualTo(DataType.Tuple));
                    Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("123e"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void ReadNumberParsesHexLiteralInput()
        {
            DynValue tuple = ReadNumberFromContent("0x1p2\n");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Type, Is.EqualTo(DataType.Number));
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(4d));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("\n"));
            });
        }

        [Test]
        public void StdStreamsAreAccessibleViaProperties()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                "return io.stdin ~= nil, io.stdout ~= nil, io.stderr ~= nil, io.unknown == nil"
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].Boolean, Is.True);
                Assert.That(tuple.Tuple[2].Boolean, Is.True);
                Assert.That(tuple.Tuple[3].Boolean, Is.True);
            });
        }

        [Test]
        public void SetDefaultFileOverridesStdInStream()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("override\n"));

            IoModule.SetDefaultFile(script, StandardFileType.StdIn, stream);

            DynValue result = script.DoString("return io.read('*l')");

            Assert.That(result.String, Is.EqualTo("override"));
        }

        [Test]
        public void SetDefaultFileOverridesStdOutStream()
        {
            Script script = CreateScript();
            using MemoryStream stream = new MemoryStream();

            IoModule.SetDefaultFile(script, StandardFileType.StdOut, stream);

            script.DoString("io.write('buffered'); io.flush()");

            stream.Position = 0;
            string content = Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(content, Is.EqualTo("buffered"));
        }

        [Test]
        public void OpenSupportsBinaryEncodingParameter()
        {
            Script script = CreateScript();
            string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
            File.WriteAllBytes(temp, new byte[] { 0x41, 0x42 });
            string path = EscapePath(temp);

            try
            {
                DynValue result = script.DoString(
                    $@"
                local f = assert(io.open('{path}', 'rb', 'binary'))
                local data = f:read('*a')
                f:close()
                return data
                "
                );

                Assert.That(result.String, Is.EqualTo("AB"));
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void OpenReturnsErrorWhenEncodingSpecifiedForBinaryMode()
        {
            Script script = CreateScript();
            string temp = Path.GetTempFileName();
            string path = EscapePath(temp);

            try
            {
                DynValue result = script.DoString($"return io.open('{path}', 'rb', 'utf-8')");

                Assert.Multiple(() =>
                {
                    Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                    Assert.That(result.Tuple[0].IsNil(), Is.True);
                    Assert.That(result.Tuple[1].String, Does.Contain("Can't specify encodings"));
                });
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Test]
        public void TypeReturnsNilForNonUserData()
        {
            Script script = CreateScript();

            DynValue result = script.DoString("return io.Type(123)");

            Assert.That(result.IsNil(), Is.True);
        }

        [TestCase("0x1p1\n", 2d, "\n")]
        [TestCase("0x1.8p1\n", 3d, "\n")]
        [TestCase("0X.Cp+2\n", 3d, "\n")]
        [TestCase("-0x1p1\n", -2d, "\n")]
        [TestCase("+0xAp-1\n", 5d, "\n")]
        public void ReadNumberParsesHexVariants(
            string literal,
            double expected,
            string expectedRemainder
        )
        {
            DynValue tuple = ReadNumberFromContent(literal);

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Type, Is.EqualTo(DataType.Number));
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(expected));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo(expectedRemainder));
            });
        }

        [Test]
        public void ReadNumberReturnsInfinityForHugeExponent()
        {
            DynValue tuple = ReadNumberFromContent("1e400");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Type, Is.EqualTo(DataType.Number));
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(double.PositiveInfinity));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void ReadNumberParsesHugeInteger()
        {
            const string literal = "123456789012345678901234567890\n";
            DynValue tuple = ReadNumberFromContent(literal);

            double expected = double.Parse(
                "123456789012345678901234567890",
                CultureInfo.InvariantCulture
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Type, Is.EqualTo(DataType.Number));
                Assert.That(tuple.Tuple[0].Number, Is.EqualTo(expected));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("\n"));
            });
        }

        [Test]
        public void TypeReturnsNilForNonUserDataArguments()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.Type(42), io.Type({})");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].IsNil(), Is.True);
            });
        }

        [Test]
        public void TypeReturnsNilForNonFileUserData()
        {
            Script script = CreateScript();
            script.Globals["sampleUserData"] = UserData.Create(new SampleUserData());

            DynValue result = script.DoString("return io.Type(sampleUserData)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void CloseClosesExplicitFileHandle()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string escapedPath = EscapePath(path);

            try
            {
                Script script = CreateScript();
                DynValue tuple = script.DoString(
                    $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    local result = io.close(f)
                    return result, io.Type(f)
                    "
                );

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].Boolean, Is.True);
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("closed file"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void CloseWithoutParameterUsesCurrentOutput()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string escapedPath = EscapePath(path);

            try
            {
                Script script = CreateScript();
                DynValue tuple = script.DoString(
                    $@"
                    local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    local closed = io.close()
                    return closed, io.Type(f)
                    "
                );

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].Boolean, Is.True);
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("closed file"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void FlushReturnsTrueForCurrentOutput()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string escapedPath = EscapePath(path);

            try
            {
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

                Assert.That(result.Boolean, Is.True);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void InputReturnsCurrentFileWhenNoArguments()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(path, "data");
            string escapedPath = EscapePath(path);

            try
            {
                Script script = CreateScript();
                DynValue tuple = script.DoString(
                    $@"
                    local f = assert(io.open('{escapedPath}', 'r'))
                    io.input(f)
                    local current = io.input()
                    return io.Type(current), io.Type(f)
                    "
                );

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].String, Is.EqualTo("file"));
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("file"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void LinesIteratesOverFileContent()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(path, "alpha\nbeta\ngamma\n");
            string escapedPath = EscapePath(path);

            try
            {
                Script script = CreateScript();
                DynValue tuple = script.DoString(
                    $@"
                    local iter = io.lines('{escapedPath}')
                    return iter(), iter(), iter(), iter()
                    "
                );

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].String, Is.EqualTo("alpha"));
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("beta"));
                    Assert.That(tuple.Tuple[2].String, Is.EqualTo("gamma"));
                    Assert.That(tuple.Tuple[3].IsNil(), Is.True);
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void LinesRaisesUsefulMessageWhenFileMissing()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function() return io.lines('missing-file.txt') end)
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("No such file"));
            });
        }

        [Test]
        public void CloseStdErrReturnsErrorTuple()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.close(io.stderr)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Does.Contain("standard file"));
            });
        }

        [Test]
        public void StdErrMethodCloseReturnsErrorTuple()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return io.stderr:close()");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Does.Contain("standard file"));
            });
        }

        [Test]
        public void LinesMethodIteratesOverHandle()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(path, "first\nsecond\nthird\n");
            string escapedPath = EscapePath(path);

            try
            {
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

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].String, Is.EqualTo("first"));
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("second"));
                    Assert.That(tuple.Tuple[2].String, Is.EqualTo("third"));
                    Assert.That(tuple.Tuple[3].String, Is.EqualTo("file"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void LinesMethodSupportsReadOptions()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(path, "abcdef");
            string escapedPath = EscapePath(path);

            try
            {
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

                Assert.Multiple(() =>
                {
                    Assert.That(tuple.Tuple[0].String, Is.EqualTo("ab"));
                    Assert.That(tuple.Tuple[1].String, Is.EqualTo("cd"));
                    Assert.That(tuple.Tuple[2].String, Is.EqualTo("ef"));
                });
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void OpenReturnsErrorTupleForUnknownEncoding()
        {
            string escapedPath = EscapePath(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")
            );

            Script script = CreateScript();
            DynValue tuple = script.DoString(
                $@"
                local file, message = io.open('{escapedPath}', 'w', 'does-not-exist')
                return file, message
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].IsNil(), Is.True);
                Assert.That(tuple.Tuple[1].String, Does.Contain("does-not-exist"));
            });
        }

        [Test]
        public void OpenSupportsExplicitEncoding()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            string escapedPath = EscapePath(path);

            try
            {
                Script script = CreateScript();
                script.DoString(
                    $@"
                local f = assert(io.open('{escapedPath}', 'w', 'utf-16'))
                f:write('hello')
                f:close()
                "
                );

                string content = File.ReadAllText(path);
                Assert.That(content, Does.Contain("hello"));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void OpenRejectsEncodingWhenBinaryModeSpecified()
        {
            string escapedPath = EscapePath(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].IsNil(), Is.True);
                Assert.That(tuple.Tuple[2].String, Does.Contain("Can't specify encodings"));
            });
        }

        [Test]
        public void TmpFileCreatesWritableStream()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("file"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("closed file"));
            });
        }

        [Test]
        public void OpenFileInvokesPlatformAccessorAndStillWritesToDisk()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
            string escapedPath = EscapePath(path);

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

            Assert.Multiple(() =>
            {
                Assert.That(accessor.OpenCalls, Is.Not.Empty);
                Assert.That(accessor.OpenCalls[0].FileName, Is.EqualTo(path));
                Assert.That(accessor.GetCapturedFileContent(path), Is.EqualTo("hooked payload"));
                Assert.That(File.ReadAllText(path), Is.EqualTo("hooked payload"));
            });

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        [Test]
        public void StdOutWritesHonorCustomScriptOptionStream()
        {
            MemoryStream capture = new();
            ScriptOptions options = new ScriptOptions()
            {
                Stdout = new UndisposableStream(capture),
            };

            Script script = new(options);
            script.DoString("io.write('brokered output'); io.flush()");

            capture.Position = 0;
            using StreamReader reader = new(
                capture,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true
            );
            Assert.That(reader.ReadToEnd(), Does.Contain("brokered output"));
        }

        private static DynValue ReadNumberFromContent(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

            try
            {
                File.WriteAllText(path, content);
                string escapedPath = EscapePath(path);
                Script script = CreateScript();

                return script.DoString(
                    $@"
                local f = assert(io.open('{escapedPath}', 'r'))
                io.input(f)
                local number = io.read('*n')
                local remainder = io.read('*a')
                f:close()
                return number, remainder
                "
                );
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static string EscapePath(string path) =>
            path.Replace("\\", "\\\\", StringComparison.Ordinal);

        private sealed class SampleUserData { }

        private sealed class PlatformScope : IDisposable
        {
            private readonly IPlatformAccessor _original;

            internal PlatformScope(IPlatformAccessor replacement)
            {
                _original = Script.GlobalOptions.Platform;
                Script.GlobalOptions.Platform = replacement;
            }

            public void Dispose()
            {
                Script.GlobalOptions.Platform = _original;
            }
        }

        private sealed class RecordingPlatformAccessor : IPlatformAccessor
        {
            private readonly IPlatformAccessor _inner;
            private readonly List<(string FileName, string Mode)> _openCalls = new();
            private readonly Dictionary<string, CapturedFile> _captures = new();

            internal RecordingPlatformAccessor(IPlatformAccessor inner)
            {
                _inner = inner;
            }

            internal List<(string FileName, string Mode)> OpenCalls => _openCalls;

            internal string GetCapturedFileContent(string file)
            {
                if (_captures.TryGetValue(file, out CapturedFile captured))
                {
                    return captured.Encoding.GetString(captured.Buffer.ToArray());
                }

                return null;
            }

            public CoreModules FilterSupportedCoreModules(CoreModules coreModules)
            {
                return _inner.FilterSupportedCoreModules(coreModules);
            }

            public string GetEnvironmentVariable(string envvarname)
            {
                return _inner.GetEnvironmentVariable(envvarname);
            }

            public bool IsRunningOnAOT()
            {
                return _inner.IsRunningOnAOT();
            }

            public string GetPlatformName()
            {
                return _inner.GetPlatformName();
            }

            public void DefaultPrint(string content)
            {
                _inner.DefaultPrint(content);
            }

            public string DefaultInput(string prompt)
            {
                return _inner.DefaultInput(prompt);
            }

            public Stream OpenFile(Script script, string filename, Encoding encoding, string mode)
            {
                _openCalls.Add((filename, mode));
                Stream stream = _inner.OpenFile(script, filename, encoding, mode);

                if (stream == null)
                {
                    return null;
                }

                if (!string.IsNullOrEmpty(mode) && ContainsWriteOrAppendMode(mode))
                {
                    CapturedFile captured = new(encoding ?? Encoding.UTF8);
                    _captures[filename] = captured;
                    return new TeeStream(stream, captured.Buffer);
                }

                return stream;
            }

            public Stream GetStandardStream(StandardFileType type)
            {
                return _inner.GetStandardStream(type);
            }

            public string GetTempFileName()
            {
                return _inner.GetTempFileName();
            }

            public void ExitFast(int exitCode)
            {
                _inner.ExitFast(exitCode);
            }

            public bool FileExists(string file)
            {
                return _inner.FileExists(file);
            }

            public void DeleteFile(string file)
            {
                _inner.DeleteFile(file);
            }

            public void MoveFile(string src, string dst)
            {
                _inner.MoveFile(src, dst);
            }

            public int ExecuteCommand(string cmdline)
            {
                return _inner.ExecuteCommand(cmdline);
            }

            private static bool ContainsWriteOrAppendMode(string mode)
            {
                ReadOnlySpan<char> span = mode.AsSpan();
                return span.IndexOf('w') >= 0 || span.IndexOf('a') >= 0;
            }

            private sealed class CapturedFile
            {
                internal CapturedFile(Encoding encoding)
                {
                    Encoding = encoding ?? Encoding.UTF8;
                    Buffer = new MemoryStream();
                }

                internal Encoding Encoding { get; }

                internal MemoryStream Buffer { get; }
            }
        }

        private sealed class TeeStream : Stream
        {
            private readonly Stream _inner;
            private readonly Stream _mirror;
            private readonly bool _leaveInnerOpen;

            internal TeeStream(Stream inner, Stream mirror, bool leaveInnerOpen = false)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
                _leaveInnerOpen = leaveInnerOpen;
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
                _mirror.Flush();
                _inner.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _mirror.Write(buffer, offset, count);
                _inner.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_leaveInnerOpen)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
