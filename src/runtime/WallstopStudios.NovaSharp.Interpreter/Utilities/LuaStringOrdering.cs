namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    using System;

    /// <summary>
    /// Orders decoded source text the way Lua's <c>l_strcmp</c> compares UTF-8 source bytes.
    /// NovaSharp stores source text as UTF-16, so comparing code points reproduces the byte
    /// order for BMP and supplementary-plane text. Raw byte strings from <c>string.char</c>
    /// or byte escapes remain governed by the string byte-identity work in issue #201.
    /// </summary>
    internal static class LuaStringOrdering
    {
        /// <summary>
        /// Compares two strings using Lua's built-in string ordering for Unicode source text.
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

            int commonLength = Math.Min(left.Length, right.Length);
            int index = 0;
            while (index < commonLength)
            {
                char leftChar = left[index];
                char rightChar = right[index];

                if (leftChar == rightChar)
                {
                    index++;
                    continue;
                }

                int leftCodePoint = leftChar;
                int leftLength = 1;
                if (
                    char.IsHighSurrogate(leftChar)
                    && index + 1 < left.Length
                    && char.IsLowSurrogate(left[index + 1])
                )
                {
                    leftCodePoint = char.ConvertToUtf32(leftChar, left[index + 1]);
                    leftLength = 2;
                }

                int rightCodePoint = rightChar;
                int rightLength = 1;
                if (
                    char.IsHighSurrogate(rightChar)
                    && index + 1 < right.Length
                    && char.IsLowSurrogate(right[index + 1])
                )
                {
                    rightCodePoint = char.ConvertToUtf32(rightChar, right[index + 1]);
                    rightLength = 2;
                }

                if (leftCodePoint != rightCodePoint)
                {
                    return leftCodePoint < rightCodePoint ? -1 : 1;
                }

                index += Math.Max(leftLength, rightLength);
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
