namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Compiled Lua chunk wrapper.
    /// </summary>
    public sealed class LuaChunk
    {
        private readonly Script _script;
        private readonly CompiledScript _compiled;

        internal LuaChunk(Script script, CompiledScript compiled)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _compiled = compiled;
        }

        /// <summary>
        /// Executes the chunk with no arguments.
        /// </summary>
        public LuaValue Run()
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(_script, _compiled.Execute());
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Executes the chunk with one argument.
        /// </summary>
        public LuaValue Run(LuaValue arg0)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(_script, _compiled.Execute(arg0.ToDynValue(_script)));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Executes the chunk with two arguments.
        /// </summary>
        public LuaValue Run(LuaValue arg0, LuaValue arg1)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _compiled.Execute(arg0.ToDynValue(_script), arg1.ToDynValue(_script))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Executes the chunk with three arguments.
        /// </summary>
        public LuaValue Run(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _compiled.Execute(
                        arg0.ToDynValue(_script),
                        arg1.ToDynValue(_script),
                        arg2.ToDynValue(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Executes the chunk with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Run(ReadOnlySpan<LuaValue> args)
        {
            _script.ThrowIfDisposed();
            try
            {
                if (args.Length == 0)
                {
                    return LuaValue.WrapResult(_script, _compiled.Execute());
                }

                using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                    args.Length,
                    out DynValue[] converted
                );
                for (int i = 0; i < args.Length; i++)
                {
                    converted[i] = args[i].ToDynValue(_script);
                }

                return LuaValue.WrapResult(
                    _script,
                    _compiled.Execute(converted.AsSpan(0, args.Length))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }
    }
}
