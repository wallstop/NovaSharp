namespace NovaSharp.Interpreter.Execution
{
    using System.Globalization;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Helper methods that replicate Lua's integer conversion and shift semantics.
    /// </summary>
    internal static class LuaIntegerHelper
    {
        private const int IntegerBitCount = 64;

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

        public static bool TryGetInteger(DynValue value, out long integer)
        {
            DynValue scalar = value.ToScalar();

            if (scalar.Type == DataType.Number)
            {
                return TryGetInteger(scalar.Number, out integer);
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

        public static long ShiftRight(long value, long shift)
        {
            if (shift < 0)
            {
                return ShiftLeft(value, -shift);
            }

            if (shift >= IntegerBitCount)
            {
                return value < 0 ? -1 : 0;
            }

            return value >> (int)shift;
        }
    }
}
