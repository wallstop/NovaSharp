namespace NovaSharp
{
    using System;

    /// <summary>
    /// Context passed to facade host callbacks.
    /// </summary>
    public readonly struct LuaContext : IEquatable<LuaContext>
    {
        private readonly LuaEngine _engine;

        internal LuaContext(LuaEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Gets the engine currently invoking the callback.
        /// </summary>
        public LuaEngine Engine
        {
            get
            {
                if (_engine == null)
                {
                    throw new InvalidOperationException("Lua callback context is not initialized.");
                }

                _engine.ThrowIfDisposed();
                return _engine;
            }
        }

        /// <inheritdoc />
        public bool Equals(LuaContext other)
        {
            return ReferenceEquals(_engine, other._engine);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LuaContext other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _engine == null ? 0 : _engine.GetHashCode();
        }

        /// <summary>
        /// Determines whether two callback contexts refer to the same engine.
        /// </summary>
        public static bool operator ==(LuaContext left, LuaContext right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two callback contexts refer to different engines.
        /// </summary>
        public static bool operator !=(LuaContext left, LuaContext right)
        {
            return !left.Equals(right);
        }
    }
}
