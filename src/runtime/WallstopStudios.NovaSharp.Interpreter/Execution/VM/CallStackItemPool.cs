namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Provides pooled <see cref="CallStackItem"/> instances to reduce allocations in the VM hot path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every function call in the interpreter creates a new <see cref="CallStackItem"/> for tracking
    /// locals, closures, error handlers, and debugging information. This pool reuses these objects
    /// to avoid heap allocations on each call.
    /// </para>
    /// <para>
    /// Usage pattern with automatic cleanup:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;CallStackItem&gt; pooled = CallStackItemPool.Get(out CallStackItem item))
    /// {
    ///     item.BasePointer = currentBase;
    ///     item.ReturnAddress = returnAddr;
    ///     // ...use item...
    /// } // Automatically returned to pool here
    /// </code>
    /// </remarks>
    internal static class CallStackItemPool
    {
        private const int MaxPoolSize = 64;
        private const int EstimatedItemBytes = 128;
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(60);

        [ThreadStatic]
        private static CallStackCache ThreadLocalPool;

        private static readonly Action<CallStackItem> ReturnToPool = item => Return(item);
        private static readonly object CacheRegistrySyncRoot = new();
        private static readonly List<WeakReference<CallStackCache>> Caches = new();
        private static long TrimEpoch;
        private static long TrimCount;
        private static long DroppedCount;
        private static long RetainedBytes;
        private static long PeakRetainedBytes;

        static CallStackItemPool()
        {
            SharedPoolRegistry.Register(new CallStackItemPoolTrimTarget());
        }

        private readonly struct PoolEntry
        {
            internal PoolEntry(CallStackItem item, long returnedAt)
            {
                Item = item;
                ReturnedAt = returnedAt;
            }

            internal CallStackItem Item { get; }

            internal long ReturnedAt { get; }
        }

        private sealed class CallStackCache
        {
            internal readonly object _syncRoot = new();
            internal readonly Stack<PoolEntry> _pool = new(16);
            internal long _observedTrimEpoch;
        }

        private sealed class CallStackItemPoolTrimTarget : IPoolTrimTarget
        {
            public string Name
            {
                get { return nameof(CallStackItemPool); }
            }

            public PoolStatistics GetStatistics()
            {
                return CallStackItemPool.GetStatistics();
            }

            public PoolTrimResult Trim(PoolTrimLevel level)
            {
                return CallStackItemPool.Trim(level);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CallStackCache GetPool()
        {
            CallStackCache cache = ThreadLocalPool;
            if (cache == null)
            {
                cache = new CallStackCache();
                Volatile.Write(ref cache._observedTrimEpoch, Volatile.Read(ref TrimEpoch));
                ThreadLocalPool = cache;
                RegisterCache(cache);
            }
            ApplyPendingTrim(cache);
            return cache;
        }

        /// <summary>
        /// Gets a pooled <see cref="CallStackItem"/> and outputs it for immediate use.
        /// </summary>
        /// <param name="item">The rented call stack item, reset to default state.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the item to the pool when disposed.</returns>
        public static PooledResource<CallStackItem> Get(out CallStackItem item)
        {
            item = Rent();
            return new PooledResource<CallStackItem>(item, ReturnToPool);
        }

        /// <summary>
        /// Rents a <see cref="CallStackItem"/> from the pool.
        /// </summary>
        /// <returns>A pooled or new call stack item, reset to default state.</returns>
        /// <remarks>
        /// Always pair with <see cref="Return"/> to avoid memory leaks.
        /// Prefer using the <see cref="Get(out CallStackItem)"/> method with a using statement instead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallStackItem Rent()
        {
            CallStackCache cache = GetPool();
            lock (cache._syncRoot)
            {
                if (cache._pool.Count > 0)
                {
                    PoolEntry entry = cache._pool.Pop();
                    RemoveRetainedItem();
                    return entry.Item;
                }
            }

            return new CallStackItem();
        }

        /// <summary>
        /// Returns a rented <see cref="CallStackItem"/> to the pool for reuse.
        /// </summary>
        /// <param name="item">The item to return. May be null (no-op).</param>
        public static void Return(CallStackItem item)
        {
            if (item == null)
            {
                return;
            }

            item.Reset();

            CallStackCache cache = GetPool();
            lock (cache._syncRoot)
            {
                if (cache._pool.Count < MaxPoolSize)
                {
                    cache._pool.Push(new PoolEntry(item, SharedPoolRegistry.Clock.GetTimestamp()));
                    AddRetainedItem();
                    return;
                }
            }

            Interlocked.Increment(ref DroppedCount);
        }

        internal static PoolStatistics GetStatistics()
        {
            int retainedCount = 0;
            CallStackCache[] caches = SnapshotCaches();
            for (int i = 0; i < caches.Length; i++)
            {
                CallStackCache cache = caches[i];
                ApplyPendingTrim(cache);
                lock (cache._syncRoot)
                {
                    retainedCount += cache._pool.Count;
                }
            }

            long retainedBytes = (long)retainedCount * EstimatedItemBytes;
            return new PoolStatistics(
                nameof(CallStackItemPool),
                retainedCount,
                retainedBytes,
                Volatile.Read(ref PeakRetainedBytes),
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

            CallStackCache[] caches = SnapshotCaches();
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

        private static PoolTrimResult TrimCache(CallStackCache cache, PoolTrimLevel level, long now)
        {
            int trimmedCount = 0;
            long idleTimeoutTicks = SharedPoolRegistry.Clock.ToTimestampTicks(IdleTimeout);

            lock (cache._syncRoot)
            {
                if (cache._pool.Count == 0)
                {
                    Volatile.Write(ref cache._observedTrimEpoch, Volatile.Read(ref TrimEpoch));
                    return PoolTrimResult.Empty;
                }

                if (level != PoolTrimLevel.Idle)
                {
                    while (cache._pool.Count > 0)
                    {
                        cache._pool.Pop();
                        trimmedCount++;
                        RemoveRetainedItem();
                        Interlocked.Increment(ref DroppedCount);
                    }

                    Volatile.Write(ref cache._observedTrimEpoch, Volatile.Read(ref TrimEpoch));
                    return new PoolTrimResult(
                        trimmedCount,
                        (long)trimmedCount * EstimatedItemBytes
                    );
                }

                Stack<PoolEntry> kept = new(cache._pool.Count);
                while (cache._pool.Count > 0)
                {
                    PoolEntry entry = cache._pool.Pop();
                    bool trim = now - entry.ReturnedAt >= idleTimeoutTicks;
                    if (trim)
                    {
                        trimmedCount++;
                        RemoveRetainedItem();
                        Interlocked.Increment(ref DroppedCount);
                    }
                    else
                    {
                        kept.Push(entry);
                    }
                }

                while (kept.Count > 0)
                {
                    cache._pool.Push(kept.Pop());
                }

                Volatile.Write(ref cache._observedTrimEpoch, Volatile.Read(ref TrimEpoch));
            }

            return new PoolTrimResult(trimmedCount, (long)trimmedCount * EstimatedItemBytes);
        }

        private static void ApplyPendingTrim(CallStackCache cache)
        {
            long epoch = Volatile.Read(ref TrimEpoch);
            if (Volatile.Read(ref cache._observedTrimEpoch) == epoch)
            {
                return;
            }

            TrimCache(cache, PoolTrimLevel.Critical, SharedPoolRegistry.Clock.GetTimestamp());
        }

        private static void RegisterCache(CallStackCache cache)
        {
            lock (CacheRegistrySyncRoot)
            {
                Caches.Add(new WeakReference<CallStackCache>(cache));
            }
        }

        private static CallStackCache[] SnapshotCaches()
        {
            lock (CacheRegistrySyncRoot)
            {
                List<CallStackCache> caches = new(Caches.Count);
                for (int i = Caches.Count - 1; i >= 0; i--)
                {
                    WeakReference<CallStackCache> weakReference = Caches[i];
                    if (weakReference.TryGetTarget(out CallStackCache cache))
                    {
                        caches.Add(cache);
                    }
                    else
                    {
                        Caches.RemoveAt(i);
                    }
                }

                return caches.ToArray();
            }
        }

        private static void AddRetainedItem()
        {
            long bytes = Interlocked.Add(ref RetainedBytes, EstimatedItemBytes);
            UpdatePeak(ref PeakRetainedBytes, bytes);
        }

        private static void RemoveRetainedItem()
        {
            Interlocked.Add(ref RetainedBytes, -EstimatedItemBytes);
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
