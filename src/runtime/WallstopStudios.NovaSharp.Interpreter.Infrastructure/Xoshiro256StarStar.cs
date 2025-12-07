namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;

    /// <summary>
    /// Implementation of the xoshiro256** (xoshiro256starstar) pseudo-random number generator.
    /// This is the algorithm used by Lua 5.4 (§6.7) for math.random.
    /// </summary>
    /// <remarks>
    /// xoshiro256** is an all-purpose, rock-solid generator with a period of 2^256 - 1.
    /// It is the fastest full-period generator passing BigCrush without systematic failures.
    /// Reference: https://prng.di.unimi.it/
    /// Original C implementation: https://prng.di.unimi.it/xoshiro256starstar.c
    /// </remarks>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Lua math.random is intentionally non-cryptographic per §6.7."
    )]
    public sealed class Xoshiro256StarStar
    {
        // Internal state: four 64-bit unsigned integers (256 bits total)
        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        private readonly object _lock = new object();

        /// <summary>
        /// Gets the first component of the current 128-bit seed.
        /// </summary>
        public long SeedX { get; private set; }

        /// <summary>
        /// Gets the second component of the current 128-bit seed.
        /// </summary>
        public long SeedY { get; private set; }

        /// <summary>
        /// Initializes a new instance with a random seed (using cryptographic RNG).
        /// </summary>
        public Xoshiro256StarStar()
        {
            SetSeedFromSystemRandom();
        }

        /// <summary>
        /// Initializes a new instance with the specified 128-bit seed.
        /// </summary>
        /// <param name="x">First 64-bit seed component.</param>
        /// <param name="y">Second 64-bit seed component.</param>
        public Xoshiro256StarStar(long x, long y)
        {
            SetSeed(x, y);
        }

        /// <summary>
        /// Reinitializes the generator with a random seed using cryptographic RNG.
        /// </summary>
        /// <returns>A tuple containing the two seed components that were used.</returns>
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            // Generate 16 random bytes (128 bits) for the seed
            byte[] seedBytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }

            long x = BitConverter.ToInt64(seedBytes, 0);
            long y = BitConverter.ToInt64(seedBytes, 8);
            SetSeed(x, y);
            return (x, y);
        }

        /// <summary>
        /// Reinitializes the generator with a single integer seed (converted to 128-bit).
        /// </summary>
        /// <param name="seed">The seed value.</param>
        /// <returns>A tuple containing the two seed components that were used.</returns>
        public (long X, long Y) SetSeed(int seed)
        {
            // Convert single 32-bit seed to 128-bit by using it as both components
            // (shifted to provide differentiation)
            long x = seed;
            long y = (long)seed << 32 | (uint)seed;
            SetSeed(x, y);
            return (x, y);
        }

        /// <summary>
        /// Reinitializes the generator with a 128-bit seed (two 64-bit values).
        /// </summary>
        /// <param name="x">First 64-bit seed component.</param>
        /// <param name="y">Second 64-bit seed component.</param>
        /// <returns>A tuple containing the two seed components that were used.</returns>
        public (long X, long Y) SetSeed(long x, long y)
        {
            lock (_lock)
            {
                SeedX = x;
                SeedY = y;

                // Initialize state using SplitMix64 (as recommended by xoshiro authors)
                // This expands the 128-bit seed to 256-bit state
                ulong state = (ulong)x;
                _s0 = SplitMix64(ref state);
                _s1 = SplitMix64(ref state);
                state = (ulong)y;
                _s2 = SplitMix64(ref state);
                _s3 = SplitMix64(ref state);

                // If all state is zero, the generator will output only zeros
                // Use a fallback initialization in this edge case
                if (_s0 == 0 && _s1 == 0 && _s2 == 0 && _s3 == 0)
                {
                    _s0 = 0x9E3779B97F4A7C15UL; // Golden ratio-derived constant
                    _s1 = 0xBF58476D1CE4E5B9UL;
                    _s2 = 0x94D049BB133111EBUL;
                    _s3 = 0x6A09E667BB67AE85UL;
                }

                return (x, y);
            }
        }

        /// <summary>
        /// SplitMix64 is used to initialize the state from a seed.
        /// </summary>
        private static ulong SplitMix64(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>
        /// Generates the next 64-bit pseudo-random integer.
        /// This is the core xoshiro256** algorithm.
        /// </summary>
        /// <returns>A 64-bit unsigned integer with all bits pseudo-random.</returns>
        public ulong NextUInt64()
        {
            lock (_lock)
            {
                // xoshiro256** algorithm
                ulong result = RotateLeft(_s1 * 5, 7) * 9;

                ulong t = _s1 << 17;

                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;

                _s2 ^= t;

                _s3 = RotateLeft(_s3, 45);

                return result;
            }
        }

        /// <summary>
        /// Generates the next 64-bit signed pseudo-random integer.
        /// </summary>
        /// <returns>A 64-bit signed integer.</returns>
        public long NextInt64()
        {
            return (long)NextUInt64();
        }

        /// <summary>
        /// Generates a non-negative 32-bit pseudo-random integer.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0.</returns>
        public int NextInt()
        {
            return (int)(NextUInt64() >> 33); // Use upper bits, mask off sign
        }

        /// <summary>
        /// Generates a random integer in the range [0, maxValue).
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound.</param>
        /// <returns>A non-negative integer less than maxValue.</returns>
        public int NextInt(int maxValue)
        {
            if (maxValue <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxValue),
                    maxValue,
                    "maxValue must be positive."
                );
            }

            return NextInt(0, maxValue);
        }

        /// <summary>
        /// Generates a random integer in the range [minValue, maxValue).
        /// Uses unbiased rejection sampling as recommended for Lua 5.4.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound.</param>
        /// <param name="maxValue">The exclusive upper bound.</param>
        /// <returns>An integer in [minValue, maxValue).</returns>
        public int NextInt(int minValue, int maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxValue),
                    maxValue,
                    "maxValue must be greater than or equal to minValue."
                );
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            long range = (long)maxValue - minValue;
            return (int)((long)minValue + (long)NextUnbiasedRange((ulong)range));
        }

        /// <summary>
        /// Generates a random long integer in the range [minValue, maxValue] (inclusive).
        /// This matches Lua 5.4's math.random(m, n) semantics.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound.</param>
        /// <param name="maxValue">The inclusive upper bound.</param>
        /// <returns>A long integer in [minValue, maxValue].</returns>
        public long NextLong(long minValue, long maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxValue),
                    maxValue,
                    "maxValue must be greater than or equal to minValue."
                );
            }

            if (minValue == maxValue)
            {
                return minValue;
            }

            // Calculate range (may overflow for very large ranges)
            ulong range = (ulong)(maxValue - minValue);

            // Special case: full range
            if (range == ulong.MaxValue)
            {
                return (long)NextUInt64();
            }

            return minValue + (long)NextUnbiasedRange(range + 1);
        }

        /// <summary>
        /// Generates an unbiased random value in [0, range).
        /// Uses rejection sampling to avoid modulo bias.
        /// </summary>
        private ulong NextUnbiasedRange(ulong range)
        {
            // Compute threshold for rejection sampling
            // threshold = (2^64 - range) % range = -range % range
            ulong threshold = (0UL - range) % range;

            // Rejection loop: reject values that would cause bias
            ulong r;
            do
            {
                r = NextUInt64();
            } while (r < threshold);

            return r % range;
        }

        /// <summary>
        /// Generates a random double in the range [0, 1).
        /// Uses the upper 53 bits for IEEE 754 double precision.
        /// </summary>
        /// <returns>A double in [0.0, 1.0).</returns>
        public double NextDouble()
        {
            // Extract 53 bits (the mantissa precision of IEEE 754 double)
            // and convert to [0, 1)
            ulong bits = NextUInt64() >> 11; // Use upper 53 bits
            return bits * (1.0 / (1UL << 53));
        }

        /// <summary>
        /// Left rotate a 64-bit value.
        /// </summary>
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}
