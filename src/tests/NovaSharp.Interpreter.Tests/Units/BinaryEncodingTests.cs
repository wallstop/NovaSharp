namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.CoreLib.IO;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BinaryEncodingTests
    {
        private readonly BinaryEncoding _encoding = new();
        private static readonly byte[] _expectedDestinationBytes = { 1, 2, 3 };
        private static readonly char[] _expectedCharSnapshot = { 'a', 'b', 'c' };

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
        public void GetBytesThrowsWhenDestinationIsNull()
        {
            char[] source = new[] { 'a' };

            Assert.That(
                () => _encoding.GetBytes(source, 0, source.Length, null, 0),
                Throws.InstanceOf<ArgumentNullException>()
            );
        }

        [Test]
        public void GetBytesRespectsOffsets()
        {
            char[] source = new[] { 'x', 'A', 'B', 'y' };
            byte[] destination = new byte[6];
            Array.Fill(destination, (byte)0xFF);

            int written = _encoding.GetBytes(source, 1, 2, destination, 3);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(2));
                Assert.That(destination[3], Is.EqualTo((byte)'A'));
                Assert.That(destination[4], Is.EqualTo((byte)'B'));
                Assert.That(destination[5], Is.EqualTo(0xFF));
            });
        }

        [Test]
        public void GetBytesAllowsZeroCountWithoutWriting()
        {
            char[] source = new[] { 'a', 'b', 'c' };
            byte[] destination = new byte[] { 1, 2, 3 };

            int written = _encoding.GetBytes(source, source.Length, 0, destination, 0);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(0));
                Assert.That(destination, Is.EqualTo(_expectedDestinationBytes));
            });
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
        public void GetCharsThrowsWhenDestinationIsNull()
        {
            byte[] source = new byte[] { 0x42 };

            Assert.That(
                () => _encoding.GetChars(source, 0, source.Length, null, 0),
                Throws.InstanceOf<ArgumentNullException>()
            );
        }

        [Test]
        public void GetCharsRespectsOffsets()
        {
            byte[] source = new byte[] { 0x10, (byte)'C', (byte)'D', 0x20 };
            char[] destination = new char[6];
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = '\uFFFF';
            }

            int read = _encoding.GetChars(source, 1, 2, destination, 2);

            Assert.Multiple(() =>
            {
                Assert.That(read, Is.EqualTo(2));
                Assert.That(destination[2], Is.EqualTo('C'));
                Assert.That(destination[3], Is.EqualTo('D'));
                Assert.That(destination[4], Is.EqualTo('\uFFFF'));
            });
        }

        [Test]
        public void GetCharsAllowsZeroCountWithoutWriting()
        {
            byte[] source = new byte[] { 0x01, 0x02 };
            char[] destination = new char[] { 'a', 'b', 'c' };

            int read = _encoding.GetChars(source, source.Length, 0, destination, 1);

            Assert.Multiple(() =>
            {
                Assert.That(read, Is.EqualTo(0));
                Assert.That(destination, Is.EqualTo(_expectedCharSnapshot));
            });
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
