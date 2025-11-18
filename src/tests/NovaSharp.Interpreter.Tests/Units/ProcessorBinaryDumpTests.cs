namespace NovaSharp.Interpreter.Tests.Units
{
    using System.IO;
    using System.Reflection;
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
            Processor processor = GetProcessor(script);

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
            Processor processor = GetProcessor(script);

            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(DumpChunkMagic);
                writer.Write(0); // wrong version
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
            Processor processor = GetProcessor(script);
            ByteCode byteCode = GetByteCode(script);

            int entry = chunk.Function.EntryPointByteCodeLocation;
            int invalidBase = entry;

            while (
                invalidBase < byteCode.code.Count
                && (
                    byteCode.code[invalidBase].OpCode == OpCode.Meta
                    || byteCode.code[invalidBase].OpCode == OpCode.Nop
                )
            )
            {
                invalidBase++;
            }

            Assert.That(invalidBase, Is.LessThan(byteCode.code.Count));

            using MemoryStream stream = new();
            Assert.That(
                () => processor.Dump(stream, invalidBase, hasUpvalues: false),
                Throws.ArgumentException.With.Message.Contains("baseAddress")
            );
        }

        private static Processor GetProcessor(Script script)
        {
            FieldInfo field = typeof(Script).GetField(
                "_mainProcessor",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (Processor)field.GetValue(script)!;
        }

        private static ByteCode GetByteCode(Script script)
        {
            FieldInfo field = typeof(Script).GetField(
                "_byteCode",
                BindingFlags.NonPublic | BindingFlags.Instance
            )!;
            return (ByteCode)field.GetValue(script)!;
        }
    }
}
