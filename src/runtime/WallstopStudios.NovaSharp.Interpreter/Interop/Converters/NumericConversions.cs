namespace WallstopStudios.NovaSharp.Interpreter.Interop.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Static functions to handle conversions of numeric types
    /// </summary>
    internal static class NumericConversions
    {
        /// <summary>
        /// Array of numeric types in order used for some conversions
        /// </summary>
        internal static readonly Type[] NumericTypesOrdered =
        {
            typeof(double),
            typeof(decimal),
            typeof(float),
            typeof(long),
            typeof(int),
            typeof(short),
            typeof(sbyte),
            typeof(ulong),
            typeof(uint),
            typeof(ushort),
            typeof(byte),
        };

        /// <summary>
        /// HashSet of numeric types
        /// </summary>
        internal static readonly HashSet<Type> NumericTypes = new(NumericTypesOrdered);

        /// <summary>
        /// Converts a double to another type
        /// </summary>
        internal static object DoubleToType(Type type, double d)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            try
            {
                if (type == typeof(double))
                {
                    return d;
                }

                if (type == typeof(sbyte))
                {
                    return Convert.ToSByte(d);
                }

                if (type == typeof(byte))
                {
                    return Convert.ToByte(d);
                }

                if (type == typeof(short))
                {
                    return Convert.ToInt16(d);
                }

                if (type == typeof(ushort))
                {
                    return Convert.ToUInt16(d);
                }

                if (type == typeof(int))
                {
                    return Convert.ToInt32(d);
                }

                if (type == typeof(uint))
                {
                    return Convert.ToUInt32(d);
                }

                if (type == typeof(long))
                {
                    return Convert.ToInt64(d);
                }

                if (type == typeof(ulong))
                {
                    return Convert.ToUInt64(d);
                }

                if (type == typeof(float))
                {
                    return Convert.ToSingle(d);
                }

                if (type == typeof(decimal))
                {
                    return Convert.ToDecimal(d);
                }
            }
            catch (Exception ex)
                when (ex is InvalidCastException
                    || ex is OverflowException
                    || ex is FormatException
                    || ex is ArgumentException
                )
            {
                // Swallow conversion failures so the original double value can be returned.
            }

            return d;
        }

        /// <summary>
        /// Converts a type to double
        /// </summary>
        internal static double TypeToDouble(Type type, object d)
        {
            if (
                type != typeof(double)
                && type != typeof(sbyte)
                && type != typeof(byte)
                && type != typeof(short)
                && type != typeof(ushort)
                && type != typeof(int)
                && type != typeof(uint)
                && type != typeof(long)
                && type != typeof(ulong)
                && type != typeof(float)
                && type != typeof(decimal)
            )
            {
                return (double)d;
            }

            return Convert.ToDouble(d, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// HashSet of integer numeric types (excluding floating-point types).
        /// These types should be converted to Lua integer subtype.
        /// </summary>
        internal static readonly HashSet<Type> IntegerTypes = new()
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
        };

        /// <summary>
        /// HashSet of floating-point numeric types.
        /// These types should be converted to Lua float subtype.
        /// </summary>
        internal static readonly HashSet<Type> FloatingPointTypes = new()
        {
            typeof(float),
            typeof(double),
            typeof(decimal),
        };

        /// <summary>
        /// Determines whether the specified type is a CLR integer type that should
        /// be converted to a Lua integer subtype.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is an integer type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsIntegerType(Type type)
        {
            return IntegerTypes.Contains(type);
        }

        /// <summary>
        /// Determines whether the specified type is a CLR floating-point type that should
        /// be converted to a Lua float subtype.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if the type is a floating-point type; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFloatingPointType(Type type)
        {
            return FloatingPointTypes.Contains(type);
        }

        /// <summary>
        /// Converts a CLR object to a 64-bit signed integer.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The integer representation of the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long TypeToLong(Type type, object obj)
        {
            return Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }
    }
}
