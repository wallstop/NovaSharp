namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Public Lua function wrapper.
    /// </summary>
    public sealed class LuaFunction
    {
        private readonly Script _script;
        private readonly DynValue _function;

        internal LuaFunction(Script script, DynValue function)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _function = function;
        }

        /// <summary>
        /// Calls this function with no arguments.
        /// </summary>
        public LuaValue Call()
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(_script, _script.Call(_function));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls this function with one argument.
        /// </summary>
        public LuaValue Call(LuaValue arg0)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _script.Call(_function, arg0.ToDynValueAfterOwnerChecked(_script))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls this function with two arguments.
        /// </summary>
        public LuaValue Call(LuaValue arg0, LuaValue arg1)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _script.Call(
                        _function,
                        arg0.ToDynValueAfterOwnerChecked(_script),
                        arg1.ToDynValueAfterOwnerChecked(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls this function with three arguments.
        /// </summary>
        public LuaValue Call(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _script.Call(
                        _function,
                        arg0.ToDynValueAfterOwnerChecked(_script),
                        arg1.ToDynValueAfterOwnerChecked(_script),
                        arg2.ToDynValueAfterOwnerChecked(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls this function with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Call(ReadOnlySpan<LuaValue> args)
        {
            switch (args.Length)
            {
                case 0:
                    return Call();
                case 1:
                    return Call(args[0]);
                case 2:
                    return Call(args[0], args[1]);
                case 3:
                    return Call(args[0], args[1], args[2]);
            }

            _script.ThrowIfDisposed();
            try
            {
                using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                    args.Length,
                    out DynValue[] converted
                );
                for (int i = 0; i < args.Length; i++)
                {
                    converted[i] = args[i].ToDynValueAfterOwnerChecked(_script);
                }

                return LuaValue.WrapResult(
                    _script,
                    _script.Call(_function, converted.AsSpan(0, args.Length))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Wraps this function as a Lua value for assignment or calls.
        /// </summary>
        public LuaValue ToValue()
        {
            _script.ThrowIfDisposed();
            return LuaValue.Wrap(_script, _function);
        }

        /// <summary>
        /// Returns the underlying VM function after validating engine ownership.
        /// </summary>
        internal DynValue ToDynValue(Script expectedOwner)
        {
            _script.ThrowIfDisposed();
            LuaEngine.EnsureSameOwner(_script, expectedOwner);
            return _function;
        }
    }
}
