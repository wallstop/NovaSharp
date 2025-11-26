namespace NovaSharp.Interpreter.Tests.Units
{
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution.VM;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProcessorBinaryDumpTests
    {
        private const ulong DumpChunkMagic = 0x1A0D234E4F4F4D1D;
        private const int DumpChunkVersion = 0x150;

        [Test]
        public void UndumpThrowsWhenHeaderMissing()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0UL);
            }

            stream.Position = 0;

            Assert.That(
                () => processor.Undump(stream, 0, script.Globals, out bool _),
                Throws.ArgumentException.With.Message.Contains("Not a NovaSharp chunk")
            );
        }

        [Test]
        public void UndumpThrowsWhenVersionInvalid()
        {
            Script script = new();
            Processor processor = script.GetMainProcessorForTests();

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(DumpChunkMagic);
                writer.Write(DumpChunkVersion - 1); // wrong version
            }

            stream.Position = 0;

            Assert.That(
                () => processor.Undump(stream, 0, script.Globals, out bool _),
                Throws.ArgumentException.With.Message.Contains("Invalid version")
            );
        }

        [Test]
        public void DumpThrowsWhenMetaInstructionMissing()
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

            Assert.That(invalidBase, Is.LessThan(byteCode.Code.Count));

            using MemoryStream stream = new();
            Assert.That(
                () => processor.Dump(stream, invalidBase, hasUpValues: false),
                Throws.ArgumentException.With.Message.Contains("baseAddress")
            );
        }
    }
}
