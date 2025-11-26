namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Globalization;
    using System.IO;
    using System.Text;
    using IO;
    using NUnit.Framework;

    [TestFixture]
    public class BinDumpStreamTests
    {
        [Test]
        public void BinDumpBinaryStreamsTestIntWrites()
        {
            int[] values = new int[]
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
            UndisposableStream ms = new(msOrig);

            using (BinDumpBinaryWriter bdbw = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    bdbw.Write(values[i]);
                }
            }

            ms.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader bdbr = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    int v = bdbr.ReadInt32();
                    Assert.That(
                        v,
                        Is.EqualTo(values[i]),
                        $"i = {i.ToString(CultureInfo.InvariantCulture)}"
                    );
                }
            }
        }

        [Test]
        public void BinDumpBinaryStreamsTestUIntWrites()
        {
            uint[] values = new uint[]
            {
                0,
                1,
                0x7F,
                10,
                0x7E,
                32767,
                32768,
                uint.MinValue,
                uint.MaxValue,
            };

            using MemoryStream msOrig = new();
            UndisposableStream ms = new(msOrig);

            using (BinDumpBinaryWriter bdbw = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    bdbw.Write(values[i]);
                }
            }

            ms.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader bdbr = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    uint v = bdbr.ReadUInt32();
                    Assert.That(
                        v,
                        Is.EqualTo(values[i]),
                        $"i = {i.ToString(CultureInfo.InvariantCulture)}"
                    );
                }
            }
        }

        [Test]
        public void BinDumpBinaryStreamsTestStringWrites()
        {
            string[] values = new string[] { "hello", "you", "fool", "hello", "I", "love", "you" };

            using MemoryStream msOrig = new();
            UndisposableStream ms = new(msOrig);

            using (BinDumpBinaryWriter bdbw = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    bdbw.Write(values[i]);
                }
            }

            ms.Seek(0, SeekOrigin.Begin);

            using (BinDumpBinaryReader bdbr = new(ms, Encoding.UTF8))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    string v = bdbr.ReadString();
                    Assert.That(
                        v,
                        Is.EqualTo(values[i]),
                        $"i = {i.ToString(CultureInfo.InvariantCulture)}"
                    );
                }
            }
        }
    }
}
