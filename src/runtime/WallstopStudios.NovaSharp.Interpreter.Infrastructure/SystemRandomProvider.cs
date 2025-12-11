namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;

    /// <summary>
    /// Random provider that wraps <see cref="System.Random"/> for basic random number generation.
    /// This is a legacy provider that does NOT match Lua 5.4's xoshiro256** algorithm.
    /// </summary>
    /// <remarks>
    /// For Lua 5.4 compliance, use <see cref="LuaRandomProvider"/> instead.
    /// This provider is retained for:
    /// - Backwards compatibility with existing code
    /// - Scenarios where System.Random behavior is explicitly desired
    /// - .NET 6+ where System.Random uses xoshiro128** internally
    /// </remarks>
    [Obsolete(
        "Use LuaRandomProvider for Lua 5.4 compliance. SystemRandomProvider uses System.Random which may not match Lua's xoshiro256** output."
    )]
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Lua math.random is intentionally non-cryptographic per §6.7."
    )]
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private Random _random;
        private readonly object _lock = new object();
        private long _seedX;
        private long _seedY;

        /// <summary>
        /// Initializes a new instance with a cryptographically secure seed.
        /// </summary>
        public SystemRandomProvider()
        {
            byte[] seedBytes = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }

            int seed = BitConverter.ToInt32(seedBytes, 0);
            _seedX = seed;
            _seedY = BitConverter.ToInt32(seedBytes, 4);
            _random = new Random(seed);
        }

        /// <inheritdoc />
        public long SeedX => _seedX;

        /// <inheritdoc />
        public long SeedY => _seedY;

        /// <inheritdoc />
        public long NextInt64()
        {
            lock (_lock)
            {
                // System.Random doesn't have native 64-bit support on older frameworks
                // Generate two 32-bit values and combine them
                int high = _random.Next();
                int low = _random.Next();
                return ((long)high << 32) | (uint)low;
            }
        }

        /// <inheritdoc />
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

            lock (_lock)
            {
                // Use double-precision approach for range calculation
                ulong range = (ulong)(maxValue - minValue);
                if (range <= int.MaxValue)
                {
                    return minValue + _random.Next((int)range + 1);
                }

                // For larger ranges, use 64-bit generation
                double normalized = _random.NextDouble();
                return minValue + (long)(normalized * (range + 1));
            }
        }

        /// <inheritdoc />
        public int NextInt()
        {
            lock (_lock)
            {
                return _random.Next();
            }
        }

        /// <inheritdoc />
        public int NextInt(int maxValue)
        {
            lock (_lock)
            {
                return _random.Next(maxValue);
            }
        }

        /// <inheritdoc />
        public int NextInt(int minValue, int maxValue)
        {
            lock (_lock)
            {
                return _random.Next(minValue, maxValue);
            }
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            lock (_lock)
            {
                return _random.NextDouble();
            }
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(int seed)
        {
            lock (_lock)
            {
                _seedX = seed;
                _seedY = (long)seed << 32 | (uint)seed;
                _random = new Random(seed);
                return (_seedX, _seedY);
            }
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(long x, long y)
        {
            lock (_lock)
            {
                _seedX = x;
                _seedY = y;
                // System.Random only takes a 32-bit seed, so we combine x and y
                int combinedSeed = (int)(x ^ y ^ (x >> 32) ^ (y >> 32));
                _random = new Random(combinedSeed);
                return (x, y);
            }
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            byte[] seedBytes = new byte[16];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }

            long x = BitConverter.ToInt64(seedBytes, 0);
            long y = BitConverter.ToInt64(seedBytes, 8);
            return SetSeed(x, y);
        }
    }
}
