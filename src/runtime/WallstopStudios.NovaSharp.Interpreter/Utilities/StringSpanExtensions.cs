namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Common string/span helpers shared across runtime components to avoid repeated trimming/normalization logic.
    /// </summary>
    public static class StringSpanExtensions
    {
        /// <summary>
        /// Trims leading and trailing whitespace from the provided span without allocating.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> TrimWhitespace(this ReadOnlySpan<char> span)
        {
            int start = 0;
            int end = span.Length - 1;

            while (start <= end && char.IsWhiteSpace(span[start]))
            {
                start++;
            }

            while (end >= start && char.IsWhiteSpace(span[end]))
            {
                end--;
            }

            return start > end ? ReadOnlySpan<char>.Empty : span.Slice(start, end - start + 1);
        }

        /// <summary>
        /// Returns <c>true</c> when the span contains at least one non-whitespace character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasContent(this ReadOnlySpan<char> span)
        {
            foreach (char c in span)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
