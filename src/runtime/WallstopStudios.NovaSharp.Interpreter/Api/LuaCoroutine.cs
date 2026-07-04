namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Public Lua coroutine wrapper.
    /// </summary>
    public sealed class LuaCoroutine
    {
        private readonly LuaEngine _owner;
        private readonly DynValue _coroutineValue;

        internal LuaCoroutine(LuaEngine owner, DynValue coroutineValue)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _coroutineValue =
                coroutineValue ?? throw new ArgumentNullException(nameof(coroutineValue));
        }

        /// <summary>
        /// Gets the underlying coroutine state.
        /// </summary>
        public LuaCoroutineState State
        {
            get
            {
                _owner.ThrowIfDisposed();
                return ToFacadeState(_coroutineValue.Coroutine.State);
            }
        }

        /// <summary>
        /// Resumes the coroutine with no arguments.
        /// </summary>
        public LuaValue Resume()
        {
            _owner.ThrowIfDisposed();
            return _owner.WrapResult(_coroutineValue.Coroutine.Resume());
        }

        /// <summary>
        /// Resumes the coroutine with one argument.
        /// </summary>
        public LuaValue Resume(LuaValue arg0)
        {
            _owner.ThrowIfDisposed();
            return _owner.WrapResult(_coroutineValue.Coroutine.Resume(arg0.ToDynValue(_owner)));
        }

        /// <summary>
        /// Resumes the coroutine with two arguments.
        /// </summary>
        public LuaValue Resume(LuaValue arg0, LuaValue arg1)
        {
            _owner.ThrowIfDisposed();
            return _owner.WrapResult(
                _coroutineValue.Coroutine.Resume(arg0.ToDynValue(_owner), arg1.ToDynValue(_owner))
            );
        }

        /// <summary>
        /// Resumes the coroutine with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Resume(ReadOnlySpan<LuaValue> args)
        {
            _owner.ThrowIfDisposed();
            if (args.Length == 0)
            {
                return _owner.WrapResult(_coroutineValue.Coroutine.Resume());
            }

            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                args.Length,
                out DynValue[] converted
            );
            for (int i = 0; i < args.Length; i++)
            {
                converted[i] = args[i].ToDynValue(_owner);
            }

            return _owner.WrapResult(
                _coroutineValue.Coroutine.Resume(converted.AsSpan(0, args.Length))
            );
        }

        /// <summary>
        /// Closes the coroutine.
        /// </summary>
        public LuaValue Close()
        {
            _owner.ThrowIfDisposed();
            return _owner.WrapResult(_coroutineValue.Coroutine.Close());
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
