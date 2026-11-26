namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    /// <summary>
    /// Orders strings the way Lua's <c>l_strcmp</c> does: by byte. NovaSharp stores Lua
    /// bytes as single UTF-16 code units up to U+00FF and decoded source text as Unicode
    /// code points, so comparisons read raw bytes for the former and UTF-8 bytes for the
    /// latter.
    /// </summary>
    internal static class LuaStringOrdering
    {
        /// <summary>
        /// Compares two strings using Lua's built-in string ordering.
        /// </summary>
        /// <returns>A negative value when <paramref name="left"/> sorts first, a positive value when
        /// <paramref name="right"/> does, and zero when the strings are equal.</returns>
        public static int Compare(string left, string right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return right == null ? 0 : -1;
            }

            if (right == null)
            {
                return 1;
            }

            ByteReader leftReader = new(left);
            ByteReader rightReader = new(right);
            while (true)
            {
                int leftByte = leftReader.ReadByte();
                int rightByte = rightReader.ReadByte();
                if (leftByte != rightByte)
                {
                    return leftByte < rightByte ? -1 : 1;
                }

                if (leftByte < 0)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Streams Lua bytes from a CLR string without allocating an encoded copy.
        /// </summary>
        private struct ByteReader
        {
            private readonly string _value;
            private int _index;
            private int _pendingBytes;
            private int _pendingCount;

            internal ByteReader(string value)
            {
                _value = value;
                _index = 0;
                _pendingBytes = 0;
                _pendingCount = 0;
            }

            internal int ReadByte()
            {
                if (_pendingCount > 0)
                {
                    _pendingCount--;
                    int pending = _pendingBytes & 0xFF;
                    _pendingBytes >>= 8;
                    return pending;
                }

                if (_index >= _value.Length)
                {
                    return -1;
                }

                char current = _value[_index++];
                if (current <= '\u00FF')
                {
                    return current;
                }

                int codePoint = current;
                if (
                    char.IsHighSurrogate(current)
                    && _index < _value.Length
                    && char.IsLowSurrogate(_value[_index])
                )
                {
                    codePoint = char.ConvertToUtf32(current, _value[_index]);
                    _index++;
                }

                if (codePoint <= 0x7FF)
                {
                    _pendingBytes = 0x80 | (codePoint & 0x3F);
                    _pendingCount = 1;
                    return 0xC0 | (codePoint >> 6);
                }

                if (codePoint <= 0xFFFF)
                {
                    _pendingBytes =
                        (0x80 | ((codePoint >> 6) & 0x3F)) | ((0x80 | (codePoint & 0x3F)) << 8);
                    _pendingCount = 2;
                    return 0xE0 | (codePoint >> 12);
                }

                _pendingBytes =
                    (0x80 | ((codePoint >> 12) & 0x3F))
                    | ((0x80 | ((codePoint >> 6) & 0x3F)) << 8)
                    | ((0x80 | (codePoint & 0x3F)) << 16);
                _pendingCount = 3;
                return 0xF0 | (codePoint >> 18);
            }
        }
    }
}
