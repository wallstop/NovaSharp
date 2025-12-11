namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class used to support "tail" continuations - a way for C# / Lua interaction which supports
    /// coroutine yielding (at the expense of a LOT of added complexity in calling code).
    /// </summary>
    public class TailCallData
    {
        private DynValue[] _args = Array.Empty<DynValue>();

        /// <summary>
        /// Gets or sets the function to call
        /// </summary>
        public DynValue Function { get; set; }

        /// <summary>
        /// Gets the arguments to the function as a read-only memory block.
        /// </summary>
        public ReadOnlyMemory<DynValue> Args
        {
            get { return _args; }
            internal set { _args = ExtractBackingArray(value); }
        }

        /// <summary>
        /// Provides a span view over the argument buffer for callers that need indexed access.
        /// </summary>
        internal ReadOnlySpan<DynValue> ArgsSpan => _args;

        /// <summary>
        /// Exposes the underlying argument buffer so VM internals can reuse it without allocating.
        /// </summary>
        internal DynValue[] BorrowArgsBuffer()
        {
            return _args;
        }

        private static DynValue[] ExtractBackingArray(ReadOnlyMemory<DynValue> value)
        {
            if (value.IsEmpty)
            {
                return Array.Empty<DynValue>();
            }

            if (
                MemoryMarshal.TryGetArray(value, out ArraySegment<DynValue> segment)
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
        public DynValue ErrorHandlerBeforeUnwind { get; set; }
    }
}
