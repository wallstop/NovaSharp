namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.VM;

    public sealed class ProcessorBinaryDumpTUnitTests
    {
        private const ulong DumpChunkMagic = 0x1A0D234E4F4F4D1D;
        private const int DumpChunkVersion = 0x150;

        [global::TUnit.Core.Test]
        public async Task UndumpThrowsWhenHeaderMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0UL);
            }

            stream.Position = 0;

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                processor.Undump(stream, 0, script.Globals, out bool _)
            );

            await Assert.That(exception.Message).Contains("Not a NovaSharp chunk");
        }

        [global::TUnit.Core.Test]
        public async Task UndumpThrowsWhenVersionInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(DumpChunkMagic);
                writer.Write(DumpChunkVersion - 1);
            }

            stream.Position = 0;

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                processor.Undump(stream, 0, script.Globals, out bool _)
            );

            await Assert.That(exception.Message).Contains("Invalid version");
        }

        [global::TUnit.Core.Test]
        public async Task DumpThrowsWhenMetaInstructionMissing()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 1");
            Processor processor = script.GetMainProcessorForTests();
            ByteCode byteCode = script.GetByteCodeForTests();

            int entry = chunk.Function.EntryPointByteCodeLocation;
            int invalidBase = entry;

            while (
                invalidBase < byteCode.Code.Count
                && (
                    byteCode.Code[invalidBase].OpCode == OpCode.Meta
                    || byteCode.Code[invalidBase].OpCode == OpCode.Nop
                )
            )
            {
                invalidBase++;
            }

            await Assert.That(invalidBase < byteCode.Code.Count).IsTrue();

            using MemoryStream stream = new();
            ArgumentException exception = ExpectException<ArgumentException>(() =>
                processor.Dump(stream, invalidBase, hasUpValues: false)
            );

            await Assert.That(exception.Message).Contains("baseAddress");
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
