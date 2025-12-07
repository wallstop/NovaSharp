namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
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
        public async Task FloatModuloByZeroReturnsNaN()
        {
            // Per Lua 5.3+ spec: float modulo by zero returns NaN (IEEE 754)
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
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
        public async Task TointegerOfOverflowValueReturnsMininteger()
        {
            // KNOWN DIVERGENCE: 2^63 overflows when cast to long, wrapping to mininteger.
            // Lua returns nil because it can track that the value is out of integer range.
            // NovaSharp cannot distinguish this since all numbers are doubles.
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            // 2^63 is one beyond maxinteger
            DynValue result = script.DoString("return math.tointeger(2^63)");

            // NovaSharp returns mininteger (overflow wraps) instead of nil
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        #endregion

        #region math.ult Edge Cases

        [Test]
        public async Task UltWithMaxintegerAndMinintegerDivergence()
        {
            // KNOWN DIVERGENCE: Due to double precision loss with math.maxinteger,
            // both operands end up as mininteger after conversion to long, so the
            // unsigned comparison returns false (equal) instead of true (less than).
            Script script = CreateScript(LuaCompatibilityVersion.Lua54);
            DynValue result = script.DoString("return math.ult(math.maxinteger, math.mininteger)");

            // NovaSharp returns false due to both being converted to mininteger
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
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
