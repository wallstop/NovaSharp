namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides pooled <see cref="DynValue"/> arrays to reduce allocations in the VM hot path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Function calls are the most frequent allocation source in the interpreter loop.
    /// This pool uses thread-local caching for small arrays (≤16 elements) and falls back
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
        private const int MaxCachedLargeArrayBytes = 1024 * 1024;
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(60);

        [ThreadStatic]
        private static SmallArrayCache ThreadLocalSmallArrays;

        private static readonly Action<DynValue[]> ReturnToPool = array =>
            Return(array, clearArray: true);
        private static readonly Action<DynValue[]> NoOpReturn = _ => { };
        private static readonly object CacheRegistrySyncRoot = new();
        private static readonly List<WeakReference<SmallArrayCache>> SmallArrayCaches = new();
        private static long TrimEpoch;
        private static long TrimCount;
        private static long DroppedCount;
        private static long RetainedSmallBytes;
        private static long PeakRetainedSmallBytes;

        static DynValueArrayPool()
        {
            SharedPoolRegistry.Register(new DynValueArrayPoolTrimTarget());
        }

        private readonly struct SmallArrayEntry
        {
            internal SmallArrayEntry(DynValue[] array, long returnedAt)
            {
                Array = array;
                ReturnedAt = returnedAt;
            }

            internal DynValue[] Array { get; }

            internal long ReturnedAt { get; }
        }

        private sealed class SmallArrayCache
        {
            internal readonly object _syncRoot = new();
            internal readonly SmallArrayEntry[] _entries = new SmallArrayEntry[
                MaxSmallArraySize + 1
            ];
            internal long _observedTrimEpoch;
        }

        private sealed class DynValueArrayPoolTrimTarget : IPoolTrimTarget
        {
            public string Name
            {
                get { return nameof(DynValueArrayPool); }
            }

            public PoolStatistics GetStatistics()
            {
                return DynValueArrayPool.GetStatistics();
            }

            public PoolTrimResult Trim(PoolTrimLevel level)
            {
                return DynValueArrayPool.Trim(level);
            }
        }

        private static SmallArrayCache GetSmallArrays()
        {
            SmallArrayCache cache = ThreadLocalSmallArrays;
            if (cache == null)
            {
                cache = new SmallArrayCache();
                cache._observedTrimEpoch = Volatile.Read(ref TrimEpoch);
                ThreadLocalSmallArrays = cache;
                RegisterCache(cache);
            }
            ApplyPendingTrim(cache);
            return cache;
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
                SmallArrayCache cache = GetSmallArrays();
                lock (cache._syncRoot)
                {
                    SmallArrayEntry entry = cache._entries[minimumLength];
                    DynValue[] cached = entry.Array;
                    if (cached != null)
                    {
                        cache._entries[minimumLength] = default;
                        RemoveRetainedSmallArray(cached);
                        array = cached;
                    }
                    else
                    {
                        array = new DynValue[minimumLength];
                    }
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
        /// For small arrays (≤16 elements), uses thread-local caching for zero-contention access.
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
                SmallArrayCache cache = GetSmallArrays();
                lock (cache._syncRoot)
                {
                    SmallArrayEntry entry = cache._entries[minimumLength];
                    DynValue[] cached = entry.Array;
                    if (cached != null)
                    {
                        cache._entries[minimumLength] = default;
                        RemoveRetainedSmallArray(cached);
                        return cached;
                    }
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

                SmallArrayCache cache = GetSmallArrays();
                lock (cache._syncRoot)
                {
                    SmallArrayEntry existing = cache._entries[array.Length];
                    if (existing.Array != null && !ReferenceEquals(existing.Array, array))
                    {
                        DropSmallArray(existing.Array);
                        AddRetainedSmallArray(array);
                    }
                    else if (existing.Array == null)
                    {
                        AddRetainedSmallArray(array);
                    }

                    cache._entries[array.Length] = new SmallArrayEntry(
                        array,
                        SharedPoolRegistry.Clock.GetTimestamp()
                    );
                }
            }
            else if (EstimateArrayBytes(array.Length) <= MaxCachedLargeArrayBytes)
            {
                ArrayPool<DynValue>.Shared.Return(array, clearArray);
            }
            else
            {
                Interlocked.Increment(ref DroppedCount);
            }
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

        internal static PoolStatistics GetStatistics()
        {
            int retainedCount = 0;
            long retainedBytes = 0L;

            SmallArrayCache[] caches = SnapshotCaches();
            for (int i = 0; i < caches.Length; i++)
            {
                SmallArrayCache cache = caches[i];
                ApplyPendingTrim(cache);
                lock (cache._syncRoot)
                {
                    for (int slot = 1; slot < cache._entries.Length; slot++)
                    {
                        DynValue[] array = cache._entries[slot].Array;
                        if (array != null)
                        {
                            retainedCount++;
                            retainedBytes += EstimateArrayBytes(array.Length);
                        }
                    }
                }
            }

            return new PoolStatistics(
                nameof(DynValueArrayPool),
                retainedCount,
                retainedBytes,
                Volatile.Read(ref PeakRetainedSmallBytes),
                Volatile.Read(ref TrimCount),
                Volatile.Read(ref DroppedCount)
            );
        }

        internal static PoolTrimResult Trim(PoolTrimLevel level)
        {
            Interlocked.Increment(ref TrimCount);
            if (level == PoolTrimLevel.Critical)
            {
                Interlocked.Increment(ref TrimEpoch);
            }

            SmallArrayCache[] caches = SnapshotCaches();
            int trimmedCount = 0;
            long releasedBytes = 0L;
            long now = SharedPoolRegistry.Clock.GetTimestamp();

            for (int i = 0; i < caches.Length; i++)
            {
                PoolTrimResult result = TrimCache(caches[i], level, now);
                trimmedCount += result.TrimmedCount;
                releasedBytes += result.EstimatedReleasedBytes;
            }

            return new PoolTrimResult(trimmedCount, releasedBytes);
        }

        private static PoolTrimResult TrimCache(
            SmallArrayCache cache,
            PoolTrimLevel level,
            long now
        )
        {
            int trimmedCount = 0;
            long releasedBytes = 0L;
            long idleTimeoutTicks = SharedPoolRegistry.Clock.ToTimestampTicks(IdleTimeout);

            lock (cache._syncRoot)
            {
                for (int i = 1; i < cache._entries.Length; i++)
                {
                    SmallArrayEntry entry = cache._entries[i];
                    DynValue[] array = entry.Array;
                    if (array == null)
                    {
                        continue;
                    }

                    bool trim =
                        level != PoolTrimLevel.Idle || now - entry.ReturnedAt >= idleTimeoutTicks;
                    if (!trim)
                    {
                        continue;
                    }

                    cache._entries[i] = default;
                    trimmedCount++;
                    releasedBytes += EstimateArrayBytes(array.Length);
                    RemoveRetainedSmallArray(array);
                    Interlocked.Increment(ref DroppedCount);
                }

                cache._observedTrimEpoch = Volatile.Read(ref TrimEpoch);
            }

            return new PoolTrimResult(trimmedCount, releasedBytes);
        }

        private static void ApplyPendingTrim(SmallArrayCache cache)
        {
            long epoch = Volatile.Read(ref TrimEpoch);
            if (Volatile.Read(ref cache._observedTrimEpoch) == epoch)
            {
                return;
            }

            TrimCache(cache, PoolTrimLevel.Critical, SharedPoolRegistry.Clock.GetTimestamp());
        }

        private static void RegisterCache(SmallArrayCache cache)
        {
            lock (CacheRegistrySyncRoot)
            {
                SmallArrayCaches.Add(new WeakReference<SmallArrayCache>(cache));
            }
        }

        private static SmallArrayCache[] SnapshotCaches()
        {
            lock (CacheRegistrySyncRoot)
            {
                List<SmallArrayCache> caches = new(SmallArrayCaches.Count);
                for (int i = SmallArrayCaches.Count - 1; i >= 0; i--)
                {
                    WeakReference<SmallArrayCache> weakReference = SmallArrayCaches[i];
                    if (weakReference.TryGetTarget(out SmallArrayCache cache))
                    {
                        caches.Add(cache);
                    }
                    else
                    {
                        SmallArrayCaches.RemoveAt(i);
                    }
                }

                return caches.ToArray();
            }
        }

        private static void AddRetainedSmallArray(DynValue[] array)
        {
            long retainedBytes = Interlocked.Add(
                ref RetainedSmallBytes,
                EstimateArrayBytes(array.Length)
            );
            UpdatePeak(ref PeakRetainedSmallBytes, retainedBytes);
        }

        private static void RemoveRetainedSmallArray(DynValue[] array)
        {
            Interlocked.Add(ref RetainedSmallBytes, -EstimateArrayBytes(array.Length));
        }

        private static void DropSmallArray(DynValue[] array)
        {
            RemoveRetainedSmallArray(array);
            Interlocked.Increment(ref DroppedCount);
        }

        private static long EstimateArrayBytes(int length)
        {
            return IntPtr.Size + ((long)length * PoolElementSize<DynValue>.EstimatedBytes);
        }

        private static void UpdatePeak(ref long target, long value)
        {
            long snapshot;
            do
            {
                snapshot = Volatile.Read(ref target);
                if (value <= snapshot)
                {
                    return;
                }
            } while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
        }
    }
}
