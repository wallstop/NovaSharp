namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BinaryEncodingTests
    {
        private readonly Encoding _encoding = new BinaryEncoding();

        [Test]
        public void GetBytesCopiesCharactersVerbatim()
        {
            char[] source = new[] { '\u0001', '\u00FF', '\u1234' };
            byte[] destination = new byte[source.Length];

            int written = _encoding.GetBytes(source, 0, source.Length, destination, 0);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(source.Length));
                Assert.That(destination[0], Is.EqualTo((byte)source[0]));
                Assert.That(destination[1], Is.EqualTo((byte)source[1]));
                Assert.That(destination[2], Is.EqualTo((byte)source[2]));
            });
        }

        [Test]
        public void GetBytesThrowsWhenSourceIsNull()
        {
            Assert.That(
                () => _encoding.GetBytes((char[])null, 0, 0, Array.Empty<byte>(), 0),
                Throws.InstanceOf<ArgumentNullException>()
            );
        }

        [Test]
        public void GetBytesThrowsWhenSourceRangeInvalid()
        {
            char[] source = new[] { 'a', 'b', 'c' };
            byte[] destination = new byte[source.Length];

            Assert.That(
                () => _encoding.GetBytes(source, -1, 2, destination, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
            Assert.That(
                () => _encoding.GetBytes(source, 1, 4, destination, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void GetBytesThrowsWhenDestinationTooSmall()
        {
            char[] source = new[] { 'a', 'b', 'c' };
            byte[] destination = new byte[source.Length - 1];

            Assert.That(
                () => _encoding.GetBytes(source, 0, source.Length, destination, 0),
                Throws.InstanceOf<ArgumentException>()
            );
        }

        [Test]
        public void GetCharsCopiesBytesVerbatim()
        {
            byte[] source = new byte[] { 0x01, 0x7F, 0xFF };
            char[] destination = new char[source.Length];

            int read = _encoding.GetChars(source, 0, source.Length, destination, 0);

            Assert.Multiple(() =>
            {
                Assert.That(read, Is.EqualTo(source.Length));
                Assert.That(destination[0], Is.EqualTo((char)source[0]));
                Assert.That(destination[1], Is.EqualTo((char)source[1]));
                Assert.That(destination[2], Is.EqualTo((char)source[2]));
            });
        }

        [Test]
        public void GetCharsThrowsWhenSourceIsNull()
        {
            Assert.That(
                () => _encoding.GetChars((byte[])null, 0, 0, Array.Empty<char>(), 0),
                Throws.InstanceOf<ArgumentNullException>()
            );
        }

        [Test]
        public void GetCharsThrowsWhenSourceRangeInvalid()
        {
            byte[] source = new byte[] { 0x01, 0x02, 0x03 };
            char[] destination = new char[source.Length];

            Assert.That(
                () => _encoding.GetChars(source, -1, 2, destination, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
            Assert.That(
                () => _encoding.GetChars(source, 1, 4, destination, 0),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void GetCharsThrowsWhenDestinationTooSmall()
        {
            byte[] source = new byte[] { 0x01, 0x02, 0x03 };
            char[] destination = new char[source.Length - 1];

            Assert.That(
                () => _encoding.GetChars(source, 0, source.Length, destination, 0),
                Throws.InstanceOf<ArgumentException>()
            );
        }

        [Test]
        public void GetMaxCountThrowsWhenNegative()
        {
            Assert.That(
                () => _encoding.GetMaxByteCount(-1),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
            Assert.That(
                () => _encoding.GetMaxCharCount(-1),
                Throws.InstanceOf<ArgumentOutOfRangeException>()
            );
        }

        [Test]
        public void GetMaxCountsReturnRequestedSize()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_encoding.GetMaxByteCount(5), Is.EqualTo(5));
                Assert.That(_encoding.GetMaxCharCount(7), Is.EqualTo(7));
            });
        }

        [Test]
        public void GetByteCountMatchesSourceLength()
        {
            char[] chars = new[] { 'a', 'b', 'c', 'd' };
            int count = _encoding.GetByteCount(chars, 0, chars.Length);

            Assert.That(count, Is.EqualTo(chars.Length));
        }

        [Test]
        public void GetCharCountMatchesSourceLength()
        {
            byte[] bytes = new byte[] { 0x10, 0x20, 0x30 };
            int count = _encoding.GetCharCount(bytes, 0, bytes.Length);

            Assert.That(count, Is.EqualTo(bytes.Length));
        }
    }
}
