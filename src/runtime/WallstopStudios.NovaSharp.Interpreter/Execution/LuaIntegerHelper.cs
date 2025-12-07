namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System.Globalization;
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
        public static bool TryGetInteger(double number, out long value)
        {
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                value = 0;
                return false;
            }

            if (number < long.MinValue || number > long.MaxValue)
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
