namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Describes how aggressively retained pool entries should be trimmed.
    /// </summary>
    internal enum PoolTrimLevel
    {
        /// <summary>
        /// Drop entries that have exceeded their idle timeout while preserving warm entries.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Drop retained entries down to the warm-retain floor.
        /// </summary>
        MemoryPressure = 1,

        /// <summary>
        /// Drop retained entries down to the minimum-retain floor.
        /// </summary>
        Critical = 2,
    }

    /// <summary>
    /// Monotonic clock used by trim-aware pools.
    /// </summary>
    internal interface IPoolClock
    {
        /// <summary>
        /// Gets the current monotonic timestamp.
        /// </summary>
        public long GetTimestamp();

        /// <summary>
        /// Converts a wall-clock duration into this clock's timestamp units.
        /// </summary>
        public long ToTimestampTicks(TimeSpan duration);
    }

    /// <summary>
    /// Target registered with the shared pool registry for stats and explicit trimming.
    /// </summary>
    internal interface IPoolTrimTarget
    {
        /// <summary>
        /// Gets a stable diagnostic name for the target.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets approximate statistics for this pool.
        /// </summary>
        public PoolStatistics GetStatistics();

        /// <summary>
        /// Trims retained entries according to the requested level.
        /// </summary>
        public PoolTrimResult Trim(PoolTrimLevel level);
    }

    /// <summary>
    /// Approximate retained-memory statistics for one or more pools.
    /// </summary>
    internal readonly struct PoolStatistics
    {
        internal PoolStatistics(
            string name,
            int retainedCount,
            long estimatedRetainedBytes,
            long peakRetainedBytes,
            long trimCount,
            long droppedCount
        )
        {
            Name = name;
            RetainedCount = retainedCount;
            EstimatedRetainedBytes = estimatedRetainedBytes;
            PeakRetainedBytes = peakRetainedBytes;
            TrimCount = trimCount;
            DroppedCount = droppedCount;
        }

        internal string Name { get; }

        internal int RetainedCount { get; }

        internal long EstimatedRetainedBytes { get; }

        internal long PeakRetainedBytes { get; }

        internal long TrimCount { get; }

        internal long DroppedCount { get; }

        internal static PoolStatistics Empty(string name)
        {
            return new PoolStatistics(name, 0, 0L, 0L, 0L, 0L);
        }
    }

    /// <summary>
    /// Result of a trim operation for one or more pools.
    /// </summary>
    internal readonly struct PoolTrimResult
    {
        internal PoolTrimResult(int trimmedCount, long estimatedReleasedBytes)
        {
            TrimmedCount = trimmedCount;
            EstimatedReleasedBytes = estimatedReleasedBytes;
        }

        internal int TrimmedCount { get; }

        internal long EstimatedReleasedBytes { get; }

        internal static PoolTrimResult Empty { get; } = new PoolTrimResult(0, 0L);
    }

    /// <summary>
    /// Stopwatch-backed monotonic clock for production pool lifecycle decisions.
    /// </summary>
    internal sealed class StopwatchPoolClock : IPoolClock
    {
        internal static readonly StopwatchPoolClock Instance = new();

        private StopwatchPoolClock() { }

        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public long ToTimestampTicks(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                return 0L;
            }

            double ticks = duration.TotalSeconds * Stopwatch.Frequency;
            if (ticks >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)ticks;
        }
    }

    /// <summary>
    /// Central registry for NovaSharp-owned shared pools.
    /// </summary>
    internal static class SharedPoolRegistry
    {
        private static readonly object SyncRoot = new();
        private static readonly List<IPoolTrimTarget> Targets = new();
        private static IPoolClock ClockSnapshot = StopwatchPoolClock.Instance;
        private static long PeakEstimatedRetainedBytes;

        internal static IPoolClock Clock
        {
            get { return Volatile.Read(ref ClockSnapshot); }
        }

        internal static void Register(IPoolTrimTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            lock (SyncRoot)
            {
                if (!Targets.Contains(target))
                {
                    Targets.Add(target);
                }
            }
        }

        internal static void Unregister(IPoolTrimTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            lock (SyncRoot)
            {
                Targets.Remove(target);
            }
        }

        internal static PoolTrimResult Trim(PoolTrimLevel level)
        {
            IPoolTrimTarget[] targets = SnapshotTargets();
            int trimmedCount = 0;
            long releasedBytes = 0L;

            for (int i = 0; i < targets.Length; i++)
            {
                PoolTrimResult result = targets[i].Trim(level);
                trimmedCount += result.TrimmedCount;
                releasedBytes += result.EstimatedReleasedBytes;
            }

            return new PoolTrimResult(trimmedCount, releasedBytes);
        }

        internal static PoolStatistics GetStatistics()
        {
            IPoolTrimTarget[] targets = SnapshotTargets();
            int retainedCount = 0;
            long estimatedRetainedBytes = 0L;
            long trimCount = 0L;
            long droppedCount = 0L;

            for (int i = 0; i < targets.Length; i++)
            {
                PoolStatistics statistics = targets[i].GetStatistics();
                retainedCount += statistics.RetainedCount;
                estimatedRetainedBytes += statistics.EstimatedRetainedBytes;
                trimCount += statistics.TrimCount;
                droppedCount += statistics.DroppedCount;
            }
            long peakRetainedBytes = UpdatePeakEstimatedRetainedBytes(estimatedRetainedBytes);

            return new PoolStatistics(
                "SharedPoolRegistry",
                retainedCount,
                estimatedRetainedBytes,
                peakRetainedBytes,
                trimCount,
                droppedCount
            );
        }

        private static IPoolTrimTarget[] SnapshotTargets()
        {
            lock (SyncRoot)
            {
                return Targets.ToArray();
            }
        }

        private static long UpdatePeakEstimatedRetainedBytes(long value)
        {
            long snapshot;
            do
            {
                snapshot = Volatile.Read(ref PeakEstimatedRetainedBytes);
                if (value <= snapshot)
                {
                    return snapshot;
                }
            } while (
                Interlocked.CompareExchange(ref PeakEstimatedRetainedBytes, value, snapshot)
                != snapshot
            );

            return value;
        }

        internal static class TestHooks
        {
            internal static bool IsRegistered(IPoolTrimTarget target)
            {
                lock (SyncRoot)
                {
                    return Targets.Contains(target);
                }
            }

            internal static int CountTargetsByName(string name)
            {
                int count = 0;

                lock (SyncRoot)
                {
                    for (int i = 0; i < Targets.Count; i++)
                    {
                        if (string.Equals(Targets[i].Name, name, StringComparison.Ordinal))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            internal static void SetClock(IPoolClock clock)
            {
                IPoolClock effectiveClock = clock != null ? clock : StopwatchPoolClock.Instance;
                Volatile.Write(ref ClockSnapshot, effectiveClock);
            }

            internal static void ResetClock()
            {
                Volatile.Write(ref ClockSnapshot, StopwatchPoolClock.Instance);
            }

            internal static void ResetPeakEstimatedRetainedBytes()
            {
                Volatile.Write(ref PeakEstimatedRetainedBytes, 0L);
            }
        }
    }

    /// <summary>
    /// Provides conservative per-element byte estimates for pool diagnostics and byte caps.
    /// </summary>
    internal static class PoolElementSize<T>
    {
        internal static readonly int EstimatedBytes = EstimateBytes();

        private static int EstimateBytes()
        {
            Type type = typeof(T);
            if (!type.IsValueType)
            {
                return IntPtr.Size;
            }

            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return 1;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Single:
                    return 4;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Double:
                    return 8;
                case TypeCode.Decimal:
                    return 16;
                default:
                    return 32;
            }
        }
    }
}
