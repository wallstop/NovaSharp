namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;

    /// <summary>
    /// Adapts the root facade loader contract to the current VM loader interface.
    /// </summary>
    internal sealed class LuaScriptLoaderAdapter : IScriptLoader
    {
        private readonly LuaEngine _owner;
        private readonly ILuaScriptLoader _loader;

        /// <summary>
        /// Initializes a new loader adapter.
        /// </summary>
        public LuaScriptLoaderAdapter(LuaEngine owner, ILuaScriptLoader loader)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <inheritdoc />
        public object LoadFile(string file, Table globalContext)
        {
            return _loader.LoadFile(file, new LuaTable(_owner, globalContext));
        }

        /// <inheritdoc />
        [Obsolete(
            "This serves almost no purpose. Kept here just to preserve backward compatibility."
        )]
        public string ResolveFileName(string filename, Table globalContext)
        {
            return _loader.ResolveFileName(filename, new LuaTable(_owner, globalContext));
        }

        /// <inheritdoc />
        public string ResolveModuleName(string modname, Table globalContext)
        {
            return _loader.ResolveModuleName(modname, new LuaTable(_owner, globalContext));
        }
    }

    /// <summary>
    /// Adapts the root facade time provider contract to the current VM provider interface.
    /// </summary>
    internal sealed class LuaTimeProviderAdapter : ITimeProvider
    {
        private readonly ILuaTimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new time provider adapter.
        /// </summary>
        public LuaTimeProviderAdapter(ILuaTimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <inheritdoc />
        public DateTimeOffset GetUtcNow()
        {
            return _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Adapts the root facade random provider contract to the current VM provider interface.
    /// </summary>
    internal sealed class LuaRandomProviderAdapter : IRandomProvider
    {
        private readonly ILuaRandomProvider _randomProvider;

        /// <summary>
        /// Initializes a new random provider adapter.
        /// </summary>
        public LuaRandomProviderAdapter(ILuaRandomProvider randomProvider)
        {
            _randomProvider =
                randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <inheritdoc />
        public long SeedX => _randomProvider.SeedX;

        /// <inheritdoc />
        public long SeedY => _randomProvider.SeedY;

        /// <inheritdoc />
        public long NextInt64()
        {
            return _randomProvider.NextInt64();
        }

        /// <inheritdoc />
        public long NextLong(long minValue, long maxValue)
        {
            return _randomProvider.NextLong(minValue, maxValue);
        }

        /// <inheritdoc />
        public int NextInt()
        {
            return _randomProvider.NextInteger();
        }

        /// <inheritdoc />
        public int NextInt(int maxValue)
        {
            return _randomProvider.NextInteger(maxValue);
        }

        /// <inheritdoc />
        public int NextInt(int minValue, int maxValue)
        {
            return _randomProvider.NextInteger(minValue, maxValue);
        }

        /// <inheritdoc />
        public double NextDouble()
        {
            return _randomProvider.NextDouble();
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(int seed)
        {
            return _randomProvider.SetSeed(seed);
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeed(long x, long y)
        {
            return _randomProvider.SetSeed(x, y);
        }

        /// <inheritdoc />
        public (long X, long Y) SetSeedFromSystemRandom()
        {
            return _randomProvider.SetSeedFromSystemRandom();
        }
    }
}
