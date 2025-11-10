namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BinaryEncodingTests
    {
        [Test]
        public void EncodingRoundtripMatchesByteValues()
        {
            Encoding encoding = new BinaryEncoding();
            char[] characters = new[] { (char)0x00, (char)0x7F, (char)0x80, (char)0xFF };

            byte[] buffer = new byte[characters.Length];
            int bytesWritten = encoding.GetBytes(characters, 0, characters.Length, buffer, 0);

            Assert.Multiple(() =>
            {
                Assert.That(bytesWritten, Is.EqualTo(characters.Length));
                Assert.That(buffer[0], Is.EqualTo(0x00));
                Assert.That(buffer[1], Is.EqualTo(0x7F));
                Assert.That(buffer[2], Is.EqualTo(0x80));
                Assert.That(buffer[3], Is.EqualTo(0xFF));
            });

            char[] roundtrip = new char[buffer.Length];
            int charsWritten = encoding.GetChars(buffer, 0, buffer.Length, roundtrip, 0);

            Assert.Multiple(() =>
            {
                Assert.That(charsWritten, Is.EqualTo(buffer.Length));
                Assert.That(roundtrip, Is.EqualTo(characters));
            });
        }

        [Test]
        public void MaxCountsMatchRequestedSizes()
        {
            BinaryEncoding encoding = new BinaryEncoding();

            Assert.Multiple(() =>
            {
                Assert.That(encoding.GetMaxByteCount(5), Is.EqualTo(5));
                Assert.That(encoding.GetMaxCharCount(7), Is.EqualTo(7));
            });
        }

        [Test]
        public void GetByteCountEqualsCharCount()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            char[] chars = new[] { 'a', 'b', 'c', 'd' };

            int byteCount = encoding.GetByteCount(chars, 0, chars.Length);

            Assert.That(byteCount, Is.EqualTo(chars.Length));
        }

        [Test]
        public void GetCharCountEqualsByteCount()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            byte[] bytes = new byte[] { 0x01, 0x02, 0x03 };

            int charCount = encoding.GetCharCount(bytes, 0, bytes.Length);

            Assert.That(charCount, Is.EqualTo(bytes.Length));
        }
    }
}
