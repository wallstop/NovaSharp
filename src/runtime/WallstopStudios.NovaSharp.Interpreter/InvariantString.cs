namespace WallstopStudios.NovaSharp.Interpreter
{
    using System;

    /// <summary>
    /// Provides helper methods that convert strings using invariant culture only when casing changes are required.
    /// </summary>
    public static class InvariantString
    {
        /// <summary>
        /// Converts <paramref name="value"/> to lower invariant, avoiding allocations when the string is already lowercase.
        /// </summary>
        /// <param name="value">Input string to normalize.</param>
        /// <returns>The normalized string (possibly the original instance).</returns>
        public static string ToLowerInvariantIfNeeded(string value)
        {
            return ConvertIfNeeded(value, lowerCase: true);
        }

        /// <summary>
        /// Converts <paramref name="value"/> to upper invariant, avoiding allocations when the string is already uppercase.
        /// </summary>
        /// <param name="value">Input string to normalize.</param>
        /// <returns>The normalized string (possibly the original instance).</returns>
        public static string ToUpperInvariantIfNeeded(string value)
        {
            return ConvertIfNeeded(value, lowerCase: false);
        }

        /// <summary>
        /// Converts the provided string to the requested casing if any character differs; otherwise returns the original reference.
        /// </summary>
        private static string ConvertIfNeeded(string value, bool lowerCase)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                char converted = lowerCase
                    ? char.ToLowerInvariant(current)
                    : char.ToUpperInvariant(current);

                if (converted != current)
                {
                    return string.Create(
                        value.Length,
                        (value, lowerCase),
                        static (destination, state) =>
                        {
                            string source = state.value;

                            if (state.lowerCase)
                            {
                                for (int i = 0; i < destination.Length; i++)
                                {
                                    destination[i] = char.ToLowerInvariant(source[i]);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < destination.Length; i++)
                                {
                                    destination[i] = char.ToUpperInvariant(source[i]);
                                }
                            }
                        }
                    );
                }
            }

            return value;
        }
    }
}
