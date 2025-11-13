namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Globalization;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class IoModuleTests
    {
        [Test]
        public void OpenReturnsNilTupleWhenFileDoesNotExist()
        {
            Script script = CreateScript();
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")
                .Replace("\\", "\\\\");

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
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")
                .Replace("\\", "\\\\");

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
            string path = temp.Replace("\\", "\\\\");

            try
            {
                DynValue tuple = script.DoString(
                    $@"
                local f = io.open('{path}', 'w')
                local openType = io.type(f)
                f:close()
                return openType, io.type(f)
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
            string path = temp.Replace("\\", "\\\\");

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
            string path = temp.Replace("\\", "\\\\");

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
        public void TmpfileCreatesWritableFile()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local f = io.tmpfile()
                f:write('temp-data')
                return io.type(f)
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
                string escapedPath = path.Replace("\\", "\\\\");
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

        [TestCase("0x1p1\n", 2d, "\n")]
        [TestCase("0x1.8p1\n", 3d, "\n")]
        [TestCase("0X.Cp+2\n", 3d, "\n")]
        [TestCase("-0x1p1\n", -2d, "\n")]
        [TestCase("+0xAp-1\n", 5d, "\n")]
        public void ReadNumberParsesHexVariants(string literal, double expected, string expectedRemainder)
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

        private static DynValue ReadNumberFromContent(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

            try
            {
                File.WriteAllText(path, content);
                string escapedPath = path.Replace("\\", "\\\\");
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
    }
}
