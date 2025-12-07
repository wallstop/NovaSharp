namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;

    /// <summary>
    /// Random provider for Lua 5.1/5.2/5.3 compatibility.
    /// Uses a Linear Congruential Generator (LCG) to match the behavior of C library <c>rand()</c>
    /// which these Lua versions use internally.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lua 5.1, 5.2, and 5.3 delegate random number generation to the platform's C library <c>rand()</c>.
    /// This provider implements a portable LCG with parameters matching the common POSIX implementation
    /// (glibc) to provide predictable cross-platform behavior for scripts targeting these versions.
    /// </para>
    /// <para>
    /// LCG parameters (glibc-compatible):
    /// <list type="bullet">
    ///   <item><c>a = 1103515245</c> (multiplier)</item>
    ///   <item><c>c = 12345</c> (increment)</item>
    ///   <item><c>m = 2^31</c> (modulus)</item>
    /// </list>
    /// Formula: <c>next = (a * current + c) mod m</c>
    /// </para>
    /// <para>
    /// Key differences from Lua 5.4's xoshiro256**:
    /// <list type="bullet">
    ///   <item>Single 32-bit seed (not 128-bit)</item>
    ///   <item><c>math.randomseed(x)</c> does not return the seed value</item>
    ///   <item>Period is only 2^31 (vs 2^256-1)</item>
    ///   <item>Lower statistical quality (visible patterns in low bits)</item>
    /// </list>
    /// </para>
    /// </remarks>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Lua math.random is intentionally non-cryptographic per Lua documentation."
    )]
    public sealed class Lua51RandomProvider : IRandomProvider
    {
        // LCG parameters matching glibc's rand() implementation
        // These are the POSIX.1-2001 recommended values
        private const long Multiplier = 1103515245;
        private const long Increment = 12345;
        private const long Modulus = 1L << 31; // 2^31 = 2147483648

        private readonly object _lock = new object();
        private long _state;
        private int _seedValue;

        /// <summary>
        /// Gets the first seed component.
        /// For Lua 5.1-5.3 compatibility, this returns the original 32-bit seed as a 64-bit value.
        /// </summary>
        public long SeedX => _seedValue;

        /// <summary>
        /// Gets the second seed component.
        /// For Lua 5.1-5.3 compatibility, this always returns 0 (single-seed system).
        /// </summary>
        public long SeedY => 0;

        /// <summary>
        /// Initializes a new instance with a time-based seed (mimicking C's <c>srand(time(NULL))</c>).
        /// </summary>
        public Lua51RandomProvider()
        {
            SetSeedFromSystemRandom();
        }

        /// <summary>
        /// Initializes a new instance with the specified seed.
        /// </summary>
        /// <param name="seed">The 32-bit seed value.</param>
        public Lua51RandomProvider(int seed)
        {
            SetSeed(seed);
        }

        /// <summary>
        /// Initializes a new instance with the specified seed (only the first component is used).
        /// </summary>
        /// <param name="x">First seed component (truncated to 32-bit for LCG).</param>
        /// <param name="y">Second seed component (ignored for Lua 5.1-5.3 compatibility).</param>
        public Lua51RandomProvider(long x, long y)
        {
            SetSeed(x, y);
        }

        /// <inheritdoc />
        public long NextInt64()
        {
            lock (_lock)
            {
                // Generate two 32-bit values and combine them for 64-bit output
                // This matches how Lua 5.4's math.random(0) works, adapted for LCG
                long high = NextRaw();
                long low = NextRaw();
                return (high << 32) | (low & 0xFFFFFFFFL);
            }
        }

        /// <inheritdoc />
        public long NextLong(long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minValue),
                    "minValue must be less than or equal to maxValue"
                );
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            // Calculate range using unsigned arithmetic to handle full Int64 range
            ulong range = (ulong)(maxValue - minValue);

            // For small ranges, use rejection sampling for uniformity
            if (range <= int.MaxValue)
            {
                return minValue + NextInt((int)range + 1);
            }

            // For larger ranges, combine two 32-bit values
            lock (_lock)
            {
                ulong value = (ulong)NextRaw() << 32 | (uint)NextRaw();
                return minValue + (long)(value % (range + 1));
            }
        }

        /// <inheritdoc />
        public int NextInt()
        {
            lock (_lock)
            {
                return NextRaw() & int.MaxValue;
            }
        }

        /// <inheritdoc />
        public int NextInt(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxValue),
                    "maxValue must be non-negative"
                );
            }

            if (maxValue == 0)
            {
                return 0;
            }

            lock (_lock)
            {
                // Use rejection sampling for better uniformity
                // This avoids modulo bias
                int threshold = int.MaxValue - (int.MaxValue % maxValue);
                int result;
                do
                {
                    result = NextRaw() & int.MaxValue;
                } while (result >= threshold);

                return result % maxValue;
            }
        }

        /// <inheritdoc />
        public int NextInt(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minValue),
                    "minValue must be less than or equal to maxValue"
                );
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            long range = (long)maxValue - minValue;
            return minValue + NextInt((int)range);
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            lock (_lock)
            {
                // Return value in [0, 1) matching Lua's math.random() behavior
                // Use 31 bits of precision (matches typical C rand() implementations)
                int raw = NextRaw() & int.MaxValue;
                return raw / (double)(int.MaxValue + 1L);
            }
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(int seed)
        {
            lock (_lock)
            {
                _seedValue = seed;
                _state = (uint)seed; // Convert to unsigned for consistent behavior
                return (seed, 0);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// For Lua 5.1-5.3 compatibility, only the first seed component (<paramref name="x"/>) is used.
        /// The second component (<paramref name="y"/>) is ignored.
        /// </remarks>
        public (long X, long Y) SetSeed(long x, long y)
        {
            // For Lua 5.1-5.3, only use the first seed value (truncated to 32-bit)
            return SetSeed((int)x);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Mimics Lua 5.1-5.3's <c>srand(time(NULL))</c> behavior by using the current time.
        /// For better randomness in test scenarios, we use cryptographic RNG for the initial seed.
        /// </remarks>
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            // Use cryptographic RNG for the initial seed (better than time-based)
            byte[] seedBytes = new byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }

            int seed = BitConverter.ToInt32(seedBytes, 0);
            return SetSeed(seed);
        }

        /// <summary>
        /// Generates the next raw 32-bit LCG value.
        /// This is the core algorithm matching glibc's rand() implementation.
        /// </summary>
        /// <returns>A 32-bit value from the LCG sequence.</returns>
        private int NextRaw()
        {
            // LCG formula: next = (a * current + c) mod m
            _state = (_state * Multiplier + Increment) % Modulus;
            return (int)_state;
        }
    }
}
