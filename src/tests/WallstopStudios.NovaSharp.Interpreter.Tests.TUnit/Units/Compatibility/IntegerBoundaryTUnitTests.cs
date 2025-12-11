namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Compatibility
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Exhaustive tests for integer boundary conditions, precision preservation, and
    /// architecture-independent behavior. These tests ensure correct handling of:
    /// - 64-bit integer boundaries (long.MinValue, long.MaxValue)
    /// - IEEE 754 double precision limits
    /// - Float-to-integer conversions at edge cases
    /// - math.tointeger, math.ult, and related functions
    /// </summary>
    /// <remarks>
    /// These tests were added after discovering platform-dependent bugs in integer
    /// conversion where ARM64 and x64 produced different results for values near
    /// the 64-bit integer boundary.
    /// </remarks>
    public sealed class IntegerBoundaryTUnitTests
    {
        #region LuaIntegerHelper.TryGetInteger(double) - Direct API Tests

        /// <summary>
        /// Tests that TryGetInteger correctly handles values at and beyond the 64-bit boundary.
        /// Note: (double)long.MaxValue loses precision and rounds up to 2^63, which is out of range.
        /// </summary>
        [Test]
        [Arguments(
            -9223372036854775808.0,
            true,
            -9223372036854775808L,
            "long.MinValue as double (exactly representable)"
        )]
        [Arguments(9223372036854775808.0, false, 0L, "2^63 (out of range)")]
        [Arguments(0.0, true, 0L, "zero")]
        [Arguments(-0.0, true, 0L, "negative zero")]
        [Arguments(1.0, true, 1L, "one")]
        [Arguments(-1.0, true, -1L, "negative one")]
        [Arguments(double.MaxValue, false, 0L, "double.MaxValue")]
        [Arguments(double.MinValue, false, 0L, "double.MinValue")]
        [Arguments(double.NaN, false, 0L, "NaN")]
        [Arguments(double.PositiveInfinity, false, 0L, "positive infinity")]
        [Arguments(double.NegativeInfinity, false, 0L, "negative infinity")]
        [Arguments(1e18, true, 1000000000000000000L, "1e18 (representable)")]
        [Arguments(9e18, true, 9000000000000000000L, "9e18 (near boundary)")]
        public async Task TryGetIntegerDoubleBoundaryValues(
            double input,
            bool expectedSuccess,
            long expectedValue,
            string description
        )
        {
            bool success = LuaIntegerHelper.TryGetInteger(input, out long result);

            await Assert
                .That(success)
                .IsEqualTo(expectedSuccess)
                .Because(
                    $"TryGetInteger({input}) [{description}] success should be {expectedSuccess}"
                );

            await Assert
                .That(result)
                .IsEqualTo(expectedValue)
                .Because(
                    $"TryGetInteger({input}) [{description}] result should be {expectedValue}"
                );
        }

        /// <summary>
        /// Tests that fractional values are correctly rejected.
        /// </summary>
        [Test]
        [Arguments(0.5, "0.5")]
        [Arguments(-0.5, "-0.5")]
        [Arguments(0.1, "0.1")]
        [Arguments(0.9, "0.9")]
        [Arguments(1.1, "1.1")]
        [Arguments(-1.1, "-1.1")]
        [Arguments(123.456, "123.456")]
        [Arguments(1e-10, "1e-10 (tiny positive)")]
        [Arguments(-1e-10, "-1e-10 (tiny negative)")]
        [Arguments(9223372036854775807.5, "maxValue + 0.5")]
        public async Task TryGetIntegerRejectsFractionalValues(double input, string description)
        {
            bool success = LuaIntegerHelper.TryGetInteger(input, out long result);

            await Assert
                .That(success)
                .IsFalse()
                .Because($"TryGetInteger({input}) [{description}] should reject fractional value");

            await Assert
                .That(result)
                .IsEqualTo(0L)
                .Because(
                    $"TryGetInteger({input}) [{description}] should return 0 for rejected value"
                );
        }

        /// <summary>
        /// Tests specific powers of 2 that are significant for integer precision.
        /// Note: long.MaxValue loses precision when converted to double (rounds up to 2^63),
        /// so it will fail the range check.
        /// </summary>
        [Test]
        [Arguments(1L << 52, true, "2^52 (double mantissa bits)")]
        [Arguments(1L << 53, true, "2^53 (max exact integer in double)")]
        [Arguments((1L << 53) + 1, true, "2^53 + 1 (rounds to 2^53 in double)")]
        [Arguments(1L << 62, true, "2^62")]
        public async Task TryGetIntegerPowersOfTwo(
            long value,
            bool expectedSuccess,
            string description
        )
        {
            double input = (double)value;
            bool success = LuaIntegerHelper.TryGetInteger(input, out long result);

            await Assert
                .That(success)
                .IsEqualTo(expectedSuccess)
                .Because(
                    $"TryGetInteger({value}) [{description}] success should be {expectedSuccess}"
                );

            if (expectedSuccess)
            {
                // Note: due to double precision loss, we check if the result round-trips correctly
                await Assert
                    .That(result)
                    .IsEqualTo((long)input)
                    .Because($"TryGetInteger result should match (long)(double){value}");
            }
        }

        /// <summary>
        /// Tests that long.MaxValue as double fails because it loses precision and becomes 2^63.
        /// </summary>
        [Test]
        public async Task TryGetIntegerMaxValueAsDoubleFailsDueToPrecisionLoss()
        {
            // long.MaxValue = 2^63 - 1 = 9223372036854775807
            // When converted to double, it rounds up to 2^63 = 9223372036854775808
            // This is >= our threshold, so it should be rejected
            double maxAsDouble = (double)long.MaxValue;

            // Verify precision loss occurs
            await Assert
                .That(maxAsDouble)
                .IsEqualTo(9223372036854775808.0)
                .Because("(double)long.MaxValue should round up to 2^63");

            bool success = LuaIntegerHelper.TryGetInteger(maxAsDouble, out long result);

            await Assert
                .That(success)
                .IsFalse()
                .Because("(double)long.MaxValue should be rejected because it rounds to 2^63");
            await Assert.That(result).IsEqualTo(0L);
        }

        #endregion

        #region LuaIntegerHelper.TryGetInteger(DynValue) - DynValue API Tests

        /// <summary>
        /// Tests that DynValue integers preserve precision for boundary values.
        /// </summary>
        [Test]
        [Arguments(9223372036854775807L, "long.MaxValue")]
        [Arguments(-9223372036854775808L, "long.MinValue")]
        [Arguments(0L, "zero")]
        [Arguments(1L, "one")]
        [Arguments(-1L, "negative one")]
        [Arguments(9223372036854775806L, "MaxValue - 1")]
        [Arguments(-9223372036854775807L, "MinValue + 1")]
        public async Task TryGetIntegerDynValuePreservesIntegerPrecision(
            long value,
            string description
        )
        {
            // Create a DynValue with native integer storage
            DynValue dynValue = DynValue.NewInteger(value);

            bool success = LuaIntegerHelper.TryGetInteger(dynValue, out long result);

            await Assert
                .That(success)
                .IsTrue()
                .Because($"DynValue integer {value} [{description}] should be convertible");

            await Assert
                .That(result)
                .IsEqualTo(value)
                .Because($"DynValue integer {value} [{description}] should preserve exact value");
        }

        /// <summary>
        /// Tests that DynValue floats at boundary are handled correctly.
        /// Note: -9223372036854775809.0 as a double actually equals long.MinValue due to precision.
        /// </summary>
        [Test]
        [Arguments(9223372036854775808.0, false, "2^63 as float (out of range)")]
        [Arguments(9223372036854774784.0, true, "large representable float")]
        public async Task TryGetIntegerDynValueFloatBoundary(
            double value,
            bool expectedSuccess,
            string description
        )
        {
            // Create a DynValue with float storage (not integer)
            DynValue dynValue = DynValue.NewNumber(value);

            bool success = LuaIntegerHelper.TryGetInteger(dynValue, out long result);

            await Assert
                .That(success)
                .IsEqualTo(expectedSuccess)
                .Because(
                    $"DynValue float {value} [{description}] success should be {expectedSuccess}"
                );
        }

        #endregion

        #region math.tointeger Script Tests

        /// <summary>
        /// Tests math.tointeger with boundary values via script execution.
        /// Note: math.maxinteger + 0.0 promotes to float, and (double)maxinteger rounds to 2^63,
        /// which is out of range, so it returns nil.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", true, 9223372036854775807L, "maxinteger")]
        [Arguments("math.mininteger", true, -9223372036854775808L, "mininteger")]
        [Arguments("0", true, 0L, "zero")]
        [Arguments("1", true, 1L, "one")]
        [Arguments("-1", true, -1L, "negative one")]
        [Arguments("2^63", false, 0L, "2^63 (overflow)")]
        [Arguments("1/0", false, 0L, "infinity")]
        [Arguments("-1/0", false, 0L, "negative infinity")]
        [Arguments("0/0", false, 0L, "NaN")]
        [Arguments("1.5", false, 0L, "fractional")]
        [Arguments(
            "math.mininteger + 0.0",
            true,
            -9223372036854775808L,
            "mininteger as float (exactly representable)"
        )]
        public async Task MathTointegerBoundaryValues(
            string expression,
            bool shouldSucceed,
            long expectedValue,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.tointeger({expression})");

            if (shouldSucceed)
            {
                await Assert
                    .That(result.Type)
                    .IsEqualTo(DataType.Number)
                    .Because($"math.tointeger({expression}) [{description}] should return number");

                await Assert
                    .That(result.LuaNumber.IsInteger)
                    .IsTrue()
                    .Because(
                        $"math.tointeger({expression}) [{description}] should return integer subtype"
                    );

                await Assert
                    .That(result.LuaNumber.AsInteger)
                    .IsEqualTo(expectedValue)
                    .Because(
                        $"math.tointeger({expression}) [{description}] should equal {expectedValue}"
                    );
            }
            else
            {
                await Assert
                    .That(result.IsNil())
                    .IsTrue()
                    .Because($"math.tointeger({expression}) [{description}] should return nil");
            }
        }

        /// <summary>
        /// Tests that math.tointeger correctly handles string arguments.
        /// Note: String parsing goes through double.Parse, so maxinteger as string
        /// loses precision and becomes 2^63, which is out of range.
        /// </summary>
        [Test]
        [Arguments("'123'", true, 123L, "simple integer string")]
        [Arguments("'-456'", true, -456L, "negative integer string")]
        [Arguments("'  789  '", true, 789L, "whitespace padded")]
        [Arguments("'1.5'", false, 0L, "fractional string")]
        [Arguments("'abc'", false, 0L, "non-numeric string")]
        [Arguments("'1e10'", true, 10000000000L, "scientific notation")]
        public async Task MathTointegerStringArguments(
            string expression,
            bool shouldSucceed,
            long expectedValue,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.tointeger({expression})");

            if (shouldSucceed)
            {
                await Assert
                    .That(result.Type)
                    .IsEqualTo(DataType.Number)
                    .Because($"math.tointeger({expression}) [{description}] should return number");

                await Assert
                    .That(result.LuaNumber.AsInteger)
                    .IsEqualTo(expectedValue)
                    .Because(
                        $"math.tointeger({expression}) [{description}] should equal {expectedValue}"
                    );
            }
            else
            {
                await Assert
                    .That(result.IsNil())
                    .IsTrue()
                    .Because($"math.tointeger({expression}) [{description}] should return nil");
            }
        }

        #endregion

        #region math.ult Script Tests

        /// <summary>
        /// Tests math.ult (unsigned less than) with boundary and edge case values.
        /// </summary>
        [Test]
        [Arguments(
            "math.maxinteger",
            "math.mininteger",
            true,
            "maxinteger < mininteger (unsigned)"
        )]
        [Arguments(
            "math.mininteger",
            "math.maxinteger",
            false,
            "mininteger < maxinteger (unsigned)"
        )]
        [Arguments("0", "-1", true, "0 < -1 (unsigned, -1 is max)")]
        [Arguments("-1", "0", false, "-1 < 0 (unsigned)")]
        [Arguments("0", "1", true, "0 < 1")]
        [Arguments("1", "0", false, "1 < 0")]
        [Arguments("-1", "-2", false, "-1 < -2 (unsigned)")]
        [Arguments("-2", "-1", true, "-2 < -1 (unsigned)")]
        [Arguments("math.maxinteger", "math.maxinteger", false, "maxinteger < maxinteger")]
        [Arguments("math.mininteger", "math.mininteger", false, "mininteger < mininteger")]
        [Arguments("0", "0", false, "0 < 0")]
        [Arguments("-1", "-1", false, "-1 < -1")]
        [Arguments("1", "2", true, "1 < 2")]
        [Arguments("math.maxinteger - 1", "math.maxinteger", true, "maxinteger-1 < maxinteger")]
        [Arguments(
            "math.mininteger",
            "math.mininteger + 1",
            true,
            "mininteger < mininteger+1 (unsigned)"
        )]
        public async Task MathUltBoundaryValues(
            string left,
            string right,
            bool expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.ult({left}, {right})");

            await Assert
                .That(result.Boolean)
                .IsEqualTo(expected)
                .Because($"math.ult({left}, {right}) [{description}] should be {expected}");
        }

        /// <summary>
        /// Tests that math.ult preserves integer precision for maxinteger/mininteger.
        /// This was the original bug: precision loss caused incorrect comparisons.
        /// </summary>
        [Test]
        public async Task MathUltPreservesIntegerPrecision()
        {
            Script script = CreateScript();

            // Verify the values are stored correctly as integers
            DynValue maxInt = script.DoString("return math.maxinteger");
            DynValue minInt = script.DoString("return math.mininteger");

            await Assert.That(maxInt.LuaNumber.IsInteger).IsTrue();
            await Assert.That(minInt.LuaNumber.IsInteger).IsTrue();
            await Assert.That(maxInt.LuaNumber.AsInteger).IsEqualTo(long.MaxValue);
            await Assert.That(minInt.LuaNumber.AsInteger).IsEqualTo(long.MinValue);

            // The key test: unsigned comparison should work correctly
            // In unsigned: maxinteger (0x7FFFFFFF...) < mininteger (0x80000000...)
            DynValue ult = script.DoString("return math.ult(math.maxinteger, math.mininteger)");
            await Assert
                .That(ult.Boolean)
                .IsTrue()
                .Because(
                    "maxinteger (0x7FFFFFFFFFFFFFFF) should be less than mininteger (0x8000000000000000) in unsigned comparison"
                );
        }

        #endregion

        #region math.type Script Tests

        /// <summary>
        /// Tests math.type correctly identifies integer vs float subtypes.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", "integer", "maxinteger is integer")]
        [Arguments("math.mininteger", "integer", "mininteger is integer")]
        [Arguments("0", "integer", "zero literal is integer")]
        [Arguments("1", "integer", "one literal is integer")]
        [Arguments("-1", "integer", "negative one is integer")]
        [Arguments("1.0", "float", "1.0 is float")]
        [Arguments("0.0", "float", "0.0 is float")]
        [Arguments("1/0", "float", "infinity is float")]
        [Arguments("0/0", "float", "NaN is float")]
        [Arguments("math.huge", "float", "huge is float")]
        [Arguments("2^63", "float", "2^63 is float (overflow)")]
        [Arguments("math.maxinteger + 0.0", "float", "maxinteger coerced to float")]
        [Arguments("math.maxinteger + 1", "integer", "maxinteger + 1 wraps to integer")]
        [Arguments("math.mininteger - 1", "integer", "mininteger - 1 wraps to integer")]
        public async Task MathTypeIdentifiesSubtype(
            string expression,
            string expectedType,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.type({expression})");

            await Assert
                .That(result.String)
                .IsEqualTo(expectedType)
                .Because($"math.type({expression}) [{description}] should be '{expectedType}'");
        }

        #endregion

        #region Integer Arithmetic Overflow Tests

        /// <summary>
        /// Tests integer arithmetic overflow wrapping behavior.
        /// </summary>
        [Test]
        [Arguments(
            "math.maxinteger + 1",
            -9223372036854775808L,
            "maxinteger + 1 wraps to mininteger"
        )]
        [Arguments(
            "math.mininteger - 1",
            9223372036854775807L,
            "mininteger - 1 wraps to maxinteger"
        )]
        [Arguments("math.maxinteger * 2", -2L, "maxinteger * 2 wraps")]
        [Arguments("-math.mininteger", -9223372036854775808L, "negation of mininteger wraps")]
        public async Task IntegerArithmeticOverflowWraps(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.LuaNumber.IsInteger)
                .IsTrue()
                .Because($"{expression} [{description}] should remain integer type");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests that mininteger // -1 wraps to mininteger (two's complement behavior).
        /// In two's complement arithmetic: -mininteger = mininteger because the positive value
        /// would overflow. Lua correctly preserves this wrapping behavior.
        /// </summary>
        [Test]
        public async Task MinintegerDividedByNegativeOneWrapsToMininteger()
        {
            Script script = CreateScript();

            // Per Lua 5.3/5.4 spec: mininteger // -1 = mininteger (wraps due to two's complement)
            DynValue result = script.DoString("return math.mininteger // -1");

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);

            await Assert
                .That(result.IsInteger)
                .IsTrue()
                .Because("floor division of integers should return integer")
                .ConfigureAwait(false);

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(long.MinValue)
                .Because("mininteger // -1 wraps to mininteger")
                .ConfigureAwait(false);
        }

        #endregion

        #region Bitwise Operations with Boundary Values

        /// <summary>
        /// Tests bitwise operations preserve integer precision at boundaries.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger | 0", 9223372036854775807L, "maxinteger OR 0")]
        [Arguments("math.mininteger | 0", -9223372036854775808L, "mininteger OR 0")]
        [Arguments(
            "math.maxinteger & math.maxinteger",
            9223372036854775807L,
            "maxinteger AND maxinteger"
        )]
        [Arguments(
            "math.mininteger & math.mininteger",
            -9223372036854775808L,
            "mininteger AND mininteger"
        )]
        [Arguments("~math.mininteger", 9223372036854775807L, "NOT mininteger = maxinteger")]
        [Arguments("~math.maxinteger", -9223372036854775808L, "NOT maxinteger = mininteger")]
        [Arguments("math.maxinteger ~ math.maxinteger", 0L, "maxinteger XOR maxinteger")]
        [Arguments("1 << 63", -9223372036854775808L, "1 << 63 = mininteger")]
        [Arguments("math.mininteger >> 63", 1L, "mininteger >> 63 (logical)")]
        [Arguments("-1 >> 63", 1L, "-1 >> 63 (logical shift, not arithmetic)")]
        public async Task BitwiseOperationsPreservePrecision(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.LuaNumber.IsInteger)
                .IsTrue()
                .Because($"{expression} [{description}] should be integer type");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should equal {expected}");
        }

        #endregion

        #region Shift Operations Edge Cases

        /// <summary>
        /// Tests shift left operations at boundary shift amounts.
        /// </summary>
        [Test]
        [Arguments(1L, 0, 1L, "shift by 0")]
        [Arguments(1L, 1, 2L, "shift left by 1")]
        [Arguments(1L, 63, -9223372036854775808L, "shift left by 63")]
        [Arguments(1L, 64, 0L, "shift left by 64 (returns 0)")]
        [Arguments(1L, 100, 0L, "shift left by 100 (returns 0)")]
        public async Task ShiftLeftEdgeCases(
            long value,
            int shift,
            long expected,
            string description
        )
        {
            long result = LuaIntegerHelper.ShiftLeft(value, shift);

            await Assert
                .That(result)
                .IsEqualTo(expected)
                .Because($"ShiftLeft({value}, {shift}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests right shift operations (logical, not arithmetic).
        /// </summary>
        [Test]
        [Arguments(-1L, 1, 9223372036854775807L, "-1 >> 1 (logical)")]
        [Arguments(-1L, 63, 1L, "-1 >> 63 (logical)")]
        [Arguments(long.MinValue, 1, 4611686018427387904L, "mininteger >> 1")]
        [Arguments(long.MinValue, 62, 2L, "mininteger >> 62")]
        [Arguments(long.MinValue, 63, 1L, "mininteger >> 63")]
        [Arguments(long.MaxValue, 62, 1L, "maxinteger >> 62")]
        public async Task ShiftRightIsLogical(
            long value,
            int shift,
            long expected,
            string description
        )
        {
            long result = LuaIntegerHelper.ShiftRight(value, shift);

            await Assert
                .That(result)
                .IsEqualTo(expected)
                .Because($"ShiftRight({value}, {shift}) [{description}] should equal {expected}");
        }

        #endregion

        #region IEEE 754 Double Precision Edge Cases

        /// <summary>
        /// Tests the boundary where double precision starts losing integer precision.
        /// 2^53 is the largest integer that can be exactly represented in a double.
        /// </summary>
        [Test]
        public async Task DoublePrecisionBoundary()
        {
            // 2^53 = 9007199254740992 is the max exact integer in double
            const long maxExact = 9007199254740992L;

            // This should work
            bool exactSuccess = LuaIntegerHelper.TryGetInteger(
                (double)maxExact,
                out long exactResult
            );
            await Assert.That(exactSuccess).IsTrue();
            await Assert.That(exactResult).IsEqualTo(maxExact);

            // 2^53 + 1 cannot be exactly represented, but the double rounds to 2^53
            // The conversion should still succeed because the double value is integral
            double notExact = (double)(maxExact + 1);
            bool notExactSuccess = LuaIntegerHelper.TryGetInteger(
                notExact,
                out long notExactResult
            );
            await Assert.That(notExactSuccess).IsTrue();
            // The result will be the rounded value
            await Assert.That(notExactResult).IsEqualTo((long)notExact);
        }

        /// <summary>
        /// Tests that values just beyond the 64-bit boundary are correctly rejected.
        /// </summary>
        [Test]
        public async Task BeyondInt64Boundary()
        {
            // 2^63 = 9223372036854775808 (one beyond MaxValue)
            const double twoTo63 = 9223372036854775808.0;

            bool success = LuaIntegerHelper.TryGetInteger(twoTo63, out long result);

            await Assert
                .That(success)
                .IsFalse()
                .Because("2^63 should be rejected as out of int64 range");
            await Assert.That(result).IsEqualTo(0L);

            // Also test via script
            Script script = CreateScript();
            DynValue scriptResult = script.DoString("return math.tointeger(2^63)");
            await Assert.That(scriptResult.IsNil()).IsTrue();
        }

        #endregion

        #region Cross-Platform Consistency Tests

        /// <summary>
        /// Tests that demonstrate the fix for the original ARM64 vs x64 discrepancy.
        /// These specific cases were failing before the fix.
        /// </summary>
        [Test]
        public async Task OriginalArm64DiscrepancyFixed()
        {
            Script script = CreateScript();

            // Original bug 1: math.ult(maxinteger, mininteger) returned false on x64
            // due to precision loss when converting to double before comparison.
            DynValue ultResult = script.DoString(
                "return math.ult(math.maxinteger, math.mininteger)"
            );
            await Assert
                .That(ultResult.Boolean)
                .IsTrue()
                .Because(
                    "math.ult(maxinteger, mininteger) should be true (this was the original x64 bug)"
                );

            // Original bug 2: math.tointeger(2^63) returned different values on x64 vs ARM64
            // due to undefined behavior in (long)double conversion for out-of-range values.
            DynValue tointResult = script.DoString("return math.tointeger(2^63)");
            await Assert
                .That(tointResult.IsNil())
                .IsTrue()
                .Because(
                    "math.tointeger(2^63) should return nil (this returned platform-dependent garbage before)"
                );
        }

        /// <summary>
        /// Verifies that maxinteger and mininteger are stored with full precision.
        /// </summary>
        [Test]
        public async Task MaxMinIntegerStoredWithFullPrecision()
        {
            Script script = CreateScript();

            DynValue max = script.DoString("return math.maxinteger");
            DynValue min = script.DoString("return math.mininteger");

            // Verify they're stored as integers
            await Assert.That(max.LuaNumber.IsInteger).IsTrue();
            await Assert.That(min.LuaNumber.IsInteger).IsTrue();

            // Verify exact values
            await Assert.That(max.LuaNumber.AsInteger).IsEqualTo(long.MaxValue);
            await Assert.That(min.LuaNumber.AsInteger).IsEqualTo(long.MinValue);

            // Verify round-trip through tointeger preserves values
            DynValue maxRoundTrip = script.DoString("return math.tointeger(math.maxinteger)");
            DynValue minRoundTrip = script.DoString("return math.tointeger(math.mininteger)");

            await Assert.That(maxRoundTrip.LuaNumber.AsInteger).IsEqualTo(long.MaxValue);
            await Assert.That(minRoundTrip.LuaNumber.AsInteger).IsEqualTo(long.MinValue);
        }

        #endregion

        #region Helpers

        private static Script CreateScript()
        {
            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua54,
            };
            return new Script(CoreModulePresets.Complete, options);
        }

        #endregion
    }
}
