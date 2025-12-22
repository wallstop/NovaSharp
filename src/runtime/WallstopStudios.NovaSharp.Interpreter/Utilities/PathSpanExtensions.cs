namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Path-related helpers that operate on spans to avoid transient string allocations.
    /// </summary>
    public static class PathSpanExtensions
    {
        private const char UnixSeparator = '/';
        private const char WindowsSeparator = '\\';

        /// <summary>
        /// Returns the substring that follows the last directory separator in <paramref name="path"/>,
        /// reusing the original string when no trimming is required.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SliceAfterLastSeparator(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            ReadOnlySpan<char> span = path.AsSpan();
            int slash = span.LastIndexOf(UnixSeparator);
            int backslash = span.LastIndexOf(WindowsSeparator);
            int start = Math.Max(slash, backslash) + 1;

            if (start <= 0)
            {
                return path;
            }

            ReadOnlySpan<char> slice = span.Slice(start);
            return slice.Length == path.Length ? path : new string(slice);
        }

        /// <summary>
        /// Copies <paramref name="source"/> into <paramref name="destination"/>, replacing both
        /// directory separator characters with <paramref name="replacement"/>.
        /// </summary>
        public static void CopyReplacingDirectorySeparators(
            ReadOnlySpan<char> source,
            Span<char> destination,
            char replacement
        )
        {
            if (destination.Length < source.Length)
            {
                throw new ArgumentException(
                    "Destination span must be at least as long as the source span.",
                    nameof(destination)
                );
            }

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                destination[i] = c == UnixSeparator || c == WindowsSeparator ? replacement : c;
            }
        }

        /// <summary>
        /// Normalizes directory separators within <paramref name="path"/> by replacing backslashes or slashes
        /// with <paramref name="replacement"/> only when needed.
        /// </summary>
        public static string NormalizeDirectorySeparators(this string path, char replacement)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            bool needsNormalization = false;
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if ((c == UnixSeparator || c == WindowsSeparator) && c != replacement)
                {
                    needsNormalization = true;
                    break;
                }
            }

            if (!needsNormalization)
            {
                return path;
            }

            return string.Create(
                path.Length,
                (path, replacement),
                static (span, state) =>
                {
                    ReadOnlySpan<char> source = state.path.AsSpan();
                    for (int i = 0; i < source.Length; i++)
                    {
                        char c = source[i];
                        span[i] =
                            (c == UnixSeparator || c == WindowsSeparator) ? state.replacement : c;
                    }
                }
            );
        }
    }
}
