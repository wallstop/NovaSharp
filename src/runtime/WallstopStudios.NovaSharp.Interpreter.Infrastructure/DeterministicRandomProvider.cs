namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Random provider for deterministic execution using xoshiro256**.
    /// Uses a fixed seed to produce repeatable random sequences across script executions,
    /// making it suitable for lockstep multiplayer, replays, and testing scenarios.
    /// </summary>
    /// <remarks>
    /// This provider uses the same xoshiro256** algorithm as <see cref="LuaRandomProvider"/>
    /// but is explicitly designed for deterministic scenarios where:
    /// - The same seed always produces the same sequence
    /// - <see cref="SetSeedFromSystemRandom"/> is disabled (throws)
    /// </remarks>
    [SuppressMessage(
        "Security",
        "CA5394:Do not use insecure randomness",
        Justification = "Lua math.random is intentionally non-cryptographic per §6.7."
    )]
    public sealed class DeterministicRandomProvider : IRandomProvider
    {
        /// <summary>
        /// Default seed (first component) used when none is specified.
        /// Chosen to be a recognizable value that produces a reasonable distribution.
        /// </summary>
        public const long DefaultSeedX = 42;

        /// <summary>
        /// Default seed (second component) used when none is specified.
        /// </summary>
        public const long DefaultSeedY = 0;

        private readonly Xoshiro256StarStar _rng;
        private readonly bool _allowSystemRandom;

        /// <summary>
        /// Gets the first component of the current 128-bit seed.
        /// </summary>
        public long SeedX => _rng.SeedX;

        /// <summary>
        /// Gets the second component of the current 128-bit seed.
        /// </summary>
        public long SeedY => _rng.SeedY;

        /// <summary>
        /// Initializes a new instance with the default seed.
        /// </summary>
        public DeterministicRandomProvider()
            : this(DefaultSeedX, DefaultSeedY, allowSystemRandom: false) { }

        /// <summary>
        /// Initializes a new instance with the specified single seed (expanded to 128-bit).
        /// </summary>
        /// <param name="seed">The seed to use for the random sequence.</param>
        public DeterministicRandomProvider(int seed)
            : this(seed, (long)seed << 32 | (uint)seed, allowSystemRandom: false) { }

        /// <summary>
        /// Initializes a new instance with the specified 128-bit seed.
        /// </summary>
        /// <param name="seedX">First 64-bit seed component.</param>
        /// <param name="seedY">Second 64-bit seed component.</param>
        public DeterministicRandomProvider(long seedX, long seedY)
            : this(seedX, seedY, allowSystemRandom: false) { }

        /// <summary>
        /// Initializes a new instance with the specified 128-bit seed and system random policy.
        /// </summary>
        /// <param name="seedX">First 64-bit seed component.</param>
        /// <param name="seedY">Second 64-bit seed component.</param>
        /// <param name="allowSystemRandom">
        /// If true, <see cref="SetSeedFromSystemRandom"/> uses cryptographic RNG.
        /// If false (default), it throws to enforce determinism.
        /// </param>
        public DeterministicRandomProvider(long seedX, long seedY, bool allowSystemRandom)
        {
            _rng = new Xoshiro256StarStar(seedX, seedY);
            _allowSystemRandom = allowSystemRandom;
        }

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
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when <paramref name="allowSystemRandom"/> was false at construction time,
        /// indicating deterministic mode does not permit random seeding.
        /// </exception>
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            if (!_allowSystemRandom)
            {
                throw new System.InvalidOperationException(
                    "DeterministicRandomProvider does not allow random seeding. "
                        + "Use SetSeed(x, y) with explicit values, or construct with allowSystemRandom: true."
                );
            }

            return _rng.SetSeedFromSystemRandom();
        }
    }
}
