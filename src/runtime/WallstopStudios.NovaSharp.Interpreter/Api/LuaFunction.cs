namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Public Lua function wrapper.
    /// </summary>
    public sealed class LuaFunction
    {
        private readonly LuaEngine _owner;
        private readonly DynValue _function;

        internal LuaFunction(LuaEngine owner, DynValue function)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _function = function ?? throw new ArgumentNullException(nameof(function));
        }

        /// <summary>
        /// Calls this function with no arguments.
        /// </summary>
        public LuaValue Call()
        {
            return _owner.Call(this);
        }

        /// <summary>
        /// Calls this function with one argument.
        /// </summary>
        public LuaValue Call(LuaValue arg0)
        {
            return _owner.Call(this, arg0);
        }

        /// <summary>
        /// Calls this function with two arguments.
        /// </summary>
        public LuaValue Call(LuaValue arg0, LuaValue arg1)
        {
            return _owner.Call(this, arg0, arg1);
        }

        /// <summary>
        /// Calls this function with three arguments.
        /// </summary>
        public LuaValue Call(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            return _owner.Call(this, arg0, arg1, arg2);
        }

        /// <summary>
        /// Calls this function with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Call(ReadOnlySpan<LuaValue> args)
        {
            return _owner.Call(this, args);
        }

        /// <summary>
        /// Returns the underlying VM function after validating engine ownership.
        /// </summary>
        internal DynValue ToDynValue(LuaEngine expectedOwner)
        {
            _owner.ThrowIfDisposed();
            LuaEngine.EnsureSameOwner(_owner, expectedOwner);
            return _function;
        }
    }
}
