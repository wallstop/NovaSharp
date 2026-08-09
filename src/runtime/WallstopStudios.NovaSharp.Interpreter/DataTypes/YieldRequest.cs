namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Runtime.InteropServices;
    using global::NovaSharp;

    /// <summary>
    /// Class wrapping a request to yield a coroutine
    /// </summary>
    public class YieldRequest
    {
        /// <summary>
        /// The return values of the coroutine
        /// </summary>
        private LuaValue[] _returnValues;
        private LuaValue _arg0;
        private LuaValue _arg1;
        private LuaValue _arg2;
        private LuaValue _arg3;
        private int _count;

        /// <summary>
        /// Gets the returned values as a read-only memory block.
        /// </summary>
        public ReadOnlyMemory<LuaValue> ReturnValues
        {
            get { return GetReturnValuesBuffer(); }
            internal set
            {
                _returnValues = ExtractBackingArray(value);
                _count = _returnValues.Length;
                _arg0 = default;
                _arg1 = default;
                _arg2 = default;
                _arg3 = default;
            }
        }

        /// <summary>
        /// Gets a span view over the return values for low-level consumers.
        /// </summary>
        internal ReadOnlySpan<LuaValue> ReturnValuesSpan => GetReturnValuesBuffer();

        /// <summary>
        /// Creates a yield request storing one return value without allocating a return array.
        /// </summary>
        /// <param name="arg">The yielded return value.</param>
        /// <returns>A new <see cref="YieldRequest"/>.</returns>
        internal static YieldRequest New(LuaValue arg)
        {
            return new YieldRequest { _count = 1, _arg0 = arg };
        }

        /// <summary>
        /// Creates a yield request storing two return values without allocating a return array.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <returns>A new <see cref="YieldRequest"/>.</returns>
        internal static YieldRequest New(LuaValue arg0, LuaValue arg1)
        {
            return new YieldRequest
            {
                _count = 2,
                _arg0 = arg0,
                _arg1 = arg1,
            };
        }

        /// <summary>
        /// Creates a yield request storing three return values without allocating a return array.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <param name="arg2">The third yielded return value.</param>
        /// <returns>A new <see cref="YieldRequest"/>.</returns>
        internal static YieldRequest New(LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            return new YieldRequest
            {
                _count = 3,
                _arg0 = arg0,
                _arg1 = arg1,
                _arg2 = arg2,
            };
        }

        /// <summary>
        /// Creates a yield request storing four return values without allocating a return array.
        /// </summary>
        /// <param name="arg0">The first yielded return value.</param>
        /// <param name="arg1">The second yielded return value.</param>
        /// <param name="arg2">The third yielded return value.</param>
        /// <param name="arg3">The fourth yielded return value.</param>
        /// <returns>A new <see cref="YieldRequest"/>.</returns>
        internal static YieldRequest New(LuaValue arg0, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            return new YieldRequest
            {
                _count = 4,
                _arg0 = arg0,
                _arg1 = arg1,
                _arg2 = arg2,
                _arg3 = arg3,
            };
        }

        /// <summary>
        /// Exposes the underlying return-value buffer so coroutine machinery can reuse it without copying.
        /// </summary>
        internal LuaValue[] BorrowReturnValuesBuffer()
        {
            return GetReturnValuesBuffer();
        }

        /// <summary>
        /// Converts the yielded values into the tuple shape returned by coroutine resume.
        /// </summary>
        /// <returns>The yielded values as a scalar, tuple, or nil value.</returns>
        internal LuaValue ToTuple()
        {
            if (_returnValues != null)
            {
                if (_returnValues.Length == 0)
                {
                    return LuaValue.EmptyTuple;
                }

                return LuaValue.NewTuple(_returnValues);
            }

            switch (_count)
            {
                case 0:
                    return LuaValue.EmptyTuple;
                case 1:
                    return _arg0;
                case 2:
                case 3:
                case 4:
                    return LuaValue.NewTuple(GetReturnValuesBuffer());
                default:
                    return LuaValue.NewTuple(GetReturnValuesBuffer());
            }
        }

        private LuaValue[] GetReturnValuesBuffer()
        {
            if (_returnValues != null)
            {
                return _returnValues;
            }

            switch (_count)
            {
                case 0:
                    _returnValues = Array.Empty<LuaValue>();
                    break;
                case 1:
                    _returnValues = new[] { _arg0 };
                    break;
                case 2:
                    _returnValues = new[] { _arg0, _arg1 };
                    break;
                case 3:
                    _returnValues = new[] { _arg0, _arg1, _arg2 };
                    break;
                case 4:
                    _returnValues = new[] { _arg0, _arg1, _arg2, _arg3 };
                    break;
                default:
                    throw new InvalidOperationException("Invalid yield return value count.");
            }

            return _returnValues;
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
        /// Gets or sets a value indicating whether this <see cref="YieldRequest"/> is a forced yield.
        /// </summary>
        public bool Forced { get; internal set; }
    }
}
