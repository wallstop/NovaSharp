#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.IO;

    public sealed class BinDumpStreamTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BinaryWriterPreservesSignedIntegers()
        {
            int[] values =
            {
                0,
                1,
                -1,
                10,
                -10,
                32767,
                32768,
                -32767,
                -32768,
                int.MinValue,
                int.MaxValue,
            };

            using MemoryStream msOrig = new();
            UndisposableStream stream = new(msOrig);

            using (BinDumpBinaryWriter writer = new(stream, Encoding.UTF8))
            {
                foreach (int value in values)
                {
                    writer.Write(value);
                }
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader reader = new(stream, Encoding.UTF8))
            {
                foreach (int expected in values)
                {
                    int actual = reader.ReadInt32();
                    await Assert.That(actual).IsEqualTo(expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task BinaryWriterPreservesUnsignedIntegers()
        {
            uint[] values = { 0, 1, 0x7F, 10, 0x7E, 32767, 32768, uint.MinValue, uint.MaxValue };

            using MemoryStream msOrig = new();
            UndisposableStream stream = new(msOrig);

            using (BinDumpBinaryWriter writer = new(stream, Encoding.UTF8))
            {
                foreach (uint value in values)
                {
                    writer.Write(value);
                }
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader reader = new(stream, Encoding.UTF8))
            {
                foreach (uint expected in values)
                {
                    uint actual = reader.ReadUInt32();
                    await Assert.That(actual).IsEqualTo(expected);
                }
            }
        }

        [global::TUnit.Core.Test]
        public async Task BinaryWriterPreservesStrings()
        {
            string[] values = { "hello", "you", "fool", "hello", "I", "love", "you" };

            using MemoryStream msOrig = new();
            UndisposableStream stream = new(msOrig);

            using (BinDumpBinaryWriter writer = new(stream, Encoding.UTF8))
            {
                foreach (string value in values)
                {
                    writer.Write(value);
                }
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader reader = new(stream, Encoding.UTF8))
            {
                foreach (string expected in values)
                {
                    string actual = reader.ReadString();
                    await Assert.That(actual).IsEqualTo(expected);
                }
            }
        }
    }
}
#pragma warning restore CA2007
