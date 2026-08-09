namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Public Lua coroutine wrapper.
    /// </summary>
    public sealed class LuaCoroutine
    {
        private readonly Script _script;
        private readonly DynValue _coroutineValue;

        internal LuaCoroutine(Script script, DynValue coroutineValue)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _coroutineValue = coroutineValue;
        }

        /// <summary>
        /// Gets the underlying coroutine state.
        /// </summary>
        public LuaCoroutineState State
        {
            get
            {
                _script.ThrowIfDisposed();
                return ToFacadeState(_coroutineValue.Coroutine.State);
            }
        }

        /// <summary>
        /// Resumes the coroutine with no arguments.
        /// </summary>
        public LuaValue Resume()
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(_script, _coroutineValue.Coroutine.Resume());
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Resumes the coroutine with one argument.
        /// </summary>
        public LuaValue Resume(LuaValue arg0)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _coroutineValue.Coroutine.Resume(arg0.ToDynValue(_script))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Resumes the coroutine with two arguments.
        /// </summary>
        public LuaValue Resume(LuaValue arg0, LuaValue arg1)
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.WrapResult(
                    _script,
                    _coroutineValue.Coroutine.Resume(
                        arg0.ToDynValue(_script),
                        arg1.ToDynValue(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Resume(ReadOnlySpan<LuaValue> args)
        {
            _script.ThrowIfDisposed();
            try
            {
                if (args.Length == 0)
                {
                    return LuaValue.WrapResult(_script, _coroutineValue.Coroutine.Resume());
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
                    _coroutineValue.Coroutine.Resume(converted.AsSpan(0, args.Length))
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Closes the coroutine.
        /// </summary>
        public LuaValue Close()
        {
            _script.ThrowIfDisposed();
            try
            {
                return LuaValue.Wrap(_script, _coroutineValue.Coroutine.Close());
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Wraps this coroutine as a Lua value for assignment or calls.
        /// </summary>
        public LuaValue ToValue()
        {
            _script.ThrowIfDisposed();
            return LuaValue.Wrap(_script, _coroutineValue);
        }

        private static LuaCoroutineState ToFacadeState(CoroutineState state)
        {
            switch (state)
            {
                case CoroutineState.Main:
                    return LuaCoroutineState.Main;
                case CoroutineState.NotStarted:
                    return LuaCoroutineState.NotStarted;
                case CoroutineState.Suspended:
                    return LuaCoroutineState.Suspended;
                case CoroutineState.ForceSuspended:
                    return LuaCoroutineState.ForceSuspended;
                case CoroutineState.Running:
                    return LuaCoroutineState.Running;
                case CoroutineState.Dead:
                    return LuaCoroutineState.Dead;
                default:
                    return LuaCoroutineState.Unknown;
            }
        }
    }
}
