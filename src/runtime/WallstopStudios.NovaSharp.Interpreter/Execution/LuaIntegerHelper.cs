namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Helper methods that replicate Lua's integer conversion and shift semantics.
    /// </summary>
    internal static class LuaIntegerHelper
    {
        private const int IntegerBitCount = 64;

        /// <summary>
        /// Attempts to coerce a double into a Lua integer (verifying range and fractional part).
        /// </summary>
        /// <remarks>
        /// The upper bound check uses >= because (double)long.MaxValue loses precision and rounds up
        /// to 2^63, which is actually out of range. Without this, values like 2^63 would pass the
        /// range check but produce undefined behavior when cast to long.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInteger(double number, out long value)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                value = 0;
                return false;
            }

            // Note: (double)long.MaxValue rounds up to 2^63 due to IEEE 754 precision loss.
            // We use >= for the upper bound to reject values that would overflow when cast.
            // The lower bound uses > because (double)long.MinValue is exactly representable.
            if (number < long.MinValue || number >= MaxDoubleIntegerValue)
            {
                value = 0;
                return false;
            }

            double truncated = System.Math.Truncate(number);
            if (truncated != number)
            {
                value = 0;
                return false;
            }

            value = (long)truncated;
            return true;
        }

        // 2^63 as a double - the smallest double value that cannot be represented as a long.
        // (double)long.MaxValue rounds up to this value, so we reject anything >= this threshold.
        private const double MaxDoubleIntegerValue = 9223372036854775808.0; // 2^63

        /// <summary>
        /// Attempts to coerce a DynValue into a Lua integer (accepting numeric strings per Lua semantics).
        /// </summary>
        public static bool TryGetInteger(DynValue value, out long integer)
        {
            DynValue scalar = value.ToScalar();

            if (scalar.Type == DataType.Number)
            {
                // Use the underlying LuaNumber to preserve integer precision
                LuaNumber num = scalar.LuaNumber;
                if (num.IsInteger)
                {
                    integer = num.AsInteger;
                    return true;
                }

                // Float case - check if convertible to integer
                return TryGetInteger(num.ToDouble, out integer);
            }

            if (
                scalar.Type == DataType.String
                && double.TryParse(
                    scalar.String,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed
                )
                && TryGetInteger(parsed, out integer)
            )
            {
                return true;
            }

            integer = 0;
            return false;
        }

        /// <summary>
        /// Performs a Lua-style left shift, clamping large shifts to zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ShiftLeft(long value, long shift)
        {
            if (shift < 0)
            {
                return ShiftRight(value, -shift);
            }

            if (shift >= IntegerBitCount)
            {
                return 0;
            }

            return value << (int)shift;
        }

        /// <summary>
        /// Performs a Lua-style logical right shift, clamping large shifts to zero.
        /// </summary>
        /// <remarks>
        /// Per Lua 5.3/5.4 specification (§3.4.2), right shifts are logical (unsigned),
        /// not arithmetic. Shifts by >= 64 bits always return 0.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ShiftRight(long value, long shift)
        {
            if (shift < 0)
            {
                return ShiftLeft(value, -shift);
            }

            if (shift >= IntegerBitCount)
            {
                return 0;
            }

            // Logical (unsigned) right shift per Lua spec
            return (long)((ulong)value >> (int)shift);
        }
    }
}
