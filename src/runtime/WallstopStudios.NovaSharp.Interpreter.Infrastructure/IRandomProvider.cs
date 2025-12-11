namespace WallstopStudios.NovaSharp.Interpreter.Infrastructure
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Abstraction over random number generation so scripts can operate deterministically
    /// when needed (e.g., lockstep multiplayer, replays, testing).
    /// </summary>
    /// <remarks>
    /// This interface is designed to support Lua 5.4's math.random semantics (§6.7):
    /// - 64-bit integer generation with all bits pseudo-random
    /// - 128-bit seeding (two 64-bit integers) via <see cref="SetSeed(long, long)"/>
    /// - Seed retrieval for math.randomseed return values
    /// </remarks>
    public interface IRandomProvider
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
        /// This corresponds to Lua 5.4's math.random(0) behavior.
        /// </summary>
        /// <returns>A 64-bit signed integer with all bits pseudo-random.</returns>
        public long NextInt64();

        /// <summary>
        /// Gets the next random integer in the range [minValue, maxValue] (inclusive).
        /// This corresponds to Lua 5.4's math.random(m, n) behavior.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound.</param>
        /// <param name="maxValue">The inclusive upper bound.</param>
        /// <returns>A 64-bit signed integer in [minValue, maxValue].</returns>
        public long NextLong(long minValue, long maxValue);

        /// <summary>
        /// Gets the next non-negative random integer.
        /// </summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0.</returns>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "NextInt mirrors System.Random.Next for familiarity."
        )]
        public int NextInt();

        /// <summary>
        /// Gets the next random integer within the specified range.
        /// </summary>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number to be generated.
        /// Must be greater than or equal to 0.
        /// </param>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>.
        /// </returns>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "NextInt mirrors System.Random.Next for familiarity."
        )]
        public int NextInt(int maxValue);

        /// <summary>
        /// Gets the next random integer within the specified range.
        /// </summary>
        /// <param name="minValue">
        /// The inclusive lower bound of the random number returned.
        /// </param>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number returned.
        /// Must be greater than or equal to <paramref name="minValue"/>.
        /// </param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
        /// </returns>
        [SuppressMessage(
            "Naming",
            "CA1716:Identifiers should not match keywords",
            Justification = "NextInt mirrors System.Random.Next for familiarity."
        )]
        public int NextInt(int minValue, int maxValue);

        /// <summary>
        /// Gets the next random floating-point number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating-point number that is greater than or equal to 0.0 and less than 1.0.
        /// </returns>
        public double NextDouble();

        /// <summary>
        /// Reinitializes the random provider with a single integer seed.
        /// </summary>
        /// <param name="seed">The seed to use for the random sequence.</param>
        /// <returns>A tuple containing the two 64-bit seed components that were effectively used.</returns>
        public (long X, long Y) SetSeed(int seed);

        /// <summary>
        /// Reinitializes the random provider with a 128-bit seed (two 64-bit values).
        /// This matches Lua 5.4's math.randomseed(x, y) semantics.
        /// </summary>
        /// <param name="x">First 64-bit seed component.</param>
        /// <param name="y">Second 64-bit seed component.</param>
        /// <returns>A tuple containing the two seed components that were used.</returns>
        public (long X, long Y) SetSeed(long x, long y);

        /// <summary>
        /// Reinitializes the random provider with a "weak attempt for randomness" as per Lua 5.4 §6.7.
        /// Called when math.randomseed() is invoked with no arguments.
        /// </summary>
        /// <returns>A tuple containing the two 64-bit seed components that were used.</returns>
        public (long X, long Y) SetSeedFromSystemRandom();
    }
}
