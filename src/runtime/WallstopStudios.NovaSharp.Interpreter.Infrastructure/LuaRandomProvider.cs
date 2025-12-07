namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Production random provider that implements Lua 5.4's math.random semantics (§6.7).
    /// Uses the xoshiro256** algorithm for pseudo-random 64-bit integer generation.
    /// </summary>
    /// <remarks>
    /// This provider matches Lua 5.4's behavior:
    /// - Uses xoshiro256** PRNG algorithm
    /// - Supports 128-bit seeding via two 64-bit integers
    /// - math.randomseed() with no arguments uses cryptographic randomness
    /// - math.randomseed(x, y) returns the effective seed values
    /// </remarks>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Lua math.random is intentionally non-cryptographic per §6.7."
    )]
    public sealed class LuaRandomProvider : IRandomProvider
    {
        private readonly Xoshiro256StarStar _rng;

        /// <summary>
        /// Initializes a new instance with a cryptographically random seed.
        /// </summary>
        public LuaRandomProvider()
        {
            _rng = new Xoshiro256StarStar();
        }

        /// <summary>
        /// Initializes a new instance with the specified 128-bit seed.
        /// </summary>
        /// <param name="x">First 64-bit seed component.</param>
        /// <param name="y">Second 64-bit seed component.</param>
        public LuaRandomProvider(long x, long y)
        {
            _rng = new Xoshiro256StarStar(x, y);
        }

        /// <inheritdoc />
        public long SeedX => _rng.SeedX;

        /// <inheritdoc />
        public long SeedY => _rng.SeedY;

        /// <inheritdoc />
        public long NextInt64()
        {
            return _rng.NextInt64();
        }

        /// <inheritdoc />
        public long NextLong(long minValue, long maxValue)
        {
            return _rng.NextLong(minValue, maxValue);
        }

        /// <inheritdoc />
        public int NextInt()
        {
            return _rng.NextInt();
        }

        /// <inheritdoc />
        public int NextInt(int maxValue)
        {
            return _rng.NextInt(maxValue);
        }

        /// <inheritdoc />
        public int NextInt(int minValue, int maxValue)
        {
            return _rng.NextInt(minValue, maxValue);
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            return _rng.NextDouble();
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(int seed)
        {
            return _rng.SetSeed(seed);
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(long x, long y)
        {
            return _rng.SetSeed(x, y);
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            return _rng.SetSeedFromSystemRandom();
        }
    }
}
