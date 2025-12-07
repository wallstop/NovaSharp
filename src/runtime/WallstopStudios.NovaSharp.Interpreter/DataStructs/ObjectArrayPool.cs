namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Provides pooled <see cref="object"/> arrays to reduce allocations in CLR interop paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CLR method invocation via reflection is a frequent allocation source.
    /// This pool uses thread-local caching for small arrays (≤8 elements) and falls back
    /// to <see cref="ArrayPool{T}.Shared"/> for larger arrays.
    /// </para>
    /// <para>
    /// Usage pattern with automatic cleanup:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;object[]&gt; pooled = ObjectArrayPool.Get(8, out object[] array))
    /// {
    ///     // Use array for reflection invocation...
    /// } // Automatically returned to pool here
    /// </code>
    /// </remarks>
    internal static class ObjectArrayPool
    {
        private const int MaxSmallArraySize = 8;
        private const int MaxCachedLargeArraySize = 256;

        [ThreadStatic]
        private static object[][] ThreadLocalSmallArrays;

        private static readonly Action<object[]> ReturnToPool = array =>
            Return(array, clearArray: true);
        private static readonly Action<object[]> NoOpReturn = _ => { };

        private static object[][] GetSmallArrays()
        {
            object[][] arrays = ThreadLocalSmallArrays;
            if (arrays == null)
            {
                arrays = new object[MaxSmallArraySize + 1][];
                ThreadLocalSmallArrays = arrays;
            }
            return arrays;
        }

        /// <summary>
        /// Gets a pooled <see cref="object"/> array and outputs it for immediate use.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <param name="array">The rented array.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the array to the pool when disposed.</returns>
        /// <remarks>
        /// Use with a using statement to ensure proper cleanup:
        /// <code>
        /// using (PooledResource&lt;object[]&gt; pooled = ObjectArrayPool.Get(8, out object[] array))
        /// {
        ///     // Use array...
        /// }
        /// </code>
        /// </remarks>
        public static PooledResource<object[]> Get(int minimumLength, out object[] array)
        {
            if (minimumLength <= 0)
            {
                array = Array.Empty<object>();
                return new PooledResource<object[]>(array, NoOpReturn);
            }

            if (minimumLength <= MaxSmallArraySize)
            {
                object[][] arrays = GetSmallArrays();
                object[] cached = arrays[minimumLength];
                if (cached != null)
                {
                    arrays[minimumLength] = null;
                    array = cached;
                }
                else
                {
                    array = new object[minimumLength];
                }
                return new PooledResource<object[]>(array, ReturnToPool);
            }

            // For larger arrays, we must use exact-size arrays because reflection
            // invocation (MethodInfo.Invoke) requires exact parameter count.
            // ArrayPool.Shared.Rent may return larger arrays which causes
            // TargetParameterCountException. We allocate exactly and don't pool
            // these larger arrays.
            array = new object[minimumLength];
            return new PooledResource<object[]>(array, NoOpReturn);
        }

        /// <summary>
        /// Gets a pooled <see cref="object"/> array.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <returns>A <see cref="PooledResource{T}"/> containing the array that returns it to the pool when disposed.</returns>
        public static PooledResource<object[]> Get(int minimumLength)
        {
            return Get(minimumLength, out _);
        }

        /// <summary>
        /// Rents an <see cref="object"/> array of at least the specified size.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <returns>
        /// A pooled array with length greater than or equal to <paramref name="minimumLength"/>.
        /// The array may contain stale data and should be cleared or overwritten before use.
        /// </returns>
        /// <remarks>
        /// For small arrays (≤8 elements), uses thread-local caching for zero-contention access.
        /// Larger arrays are obtained from <see cref="ArrayPool{T}.Shared"/>.
        /// Always pair with <see cref="Return"/> to avoid memory leaks.
        /// Prefer using the <see cref="Get(int, out object[])"/> method with a using statement instead.
        /// </remarks>
        public static object[] Rent(int minimumLength)
        {
            if (minimumLength <= 0)
            {
                return Array.Empty<object>();
            }

            if (minimumLength <= MaxSmallArraySize)
            {
                object[][] arrays = GetSmallArrays();
                object[] cached = arrays[minimumLength];
                if (cached != null)
                {
                    arrays[minimumLength] = null;
                    return cached;
                }
                return new object[minimumLength];
            }

            return ArrayPool<object>.Shared.Rent(minimumLength);
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
        public static void Return(object[] array, bool clearArray = true)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            if (array.Length <= MaxSmallArraySize)
            {
                if (clearArray)
                {
                    Array.Clear(array, 0, array.Length);
                }

                object[][] arrays = GetSmallArrays();
                arrays[array.Length] = array;
            }
            else if (array.Length <= MaxCachedLargeArraySize)
            {
                ArrayPool<object>.Shared.Return(array, clearArray);
            }
            // Arrays larger than MaxCachedLargeArraySize are not returned to the pool
            // to avoid bloating the shared pool with oversized arrays.
        }
    }
}
