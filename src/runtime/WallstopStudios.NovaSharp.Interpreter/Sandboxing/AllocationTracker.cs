namespace WallstopStudios.NovaSharp.Interpreter.Sandboxing
{
    using System;
    using System.Threading;

    /// <summary>
    /// Tracks memory allocations for sandbox enforcement.
    /// This is a lightweight, thread-safe counter that the VM and allocators
    /// can use to report memory usage. Enforcement is handled by the sandbox
    /// at configurable check points.
    /// </summary>
    public sealed class AllocationTracker
    {
        private long _currentBytes;
        private long _peakBytes;
        private long _totalAllocated;
        private long _totalFreed;
        private int _currentCoroutines;
        private int _peakCoroutines;
        private int _totalCoroutinesCreated;

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        public long CurrentBytes => Interlocked.Read(ref _currentBytes);

        /// <summary>
        /// Gets the peak memory usage observed since the tracker was created or reset.
        /// </summary>
        public long PeakBytes => Interlocked.Read(ref _peakBytes);

        /// <summary>
        /// Gets the total bytes allocated since the tracker was created or reset.
        /// </summary>
        public long TotalAllocated => Interlocked.Read(ref _totalAllocated);

        /// <summary>
        /// Gets the total bytes freed since the tracker was created or reset.
        /// </summary>
        public long TotalFreed => Interlocked.Read(ref _totalFreed);

        /// <summary>
        /// Gets the current number of active coroutines.
        /// </summary>
        public int CurrentCoroutines => Interlocked.CompareExchange(ref _currentCoroutines, 0, 0);

        /// <summary>
        /// Gets the peak number of concurrent coroutines observed since the tracker was created or reset.
        /// </summary>
        public int PeakCoroutines => Interlocked.CompareExchange(ref _peakCoroutines, 0, 0);

        /// <summary>
        /// Gets the total number of coroutines created since the tracker was created or reset.
        /// </summary>
        public int TotalCoroutinesCreated =>
            Interlocked.CompareExchange(ref _totalCoroutinesCreated, 0, 0);

        /// <summary>
        /// Records an allocation of the specified size.
        /// </summary>
        /// <param name="bytes">The number of bytes allocated. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bytes is negative.</exception>
        public void RecordAllocation(long bytes)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bytes),
                    bytes,
                    "Allocation size cannot be negative."
                );
            }

            if (bytes == 0)
            {
                return;
            }

            long newCurrent = Interlocked.Add(ref _currentBytes, bytes);
            Interlocked.Add(ref _totalAllocated, bytes);

            // Update peak if necessary (lock-free compare-exchange loop)
            long currentPeak = Interlocked.Read(ref _peakBytes);
            while (newCurrent > currentPeak)
            {
                long exchanged = Interlocked.CompareExchange(
                    ref _peakBytes,
                    newCurrent,
                    currentPeak
                );
                if (exchanged == currentPeak)
                {
                    break;
                }

                currentPeak = exchanged;
            }
        }

        /// <summary>
        /// Records a deallocation (free) of the specified size.
        /// </summary>
        /// <param name="bytes">The number of bytes freed. Must be non-negative.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bytes is negative.</exception>
        public void RecordDeallocation(long bytes)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bytes),
                    bytes,
                    "Deallocation size cannot be negative."
                );
            }

            if (bytes == 0)
            {
                return;
            }

            Interlocked.Add(ref _currentBytes, -bytes);
            Interlocked.Add(ref _totalFreed, bytes);
        }

        /// <summary>
        /// Resets all counters to zero.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _currentBytes, 0);
            Interlocked.Exchange(ref _peakBytes, 0);
            Interlocked.Exchange(ref _totalAllocated, 0);
            Interlocked.Exchange(ref _totalFreed, 0);
            Interlocked.Exchange(ref _currentCoroutines, 0);
            Interlocked.Exchange(ref _peakCoroutines, 0);
            Interlocked.Exchange(ref _totalCoroutinesCreated, 0);
        }

        /// <summary>
        /// Records the creation of a coroutine.
        /// </summary>
        public void RecordCoroutineCreated()
        {
            int newCurrent = Interlocked.Increment(ref _currentCoroutines);
            Interlocked.Increment(ref _totalCoroutinesCreated);

            // Update peak if necessary (lock-free compare-exchange loop)
            int currentPeak = Interlocked.CompareExchange(ref _peakCoroutines, 0, 0);
            while (newCurrent > currentPeak)
            {
                int exchanged = Interlocked.CompareExchange(
                    ref _peakCoroutines,
                    newCurrent,
                    currentPeak
                );
                if (exchanged == currentPeak)
                {
                    break;
                }

                currentPeak = exchanged;
            }
        }

        /// <summary>
        /// Records the disposal or completion of a coroutine.
        /// </summary>
        public void RecordCoroutineDisposed()
        {
            Interlocked.Decrement(ref _currentCoroutines);
        }

        /// <summary>
        /// Checks whether the current coroutine count exceeds the specified limit.
        /// </summary>
        /// <param name="maxCoroutines">The maximum allowed coroutines. Use 0 for unlimited.</param>
        /// <returns><c>true</c> if the limit is exceeded; otherwise <c>false</c>.</returns>
        public bool ExceedsCoroutineLimit(int maxCoroutines)
        {
            if (maxCoroutines <= 0)
            {
                return false;
            }

            return CurrentCoroutines >= maxCoroutines;
        }

        /// <summary>
        /// Checks whether the current coroutine count exceeds the specified sandbox options' coroutine limit.
        /// </summary>
        /// <param name="options">The sandbox options to check against.</param>
        /// <returns><c>true</c> if the limit is exceeded; otherwise <c>false</c>.</returns>
        public bool ExceedsCoroutineLimit(SandboxOptions options)
        {
            if (options == null || !options.HasCoroutineLimit)
            {
                return false;
            }

            return ExceedsCoroutineLimit(options.MaxCoroutines);
        }

        /// <summary>
        /// Checks whether the current allocation exceeds the specified limit.
        /// </summary>
        /// <param name="maxBytes">The maximum allowed bytes. Use 0 for unlimited.</param>
        /// <returns><c>true</c> if the limit is exceeded; otherwise <c>false</c>.</returns>
        public bool ExceedsLimit(long maxBytes)
        {
            if (maxBytes <= 0)
            {
                return false;
            }

            return CurrentBytes > maxBytes;
        }

        /// <summary>
        /// Checks whether the current allocation exceeds the specified sandbox options' memory limit.
        /// </summary>
        /// <param name="options">The sandbox options to check against.</param>
        /// <returns><c>true</c> if the limit is exceeded; otherwise <c>false</c>.</returns>
        public bool ExceedsLimit(SandboxOptions options)
        {
            if (options == null || !options.HasMemoryLimit)
            {
                return false;
            }

            return ExceedsLimit(options.MaxMemoryBytes);
        }

        /// <summary>
        /// Creates a snapshot of the current tracker state.
        /// </summary>
        /// <returns>A new <see cref="AllocationSnapshot"/> with the current values.</returns>
        public AllocationSnapshot CreateSnapshot()
        {
            return new AllocationSnapshot(
                CurrentBytes,
                PeakBytes,
                TotalAllocated,
                TotalFreed,
                CurrentCoroutines,
                PeakCoroutines,
                TotalCoroutinesCreated
            );
        }
    }

    /// <summary>
    /// An immutable snapshot of allocation tracker state at a point in time.
    /// </summary>
    public readonly struct AllocationSnapshot : IEquatable<AllocationSnapshot>
    {
        /// <summary>
        /// Initializes a new <see cref="AllocationSnapshot"/>.
        /// </summary>
        /// <param name="currentBytes">Current memory usage in bytes.</param>
        /// <param name="peakBytes">Peak memory usage in bytes.</param>
        /// <param name="totalAllocated">Total bytes allocated.</param>
        /// <param name="totalFreed">Total bytes freed.</param>
        /// <param name="currentCoroutines">Current number of active coroutines.</param>
        /// <param name="peakCoroutines">Peak number of concurrent coroutines.</param>
        /// <param name="totalCoroutinesCreated">Total number of coroutines created.</param>
        public AllocationSnapshot(
            long currentBytes,
            long peakBytes,
            long totalAllocated,
            long totalFreed,
            int currentCoroutines,
            int peakCoroutines,
            int totalCoroutinesCreated
        )
        {
            CurrentBytes = currentBytes;
            PeakBytes = peakBytes;
            TotalAllocated = totalAllocated;
            TotalFreed = totalFreed;
            CurrentCoroutines = currentCoroutines;
            PeakCoroutines = peakCoroutines;
            TotalCoroutinesCreated = totalCoroutinesCreated;
        }

        /// <summary>
        /// Gets the current memory usage in bytes at snapshot time.
        /// </summary>
        public long CurrentBytes { get; }

        /// <summary>
        /// Gets the peak memory usage in bytes at snapshot time.
        /// </summary>
        public long PeakBytes { get; }

        /// <summary>
        /// Gets the total bytes allocated at snapshot time.
        /// </summary>
        public long TotalAllocated { get; }

        /// <summary>
        /// Gets the total bytes freed at snapshot time.
        /// </summary>
        public long TotalFreed { get; }

        /// <summary>
        /// Gets the current number of active coroutines at snapshot time.
        /// </summary>
        public int CurrentCoroutines { get; }

        /// <summary>
        /// Gets the peak number of concurrent coroutines at snapshot time.
        /// </summary>
        public int PeakCoroutines { get; }

        /// <summary>
        /// Gets the total number of coroutines created at snapshot time.
        /// </summary>
        public int TotalCoroutinesCreated { get; }

        /// <summary>
        /// Returns a string representation of this snapshot.
        /// </summary>
        /// <returns>A formatted string with the snapshot values.</returns>
        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "AllocationSnapshot(Current={0}, Peak={1}, Allocated={2}, Freed={3}, Coroutines={4}, PeakCoroutines={5}, TotalCoroutines={6})",
                CurrentBytes,
                PeakBytes,
                TotalAllocated,
                TotalFreed,
                CurrentCoroutines,
                PeakCoroutines,
                TotalCoroutinesCreated
            );
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AllocationSnapshot other && Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(AllocationSnapshot other)
        {
            return CurrentBytes == other.CurrentBytes
                && PeakBytes == other.PeakBytes
                && TotalAllocated == other.TotalAllocated
                && TotalFreed == other.TotalFreed
                && CurrentCoroutines == other.CurrentCoroutines
                && PeakCoroutines == other.PeakCoroutines
                && TotalCoroutinesCreated == other.TotalCoroutinesCreated;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + CurrentBytes.GetHashCode();
                hash = (hash * 31) + PeakBytes.GetHashCode();
                hash = (hash * 31) + TotalAllocated.GetHashCode();
                hash = (hash * 31) + TotalFreed.GetHashCode();
                hash = (hash * 31) + CurrentCoroutines.GetHashCode();
                hash = (hash * 31) + PeakCoroutines.GetHashCode();
                hash = (hash * 31) + TotalCoroutinesCreated.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="AllocationSnapshot"/> instances are equal.
        /// </summary>
        public static bool operator ==(AllocationSnapshot left, AllocationSnapshot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="AllocationSnapshot"/> instances are not equal.
        /// </summary>
        public static bool operator !=(AllocationSnapshot left, AllocationSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
