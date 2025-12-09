namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Compatibility;
    using DataStructs;

    /// <summary>
    /// Represents a Lua number that can be either an integer (64-bit signed) or a float (64-bit IEEE 754 double).
    /// This discriminated union type enables proper Lua 5.3+ semantics where integers and floats are distinct subtypes.
    /// </summary>
    /// <remarks>
    /// Per Lua 5.3+ specification (§2.1), numbers come in two flavors:
    /// - Integer: 64-bit two's complement integers
    /// - Float: 64-bit IEEE 754 double-precision floating-point
    ///
    /// The type uses explicit struct layout to overlay the integer and float storage for memory efficiency,
    /// with a separate flag byte to track which subtype is active.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct LuaNumber : IEquatable<LuaNumber>, IComparable<LuaNumber>
    {
        /// <summary>
        /// The 64-bit signed integer value when <see cref="IsInteger"/> is true.
        /// </summary>
        [FieldOffset(0)]
        private readonly long _integer;

        /// <summary>
        /// The 64-bit IEEE 754 double value when <see cref="IsFloat"/> is true.
        /// Note: This field is overlaid with <see cref="_integer"/> at the same memory offset.
        /// </summary>
        [FieldOffset(0)]
        private readonly double _float;

        /// <summary>
        /// Flag indicating whether this value is an integer (true) or float (false).
        /// </summary>
        [FieldOffset(8)]
        private readonly bool _isInteger;

        /// <summary>
        /// Gets a value indicating whether this number is an integer subtype.
        /// </summary>
        public bool IsInteger => _isInteger;

        /// <summary>
        /// Gets a value indicating whether this number is a float subtype.
        /// </summary>
        public bool IsFloat => !_isInteger;

        /// <summary>
        /// Gets the integer value. Only valid when <see cref="IsInteger"/> is true.
        /// When called on a float, returns the truncated integer representation.
        /// </summary>
        public long AsInteger => _isInteger ? _integer : (long)_float;

        /// <summary>
        /// Gets the float value. Only valid when <see cref="IsFloat"/> is true.
        /// When called on an integer, returns the double representation (may lose precision for large values).
        /// </summary>
        public double AsFloat => _isInteger ? (double)_integer : _float;

        /// <summary>
        /// Gets the value as a double, regardless of subtype.
        /// For integers, this converts to double (may lose precision for values outside ±2^53).
        /// </summary>
        public double ToDouble => _isInteger ? (double)_integer : _float;

        /// <summary>
        /// Gets "integer" or "float" as per Lua 5.3+ math.type() semantics.
        /// </summary>
        public string LuaTypeName => _isInteger ? "integer" : "float";

        /// <summary>
        /// Zero as an integer.
        /// </summary>
        public static readonly LuaNumber Zero = FromInteger(0);

        /// <summary>
        /// One as an integer.
        /// </summary>
        public static readonly LuaNumber One = FromInteger(1);

        /// <summary>
        /// Positive infinity as a float.
        /// </summary>
        public static readonly LuaNumber PositiveInfinity = FromFloat(double.PositiveInfinity);

        /// <summary>
        /// Negative infinity as a float.
        /// </summary>
        public static readonly LuaNumber NegativeInfinity = FromFloat(double.NegativeInfinity);

        /// <summary>
        /// Not-a-number (NaN) as a float.
        /// </summary>
        public static readonly LuaNumber NaN = FromFloat(double.NaN);

        /// <summary>
        /// Maximum integer value (2^63 - 1).
        /// </summary>
        public static readonly LuaNumber MaxInteger = FromInteger(long.MaxValue);

        /// <summary>
        /// Minimum integer value (-2^63).
        /// </summary>
        public static readonly LuaNumber MinInteger = FromInteger(long.MinValue);

        /// <summary>
        /// Creates a new integer LuaNumber.
        /// </summary>
        /// <param name="value">The 64-bit signed integer value.</param>
        /// <returns>A LuaNumber representing an integer.</returns>
        public static LuaNumber FromInteger(long value)
        {
            return new LuaNumber(value, isInteger: true);
        }

        /// <summary>
        /// Creates a new integer LuaNumber from a 32-bit integer.
        /// Alternate for implicit conversion operator (CA2225).
        /// </summary>
        /// <param name="value">The 32-bit signed integer value.</param>
        /// <returns>A LuaNumber representing an integer.</returns>
        public static LuaNumber FromInt32(int value)
        {
            return new LuaNumber(value, isInteger: true);
        }

        /// <summary>
        /// Creates a new integer LuaNumber from a 64-bit integer.
        /// Alternate for implicit conversion operator (CA2225).
        /// </summary>
        /// <param name="value">The 64-bit signed integer value.</param>
        /// <returns>A LuaNumber representing an integer.</returns>
        public static LuaNumber FromInt64(long value)
        {
            return FromInteger(value);
        }

        /// <summary>
        /// Creates a new float LuaNumber.
        /// </summary>
        /// <param name="value">The IEEE 754 double value.</param>
        /// <returns>A LuaNumber representing a float.</returns>
        public static LuaNumber FromFloat(double value)
        {
            return new LuaNumber(value, isInteger: false);
        }

        /// <summary>
        /// Creates a LuaNumber from a double, automatically determining if it should be an integer.
        /// Integer-like doubles (whole numbers within long range) become integers.
        /// </summary>
        /// <param name="value">The double value to convert.</param>
        /// <returns>A LuaNumber, either integer (if value is a whole number in range) or float.</returns>
        public static LuaNumber FromDouble(double value)
        {
            if (TryConvertToInteger(value, out long intValue))
            {
                return FromInteger(intValue);
            }
            return FromFloat(value);
        }

        /// <summary>
        /// Attempts to convert a double to an integer if it represents a whole number within the integer range.
        /// </summary>
        /// <param name="value">The double value to convert.</param>
        /// <param name="intValue">When successful, contains the integer value.</param>
        /// <returns>true if conversion succeeded; false otherwise.</returns>
        public static bool TryConvertToInteger(double value, out long intValue)
        {
            // NaN and infinity cannot be converted
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                intValue = 0;
                return false;
            }

            // Negative zero (-0.0) must remain a float to preserve IEEE 754 semantics
            // (e.g., 1.0 / -0.0 must return -inf, not +inf)
            if (IsNegativeZero(value))
            {
                intValue = 0;
                return false;
            }

            // Check if it's a whole number
            double truncated = Math.Truncate(value);
            if (truncated != value)
            {
                intValue = 0;
                return false;
            }

            // Check if within long range
            // Note: We use >= and <= because long.MinValue and long.MaxValue can be exactly represented as doubles
            // (though MaxValue loses precision - it rounds to 2^63 which equals long.MaxValue + 1)
            // We need to check explicitly for this edge case
            if (value < (double)long.MinValue || value >= (double)long.MaxValue + 1.0)
            {
                intValue = 0;
                return false;
            }

            intValue = (long)truncated;
            return true;
        }

        /// <summary>
        /// Checks if the given double value is negative zero (-0.0).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>true if the value is negative zero; false otherwise.</returns>
        private static bool IsNegativeZero(double value)
        {
            return value == 0.0 && double.IsNegative(value);
        }

        /// <summary>
        /// Parses a string into a LuaNumber following Lua parsing rules.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="result">When successful, contains the parsed LuaNumber.</param>
        /// <returns>true if parsing succeeded; false otherwise.</returns>
        public static bool TryParse(string s, out LuaNumber result)
        {
            if (string.IsNullOrEmpty(s))
            {
                result = Zero;
                return false;
            }

            s = s.Trim();

            // Try parsing as integer first (handles hex integers like 0x1F)
            if (TryParseInteger(s, out long intValue))
            {
                result = FromInteger(intValue);
                return true;
            }

            // Try parsing as float
            if (
                double.TryParse(
                    s,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out double floatValue
                )
            )
            {
                result = FromFloat(floatValue);
                return true;
            }

            result = Zero;
            return false;
        }

        private static bool TryParseInteger(string s, out long value)
        {
            // Handle hexadecimal
            if (s.Length > 2 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
            {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
                if (
                    long.TryParse(
                        s.AsSpan(2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out value
                    )
                )
                {
                    return true;
                }
#else
                if (
                    long.TryParse(
                        s.Substring(2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out value
                    )
                )
                {
                    return true;
                }
#endif
            }

            // Handle decimal integers (no decimal point, no exponent)
            bool hasDecimal = s.Contains('.', StringComparison.Ordinal);
            bool hasExponent =
                s.Contains('e', StringComparison.Ordinal)
                || s.Contains('E', StringComparison.Ordinal);

            if (
                !hasDecimal
                && !hasExponent
                && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            )
            {
                return true;
            }

            value = 0;
            return false;
        }

        private LuaNumber(long integerValue, bool isInteger)
        {
            // Note: Due to struct layout, we must initialize both fields
            // The assignment order matters due to field overlay
            _float = 0;
            _integer = integerValue;
            _isInteger = isInteger;
        }

        private LuaNumber(double floatValue, bool isInteger)
        {
            // Note: Due to struct layout, we must initialize both fields
            // The assignment order matters due to field overlay
            _integer = 0;
            _float = floatValue;
            _isInteger = isInteger;
        }

        /// <summary>
        /// Adds two LuaNumbers following Lua semantics.
        /// Integer + Integer = Integer (with wrapping overflow per Lua spec).
        /// Otherwise, result is Float.
        /// </summary>
        public static LuaNumber Add(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                // Wrapping addition per Lua spec
                return FromInteger(unchecked(a._integer + b._integer));
            }
            return FromFloat(a.ToDouble + b.ToDouble);
        }

        /// <summary>
        /// Subtracts two LuaNumbers following Lua semantics.
        /// Integer - Integer = Integer (with wrapping overflow per Lua spec).
        /// Otherwise, result is Float.
        /// </summary>
        public static LuaNumber Subtract(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                return FromInteger(unchecked(a._integer - b._integer));
            }
            return FromFloat(a.ToDouble - b.ToDouble);
        }

        /// <summary>
        /// Multiplies two LuaNumbers following Lua semantics.
        /// Integer * Integer = Integer (with wrapping overflow per Lua spec).
        /// Otherwise, result is Float.
        /// </summary>
        public static LuaNumber Multiply(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                return FromInteger(unchecked(a._integer * b._integer));
            }
            return FromFloat(a.ToDouble * b.ToDouble);
        }

        /// <summary>
        /// Divides two LuaNumbers following Lua semantics.
        /// Regular division (/) always returns float per Lua spec §3.4.1.
        /// </summary>
        public static LuaNumber Divide(LuaNumber a, LuaNumber b)
        {
            // Regular division always returns float per Lua spec
            return FromFloat(a.ToDouble / b.ToDouble);
        }

        /// <summary>
        /// Floor division (//) following Lua semantics.
        /// When both operands are integers, returns integer (error on divide by zero).
        /// Otherwise, returns float (infinity on divide by zero).
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <returns>The floor quotient.</returns>
        /// <exception cref="Errors.ScriptRuntimeException">When dividing integers by zero.</exception>
        public static LuaNumber FloorDivide(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                if (b._integer == 0)
                {
                    throw new Errors.ScriptRuntimeException("attempt to divide by zero");
                }
                // Special case: mininteger // -1 wraps to mininteger in Lua
                // In C#, this would throw OverflowException, but Lua uses two's complement wrapping
                if (a._integer == long.MinValue && b._integer == -1)
                {
                    return FromInteger(long.MinValue);
                }
                // Integer floor division - use C# integer division which truncates toward zero,
                // then adjust for Lua's floor semantics (toward negative infinity)
                long quotient = a._integer / b._integer;
                long remainder = a._integer % b._integer;
                // Adjust if the result was rounded toward zero instead of floor
                if (remainder != 0 && (a._integer < 0) != (b._integer < 0))
                {
                    quotient--;
                }
                return FromInteger(quotient);
            }
            return FromFloat(Math.Floor(a.ToDouble / b.ToDouble));
        }

        /// <summary>
        /// Modulo operation (%) following Lua semantics.
        /// Integer % Integer = Integer (following floor division semantics).
        /// Otherwise, result is Float.
        /// Alternate method name for % operator (CA2225).
        /// </summary>
        public static LuaNumber Remainder(LuaNumber a, LuaNumber b)
        {
            return Modulo(a, b);
        }

        /// <summary>
        /// Modulo operation (%) following Lua semantics.
        /// Integer % Integer = Integer (following floor division semantics).
        /// Otherwise, result is Float.
        /// </summary>
        public static LuaNumber Modulo(LuaNumber a, LuaNumber b)
        {
            // Default to Lua 5.3+ behavior (throws error on integer modulo by zero)
            return Modulo(a, b, LuaVersionDefaults.CurrentDefault);
        }

        /// <summary>
        /// Modulo operation (%) following Lua semantics with version-aware behavior.
        /// Integer % Integer = Integer (following floor division semantics).
        /// Otherwise, result is Float.
        /// </summary>
        /// <param name="a">The dividend.</param>
        /// <param name="b">The divisor.</param>
        /// <param name="version">The Lua compatibility version to use for error handling.</param>
        /// <returns>The modulo result.</returns>
        /// <remarks>
        /// In Lua 5.1/5.2, integer modulo by zero returns nan (promotes to float semantics).
        /// In Lua 5.3+, integer modulo by zero throws "attempt to perform 'n%0'".
        /// Float modulo by zero always returns nan in all versions.
        /// </remarks>
        public static LuaNumber Modulo(LuaNumber a, LuaNumber b, LuaCompatibilityVersion version)
        {
            LuaCompatibilityVersion resolved = LuaVersionDefaults.Resolve(version);

            if (a.IsInteger && b.IsInteger)
            {
                if (b._integer == 0)
                {
                    // In Lua 5.1/5.2, integer modulo by zero returns nan (float semantics)
                    // In Lua 5.3+, integer modulo by zero throws an error
                    if (resolved < LuaCompatibilityVersion.Lua53)
                    {
                        // Fall through to float logic which returns nan
                        double ad = a.ToDouble;
                        double bd = b.ToDouble;
                        return FromFloat(ad - Math.Floor(ad / bd) * bd);
                    }
                    throw new Errors.ScriptRuntimeException("attempt to perform 'n%0'");
                }
                // Lua modulo follows floor division: a % b == a - floor(a/b) * b
                long remainder = a._integer % b._integer;
                // Adjust for Lua's floor semantics
                if (remainder != 0 && (a._integer < 0) != (b._integer < 0))
                {
                    remainder += b._integer;
                }
                return FromInteger(remainder);
            }
            double aDouble = a.ToDouble;
            double bDouble = b.ToDouble;
            return FromFloat(aDouble - Math.Floor(aDouble / bDouble) * bDouble);
        }

        /// <summary>
        /// Power operation (^) following Lua semantics.
        /// Power always returns float per Lua spec.
        /// </summary>
        public static LuaNumber Power(LuaNumber a, LuaNumber b)
        {
            // Power always returns float per Lua spec
            return FromFloat(Math.Pow(a.ToDouble, b.ToDouble));
        }

        /// <summary>
        /// Unary minus following Lua semantics.
        /// Negating an integer returns an integer (with wrapping for MinValue).
        /// Negating a float returns a float.
        /// </summary>
        public static LuaNumber Negate(LuaNumber a)
        {
            if (a.IsInteger)
            {
                // Wrapping negation per Lua spec (MinValue stays MinValue)
                return FromInteger(unchecked(-a._integer));
            }
            return FromFloat(-a._float);
        }

        /// <summary>
        /// Bitwise AND operation. Both operands must be convertible to integers.
        /// </summary>
        public static LuaNumber BitwiseAnd(LuaNumber a, LuaNumber b)
        {
            long ai = a.RequireInteger("band");
            long bi = b.RequireInteger("band");
            return FromInteger(ai & bi);
        }

        /// <summary>
        /// Bitwise OR operation. Both operands must be convertible to integers.
        /// </summary>
        public static LuaNumber BitwiseOr(LuaNumber a, LuaNumber b)
        {
            long ai = a.RequireInteger("bor");
            long bi = b.RequireInteger("bor");
            return FromInteger(ai | bi);
        }

        /// <summary>
        /// Bitwise XOR operation. Both operands must be convertible to integers.
        /// Alternate method name for ^ operator (CA2225).
        /// </summary>
        public static LuaNumber Xor(LuaNumber a, LuaNumber b)
        {
            return BitwiseXor(a, b);
        }

        /// <summary>
        /// Bitwise XOR operation. Both operands must be convertible to integers.
        /// </summary>
        public static LuaNumber BitwiseXor(LuaNumber a, LuaNumber b)
        {
            long ai = a.RequireInteger("bxor");
            long bi = b.RequireInteger("bxor");
            return FromInteger(ai ^ bi);
        }

        /// <summary>
        /// Bitwise NOT operation. Operand must be convertible to integer.
        /// Alternate method name for ~ operator (CA2225).
        /// </summary>
        public static LuaNumber OnesComplement(LuaNumber a)
        {
            return BitwiseNot(a);
        }

        /// <summary>
        /// Bitwise NOT operation. Operand must be convertible to integer.
        /// </summary>
        public static LuaNumber BitwiseNot(LuaNumber a)
        {
            long ai = a.RequireInteger("bnot");
            return FromInteger(~ai);
        }

        /// <summary>
        /// Left shift operation. Both operands must be convertible to integers.
        /// Alternate method name for &lt;&lt; operator (CA2225).
        /// </summary>
        public static LuaNumber LeftShift(LuaNumber a, LuaNumber b)
        {
            return ShiftLeft(a, b);
        }

        /// <summary>
        /// Left shift operation. Both operands must be convertible to integers.
        /// </summary>
        public static LuaNumber ShiftLeft(LuaNumber a, LuaNumber b)
        {
            long ai = a.RequireInteger("shl");
            long bi = b.RequireInteger("shl");
            return FromInteger(Execution.LuaIntegerHelper.ShiftLeft(ai, bi));
        }

        /// <summary>
        /// Right shift operation (logical, not arithmetic). Both operands must be convertible to integers.
        /// Alternate method name for &gt;&gt; operator (CA2225).
        /// </summary>
        public static LuaNumber RightShift(LuaNumber a, LuaNumber b)
        {
            return ShiftRight(a, b);
        }

        /// <summary>
        /// Right shift operation (logical, not arithmetic). Both operands must be convertible to integers.
        /// </summary>
        public static LuaNumber ShiftRight(LuaNumber a, LuaNumber b)
        {
            long ai = a.RequireInteger("shr");
            long bi = b.RequireInteger("shr");
            return FromInteger(Execution.LuaIntegerHelper.ShiftRight(ai, bi));
        }

        private long RequireInteger(string operation)
        {
            if (IsInteger)
            {
                return _integer;
            }

            // Try to convert float to integer
            if (TryConvertToInteger(_float, out long intValue))
            {
                return intValue;
            }

            throw new Errors.ScriptRuntimeException(
                $"number has no integer representation (in '{operation}')"
            );
        }

        /// <summary>
        /// Compares two LuaNumbers for equality following Lua semantics.
        /// An integer and float are equal if they represent the same mathematical value.
        /// </summary>
        public static bool Equal(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                return a._integer == b._integer;
            }
            if (a.IsFloat && b.IsFloat)
            {
                return a._float == b._float;
            }
            // Mixed comparison: integer vs float
            return a.ToDouble == b.ToDouble;
        }

        /// <summary>
        /// Compares two LuaNumbers for less-than following Lua semantics.
        /// </summary>
        public static bool LessThan(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                return a._integer < b._integer;
            }
            return a.ToDouble < b.ToDouble;
        }

        /// <summary>
        /// Compares two LuaNumbers for less-than-or-equal following Lua semantics.
        /// </summary>
        public static bool LessThanOrEqual(LuaNumber a, LuaNumber b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                return a._integer <= b._integer;
            }
            return a.ToDouble <= b.ToDouble;
        }

        /// <summary>
        /// Attempts to convert this number to an integer.
        /// </summary>
        /// <param name="value">When successful, contains the integer value.</param>
        /// <returns>true if conversion succeeded (integer type or float that is a whole number in range).</returns>
        public bool TryToInteger(out long value)
        {
            if (IsInteger)
            {
                value = _integer;
                return true;
            }
            return TryConvertToInteger(_float, out value);
        }

        /// <summary>
        /// Converts this LuaNumber to a double.
        /// Alternate for explicit conversion operator (CA2225).
        /// </summary>
        /// <returns>The value as a double.</returns>
        public double ToDoubleValue()
        {
            return ToDouble;
        }

        /// <summary>
        /// Converts this LuaNumber to a 64-bit integer.
        /// Alternate for explicit conversion operator (CA2225).
        /// </summary>
        /// <returns>The value as a long (truncated for floats).</returns>
        public long ToInt64()
        {
            return AsInteger;
        }

        /// <summary>
        /// Converts this number to its string representation following Lua formatting rules.
        /// Integers format without decimal point, floats with appropriate precision.
        /// </summary>
        public override string ToString()
        {
            if (IsInteger)
            {
                return _integer.ToString(CultureInfo.InvariantCulture);
            }

            // Float formatting per Lua conventions
            if (double.IsNaN(_float))
            {
                return "nan";
            }
            if (double.IsPositiveInfinity(_float))
            {
                return "inf";
            }
            if (double.IsNegativeInfinity(_float))
            {
                return "-inf";
            }

            // For integer-like floats, Lua 5.3+ adds ".0" suffix
            if (_float == Math.Floor(_float) && !double.IsInfinity(_float))
            {
                return _float.ToString("0.0", CultureInfo.InvariantCulture);
            }

            return _float.ToString("G17", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public bool Equals(LuaNumber other)
        {
            return Equal(this, other);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is LuaNumber other && Equals(other);
        }

        /// <inheritdoc />
        [SuppressMessage(
            "Design",
            "CA1065:Do not raise exceptions in unexpected locations",
            Justification = "NaN comparison follows IEEE 754 and Lua semantics where CompareTo with NaN should indicate unordered."
        )]
        public int CompareTo(LuaNumber other)
        {
            // Handle NaN - Lua comparisons with NaN always return false
            if (IsFloat && double.IsNaN(_float))
            {
                return other.IsFloat && double.IsNaN(other._float) ? 0 : -1;
            }
            if (other.IsFloat && double.IsNaN(other._float))
            {
                return 1;
            }

            if (LessThan(this, other))
            {
                return -1;
            }
            if (Equal(this, other))
            {
                return 0;
            }
            return 1;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            DeterministicHashBuilder hash = default;

            if (IsInteger)
            {
                hash.AddLong(_integer);
                return hash.ToHashCode();
            }

            // For floats that represent integers, use the same hash as the integer
            // This ensures Equal values have equal hash codes
            if (TryConvertToInteger(_float, out long intValue))
            {
                hash.AddLong(intValue);
                return hash.ToHashCode();
            }

            hash.AddDouble(_float);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Implicit conversion from long to LuaNumber (creates integer).
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromInt64 and FromInteger are the named alternates."
        )]
        public static implicit operator LuaNumber(long value)
        {
            return FromInteger(value);
        }

        /// <summary>
        /// Implicit conversion from int to LuaNumber (creates integer).
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromInt32 is the named alternate."
        )]
        public static implicit operator LuaNumber(int value)
        {
            return FromInteger(value);
        }

        /// <summary>
        /// Implicit conversion from double to LuaNumber (creates float, unless it's an integer-like value).
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "FromDouble and FromFloat are the named alternates."
        )]
        public static implicit operator LuaNumber(double value)
        {
            return FromDouble(value);
        }

        /// <summary>
        /// Explicit conversion to double (may lose precision for large integers).
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "ToDoubleValue is the named alternate."
        )]
        public static explicit operator double(LuaNumber value)
        {
            return value.ToDouble;
        }

        /// <summary>
        /// Explicit conversion to long (truncates floats).
        /// </summary>
        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "ToInt64 is the named alternate."
        )]
        public static explicit operator long(LuaNumber value)
        {
            return value.AsInteger;
        }

        public static LuaNumber operator +(LuaNumber a, LuaNumber b)
        {
            return Add(a, b);
        }

        public static LuaNumber operator -(LuaNumber a, LuaNumber b)
        {
            return Subtract(a, b);
        }

        public static LuaNumber operator *(LuaNumber a, LuaNumber b)
        {
            return Multiply(a, b);
        }

        public static LuaNumber operator /(LuaNumber a, LuaNumber b)
        {
            return Divide(a, b);
        }

        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "Remainder and Modulo are the named alternates."
        )]
        public static LuaNumber operator %(LuaNumber a, LuaNumber b)
        {
            return Modulo(a, b);
        }

        public static LuaNumber operator -(LuaNumber a)
        {
            return Negate(a);
        }

        public static LuaNumber operator &(LuaNumber a, LuaNumber b)
        {
            return BitwiseAnd(a, b);
        }

        public static LuaNumber operator |(LuaNumber a, LuaNumber b)
        {
            return BitwiseOr(a, b);
        }

        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "Xor and BitwiseXor are the named alternates."
        )]
        public static LuaNumber operator ^(LuaNumber a, LuaNumber b)
        {
            return BitwiseXor(a, b);
        }

        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "OnesComplement and BitwiseNot are the named alternates."
        )]
        public static LuaNumber operator ~(LuaNumber a)
        {
            return BitwiseNot(a);
        }

        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "LeftShift and ShiftLeft are the named alternates."
        )]
        public static LuaNumber operator <<(LuaNumber a, int b)
        {
            return ShiftLeft(a, FromInteger(b));
        }

        [SuppressMessage(
            "Usage",
            "CA2225:Operator overloads have named alternates",
            Justification = "RightShift and ShiftRight are the named alternates."
        )]
        public static LuaNumber operator >>(LuaNumber a, int b)
        {
            return ShiftRight(a, FromInteger(b));
        }

        public static bool operator ==(LuaNumber a, LuaNumber b)
        {
            return Equal(a, b);
        }

        public static bool operator !=(LuaNumber a, LuaNumber b)
        {
            return !Equal(a, b);
        }

        public static bool operator <(LuaNumber a, LuaNumber b)
        {
            return LessThan(a, b);
        }

        public static bool operator <=(LuaNumber a, LuaNumber b)
        {
            return LessThanOrEqual(a, b);
        }

        public static bool operator >(LuaNumber a, LuaNumber b)
        {
            return LessThan(b, a);
        }

        public static bool operator >=(LuaNumber a, LuaNumber b)
        {
            return LessThanOrEqual(b, a);
        }
    }
}
