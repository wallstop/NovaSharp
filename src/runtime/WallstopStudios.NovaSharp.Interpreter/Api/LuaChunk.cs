namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Compiled Lua chunk wrapper.
    /// </summary>
    public sealed class LuaChunk
    {
        private readonly LuaEngine _owner;
        private readonly CompiledScript _compiled;

        internal LuaChunk(LuaEngine owner, CompiledScript compiled)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _compiled = compiled;
        }

        /// <summary>
        /// Executes the chunk with no arguments.
        /// </summary>
        public LuaValue Run()
        {
            _owner.ThrowIfDisposed();
            return _owner.Wrap(_compiled.Execute());
        }

        /// <summary>
        /// Executes the chunk with one argument.
        /// </summary>
        public LuaValue Run(LuaValue arg0)
        {
            _owner.ThrowIfDisposed();
            return _owner.Wrap(_compiled.Execute(arg0.ToDynValue(_owner)));
        }

        /// <summary>
        /// Executes the chunk with two arguments.
        /// </summary>
        public LuaValue Run(LuaValue arg0, LuaValue arg1)
        {
            _owner.ThrowIfDisposed();
            return _owner.Wrap(_compiled.Execute(arg0.ToDynValue(_owner), arg1.ToDynValue(_owner)));
        }

        /// <summary>
        /// Executes the chunk with three arguments.
        /// </summary>
        public LuaValue Run(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            _owner.ThrowIfDisposed();
            return _owner.Wrap(
                _compiled.Execute(
                    arg0.ToDynValue(_owner),
                    arg1.ToDynValue(_owner),
                    arg2.ToDynValue(_owner)
                )
            );
        }

        /// <summary>
        /// Executes the chunk with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Run(ReadOnlySpan<LuaValue> args)
        {
            _owner.ThrowIfDisposed();
            if (args.Length == 0)
            {
                return _owner.Wrap(_compiled.Execute());
            }

            DynValue[] converted = new DynValue[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                converted[i] = args[i].ToDynValue(_owner);
            }

            return _owner.Wrap(_compiled.Execute(converted.AsSpan()));
        }
    }
}
