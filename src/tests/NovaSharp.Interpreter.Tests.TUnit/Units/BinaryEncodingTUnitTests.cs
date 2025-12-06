namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.CoreLib.IO;

    public sealed class BinaryEncodingTUnitTests
    {
        private readonly BinaryEncoding _encoding = new();
        private static readonly byte[] ExpectedDestinationBytes = { 1, 2, 3 };
        private static readonly char[] ExpectedCharSnapshot = { 'a', 'b', 'c' };

        [global::TUnit.Core.Test]
        public async Task GetBytesCopiesCharactersVerbatim()
        {
            char[] source = { '\u0001', '\u00FF', '\u1234' };
            byte[] destination = new byte[source.Length];

            int written = _encoding.GetBytes(source, 0, source.Length, destination, 0);

            await Assert.That(written).IsEqualTo(source.Length).ConfigureAwait(false);
            await Assert.That(destination[0]).IsEqualTo((byte)source[0]).ConfigureAwait(false);
            await Assert.That(destination[1]).IsEqualTo((byte)source[1]).ConfigureAwait(false);
            await Assert.That(destination[2]).IsEqualTo((byte)source[2]).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenSourceIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _encoding.GetBytes((char[])null, 0, 0, Array.Empty<byte>(), 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenSourceRangeInvalid()
        {
            char[] source = { 'a', 'b', 'c' };
            byte[] destination = new byte[source.Length];

            ArgumentOutOfRangeException negativeIndex = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    _encoding.GetBytes(source, -1, 2, destination, 0)
            );
            ArgumentOutOfRangeException excessiveCount = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    _encoding.GetBytes(source, 1, 4, destination, 0)
            );

            await Assert.That(negativeIndex).IsNotNull().ConfigureAwait(false);
            await Assert.That(excessiveCount).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenDestinationTooSmall()
        {
            char[] source = { 'a', 'b', 'c' };
            byte[] destination = new byte[source.Length - 1];

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                _encoding.GetBytes(source, 0, source.Length, destination, 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenDestinationIsNull()
        {
            char[] source = { 'a' };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _encoding.GetBytes(source, 0, source.Length, null, 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesRespectsOffsets()
        {
            char[] source = { 'x', 'A', 'B', 'y' };
            byte[] destination = new byte[6];
            Array.Fill(destination, (byte)0xFF);

            int written = _encoding.GetBytes(source, 1, 2, destination, 3);

            await Assert.That(written).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(destination[3]).IsEqualTo((byte)'A').ConfigureAwait(false);
            await Assert.That(destination[4]).IsEqualTo((byte)'B').ConfigureAwait(false);
            await Assert.That(destination[5]).IsEqualTo((byte)0xFF).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesAllowsZeroCountWithoutWriting()
        {
            char[] source = { 'a', 'b', 'c' };
            byte[] destination = (byte[])ExpectedDestinationBytes.Clone();

            int written = _encoding.GetBytes(source, source.Length, 0, destination, 0);

            await Assert.That(written).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(destination.AsSpan().SequenceEqual(ExpectedDestinationBytes))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsCopiesBytesVerbatim()
        {
            byte[] source = { 0x01, 0x7F, 0xFF };
            char[] destination = new char[source.Length];

            int read = _encoding.GetChars(source, 0, source.Length, destination, 0);

            await Assert.That(read).IsEqualTo(source.Length).ConfigureAwait(false);
            await Assert.That(destination[0]).IsEqualTo((char)source[0]).ConfigureAwait(false);
            await Assert.That(destination[1]).IsEqualTo((char)source[1]).ConfigureAwait(false);
            await Assert.That(destination[2]).IsEqualTo((char)source[2]).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenSourceIsNull()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _encoding.GetChars((byte[])null, 0, 0, Array.Empty<char>(), 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenSourceRangeInvalid()
        {
            byte[] source = { 0x01, 0x02, 0x03 };
            char[] destination = new char[source.Length];

            ArgumentOutOfRangeException negativeIndex = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    _encoding.GetChars(source, -1, 2, destination, 0)
            );
            ArgumentOutOfRangeException excessiveCount = Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                    _encoding.GetChars(source, 1, 4, destination, 0)
            );

            await Assert.That(negativeIndex).IsNotNull().ConfigureAwait(false);
            await Assert.That(excessiveCount).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenDestinationTooSmall()
        {
            byte[] source = { 0x01, 0x02, 0x03 };
            char[] destination = new char[source.Length - 1];

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                _encoding.GetChars(source, 0, source.Length, destination, 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenDestinationIsNull()
        {
            byte[] source = { 0x42 };

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                _encoding.GetChars(source, 0, source.Length, null, 0)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsRespectsOffsets()
        {
            byte[] source = { 0x10, (byte)'C', (byte)'D', 0x20 };
            char[] destination = new char[6];
            Array.Fill(destination, '\uFFFF');

            int read = _encoding.GetChars(source, 1, 2, destination, 2);

            await Assert.That(read).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(destination[2]).IsEqualTo('C').ConfigureAwait(false);
            await Assert.That(destination[3]).IsEqualTo('D').ConfigureAwait(false);
            await Assert.That(destination[4]).IsEqualTo('\uFFFF').ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsAllowsZeroCountWithoutWriting()
        {
            byte[] source = { 0x01, 0x02 };
            char[] destination = (char[])ExpectedCharSnapshot.Clone();

            int read = _encoding.GetChars(source, source.Length, 0, destination, 1);

            await Assert.That(read).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(destination.AsSpan().SequenceEqual(ExpectedCharSnapshot))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMaxCountThrowsWhenNegative()
        {
            ArgumentOutOfRangeException byteCountException =
                Assert.Throws<ArgumentOutOfRangeException>(() => _encoding.GetMaxByteCount(-1));
            ArgumentOutOfRangeException charCountException =
                Assert.Throws<ArgumentOutOfRangeException>(() => _encoding.GetMaxCharCount(-1));

            await Assert.That(byteCountException).IsNotNull().ConfigureAwait(false);
            await Assert.That(charCountException).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMaxCountsReturnRequestedSize()
        {
            await Assert.That(_encoding.GetMaxByteCount(5)).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(_encoding.GetMaxCharCount(7)).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetByteCountMatchesSourceLength()
        {
            char[] chars = { 'a', 'b', 'c', 'd' };
            int count = _encoding.GetByteCount(chars, 0, chars.Length);

            await Assert.That(count).IsEqualTo(chars.Length).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharCountMatchesSourceLength()
        {
            byte[] bytes = { 0x10, 0x20, 0x30 };
            int count = _encoding.GetCharCount(bytes, 0, bytes.Length);

            await Assert.That(count).IsEqualTo(bytes.Length).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenDestinationIndexIsNegative()
        {
            char[] source = { 'a', 'b', 'c' };
            byte[] destination = new byte[10];

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _encoding.GetBytes(source, 0, source.Length, destination, -1)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenDestinationIndexIsNegative()
        {
            byte[] source = { 0x41, 0x42, 0x43 };
            char[] destination = new char[10];

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _encoding.GetChars(source, 0, source.Length, destination, -1)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetBytesThrowsWhenDestinationIndexExceedsLength()
        {
            char[] source = { 'a' };
            byte[] destination = new byte[3];

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _encoding.GetBytes(source, 0, 1, destination, 5)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetCharsThrowsWhenDestinationIndexExceedsLength()
        {
            byte[] source = { 0x41 };
            char[] destination = new char[3];

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                _encoding.GetChars(source, 0, 1, destination, 5)
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }
    }
}
