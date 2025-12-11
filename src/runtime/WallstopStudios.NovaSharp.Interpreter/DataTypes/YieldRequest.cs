namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class wrapping a request to yield a coroutine
    /// </summary>
    public class YieldRequest
    {
        /// <summary>
        /// The return values of the coroutine
        /// </summary>
        private DynValue[] _returnValues = Array.Empty<DynValue>();

        /// <summary>
        /// Gets the returned values as a read-only memory block.
        /// </summary>
        public ReadOnlyMemory<DynValue> ReturnValues
        {
            get { return _returnValues; }
            internal set { _returnValues = ExtractBackingArray(value); }
        }

        /// <summary>
        /// Gets a span view over the return values for low-level consumers.
        /// </summary>
        internal ReadOnlySpan<DynValue> ReturnValuesSpan => _returnValues;

        /// <summary>
        /// Exposes the underlying return-value buffer so coroutine machinery can reuse it without copying.
        /// </summary>
        internal DynValue[] BorrowReturnValuesBuffer()
        {
            return _returnValues;
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
        /// Gets or sets a value indicating whether this <see cref="YieldRequest"/> is a forced yield.
        /// </summary>
        public bool Forced { get; internal set; }
    }
}
