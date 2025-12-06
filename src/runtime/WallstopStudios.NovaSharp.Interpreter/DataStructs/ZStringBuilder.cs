namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using Cysharp.Text;

    /// <summary>
    /// Provides zero-allocation string building using Cysharp's ZString library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ZString provides struct-based string builders that rent buffers from ArrayPool,
    /// avoiding StringBuilder allocation entirely. This is critical for high-frequency
    /// operations like lexing and serialization.
    /// </para>
    /// <para>
    /// Usage pattern:
    /// </para>
    /// <code>
    /// using (Utf16ValueStringBuilder sb = ZStringBuilder.Create())
    /// {
    ///     sb.Append("Hello");
    ///     sb.Append(' ');
    ///     sb.Append(42);
    ///     string result = sb.ToString();
    /// }
    /// </code>
    /// <para>
    /// For nested/recursive usage, use <see cref="CreateNested"/> which uses ArrayPool
    /// instead of the faster but non-reentrant ThreadStatic buffer.
    /// </para>
    /// </remarks>
    internal static class ZStringBuilder
    {
        /// <summary>
        /// Creates a new UTF-16 string builder using ArrayPool for safe nested usage.
        /// </summary>
        /// <returns>A new <see cref="Utf16ValueStringBuilder"/> that must be disposed.</returns>
        /// <remarks>
        /// This uses ArrayPool which is safe for nested or recursive string building operations.
        /// For hot paths that are guaranteed non-nested, use <see cref="CreateNonNested"/> for
        /// slightly better performance via ThreadStatic buffer.
        /// </remarks>
        public static Utf16ValueStringBuilder Create()
        {
            return ZString.CreateStringBuilder(notNested: false);
        }

        /// <summary>
        /// Creates a new UTF-16 string builder that supports nested/recursive usage.
        /// </summary>
        /// <returns>A new <see cref="Utf16ValueStringBuilder"/> that must be disposed.</returns>
        /// <remarks>
        /// Alias for <see cref="Create"/> - kept for explicit documentation purposes.
        /// </remarks>
        public static Utf16ValueStringBuilder CreateNested()
        {
            return ZString.CreateStringBuilder(notNested: false);
        }

        /// <summary>
        /// Creates a new UTF-16 string builder using a ThreadStatic buffer for maximum performance.
        /// </summary>
        /// <returns>A new <see cref="Utf16ValueStringBuilder"/> that must be disposed.</returns>
        /// <remarks>
        /// CAUTION: This cannot be nested - only use when you are certain no other string builder
        /// is active on the current thread. For most use cases, prefer <see cref="Create"/>.
        /// </remarks>
        public static Utf16ValueStringBuilder CreateNonNested()
        {
            return ZString.CreateStringBuilder(notNested: true);
        }

        /// <summary>
        /// Creates a new UTF-8 string builder using ArrayPool for safe nested usage.
        /// </summary>
        /// <returns>A new <see cref="Utf8ValueStringBuilder"/> that must be disposed.</returns>
        /// <remarks>
        /// Useful for building UTF-8 encoded strings directly (e.g., for network output).
        /// </remarks>
        public static Utf8ValueStringBuilder CreateUtf8()
        {
            return ZString.CreateUtf8StringBuilder(notNested: false);
        }

        /// <summary>
        /// Creates a new UTF-8 string builder that supports nested/recursive usage.
        /// </summary>
        /// <returns>A new <see cref="Utf8ValueStringBuilder"/> that must be disposed.</returns>
        public static Utf8ValueStringBuilder CreateUtf8Nested()
        {
            return ZString.CreateUtf8StringBuilder(notNested: false);
        }

        /// <summary>
        /// Concatenates values with zero allocation overhead.
        /// </summary>
        /// <typeparam name="T1">Type of first value.</typeparam>
        /// <typeparam name="T2">Type of second value.</typeparam>
        /// <param name="arg1">First value.</param>
        /// <param name="arg2">Second value.</param>
        /// <returns>Concatenated string.</returns>
        public static string Concat<T1, T2>(T1 arg1, T2 arg2)
        {
            return ZString.Concat(arg1, arg2);
        }

        /// <summary>
        /// Concatenates values with zero allocation overhead.
        /// </summary>
        /// <typeparam name="T1">Type of first value.</typeparam>
        /// <typeparam name="T2">Type of second value.</typeparam>
        /// <typeparam name="T3">Type of third value.</typeparam>
        /// <param name="arg1">First value.</param>
        /// <param name="arg2">Second value.</param>
        /// <param name="arg3">Third value.</param>
        /// <returns>Concatenated string.</returns>
        public static string Concat<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            return ZString.Concat(arg1, arg2, arg3);
        }

        /// <summary>
        /// Concatenates values with zero allocation overhead.
        /// </summary>
        public static string Concat<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return ZString.Concat(arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Formats a string with zero allocation overhead.
        /// </summary>
        /// <typeparam name="T1">Type of first argument.</typeparam>
        /// <param name="format">Format string.</param>
        /// <param name="arg1">First argument.</param>
        /// <returns>Formatted string.</returns>
        public static string Format<T1>(string format, T1 arg1)
        {
            return ZString.Format(format, arg1);
        }

        /// <summary>
        /// Formats a string with zero allocation overhead.
        /// </summary>
        public static string Format<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            return ZString.Format(format, arg1, arg2);
        }

        /// <summary>
        /// Formats a string with zero allocation overhead.
        /// </summary>
        public static string Format<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            return ZString.Format(format, arg1, arg2, arg3);
        }

        /// <summary>
        /// Joins elements with a separator with zero allocation overhead.
        /// </summary>
        /// <typeparam name="T">Type of elements.</typeparam>
        /// <param name="separator">Separator character.</param>
        /// <param name="values">Values to join.</param>
        /// <returns>Joined string.</returns>
        public static string Join<T>(char separator, T[] values)
        {
            return ZString.Join(separator, values);
        }

        /// <summary>
        /// Joins elements with a separator with zero allocation overhead.
        /// </summary>
        public static string Join<T>(string separator, T[] values)
        {
            return ZString.Join(separator, values);
        }
    }
}
