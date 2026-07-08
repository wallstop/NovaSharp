namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    /// A thread-safe generic object pool that manages reusable instances of type T.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    /// <remarks>
    /// This implementation follows the WallstopStudios.UnityHelpers pattern:
    /// - Thread-safe via ConcurrentStack
    /// - RAII pattern via PooledResource
    /// - Configurable callbacks for get/release/dispose lifecycle
    /// </remarks>
    internal sealed class GenericPool<T> : IDisposable, IPoolTrimTarget
    {
        private readonly struct PoolEntry
        {
            internal PoolEntry(T value, long returnedAt)
            {
                Value = value;
                ReturnedAt = returnedAt;
            }

            internal T Value { get; }

            internal long ReturnedAt { get; }
        }

        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;
        private readonly Func<T, bool> _shouldRetainOnReturn;
        private readonly Func<T, int> _estimateSizeBytes;
        private readonly IPoolClock _clock;
        private readonly object _syncRoot = new();
        private readonly ConcurrentStack<PoolEntry> _pool = new();
        private readonly int _maxRetainedCount;
        private readonly TimeSpan _idleTimeout;
        private int _retainedCount;
        private int _peakRetainedCount;
        private long _estimatedRetainedBytes;
        private long _peakRetainedBytes;
        private long _trimCount;
        private long _droppedCount;
        private bool _disposed;

        /// <summary>
        /// Gets the current number of instances in the pool.
        /// </summary>
        internal int Count
        {
            get { return Volatile.Read(ref _retainedCount); }
        }

        /// <summary>
        /// Gets the diagnostic pool name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the minimum number of entries retained even for critical trims.
        /// </summary>
        internal int MinRetainCount { get; }

        /// <summary>
        /// Gets the warm number of entries retained for idle and memory-pressure trims.
        /// </summary>
        internal int WarmRetainCount { get; }

        /// <summary>
        /// Gets the maximum number of entries dropped by one non-critical trim operation.
        /// </summary>
        internal int MaxTrimPerOperation { get; }

        /// <summary>
        /// Creates a new generic pool with the specified producer function and optional callbacks.
        /// </summary>
        /// <param name="producer">Function that creates new instances when the pool is empty.</param>
        /// <param name="preWarmCount">Number of instances to create during initialization.</param>
        /// <param name="maxPoolSize">Maximum number of instances to keep pooled. Default 64.</param>
        /// <param name="onGet">Optional callback invoked when an instance is retrieved.</param>
        /// <param name="onRelease">Optional callback invoked when an instance is returned.</param>
        /// <param name="onDispose">Optional callback invoked for each instance when pool is disposed.</param>
        public GenericPool(
            Func<T> producer,
            int preWarmCount = 0,
            int maxPoolSize = 64,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDispose = null,
            string name = null,
            int minRetainCount = 0,
            int warmRetainCount = 0,
            TimeSpan? idleTimeout = null,
            int maxTrimPerOperation = 64,
            Func<T, bool> shouldRetainOnReturn = null,
            Func<T, int> estimateSizeBytes = null,
            IPoolClock clock = null
        )
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            _producer = producer;
            _maxRetainedCount = maxPoolSize > 0 ? maxPoolSize : 64;
            _onGet = onGet;
            _onRelease = onRelease;
            _onDispose = onDispose;
            _shouldRetainOnReturn = shouldRetainOnReturn;
            _estimateSizeBytes = estimateSizeBytes;
            _clock = clock;
            if (name != null)
            {
                Name = name;
            }
            else if (typeof(T).FullName != null)
            {
                Name = typeof(T).FullName;
            }
            else
            {
                Name = typeof(T).Name;
            }
            MinRetainCount = NormalizeRetainCount(minRetainCount, _maxRetainedCount);
            WarmRetainCount = NormalizeRetainCount(
                Math.Max(warmRetainCount, MinRetainCount),
                _maxRetainedCount
            );
            MaxTrimPerOperation = maxTrimPerOperation > 0 ? maxTrimPerOperation : 64;
            _idleTimeout = idleTimeout.GetValueOrDefault(TimeSpan.FromSeconds(60));

            SharedPoolRegistry.Register(this);

            for (int i = 0; i < preWarmCount && i < _maxRetainedCount; i++)
            {
                T value = _producer();
                if (_onGet != null)
                {
                    _onGet(value);
                }
                Return(value);
            }
        }

        /// <summary>
        /// Gets a pooled resource. When disposed, the resource is automatically returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        /// <summary>
        /// Gets a pooled resource and outputs the value.
        /// </summary>
        /// <param name="value">The retrieved instance.</param>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get(out T value)
        {
            if (_pool.TryPop(out PoolEntry entry))
            {
                value = entry.Value;
                int sizeBytes = EstimateSizeBytes(value);
                Interlocked.Decrement(ref _retainedCount);
                Interlocked.Add(ref _estimatedRetainedBytes, -sizeBytes);
            }
            else
            {
                value = _producer();
            }

            if (_onGet != null)
            {
                _onGet(value);
            }
            return new PooledResource<T>(value, Return);
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="value">The instance to return.</param>
        public void Return(T value)
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    DisposeValue(value);
                    return;
                }

                bool shouldRetain = _shouldRetainOnReturn == null || _shouldRetainOnReturn(value);

                if (_onRelease != null)
                {
                    _onRelease(value);
                }

                if (!shouldRetain)
                {
                    DropValue(value);
                    return;
                }

                int retainedCount = Interlocked.Increment(ref _retainedCount);
                if (retainedCount > _maxRetainedCount)
                {
                    Interlocked.Decrement(ref _retainedCount);
                    DropValue(value);
                    return;
                }

                if (_disposed)
                {
                    Interlocked.Decrement(ref _retainedCount);
                    DropValue(value);
                    return;
                }

                int sizeBytes = EstimateSizeBytes(value);
                long estimatedBytes = Interlocked.Add(ref _estimatedRetainedBytes, sizeBytes);
                UpdatePeak(ref _peakRetainedCount, retainedCount);
                UpdatePeak(ref _peakRetainedBytes, estimatedBytes);
                _pool.Push(new PoolEntry(value, Clock.GetTimestamp()));
            }
        }

        /// <summary>
        /// Trims retained instances according to the requested trim level.
        /// </summary>
        public PoolTrimResult Trim(PoolTrimLevel level)
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return PoolTrimResult.Empty;
                }

                PoolEntry[] entries = Drain();
                if (entries.Length == 0)
                {
                    Interlocked.Increment(ref _trimCount);
                    return PoolTrimResult.Empty;
                }

                long now = Clock.GetTimestamp();
                int retainedFloor =
                    level == PoolTrimLevel.Critical ? MinRetainCount : WarmRetainCount;
                int maxTrim = level == PoolTrimLevel.Critical ? int.MaxValue : MaxTrimPerOperation;
                int keptCount = 0;
                int trimmedCount = 0;
                long releasedBytes = 0L;

                for (int i = 0; i < entries.Length; i++)
                {
                    PoolEntry entry = entries[i];
                    bool keepForFloor = keptCount < retainedFloor;
                    bool trimEntry =
                        !keepForFloor
                        && trimmedCount < maxTrim
                        && ShouldTrimEntry(level, entry, now);

                    if (trimEntry || _disposed)
                    {
                        trimmedCount++;
                        releasedBytes += EstimateSizeBytes(entry.Value);
                        DropValue(entry.Value);
                    }
                    else
                    {
                        RetainDrainedEntry(entry);
                        keptCount++;
                    }
                }

                Interlocked.Increment(ref _trimCount);
                return new PoolTrimResult(trimmedCount, releasedBytes);
            }
        }

        /// <summary>
        /// Gets approximate pool statistics.
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics(
                Name,
                Count,
                Math.Max(0L, Volatile.Read(ref _estimatedRetainedBytes)),
                Volatile.Read(ref _peakRetainedBytes),
                Volatile.Read(ref _trimCount),
                Volatile.Read(ref _droppedCount)
            );
        }

        /// <summary>
        /// Disposes the pool, invoking onDispose for each pooled instance if provided.
        /// </summary>
        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                SharedPoolRegistry.Unregister(this);

                while (_pool.TryPop(out PoolEntry entry))
                {
                    Interlocked.Decrement(ref _retainedCount);
                    Interlocked.Add(ref _estimatedRetainedBytes, -EstimateSizeBytes(entry.Value));
                    DisposeValue(entry.Value);
                }
            }
        }

        private PoolEntry[] Drain()
        {
            int initialCount = Count;
            if (initialCount == 0)
            {
                return Array.Empty<PoolEntry>();
            }

            PoolEntry[] entries = new PoolEntry[initialCount];
            int index = 0;

            while (index < entries.Length && _pool.TryPop(out PoolEntry entry))
            {
                entries[index] = entry;
                index++;
                Interlocked.Decrement(ref _retainedCount);
                Interlocked.Add(ref _estimatedRetainedBytes, -EstimateSizeBytes(entry.Value));
            }

            if (index == entries.Length)
            {
                return entries;
            }

            PoolEntry[] resized = new PoolEntry[index];
            Array.Copy(entries, resized, index);
            return resized;
        }

        private void RetainDrainedEntry(PoolEntry entry)
        {
            int retainedCount = Interlocked.Increment(ref _retainedCount);
            int sizeBytes = EstimateSizeBytes(entry.Value);
            long estimatedBytes = Interlocked.Add(ref _estimatedRetainedBytes, sizeBytes);
            UpdatePeak(ref _peakRetainedCount, retainedCount);
            UpdatePeak(ref _peakRetainedBytes, estimatedBytes);
            _pool.Push(entry);
        }

        private bool ShouldTrimEntry(PoolTrimLevel level, PoolEntry entry, long now)
        {
            if (level == PoolTrimLevel.Critical || level == PoolTrimLevel.MemoryPressure)
            {
                return true;
            }

            long idleTimeoutTicks = Clock.ToTimestampTicks(_idleTimeout);
            if (idleTimeoutTicks <= 0L)
            {
                return true;
            }

            return now - entry.ReturnedAt >= idleTimeoutTicks;
        }

        private IPoolClock Clock
        {
            get { return _clock != null ? _clock : SharedPoolRegistry.Clock; }
        }

        private int EstimateSizeBytes(T value)
        {
            if (_estimateSizeBytes == null)
            {
                return IntPtr.Size;
            }

            int sizeBytes = _estimateSizeBytes(value);
            return sizeBytes > 0 ? sizeBytes : IntPtr.Size;
        }

        private void DropValue(T value)
        {
            Interlocked.Increment(ref _droppedCount);
            DisposeValue(value);
        }

        private void DisposeValue(T value)
        {
            if (_onDispose != null)
            {
                _onDispose(value);
            }
        }

        private static int NormalizeRetainCount(int count, int maxRetainedCount)
        {
            if (count <= 0)
            {
                return 0;
            }

            return count > maxRetainedCount ? maxRetainedCount : count;
        }

        private static void UpdatePeak(ref int target, int value)
        {
            int snapshot;
            do
            {
                snapshot = Volatile.Read(ref target);
                if (value <= snapshot)
                {
                    return;
                }
            } while (Interlocked.CompareExchange(ref target, value, snapshot) != snapshot);
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
