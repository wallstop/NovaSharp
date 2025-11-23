namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using System.Text;

    /// <summary>
    /// Simple single-byte encoding used by Lua file IO to preserve raw byte values without conversion.
    /// </summary>
    internal class BinaryEncoding : Encoding
    {
        /// <summary>
        /// Creates a new instance of the binary encoding.
        /// </summary>
        public BinaryEncoding()
            : base() { }

        /// <inheritdoc />
        public override int GetByteCount(char[] chars, int index, int count)
        {
            ValidateBufferRange(chars, nameof(chars), index, count);
            return count;
        }

        /// <inheritdoc />
        public override int GetBytes(
            char[] chars,
            int charIndex,
            int charCount,
            byte[] bytes,
            int byteIndex
        )
        {
            ValidateBufferRange(chars, nameof(chars), charIndex, charCount);
            ValidateDestination(bytes, nameof(bytes), byteIndex, charCount);

            for (int i = 0; i < charCount; i++)
            {
                bytes[byteIndex + i] = (byte)((int)chars[charIndex + i]);
            }

            return charCount;
        }

        /// <inheritdoc />
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            ValidateBufferRange(bytes, nameof(bytes), index, count);
            return count;
        }

        /// <inheritdoc />
        public override int GetChars(
            byte[] bytes,
            int byteIndex,
            int byteCount,
            char[] chars,
            int charIndex
        )
        {
            ValidateBufferRange(bytes, nameof(bytes), byteIndex, byteCount);
            ValidateDestination(chars, nameof(chars), charIndex, byteCount);

            for (int i = 0; i < byteCount; i++)
            {
                chars[charIndex + i] = (char)((int)bytes[byteIndex + i]);
            }

            return byteCount;
        }

        /// <inheritdoc />
        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(charCount));
            }

            return charCount;
        }

        /// <inheritdoc />
        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            return byteCount;
        }

        private static void ValidateBufferRange<T>(
            T[] buffer,
            string paramName,
            int index,
            int count
        )
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (index < 0 || count < 0 || index > buffer.Length - count)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }
        }

        private static void ValidateDestination<T>(
            T[] buffer,
            string paramName,
            int index,
            int required
        )
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (index < 0 || index > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(paramName);
            }

            if (buffer.Length - index < required)
            {
                throw new ArgumentException("Destination buffer is not large enough.", paramName);
            }
        }
    }
}
