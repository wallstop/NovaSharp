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

        [Test]
        public void GetBytesThrowsWhenDestinationTooSmall()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            char[] chars = new[] { 'a', 'b' };
            byte[] buffer = new byte[1];

            Assert.That(
                () => encoding.GetBytes(chars, 0, chars.Length, buffer, 0),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public void GetBytesThrowsWhenArgumentsOutOfRange()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            char[] chars = new[] { 'a' };
            byte[] buffer = new byte[2];

            Assert.That(
                () => encoding.GetBytes(chars, 1, 1, buffer, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void GetCharsThrowsWhenDestinationTooSmall()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            byte[] bytes = new byte[] { 0x01, 0x02 };
            char[] chars = new char[1];

            Assert.That(
                () => encoding.GetChars(bytes, 0, bytes.Length, chars, 0),
                Throws.TypeOf<ArgumentException>()
            );
        }

        [Test]
        public void NullBuffersThrowArgumentNullException()
        {
            BinaryEncoding encoding = new BinaryEncoding();
            byte[] bytes = new byte[1];
            char[] chars = new[] { 'a' };

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => encoding.GetBytes((char[])null!, 0, 0, bytes, 0),
                    Throws.TypeOf<ArgumentNullException>()
                );
                Assert.That(
                    () => encoding.GetBytes(chars, 0, 1, (byte[])null!, 0),
                    Throws.TypeOf<ArgumentNullException>()
                );
                Assert.That(
                    () => encoding.GetChars((byte[])null!, 0, 0, new char[1], 0),
                    Throws.TypeOf<ArgumentNullException>()
                );
                Assert.That(
                    () => encoding.GetChars(bytes, 0, 1, (char[])null!, 0),
                    Throws.TypeOf<ArgumentNullException>()
                );
            });
        }

        [Test]
        public void NegativeMaxCountsThrowArgumentOutOfRange()
        {
            BinaryEncoding encoding = new BinaryEncoding();

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => encoding.GetMaxByteCount(-1),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                );
                Assert.That(
                    () => encoding.GetMaxCharCount(-1),
                    Throws.TypeOf<ArgumentOutOfRangeException>()
                );
            });
        }
    }
}
