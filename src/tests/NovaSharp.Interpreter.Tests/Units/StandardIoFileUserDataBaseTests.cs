namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StandardIoFileUserDataBaseTests
    {
        [Test]
        public void CreateInputStreamSupportsLineReads()
        {
            using MemoryStream stream = new();
            using (StreamWriter seed = new(stream, new UTF8Encoding(false), 1024, leaveOpen: true))
            {
                seed.WriteLine("first");
                seed.Flush();
            }

            stream.Position = 0;

            StandardIoFileUserDataBase file = StandardIoFileUserDataBase.CreateInputStream(stream);
            ScriptExecutionContext context = CreateExecutionContext();

            DynValue firstLine = file.Read(context, TestHelpers.CreateArguments());
            DynValue eof = file.Read(context, TestHelpers.CreateArguments());

            Assert.Multiple(() =>
            {
                Assert.That(firstLine.String, Is.EqualTo("first"));
                Assert.That(eof.IsNil(), Is.True);
            });
        }

        [Test]
        public void CloseReturnsErrorTupleAndLeavesStreamWritable()
        {
            using MemoryStream stream = new();
            StandardIoFileUserDataBase file = StandardIoFileUserDataBase.CreateOutputStream(stream);
            ScriptExecutionContext context = CreateExecutionContext();

            DynValue closeResult = file.Close(context, TestHelpers.CreateArguments());

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(closeResult.Tuple[0].IsNil(), Is.True);
                Assert.That(
                    closeResult.Tuple[1].String,
                    Does.Contain("cannot close standard file")
                );
                Assert.That(closeResult.Tuple[2].Number, Is.EqualTo(-1));
            });

            DynValue writeResult = file.Write(
                context,
                TestHelpers.CreateArguments(DynValue.NewString("payload"))
            );

            Assert.That(writeResult.Type, Is.EqualTo(DataType.UserData));
            Assert.That(file.Flush(), Is.True);

            stream.Position = 0;
            using StreamReader reader = new(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true
            );
            string contents = reader.ReadToEnd();

            Assert.That(contents.EndsWith("payload", StringComparison.Ordinal), Is.True);
        }

        private static ScriptExecutionContext CreateExecutionContext()
        {
            Script script = new(CoreModules.PresetComplete);
            return TestHelpers.CreateExecutionContext(script);
        }
    }
}
