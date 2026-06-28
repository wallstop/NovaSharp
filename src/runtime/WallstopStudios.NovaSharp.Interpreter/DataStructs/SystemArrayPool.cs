namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Provides pooled arrays of any element type via <see cref="ArrayPool{T}.Shared"/>,
    /// with automatic disposal using the <see cref="PooledResource{T}"/> pattern.
    /// </summary>
    /// <typeparam name="T">The element type for the pooled arrays.</typeparam>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="DynValueArrayPool"/> and <see cref="ObjectArrayPool"/> which use
    /// thread-local caching for exact-size small arrays, this pool delegates directly to
    /// <see cref="ArrayPool{T}.Shared"/> and may return arrays larger than requested.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> The returned array's <c>Length</c> property may be larger
    /// than the requested <paramref name="minimumLength"/>. Callers must track the actual
    /// number of elements used separately from the array length.
    /// </para>
    /// <para>
    /// Usage pattern with automatic cleanup:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;char[]&gt; pooled = SystemArrayPool&lt;char&gt;.Get(256, out char[] buffer))
    /// {
    ///     // buffer.Length >= 256 (may be larger!)
    ///     int written = FormatValue(value, buffer);
    ///     return new string(buffer, 0, written);
    /// } // Automatically returned to pool here
    /// </code>
    /// </remarks>
    internal static class SystemArrayPool<T>
    {
        private const int MaxCachedArraySize = 1024 * 1024; // 1MB worth of elements

        private static readonly Action<T[]> ReturnToPoolCleared = array =>
            Return(array, clearArray: true);
        private static readonly Action<T[]> ReturnToPoolUncleared = array =>
            Return(array, clearArray: false);
        private static readonly Action<T[]> NoOpReturn = _ => { };

        /// <summary>
        /// Gets a pooled array of at least the specified length and outputs it for immediate use.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <param name="array">The rented array. May be larger than <paramref name="minimumLength"/>.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the array to the pool when disposed.</returns>
        /// <remarks>
        /// <para>
        /// Use with a using statement to ensure proper cleanup:
        /// </para>
        /// <code>
        /// using (PooledResource&lt;char[]&gt; pooled = SystemArrayPool&lt;char&gt;.Get(100, out char[] buffer))
        /// {
        ///     // buffer.Length >= 100
        ///     // Track actual usage separately!
        /// }
        /// </code>
        /// <para>
        /// Arrays are cleared before being returned to the pool when the PooledResource is disposed.
        /// </para>
        /// </remarks>
        public static PooledResource<T[]> Get(int minimumLength, out T[] array)
        {
            return Get(minimumLength, clearOnReturn: true, out array);
        }

        /// <summary>
        /// Gets a pooled array of at least the specified length with control over clearing behavior.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <param name="clearOnReturn">
        /// When <c>true</c>, clears the array contents before returning to the pool.
        /// Set to <c>false</c> only when the array will be fully overwritten before next use.
        /// </param>
        /// <param name="array">The rented array. May be larger than <paramref name="minimumLength"/>.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the array to the pool when disposed.</returns>
        public static PooledResource<T[]> Get(int minimumLength, bool clearOnReturn, out T[] array)
        {
            if (minimumLength <= 0)
            {
                array = Array.Empty<T>();
                return new PooledResource<T[]>(array, NoOpReturn);
            }

            array = ArrayPool<T>.Shared.Rent(minimumLength);
            Action<T[]> returnAction = clearOnReturn ? ReturnToPoolCleared : ReturnToPoolUncleared;
            return new PooledResource<T[]>(array, returnAction);
        }

        /// <summary>
        /// Gets a pooled array of at least the specified length.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <returns>A <see cref="PooledResource{T}"/> containing the array that returns it to the pool when disposed.</returns>
        public static PooledResource<T[]> Get(int minimumLength)
        {
            return Get(minimumLength, out _);
        }

        /// <summary>
        /// Rents an array of at least the specified size from the shared pool.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <returns>
        /// A pooled array with length greater than or equal to <paramref name="minimumLength"/>.
        /// The array may contain stale data and should be cleared or overwritten before use.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> The returned array may be larger than requested.
        /// Always track the actual number of elements used separately from array.Length.
        /// </para>
        /// <para>
        /// Always pair with <see cref="Return"/> to avoid memory leaks.
        /// Prefer using the <see cref="Get(int, out T[])"/> method with a using statement instead.
        /// </para>
        /// </remarks>
        public static T[] Rent(int minimumLength)
        {
            if (minimumLength <= 0)
            {
                return Array.Empty<T>();
            }

            return ArrayPool<T>.Shared.Rent(minimumLength);
        }

        /// <summary>
        /// Returns a rented array to the pool for reuse.
        /// </summary>
        /// <param name="array">The array to return. May be null (no-op).</param>
        /// <param name="clearArray">
        /// When <c>true</c>, clears the array contents before pooling to avoid retaining references.
        /// Defaults to <c>true</c> for safety; set to <c>false</c> only when performance is critical
        /// and the array contents are known to be overwritten before next use.
        /// </param>
        /// <remarks>
        /// <para>
        /// Very large arrays (over <see cref="MaxCachedArraySize"/> elements) are not returned to
        /// the pool to avoid memory bloat.
        /// </para>
        /// </remarks>
        public static void Return(T[] array, bool clearArray = true)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            // Don't return very large arrays to prevent memory bloat
            if (array.Length > MaxCachedArraySize)
            {
                return;
            }

            ArrayPool<T>.Shared.Return(array, clearArray);
        }

        /// <summary>
        /// Creates an exact-sized copy of a portion of the pooled array's contents and returns the original to the pool.
        /// </summary>
        /// <param name="array">The pooled array to copy from.</param>
        /// <param name="length">The number of elements to copy (must be ≤ array.Length).</param>
        /// <returns>A new array containing exactly <paramref name="length"/> elements.</returns>
        /// <remarks>
        /// Use this when you need to persist the array contents beyond the current scope
        /// but still want to return the pooled array for reuse.
        /// </remarks>
        public static T[] ToArrayAndReturn(T[] array, int length)
        {
            if (array == null || length <= 0)
            {
                Return(array);
                return Array.Empty<T>();
            }

            T[] result = new T[length];
            Array.Copy(array, 0, result, 0, length);
            Return(array, clearArray: true);
            return result;
        }

        /// <summary>
        /// Creates an exact-sized copy of a portion of the pooled array's contents and returns the original to the pool.
        /// </summary>
        /// <param name="array">The pooled array to copy from.</param>
        /// <param name="startIndex">The starting index in the source array.</param>
        /// <param name="length">The number of elements to copy.</param>
        /// <returns>A new array containing exactly <paramref name="length"/> elements.</returns>
        public static T[] ToArrayAndReturn(T[] array, int startIndex, int length)
        {
            if (array == null || length <= 0)
            {
                Return(array);
                return Array.Empty<T>();
            }

            T[] result = new T[length];
            Array.Copy(array, startIndex, result, 0, length);
            Return(array, clearArray: true);
            return result;
        }
    }
}
