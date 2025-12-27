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
        /// <summary>
        /// Tests math.maxinteger value in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerMatchesExpectedValue(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger");

            // Lua 5.3+ maxinteger = 2^63 - 1 = 9223372036854775807
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.mininteger value in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerMatchesExpectedValue(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mininteger");

            // Lua 5.3+ mininteger = -2^63 = -9223372036854775808
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.type returns "integer" for math.maxinteger in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerIsInteger(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.maxinteger)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.type returns "integer" for math.mininteger in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerIsInteger(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.mininteger)");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.maxinteger is nil in Lua 5.1 and 5.2 (not available until 5.3).
        /// </summary>
        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task MaxintegerNotAvailableInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.mininteger is nil in Lua 5.1 and 5.2 (not available until 5.3).
        /// </summary>
        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task MinintegerNotAvailableInPreLua53(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mininteger");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests integer overflow wraps in two's complement (maxinteger + 1 = mininteger).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerPlusOneWrapsToMininteger(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer overflow wraps in two's complement
            // maxinteger + 1 = mininteger
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger + 1");

            // Should wrap to mininteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests integer underflow wraps in two's complement (mininteger - 1 = maxinteger).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerMinusOneWrapsToMaxinteger(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer underflow wraps in two's complement
            // mininteger - 1 = maxinteger
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mininteger - 1");

            // Should wrap to maxinteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests bitwise OR with maxinteger preserves the value (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerBitwiseOrReturnsSameValue(LuaCompatibilityVersion version)
        {
            // Now that LuaNumber stores integers natively, math.maxinteger is preserved exactly
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger | 0");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests bitwise OR with mininteger works correctly (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerBitwiseOrWorksCorrectly(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mininteger | 0");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests float division by zero returns positive infinity (IEEE 754 behavior).
        /// This behavior is consistent across all Lua versions.
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task FloatDivisionByZeroReturnsPositiveInfinity(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua spec: x/0 where x > 0 produces +inf
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 1.0 / 0");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests float division by negative zero returns negative infinity.
        /// This behavior is consistent across all Lua versions.
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task FloatDivisionByNegativeZeroReturnsNegativeInfinity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 1.0 / -0.0");

            await Assert
                .That(double.IsNegativeInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests negative float division by zero returns negative infinity.
        /// This behavior is consistent across all Lua versions.
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task NegativeFloatDivisionByZeroReturnsNegativeInfinity(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return -1.0 / 0");

            await Assert
                .That(double.IsNegativeInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests zero divided by zero returns NaN (IEEE 754 behavior).
        /// This behavior is consistent across all Lua versions.
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task ZeroDividedByZeroReturnsNaN(LuaCompatibilityVersion version)
        {
            // Per IEEE 754 and Lua: 0/0 = NaN
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 0 / 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests integer division by zero throws error in Lua 5.3+.
        /// The // operator was introduced in Lua 5.3.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerDivisionByZeroThrowsError(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer floor division by zero throws "attempt to divide by zero"
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(() => script.DoString("return 5 // 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests float integer division by zero returns infinity (IEEE 754).
        /// The // operator was introduced in Lua 5.3.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloatIntegerDivisionByZeroReturnsInfinity(LuaCompatibilityVersion version)
        {
            // When operands are floats, // follows IEEE 754 (returns inf)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 5.0 // 0.0");

            await Assert.That(double.IsInfinity(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests integer modulo by zero throws error in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerModuloByZeroThrowsError(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer modulo by zero throws "attempt to perform 'n%0'"
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(() => script.DoString("return 5 % 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task IntegerModuloByZeroReturnsNaNInLua51And52(LuaCompatibilityVersion version)
        {
            // Per Lua 5.1/5.2 behavior: integer modulo by zero returns nan (promotes to float)
            // Verified with: lua5.1 -e "print(1 % 0)"  -> -nan
            //                lua5.2 -e "print(1 % 0)"  -> -nan
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 5 % 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerModuloByZeroThrowsErrorInLua53Plus(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer modulo by zero throws "attempt to perform 'n%0'"
            // Verified with: lua5.3 -e "print(1 % 0)"  -> error
            //                lua5.4 -e "print(1 % 0)"  -> error
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(() => script.DoString("return 5 % 0"))
                .Throws<ScriptRuntimeException>()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests float modulo by zero returns NaN in all Lua versions (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task FloatModuloByZeroReturnsNaN(LuaCompatibilityVersion version)
        {
            // Float modulo by zero returns NaN in all Lua versions (IEEE 754)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 5.0 % 0.0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests infinity + infinity = infinity (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task InfinityPlusInfinityIsInfinity(LuaCompatibilityVersion version)
        {
            // math.huge is IEEE 754 positive infinity per Lua spec
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.huge + math.huge");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests infinity - infinity = NaN (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task InfinityMinusInfinityIsNaN(LuaCompatibilityVersion version)
        {
            // math.huge is IEEE 754 positive infinity per Lua spec
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.huge - math.huge");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests infinity * 0 = NaN (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task InfinityTimesZeroIsNaN(LuaCompatibilityVersion version)
        {
            // math.huge is IEEE 754 positive infinity per Lua spec
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.huge * 0");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests NaN != NaN (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task NaNNotEqualToItself(LuaCompatibilityVersion version)
        {
            // Per IEEE 754: NaN ~= NaN
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("local nan = 0/0; return nan == nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests NaN is not less than itself (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task NaNNotLessThanItself(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("local nan = 0/0; return nan < nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests NaN is not greater than itself (IEEE 754 behavior).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task NaNNotGreaterThanItself(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("local nan = 0/0; return nan > nan");

            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.type returns "float" for NaN in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathTypeReturnsFloatForNaN(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(0/0)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.type returns "float" for infinity in Lua 5.3+.
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MathTypeReturnsFloatForInfinity(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.huge)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests bitwise negation of mininteger returns maxinteger (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task BitwiseNegationOfMinintegerWrapsToMaxinteger(
            LuaCompatibilityVersion version
        )
        {
            // Per Lua spec: ~mininteger = maxinteger (two's complement)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return ~math.mininteger");

            // In two's complement: ~(-2^63) = 2^63 - 1 = maxinteger
            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests negation of mininteger wraps to mininteger (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerNegationWrapsToMininteger(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: -mininteger wraps to mininteger (two's complement)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return -math.mininteger");

            // Should wrap to mininteger (stays integer type)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests maxinteger * 2 wraps in two's complement (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerTimesTwoWraps(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: integer multiplication wraps in two's complement
            // maxinteger * 2 = -2 (0x7FFFFFFFFFFFFFFF * 2 = -2 in two's complement)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger * 2");

            await Assert.That(result.Number).IsEqualTo(-2d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests 1 &lt;&lt; 63 produces mininteger (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerLeftShiftOverflow(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 1 << 63");

            // 1 << 63 = -2^63 = mininteger (wraps in integer representation)
            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests shift by 64+ bits returns zero (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerLeftShiftBy64ReturnsZero(LuaCompatibilityVersion version)
        {
            // Shift by >= 64 bits returns 0
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 1 << 64");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests right shift by 64+ bits returns zero (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task IntegerRightShiftBy64ReturnsZero(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger >> 64");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.type returns "float" for very large numbers (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task VeryLargeNumberIsFloat(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(1e100)");

            await Assert.That(result.String).IsEqualTo("float").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests large integer literal is parsed correctly (all versions).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task LargeLiteralParsedCorrectly(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return 9223372036854775807");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests negative large integer literal is parsed correctly (all versions).
        /// </summary>
        [Test]
        [AllLuaVersions]
        public async Task NegativeLargeLiteralParsedCorrectly(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            // Note: Parser treats this as unary minus applied to positive literal
            DynValue result = script.DoString("return -9223372036854775808");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.maxinteger equals 9223372036854775807 (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MaxintegerEqualToLiteral(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.maxinteger == 9223372036854775807");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.mininteger equals -9223372036854775808 (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task MinintegerEqualToLiteral(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mininteger == -9223372036854775808");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.tointeger returns maxinteger for maxinteger (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TointegerOfMaxintegerReturnsMaxinteger(LuaCompatibilityVersion version)
        {
            // Now that LuaNumber stores integers natively, math.maxinteger is preserved exactly
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(math.maxinteger)");

            await Assert.That(result.Number).IsEqualTo(9223372036854775807d).ConfigureAwait(false);
            await Assert.That(result.LuaNumber.IsInteger).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.tointeger returns mininteger for mininteger (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TointegerOfMinintegerReturnsMininteger(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(math.mininteger)");

            await Assert.That(result.Number).IsEqualTo(-9223372036854775808d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.tointeger returns nil for infinity (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TointegerOfInfinityReturnsNil(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(math.huge)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.tointeger returns nil for NaN (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TointegerOfNaNReturnsNil(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(0/0)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.tointeger returns nil for overflow values (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TointegerOfOverflowValueReturnsNil(LuaCompatibilityVersion version)
        {
            // Per Lua 5.3+ spec: math.tointeger returns nil for values outside integer range.
            // 2^63 is exactly one beyond maxinteger and should return nil.
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(2^63)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.ult compares maxinteger and mininteger correctly (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UltWithMaxintegerAndMinintegerBehavior(LuaCompatibilityVersion version)
        {
            // math.maxinteger (0x7FFFFFFFFFFFFFFF) < math.mininteger (0x8000000000000000) as unsigned = true
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ult(math.maxinteger, math.mininteger)");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.ult(0, -1) returns true (0 &lt; max unsigned) (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UltWithZeroAndMinusOne(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ult(0, -1)");

            // 0 < -1 (as unsigned, -1 = max unsigned value)
            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests math.ult(-1, 0) returns false (max unsigned is not &lt; 0) (Lua 5.3+).
        /// </summary>
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UltWithMinusOneAndZero(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ult(-1, 0)");

            // -1 (max unsigned) is not < 0
            await Assert.That(result.Boolean).IsFalse().ConfigureAwait(false);
        }

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
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger + 0.5",
            "maxinteger plus fractional - rounds to 2^63"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger + 0.5",
            "maxinteger plus fractional - rounds to 2^63"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger + 0.5",
            "maxinteger plus fractional - rounds to 2^63"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "9223372036854775808.0",
            "exactly 2^63 as float literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "9223372036854775808.0",
            "exactly 2^63 as float literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "9223372036854775808.0",
            "exactly 2^63 as float literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "2^63",
            "2^63 computed - first value outside signed long range"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "2^63",
            "2^63 computed - first value outside signed long range"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "2^63",
            "2^63 computed - first value outside signed long range"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "2^63 + 1024", "beyond 2^63")]
        [Arguments(LuaCompatibilityVersion.Lua54, "2^63 + 1024", "beyond 2^63")]
        [Arguments(LuaCompatibilityVersion.Lua55, "2^63 + 1024", "beyond 2^63")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-9223372036854777856.0",
            "next representable double below -2^63"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-9223372036854777856.0",
            "next representable double below -2^63"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-9223372036854777856.0",
            "next representable double below -2^63"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1e100", "large negative float")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1e100", "large negative float")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1e100", "large negative float")]
        public async Task FormatDecimalRejectsBoundaryValues(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            "9223372036854775807",
            "maxinteger (2^63-1)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            "9223372036854775807",
            "maxinteger (2^63-1)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            "9223372036854775807",
            "maxinteger (2^63-1)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger",
            "-9223372036854775808",
            "mininteger (-2^63)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger",
            "-9223372036854775808",
            "mininteger (-2^63)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger",
            "-9223372036854775808",
            "mininteger (-2^63)"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0", "0", "zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "0", "0", "zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "0", "0", "zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-0.0", "0", "negative zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-0.0", "0", "negative zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-0.0", "0", "negative zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1.0", "1", "float 1.0 (exact)")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.0", "1", "float 1.0 (exact)")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.0", "1", "float 1.0 (exact)")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.0", "-1", "float -1.0 (exact)")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.0", "-1", "float -1.0 (exact)")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.0", "-1", "float -1.0 (exact)")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "9007199254740992.0",
            "9007199254740992",
            "2^53 (max safe integer for doubles)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "9007199254740992.0",
            "9007199254740992",
            "2^53 (max safe integer for doubles)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "9007199254740992.0",
            "9007199254740992",
            "2^53 (max safe integer for doubles)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-9007199254740992.0",
            "-9007199254740992",
            "-2^53"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-9007199254740992.0",
            "-9007199254740992",
            "-2^53"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-9007199254740992.0",
            "-9007199254740992",
            "-2^53"
        )]
        public async Task FormatDecimalAcceptsValidBoundaryValues(
            LuaCompatibilityVersion version,
            string luaExpression,
            string expectedOutput,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MaxintegerPlusHalfRoundsToTwoPow63(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [Arguments(LuaCompatibilityVersion.Lua53, "%d")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%d")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%d")]
        [Arguments(LuaCompatibilityVersion.Lua53, "%i")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%i")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%i")]
        [Arguments(LuaCompatibilityVersion.Lua53, "%o")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%o")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%o")]
        [Arguments(LuaCompatibilityVersion.Lua53, "%u")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%u")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%u")]
        [Arguments(LuaCompatibilityVersion.Lua53, "%x")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%x")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%x")]
        [Arguments(LuaCompatibilityVersion.Lua53, "%X")]
        [Arguments(LuaCompatibilityVersion.Lua54, "%X")]
        [Arguments(LuaCompatibilityVersion.Lua55, "%X")]
        public async Task AllIntegerSpecifiersRejectTwoPow63(
            LuaCompatibilityVersion version,
            string specifier
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RangeCheckUsesCorrectBoundaryConstants(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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

        /// <summary>
        /// Tests that math.floor returns a float (not integer) when the result is >= 2^63.
        /// This is a regression test for the fix where math.floor incorrectly promoted
        /// overflow values to integer type, causing wrapping in string.format('%d', ...).
        /// </summary>
        /// <remarks>
        /// The bug occurred because (double)long.MaxValue rounds up to 2^63 due to IEEE 754
        /// precision loss, so a naive comparison "result <= long.MaxValue" incorrectly passes
        /// for values like 2^63. The fix uses LuaIntegerHelper.TryGetInteger which has proper
        /// boundary checking.
        /// Reference: Lua 5.4 Manual §6.7 - math.floor returns integer when result fits.
        /// </remarks>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FloorReturnsFloatWhenResultExceedsIntegerRange(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.floor(math.maxinteger + 0.5) should return a FLOAT because the result (2^63)
            // does not fit in a signed 64-bit integer
            DynValue result = script.DoString(
                @"
                local v = math.floor(math.maxinteger + 0.5)
                return math.type(v), v
            "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("float")
                .Because("math.floor result exceeding integer range should be float type")
                .ConfigureAwait(false);

            // The value should be 2^63 = 9223372036854775808
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(9223372036854775808.0)
                .Because("The floored value should be 2^63")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.ceil returns a float (not integer) when the result exceeds integer range.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CeilReturnsFloatWhenResultExceedsIntegerRange(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.ceil on a large value that exceeds integer range should return float
            DynValue result = script.DoString(
                @"
                local v = math.ceil(math.mininteger - 10000000000.0)
                return math.type(v), v
            "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("float")
                .Because("math.ceil result outside integer range should be float type")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Regression test: string.format('%d', math.floor(math.maxinteger + 0.5)) should throw.
        /// Before the fix, math.floor incorrectly returned an integer type that wrapped to
        /// long.MinValue, causing string.format to silently output the wrong value.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FormatDecimalThrowsForFlooredOverflowValue(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return string.format('%d', math.floor(math.maxinteger + 0.5))")
            );

            await Assert
                .That(exception)
                .IsNotNull()
                .Because("string.format should reject floor result that exceeds integer range")
                .ConfigureAwait(false);
            await Assert
                .That(exception.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.floor returns integer type when result IS within integer range.
        /// This ensures the fix doesn't break the normal integer promotion behavior.
        /// </summary>
        /// <remarks>
        /// Note: math.maxinteger + 0.0 loses precision when converted to float (rounds to 2^63),
        /// so it is NOT a valid test case for integer promotion. We test with smaller values.
        /// </remarks>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "3.7", 3L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua54, "3.7", 3L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua55, "3.7", 3L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-3.7", -4L, "negative fractional")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-3.7", -4L, "negative fractional")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-3.7", -4L, "negative fractional")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "1000000000.0",
            1000000000L,
            "medium integer as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "1000000000.0",
            1000000000L,
            "medium integer as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "1000000000.0",
            1000000000L,
            "medium integer as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-1000000000.0",
            -1000000000L,
            "medium negative integer as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-1000000000.0",
            -1000000000L,
            "medium negative integer as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-1000000000.0",
            -1000000000L,
            "medium negative integer as float"
        )]
        public async Task FloorReturnsIntegerWhenResultFitsInRange(
            LuaCompatibilityVersion version,
            string luaExpression,
            long expectedValue,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                $@"
                local v = math.floor({luaExpression})
                return math.type(v), v
            "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("integer")
                .Because($"math.floor({luaExpression}) [{description}] should return integer type")
                .ConfigureAwait(false);

            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo((double)expectedValue)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.ceil returns integer type when result IS within integer range.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "3.2", 4L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua54, "3.2", 4L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua55, "3.2", 4L, "positive fractional")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-3.2", -3L, "negative fractional")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-3.2", -3L, "negative fractional")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-3.2", -3L, "negative fractional")]
        public async Task CeilReturnsIntegerWhenResultFitsInRange(
            LuaCompatibilityVersion version,
            string luaExpression,
            long expectedValue,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                $@"
                local v = math.ceil({luaExpression})
                return math.type(v), v
            "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("integer")
                .Because($"math.ceil({luaExpression}) [{description}] should return integer type")
                .ConfigureAwait(false);

            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo((double)expectedValue)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests Lua 5.1/5.2 behavior - floor returns a number (there's no math.type to check).
        /// </summary>
        /// <remarks>
        /// In Lua 5.1/5.2, there's no integer subtype - all numbers are doubles. The math.type
        /// function doesn't exist. NovaSharp's internal LuaNumber.IsInteger reflects whether
        /// the underlying representation can be stored as an integer, but this is an implementation
        /// detail, not a Lua 5.1/5.2 semantic distinction.
        /// </remarks>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua51)]
        [Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task FloorReturnsNumberInLua51And52(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // In Lua 5.1/5.2, math.floor just returns a number (no type distinction)
            DynValue result = script.DoString("return math.floor(3.7)");

            // The value should be 3.0 and of type number
            await Assert.That(result.Number).IsEqualTo(3.0).ConfigureAwait(false);
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
        }

        private static Script CreateScript(
            LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua54
        )
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
