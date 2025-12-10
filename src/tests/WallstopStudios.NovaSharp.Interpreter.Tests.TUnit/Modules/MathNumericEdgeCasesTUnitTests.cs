namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Tests for numeric edge cases per Lua specification (§6.7, §3.4.1).
    /// Verifies math.maxinteger, math.mininteger, division-by-zero, and overflow behavior.
    /// </summary>
    /// <remarks>
    /// Some tests document known NovaSharp divergences from reference Lua.
    /// See docs/testing/lua-divergences.md for the full divergence catalog.
    /// </remarks>
    public sealed class MathNumericEdgeCasesTUnitTests
    {
        #region math.maxinteger / math.mininteger Tests (Lua 5.3+)

        [Test]
        public async Task MaxintegerMatchesLua54Value()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger");

            // Lua 5.3/5.4 maxinteger = 2^63 - 1 = 9223372036854775807
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerMatchesLua54Value()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.mininteger");

            // Lua 5.3/5.4 mininteger = -2^63 = -9223372036854775808
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerIsInteger()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.type(math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerIsInteger()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.type(math.mininteger)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerNotAvailableInLua52()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString("return math.maxinteger");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerNotAvailableInLua52()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString("return math.mininteger");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerAvailableInLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);
            DynValue result = script.DoString("return math.maxinteger");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerAvailableInLua53()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua53);
            DynValue result = script.DoString("return math.mininteger");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerPlusOneWrapsToMininteger()
        {
            // Per Lua 5.3/5.4 spec: integer overflow wraps in two's complement
            // maxinteger + 1 = mininteger
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger + 1");

            // Should wrap to mininteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerMinusOneWrapsToMaxinteger()
        {
            // Per Lua 5.3/5.4 spec: integer underflow wraps in two's complement
            // mininteger - 1 = maxinteger
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.mininteger - 1");

            // Should wrap to maxinteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerBitwiseOrReturnsSameValue()
        {
            // Now that LuaNumber stores integers natively, math.maxinteger is preserved exactly
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger | 0");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerBitwiseOrWorksCorrectly()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.mininteger | 0");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        #endregion

        #region Division By Zero Tests

        [Test]
        public async Task FloatDivisionByZeroReturnsPositiveInfinity()
        {
            // Per Lua spec: x/0 where x > 0 produces +inf
            Script script = CreateScript();
            DynValue result = script.DoString("return 1.0 / 0");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task FloatDivisionByNegativeZeroReturnsNegativeInfinity()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return 1.0 / -0.0");

            await Assert
                .That(double.IsNegativeInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task NegativeFloatDivisionByZeroReturnsNegativeInfinity()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return -1.0 / 0");

            await Assert
                .That(double.IsNegativeInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ZeroDividedByZeroReturnsNaN()
        {
            // Per IEEE 754 and Lua: 0/0 = NaN
            Script script = CreateScript();
            DynValue result = script.DoString("return 0 / 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IntegerDivisionByZeroThrowsError()
        {
            // Per Lua 5.3+ spec: integer floor division by zero throws "attempt to divide by zero"
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            await Assert
                .That(() => script.DoString("return 5 // 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task FloatIntegerDivisionByZeroReturnsInfinity()
        {
            // When operands are floats, // follows IEEE 754 (returns inf)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return 5.0 // 0.0");

            await Assert.That(double.IsInfinity(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IntegerModuloByZeroThrowsError()
        {
            // Per Lua 5.3+ spec: integer modulo by zero throws "attempt to perform 'n%0'"
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            await Assert
                .That(() => script.DoString("return 5 % 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task IntegerModuloByZeroReturnsNaNInLua51And52(LuaCompatibilityVersion version)
        {
            // Per Lua 5.1/5.2 behavior: integer modulo by zero returns nan (promotes to float)
            // Verified with: lua5.1 -e "print(1 % 0)"  -> -nan
            //                lua5.2 -e "print(1 % 0)"  -> -nan
            Script script = CreateScript(version);
            DynValue result = script.DoString("return 5 % 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task IntegerModuloByZeroThrowsErrorInLua53Plus(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer modulo by zero throws "attempt to perform 'n%0'"
            // Verified with: lua5.3 -e "print(1 % 0)"  -> error
            //                lua5.4 -e "print(1 % 0)"  -> error
            Script script = CreateScript(version);

            await Assert
                .That(() => script.DoString("return 5 % 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task FloatModuloByZeroReturnsNaN()
        {
            // Per Lua 5.3+ spec: float modulo by zero returns NaN (IEEE 754)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return 5.0 % 0.0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        [Arguments(LuaCompatibilityVersion.Latest)]
        public async Task FloatModuloByZeroReturnsNaNAllVersions(LuaCompatibilityVersion version)
        {
            // Float modulo by zero returns NaN in all Lua versions (IEEE 754)
            Script script = CreateScript(version);
            DynValue result = script.DoString("return 5.0 % 0.0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        #endregion

        #region Infinity and NaN Arithmetic

        [Test]
        public async Task InfinityPlusInfinityIsInfinity()
        {
            // Use 1/0 for true infinity since math.huge is double.MaxValue in NovaSharp
            Script script = CreateScript();
            DynValue result = script.DoString("local inf = 1/0; return inf + inf");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task InfinityMinusInfinityIsNaN()
        {
            // Use 1/0 for true infinity since math.huge is double.MaxValue in NovaSharp
            Script script = CreateScript();
            DynValue result = script.DoString("local inf = 1/0; return inf - inf");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task InfinityTimesZeroIsNaN()
        {
            // Use 1/0 for true infinity since math.huge is double.MaxValue in NovaSharp
            Script script = CreateScript();
            DynValue result = script.DoString("local inf = 1/0; return inf * 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task NaNNotEqualToItself()
        {
            // Per IEEE 754: NaN ~= NaN
            Script script = CreateScript();
            DynValue result = script.DoString("local nan = 0/0; return nan == nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task NaNNotLessThanItself()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("local nan = 0/0; return nan < nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task NaNNotGreaterThanItself()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("local nan = 0/0; return nan > nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        [Test]
        public async Task MathTypeReturnsFloatForNaN()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.type(0/0)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        [Test]
        public async Task MathTypeReturnsFloatForInfinity()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.type(math.huge)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        #endregion

        #region Integer Overflow Wrapping (Bitwise Operations)

        [Test]
        public async Task BitwiseNegationOfMinintegerWrapsToMininteger()
        {
            // Per Lua spec: ~mininteger = mininteger (two's complement wraparound)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return ~math.mininteger");

            // In two's complement: ~(-2^63) = 2^63 - 1 = maxinteger
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerNegationWrapsToMininteger()
        {
            // Per Lua 5.3/5.4 spec: -mininteger wraps to mininteger (two's complement)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return -math.mininteger");

            // Should wrap to mininteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerTimesTwoWraps()
        {
            // Per Lua 5.3/5.4 spec: integer multiplication wraps in two's complement
            // maxinteger * 2 = -2 (0x7FFFFFFFFFFFFFFF * 2 = -2 in two's complement)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger * 2");

            await Assert.That(result.Number).IsEqualTo(-2d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IntegerLeftShiftOverflow()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return 1 << 63");

            // 1 << 63 = -2^63 = mininteger (wraps in integer representation)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        [Test]
        public async Task IntegerLeftShiftBy64ReturnsZero()
        {
            // Shift by >= 64 bits returns 0
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return 1 << 64");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [Test]
        public async Task IntegerRightShiftBy64ReturnsZero()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger >> 64");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        #endregion

        #region Large Number Handling

        [Test]
        public async Task VeryLargeNumberIsFloat()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.type(1e100)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        [Test]
        public async Task LargeLiteralParsedCorrectly()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return 9223372036854775807");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        [Test]
        public async Task NegativeLargeLiteralParsedCorrectly()
        {
            Script script = CreateScript();
            // Note: Parser treats this as unary minus applied to positive literal
            DynValue result = script.DoString("return -9223372036854775808");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        [Test]
        public async Task MaxintegerEqualToLiteral()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.maxinteger == 9223372036854775807");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task MinintegerEqualToLiteral()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.mininteger == -9223372036854775808");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        #endregion

        #region math.tointeger Edge Cases

        [Test]
        public async Task TointegerOfMaxintegerReturnsMaxinteger()
        {
            // Now that LuaNumber stores integers natively, math.maxinteger is preserved exactly
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.tointeger(math.maxinteger)");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task TointegerOfMinintegerReturnsMininteger()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.tointeger(math.mininteger)");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        [Test]
        public async Task TointegerOfInfinityReturnsNil()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.tointeger(math.huge)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task TointegerOfNaNReturnsNil()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.tointeger(0/0)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task TointegerOfOverflowValueReturnsNil()
        {
            // Per Lua 5.3/5.4 spec: math.tointeger returns nil for values outside integer range.
            // 2^63 is exactly one beyond maxinteger and should return nil.
            // Note: Due to IEEE 754 precision, 2^63 as a double equals (double)long.MaxValue,
            // but we explicitly check for this boundary case.
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.tointeger(2^63)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        #endregion

        #region math.ult Edge Cases

        [Test]
        public async Task UltWithMaxintegerAndMinintegerBehavior()
        {
            // math.maxinteger and math.mininteger are now stored as native integers in LuaNumber,
            // so precision is preserved. The unsigned comparison should work correctly:
            // maxinteger (0x7FFFFFFFFFFFFFFF) < mininteger (0x8000000000000000) as unsigned = true
            // because mininteger's unsigned value is larger.
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.ult(math.maxinteger, math.mininteger)");

            // With native integer storage, this should now return true (correct Lua behavior)
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task UltWithZeroAndMinusOne()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.ult(0, -1)");

            // 0 < -1 (as unsigned, -1 = max unsigned value)
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task UltWithMinusOneAndZero()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.ult(-1, 0)");

            // -1 (max unsigned) is not < 0
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        #endregion

        #region Integer Representation Boundary Tests (RequireIntegerRepresentation edge cases)

        /// <summary>
        /// Tests that string.format %d rejects values at/beyond the 2^63 boundary.
        /// This tests the fix for platform-dependent behavior where ARM64 vs x64 handle
        /// the double-to-long conversion of 2^63 differently (saturation vs wrapping).
        /// </summary>
        /// <remarks>
        /// The key insight is that (double)long.MaxValue == 9223372036854775808.0 (which is 2^63),
        /// so comparing against (double)long.MaxValue doesn't correctly exclude values >= 2^63.
        /// Reference: Lua 5.4 Manual §3.4.1 - Integers and floats are distinguishable subtypes.
        /// </remarks>
        [Test]
        [Arguments("math.maxinteger + 0.5", "maxinteger plus fractional - rounds to 2^63")]
        [Arguments("9223372036854775808.0", "exactly 2^63 as float literal")]
        [Arguments("2^63", "2^63 computed - first value outside signed long range")]
        [Arguments("2^63 + 1024", "beyond 2^63")]
        [Arguments("-9223372036854777856.0", "next representable double below -2^63")]
        [Arguments("-1e100", "large negative float (already tested separately)")]
        public async Task FormatDecimalRejectsBoundaryValues(
            string luaExpression,
            string description
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.format('%d', {luaExpression})")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because($"Expected exception for {description}")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .Because($"Error should indicate no integer representation for {description}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that string.format %d accepts values within the valid integer range.
        /// These are boundary cases that SHOULD work (max representable values).
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", "9223372036854775807", "maxinteger (2^63-1)")]
        [Arguments("math.mininteger", "-9223372036854775808", "mininteger (-2^63)")]
        [Arguments("0", "0", "zero")]
        [Arguments("-0.0", "0", "negative zero")]
        [Arguments("1.0", "1", "float 1.0 (exact)")]
        [Arguments("-1.0", "-1", "float -1.0 (exact)")]
        [Arguments("9007199254740992.0", "9007199254740992", "2^53 (max safe integer for doubles)")]
        [Arguments("-9007199254740992.0", "-9007199254740992", "-2^53")]
        public async Task FormatDecimalAcceptsValidBoundaryValues(
            string luaExpression,
            string expectedOutput,
            string description
        )
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString($"return string.format('%d', {luaExpression})");

            await Assert
                .That(result.String)
                .IsEqualTo(expectedOutput)
                .Because(
                    $"string.format('%d', {luaExpression}) [{description}] should produce {expectedOutput}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that the boundary value math.maxinteger + 0.5 evaluates to 2^63 (due to rounding)
        /// which is outside the valid signed integer range.
        /// </summary>
        [Test]
        public async Task MaxintegerPlusHalfRoundsToTwoPow63()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // math.maxinteger + 0.5 should round to 9223372036854775808.0 (2^63)
            // due to double precision limitations
            DynValue result = script.DoString(
                @"
                local v = math.maxinteger + 0.5
                return v, math.type(v), v == 2^63
            "
            );

            // It should be a float type (mixed int + float produces float)
            await Assert
                .That(result.Tuple[1].String)
                .IsEqualTo("float")
                .Because("integer + float should produce float")
                .ConfigureAwait(false);

            // The result should equal 2^63
            await Assert
                .That(result.Tuple[2].Boolean)
                .IsTrue()
                .Because("math.maxinteger + 0.5 should round to 2^63 due to double precision")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that all integer format specifiers (%d, %i, %o, %u, %x, %X) reject
        /// the same boundary values consistently.
        /// </summary>
        [Test]
        [Arguments("%d")]
        [Arguments("%i")]
        [Arguments("%o")]
        [Arguments("%u")]
        [Arguments("%x")]
        [Arguments("%X")]
        public async Task AllIntegerSpecifiersRejectTwoPow63(string specifier)
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString($"return string.format('{specifier}', 2^63)")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because($"Expected exception for specifier {specifier} with 2^63")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that the range check uses the correct boundary constants.
        /// 2^63 (9223372036854775808) should be rejected, while 2^63-1024 should be accepted
        /// (even though it loses precision when stored as a float).
        /// </summary>
        [Test]
        public async Task RangeCheckUsesCorrectBoundaryConstants()
        {
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);

            // This value is within range but at the edge where double precision is limited
            // The largest double strictly less than 2^63 is 9223372036854774784
            DynValue result = script.DoString(
                @"
                local just_under = 9223372036854774784.0  -- largest double < 2^63
                local at_boundary = 9223372036854775808.0  -- exactly 2^63
                
                local under_ok = pcall(function() string.format('%d', just_under) end)
                local boundary_fail = not pcall(function() string.format('%d', at_boundary) end)
                
                return under_ok, boundary_fail
            "
            );

            await Assert
                .That(result.Tuple[0].Boolean)
                .IsTrue()
                .Because("Value just under 2^63 should be accepted")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Boolean)
                .IsTrue()
                .Because("Value at exactly 2^63 should be rejected")
                .ConfigureAwait(false);
        }

        #endregion

        #region Helpers

        private static Script CreateScript(
            LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua54
        )
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModules.PresetComplete, options);
        }

        #endregion
    }
}
