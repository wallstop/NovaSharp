namespace NovaSharp
{
    /// <summary>
    /// Provides deterministic random values to a facade engine.
    /// </summary>
    public interface ILuaRandomProvider
    {
        /// <summary>
        /// Gets the first component of the current 128-bit seed.
        /// </summary>
        public long SeedX { get; }

        /// <summary>
        /// Gets the second component of the current 128-bit seed.
        /// </summary>
        public long SeedY { get; }

        /// <summary>
        /// Gets the next 64-bit pseudo-random integer with all bits random.
        /// </summary>
        public long NextInt64();

        /// <summary>
        /// Gets the next random integer in the inclusive range.
        /// </summary>
        public long NextLong(long minValue, long maxValue);

        /// <summary>
        /// Gets the next non-negative random integer.
        /// </summary>
        public int NextInteger();

        /// <summary>
        /// Gets the next random integer below the exclusive upper bound.
        /// </summary>
        public int NextInteger(int maxValue);

        /// <summary>
        /// Gets the next random integer in the half-open range.
        /// </summary>
        public int NextInteger(int minValue, int maxValue);

        /// <summary>
        /// Gets the next random floating-point number in the half-open range [0, 1).
        /// </summary>
        public double NextDouble();

        /// <summary>
        /// Reinitializes the provider with a single integer seed.
        /// </summary>
        public (long X, long Y) SetSeed(int seed);

        /// <summary>
        /// Reinitializes the provider with a 128-bit seed.
        /// </summary>
        public (long X, long Y) SetSeed(long x, long y);

        /// <summary>
        /// Reinitializes the provider from system randomness.
        /// </summary>
        public (long X, long Y) SetSeedFromSystemRandom();
    }
}
