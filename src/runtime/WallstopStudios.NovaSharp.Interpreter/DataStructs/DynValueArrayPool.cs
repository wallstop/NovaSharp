namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Buffers;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides pooled <see cref="DynValue"/> arrays to reduce allocations in the VM hot path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Function calls are the most frequent allocation source in the interpreter loop.
    /// This pool uses thread-local caching for small arrays (≤8 elements) and falls back
    /// to <see cref="ArrayPool{T}.Shared"/> for larger arrays.
    /// </para>
    /// <para>
    /// Usage pattern with automatic cleanup:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;DynValue[]&gt; pooled = DynValueArrayPool.Get(8, out DynValue[] array))
    /// {
    ///     // Use array...
    /// } // Automatically returned to pool here
    /// </code>
    /// </remarks>
    internal static class DynValueArrayPool
    {
        private const int MaxSmallArraySize = 16;
        private const int MaxCachedLargeArraySize = 1024;

        [ThreadStatic]
        private static DynValue[][] ThreadLocalSmallArrays;

        private static readonly Action<DynValue[]> ReturnToPool = array =>
            Return(array, clearArray: true);
        private static readonly Action<DynValue[]> NoOpReturn = _ => { };

        private static DynValue[][] GetSmallArrays()
        {
            DynValue[][] arrays = ThreadLocalSmallArrays;
            if (arrays == null)
            {
                arrays = new DynValue[MaxSmallArraySize + 1][];
                ThreadLocalSmallArrays = arrays;
            }
            return arrays;
        }

        /// <summary>
        /// Gets a pooled <see cref="DynValue"/> array and outputs it for immediate use.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <param name="array">The rented array.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the array to the pool when disposed.</returns>
        /// <remarks>
        /// Use with a using statement to ensure proper cleanup:
        /// <code>
        /// using (PooledResource&lt;DynValue[]&gt; pooled = DynValueArrayPool.Get(8, out DynValue[] array))
        /// {
        ///     // Use array...
        /// }
        /// </code>
        /// </remarks>
        public static PooledResource<DynValue[]> Get(int minimumLength, out DynValue[] array)
        {
            if (minimumLength <= 0)
            {
                array = Array.Empty<DynValue>();
                return new PooledResource<DynValue[]>(array, NoOpReturn);
            }

            if (minimumLength <= MaxSmallArraySize)
            {
                DynValue[][] arrays = GetSmallArrays();
                DynValue[] cached = arrays[minimumLength];
                if (cached != null)
                {
                    arrays[minimumLength] = null;
                    array = cached;
                }
                else
                {
                    array = new DynValue[minimumLength];
                }
                return new PooledResource<DynValue[]>(array, ReturnToPool);
            }

            array = ArrayPool<DynValue>.Shared.Rent(minimumLength);
            return new PooledResource<DynValue[]>(array, ReturnToPool);
        }

        /// <summary>
        /// Gets a pooled <see cref="DynValue"/> array.
        /// </summary>
        /// <param name="minimumLength">The minimum required length of the array.</param>
        /// <returns>A <see cref="PooledResource{T}"/> containing the array that returns it to the pool when disposed.</returns>
        public static PooledResource<DynValue[]> Get(int minimumLength)
        {
            return Get(minimumLength, out _);
        }

        /// <summary>
        /// Rents a <see cref="DynValue"/> array of at least the specified size.
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
        /// Prefer using the <see cref="Get(int, out DynValue[])"/> method with a using statement instead.
        /// </remarks>
        public static DynValue[] Rent(int minimumLength)
        {
            if (minimumLength <= 0)
            {
                return Array.Empty<DynValue>();
            }

            if (minimumLength <= MaxSmallArraySize)
            {
                DynValue[][] arrays = GetSmallArrays();
                DynValue[] cached = arrays[minimumLength];
                if (cached != null)
                {
                    arrays[minimumLength] = null;
                    return cached;
                }
                return new DynValue[minimumLength];
            }

            return ArrayPool<DynValue>.Shared.Rent(minimumLength);
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
        public static void Return(DynValue[] array, bool clearArray = true)
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

                DynValue[][] arrays = GetSmallArrays();
                arrays[array.Length] = array;
            }
            else if (array.Length <= MaxCachedLargeArraySize)
            {
                ArrayPool<DynValue>.Shared.Return(array, clearArray);
            }
            // Arrays larger than MaxCachedLargeArraySize are not returned to the pool
            // to avoid bloating the shared pool with oversized arrays.
        }

        /// <summary>
        /// Creates an exact-sized copy of the pooled array's contents and returns the original to the pool.
        /// </summary>
        /// <param name="array">The pooled array to copy from.</param>
        /// <param name="length">The number of elements to copy (must be ≤ array.Length).</param>
        /// <returns>A new array containing exactly <paramref name="length"/> elements.</returns>
        /// <remarks>
        /// Use this when you need to persist the array contents beyond the current scope
        /// but still want to return the pooled array for reuse.
        /// </remarks>
        public static DynValue[] ToArrayAndReturn(DynValue[] array, int length)
        {
            if (array == null || length <= 0)
            {
                Return(array);
                return Array.Empty<DynValue>();
            }

            DynValue[] result = new DynValue[length];
            Array.Copy(array, 0, result, 0, length);
            Return(array, clearArray: true);
            return result;
        }
    }
}
