namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;

    /// <summary>
    /// Compares the byte representation of Lua strings using C-locale ordering.
    /// </summary>
    internal static class LuaStringComparison
    {
        internal static int Compare(string left, string right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            int limit = Math.Min(left.Length, right.Length);
            int commonPrefix = 0;
            const int BlockLength = 32;
            if (limit >= BlockLength)
            {
                ReadOnlySpan<char> leftSpan = left.AsSpan();
                ReadOnlySpan<char> rightSpan = right.AsSpan();
                // Let the runtime compare whole blocks before inspecting the first mismatch.
                while (
                    limit - commonPrefix >= BlockLength
                    && leftSpan
                        .Slice(commonPrefix, BlockLength)
                        .SequenceEqual(rightSpan.Slice(commonPrefix, BlockLength))
                )
                {
                    commonPrefix += BlockLength;
                }
            }

            for (int i = commonPrefix; i < limit; i++)
            {
                char leftUnit = left[i];
                char rightUnit = right[i];
                if (leftUnit == rightUnit)
                {
                    continue;
                }

                // Binary constructors store each byte in one UTF-16 code unit.
                if ((leftUnit | rightUnit) <= byte.MaxValue)
                {
                    return leftUnit - rightUnit;
                }

                return CompareEncodedTail(left, right, i);
            }

            if (left.Length != right.Length && limit > 0 && char.IsHighSurrogate(left[limit - 1]))
            {
                return CompareEncodedTail(left, right, limit);
            }

            return left.Length - right.Length;
        }

        private static int CompareEncodedTail(string left, string right, int index)
        {
            // A common high surrogate can pair with the next code unit on either side.
            if (index > 0 && char.IsHighSurrogate(left[index - 1]))
            {
                index--;
            }

            ByteCursor leftCursor = new(left, index);
            ByteCursor rightCursor = new(right, index);
            while (true)
            {
                int leftByte = leftCursor.Read();
                int rightByte = rightCursor.Read();
                if (leftByte != rightByte || leftByte < 0)
                {
                    return leftByte - rightByte;
                }
            }
        }

        private struct ByteCursor
        {
            private readonly string _text;
            private int _index;
            private uint _continuationBytes;

            internal ByteCursor(string text, int index)
            {
                _text = text;
                _index = index;
                _continuationBytes = 0;
            }

            internal int Read()
            {
                if (_continuationBytes != 0)
                {
                    int next = (byte)_continuationBytes;
                    _continuationBytes >>= 8;
                    return next;
                }

                if (_index == _text.Length)
                {
                    return -1;
                }

                int scalar = _text[_index++];
                if (scalar <= byte.MaxValue)
                {
                    return scalar;
                }

                if (char.IsSurrogate((char)scalar))
                {
                    if (
                        char.IsHighSurrogate((char)scalar)
                        && _index < _text.Length
                        && char.IsLowSurrogate(_text[_index])
                    )
                    {
                        scalar = 0x10000 + ((scalar - 0xD800) << 10) + (_text[_index++] - 0xDC00);
                    }
                    else
                    {
                        scalar = 0xFFFD;
                    }
                }

                // Any code unit above the byte range originated in CLR text. Generate its
                // UTF-8 bytes lazily so comparisons do not allocate copies of either string.
                if (scalar <= 0x7FF)
                {
                    _continuationBytes = Continuation(scalar);
                    return 0xC0 | (scalar >> 6);
                }

                if (scalar <= 0xFFFF)
                {
                    _continuationBytes = Continuation(scalar >> 6) | (Continuation(scalar) << 8);
                    return 0xE0 | (scalar >> 12);
                }

                _continuationBytes =
                    Continuation(scalar >> 12)
                    | (Continuation(scalar >> 6) << 8)
                    | (Continuation(scalar) << 16);
                return 0xF0 | (scalar >> 18);
            }

            private static uint Continuation(int scalar) => 0x80u | ((uint)scalar & 0x3Fu);
        }
    }
}
