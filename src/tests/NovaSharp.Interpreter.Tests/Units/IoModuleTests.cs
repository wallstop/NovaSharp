namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
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

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }
    }
}
