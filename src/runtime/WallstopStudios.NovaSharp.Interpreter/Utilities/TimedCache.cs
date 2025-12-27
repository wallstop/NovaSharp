namespace WallstopStudios.NovaSharp.Interpreter.Utilities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Infrastructure;

    /// <summary>
    /// A thread-safe cache for expensive computations with time-based expiration.
    /// Values are lazily recomputed when the TTL expires on access.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    public sealed class TimedCache<T>
    {
        private readonly Func<T> _valueProducer;
        private readonly IHighResolutionClock _clock;
        private readonly long _cacheTtlTicks;
        private readonly long _jitterMaxTicks;
        private readonly bool _useJitter;

        private T _cachedValue;
        private long _expirationTimestamp;
        private int _isInitialized;
        private int _isComputing;

        // Lock-free random for jitter; seeded from Environment.TickCount to avoid collisions
        private uint _randomState;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedCache{T}"/> class with the default high-resolution clock.
        /// </summary>
        /// <param name="valueProducer">A function that produces the cached value.</param>
        /// <param name="cacheTtl">The time-to-live before the value expires.</param>
        /// <param name="useJitter">
        /// When <c>true</c>, adds random jitter (up to 10% of TTL) to prevent thundering herd.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="valueProducer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cacheTtl"/> is not positive.</exception>
        public TimedCache(Func<T> valueProducer, TimeSpan cacheTtl, bool useJitter = false)
            : this(valueProducer, cacheTtl, SystemHighResolutionClock.Instance, useJitter) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedCache{T}"/> class with a custom high-resolution clock.
        /// </summary>
        /// <param name="valueProducer">A function that produces the cached value.</param>
        /// <param name="cacheTtl">The time-to-live before the value expires.</param>
        /// <param name="clock">
        /// The high-resolution clock to use for timing.
        /// Use this constructor for testing with controlled time.
        /// </param>
        /// <param name="useJitter">
        /// When <c>true</c>, adds random jitter (up to 10% of TTL) to prevent thundering herd.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="valueProducer"/> or <paramref name="clock"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cacheTtl"/> is not positive.</exception>
        public TimedCache(
            Func<T> valueProducer,
            TimeSpan cacheTtl,
            IHighResolutionClock clock,
            bool useJitter = false
        )
        {
            if (valueProducer == null)
            {
                throw new ArgumentNullException(nameof(valueProducer));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (cacheTtl <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cacheTtl),
                    cacheTtl,
                    "Cache TTL must be positive."
                );
            }

            _valueProducer = valueProducer;
            _clock = clock;
            _useJitter = useJitter;

            // Convert TimeSpan to clock ticks
            double frequencyHz = clock.TimestampFrequency;
            _cacheTtlTicks = (long)(cacheTtl.TotalSeconds * frequencyHz);
            _jitterMaxTicks = (long)(_cacheTtlTicks * 0.1); // 10% jitter max

            _cachedValue = default;
            _expirationTimestamp = 0;
            _isInitialized = 0;
            _isComputing = 0;

            // Initialize random state with a mix of tick count and hash code for uniqueness
            _randomState = unchecked((uint)(Environment.TickCount ^ GetHashCode()));
            if (_randomState == 0)
            {
                _randomState = 1; // Xorshift requires non-zero seed
            }
        }

        /// <summary>
        /// Gets the high-resolution clock used by this cache.
        /// </summary>
        public IHighResolutionClock Clock => _clock;

        /// <summary>
        /// Gets the cached value, recomputing it if the TTL has expired.
        /// Thread-safe: concurrent readers will receive either the old cached value
        /// or wait for recomputation to complete.
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return GetValue(); }
        }

        /// <summary>
        /// Forces the cache to recompute its value on the next access.
        /// Does not immediately recompute; the value is computed lazily.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            // Mark as uninitialized so next access triggers recomputation
            Interlocked.Exchange(ref _isInitialized, 0);
        }

        /// <summary>
        /// Clears the cached value and marks the cache as uninitialized.
        /// The value will be recomputed on next access.
        /// </summary>
        public void Invalidate()
        {
            // Acquire the computation lock to safely clear the value
            SpinWait spinner = new SpinWait();
            while (Interlocked.CompareExchange(ref _isComputing, 1, 0) != 0)
            {
                spinner.SpinOnce();
            }

            try
            {
                _cachedValue = default;
                _expirationTimestamp = 0;
                Interlocked.Exchange(ref _isInitialized, 0);
            }
            finally
            {
                Interlocked.Exchange(ref _isComputing, 0);
            }
        }

        /// <summary>
        /// Gets the cached value, computing it if necessary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue()
        {
            long currentTimestamp = _clock.GetTimestamp();

            // Fast path: check if cache is valid without taking any locks
            if (Interlocked.CompareExchange(ref _isInitialized, 0, 0) == 1)
            {
                // Use volatile read semantics for expiration check
                long expiration = Volatile.Read(ref _expirationTimestamp);
                if (currentTimestamp < expiration)
                {
                    return _cachedValue;
                }
            }

            // Slow path: need to compute or wait for computation
            return ComputeValue(currentTimestamp);
        }

        /// <summary>
        /// Computes or waits for the cached value.
        /// </summary>
        private T ComputeValue(long currentTimestamp)
        {
            // Try to acquire the computation lock
            if (Interlocked.CompareExchange(ref _isComputing, 1, 0) == 0)
            {
                // We won the race, compute the value
                try
                {
                    // Double-check: another thread might have computed while we were waiting
                    if (Interlocked.CompareExchange(ref _isInitialized, 0, 0) == 1)
                    {
                        long expiration = Volatile.Read(ref _expirationTimestamp);
                        if (currentTimestamp < expiration)
                        {
                            return _cachedValue;
                        }
                    }

                    // Compute the new value
                    T newValue = _valueProducer();

                    // Calculate new expiration with optional jitter
                    long ttlTicks = _cacheTtlTicks;
                    if (_useJitter)
                    {
                        ttlTicks += GetJitterTicks();
                    }

                    long newExpiration = currentTimestamp + ttlTicks;

                    // Store the value and expiration
                    _cachedValue = newValue;
                    Volatile.Write(ref _expirationTimestamp, newExpiration);
                    Interlocked.Exchange(ref _isInitialized, 1);

                    return newValue;
                }
                finally
                {
                    Interlocked.Exchange(ref _isComputing, 0);
                }
            }

            // Another thread is computing; spin-wait and return the result
            SpinWait spinner = new SpinWait();
            while (Interlocked.CompareExchange(ref _isComputing, 0, 0) == 1)
            {
                spinner.SpinOnce();
            }

            // The computing thread should have initialized the value
            if (Interlocked.CompareExchange(ref _isInitialized, 0, 0) == 1)
            {
                return _cachedValue;
            }

            // Edge case: computing thread failed or Reset was called; try again
            return ComputeValue(_clock.GetTimestamp());
        }

        /// <summary>
        /// Gets a random jitter value in clock ticks using Xorshift32.
        /// This is a lock-free PRNG suitable for jitter generation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetJitterTicks()
        {
            // Xorshift32 PRNG - fast and good enough for jitter
            uint state = _randomState;
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            _randomState = state;

            // Convert to [0, 1) range and scale by max jitter
            double normalized = state / (double)uint.MaxValue;
            return (long)(normalized * _jitterMaxTicks);
        }
    }
}
