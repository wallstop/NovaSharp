#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests.Units;

    [PlatformDetectorIsolation]
    public sealed class StandardIoFileUserDataBaseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CreateInputStreamSupportsLineReads()
        {
            using MemoryStream stream = new();
            using StreamWriter seed = new(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
            await seed.WriteLineAsync("first").ConfigureAwait(false);
            await seed.FlushAsync().ConfigureAwait(false);

            stream.Position = 0;

            StandardIoFileUserDataBase file = StandardIoFileUserDataBase.CreateInputStream(stream);
            ScriptExecutionContext context = CreateExecutionContext();

            DynValue firstLine = file.Read(context, TestHelpers.CreateArguments());
            DynValue eof = file.Read(context, TestHelpers.CreateArguments());

            await Assert.That(firstLine.String).IsEqualTo("first");
            await Assert.That(eof.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CloseReturnsErrorTupleAndLeavesStreamWritable()
        {
            using MemoryStream stream = new();
            StandardIoFileUserDataBase file = StandardIoFileUserDataBase.CreateOutputStream(stream);
            ScriptExecutionContext context = CreateExecutionContext();

            DynValue closeResult = file.Close(context, TestHelpers.CreateArguments());

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].IsNil()).IsTrue();
            await Assert.That(closeResult.Tuple[1].String).Contains("cannot close standard file");
            await Assert.That(closeResult.Tuple[2].Number).IsEqualTo(-1);

            DynValue writeResult = file.Write(
                context,
                TestHelpers.CreateArguments(DynValue.NewString("payload"))
            );

            await Assert.That(writeResult.Type).IsEqualTo(DataType.UserData);
            await Assert.That(file.Flush()).IsTrue();

            stream.Position = 0;
            using StreamReader reader = new(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true
            );
            string contents = await reader.ReadToEndAsync().ConfigureAwait(false);
            await Assert.That(contents.EndsWith("payload", StringComparison.Ordinal)).IsTrue();
        }

        private static ScriptExecutionContext CreateExecutionContext()
        {
            Script script = new(CoreModules.PresetComplete);
            return TestHelpers.CreateExecutionContext(script);
        }
    }
}
#pragma warning restore CA2007
