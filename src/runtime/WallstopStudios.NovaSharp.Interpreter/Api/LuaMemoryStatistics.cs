namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Approximate retained-memory statistics for a Lua engine.
    /// </summary>
    /// <remarks>
    /// Current retained-byte estimates combine engine-local script metadata and VM stacks with
    /// process-wide NovaSharp shared pools. Peak retained bytes track the highest total estimate
    /// observed by this engine's statistics facade. Byte estimates are diagnostics, not exact
    /// managed object graph measurements. Memory retained internally by
    /// <see cref="System.Buffers.ArrayPool{T}.Shared"/> is not reported.
    /// </remarks>
    public readonly struct LuaMemoryStatistics : IEquatable<LuaMemoryStatistics>
    {
        internal LuaMemoryStatistics(
            long retainedPoolCount,
            long estimatedRetainedBytes,
            long peakRetainedBytes,
            long trimCount,
            long droppedCount,
            int compilationCacheEntryCount
        )
        {
            RetainedPoolCount = retainedPoolCount;
            EstimatedRetainedBytes = estimatedRetainedBytes;
            PeakRetainedBytes = peakRetainedBytes;
            TrimCount = trimCount;
            DroppedCount = droppedCount;
            CompilationCacheEntryCount = compilationCacheEntryCount;
        }

        /// <summary>
        /// Gets the approximate number of currently retained entries in NovaSharp-owned shared pools.
        /// </summary>
        public long RetainedPoolCount { get; }

        /// <summary>
        /// Gets the approximate retained bytes owned by shared pools and script-lifetime metadata.
        /// </summary>
        public long EstimatedRetainedBytes { get; }

        /// <summary>
        /// Gets the approximate peak retained bytes observed by this engine's statistics facade.
        /// </summary>
        public long PeakRetainedBytes { get; }

        /// <summary>
        /// Gets the number of explicit trim operations observed by registered pools.
        /// </summary>
        public long TrimCount { get; }

        /// <summary>
        /// Gets the number of retained entries dropped instead of reused by registered pools.
        /// </summary>
        public long DroppedCount { get; }

        /// <summary>
        /// Gets the number of entries currently in the per-script compilation cache.
        /// </summary>
        public int CompilationCacheEntryCount { get; }

        /// <summary>
        /// Determines whether this instance and another statistics snapshot contain the same values.
        /// </summary>
        public bool Equals(LuaMemoryStatistics other)
        {
            return RetainedPoolCount == other.RetainedPoolCount
                && EstimatedRetainedBytes == other.EstimatedRetainedBytes
                && PeakRetainedBytes == other.PeakRetainedBytes
                && TrimCount == other.TrimCount
                && DroppedCount == other.DroppedCount
                && CompilationCacheEntryCount == other.CompilationCacheEntryCount;
        }

        /// <summary>
        /// Determines whether this instance and another object contain the same values.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is LuaMemoryStatistics other && Equals(other);
        }

        /// <summary>
        /// Gets a deterministic hash code for this statistics snapshot.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCodeHelper.HashCode(
                RetainedPoolCount,
                EstimatedRetainedBytes,
                PeakRetainedBytes,
                TrimCount,
                DroppedCount,
                CompilationCacheEntryCount
            );
        }

        /// <summary>
        /// Compares two statistics snapshots for value equality.
        /// </summary>
        public static bool operator ==(LuaMemoryStatistics left, LuaMemoryStatistics right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two statistics snapshots for value inequality.
        /// </summary>
        public static bool operator !=(LuaMemoryStatistics left, LuaMemoryStatistics right)
        {
            return !left.Equals(right);
        }
    }
}
