namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Runtime.InteropServices;
    using global::NovaSharp;

    /// <summary>
    /// Class used to support "tail" continuations - a way for C# / Lua interaction which supports
    /// coroutine yielding (at the expense of a LOT of added complexity in calling code).
    /// </summary>
    public class TailCallData
    {
        private LuaValue[] _args = Array.Empty<LuaValue>();
        private LuaValue _errorHandlerBeforeUnwind = LuaValue.Nil;

        /// <summary>
        /// Gets or sets the function to call
        /// </summary>
        public LuaValue Function { get; set; }

        /// <summary>
        /// Gets the arguments to the function as a read-only memory block.
        /// </summary>
        public ReadOnlyMemory<LuaValue> Args
        {
            get { return _args; }
            internal set { _args = ExtractBackingArray(value); }
        }

        /// <summary>
        /// Provides a span view over the argument buffer for callers that need indexed access.
        /// </summary>
        internal ReadOnlySpan<LuaValue> ArgsSpan => _args;

        /// <summary>
        /// Exposes the underlying argument buffer so VM internals can reuse it without allocating.
        /// </summary>
        internal LuaValue[] BorrowArgsBuffer()
        {
            return _args;
        }

        private static LuaValue[] ExtractBackingArray(ReadOnlyMemory<LuaValue> value)
        {
            if (value.IsEmpty)
            {
                return Array.Empty<LuaValue>();
            }

            if (
                MemoryMarshal.TryGetArray(value, out ArraySegment<LuaValue> segment)
                && segment.Array != null
                && segment.Offset == 0
                && segment.Count == segment.Array.Length
            )
            {
                return segment.Array;
            }

            return value.ToArray();
        }

        /// <summary>
        /// Gets or sets the callback to be used as a continuation.
        /// </summary>
        public CallbackFunction Continuation { get; set; }

        /// <summary>
        /// Gets or sets the callback to be used in case of errors.
        /// </summary>
        public CallbackFunction ErrorHandler { get; set; }

        /// <summary>
        /// Gets or sets the error handler to be called before stack unwinding
        /// </summary>
        public LuaValue? ErrorHandlerBeforeUnwind
        {
            get
            {
                return HasErrorHandlerBeforeUnwind ? _errorHandlerBeforeUnwind : (LuaValue?)null;
            }
            set
            {
                if (!value.HasValue)
                {
                    _errorHandlerBeforeUnwind = LuaValue.Nil;
                    HasErrorHandlerBeforeUnwind = false;
                    return;
                }

                _errorHandlerBeforeUnwind = value.Value;
                HasErrorHandlerBeforeUnwind = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether an error handler was explicitly supplied for execution
        /// before stack unwinding.
        /// </summary>
        public bool HasErrorHandlerBeforeUnwind { get; private set; }

        /// <summary>
        /// Gets the non-null VM representation of the optional pre-unwind error handler.
        /// </summary>
        internal LuaValue ErrorHandlerBeforeUnwindValue => _errorHandlerBeforeUnwind;
    }
}
