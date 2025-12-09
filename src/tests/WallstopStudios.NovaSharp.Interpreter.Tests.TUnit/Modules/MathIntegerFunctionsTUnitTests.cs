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
    /// Comprehensive tests for math module functions that deal with integers,
    /// ensuring correct behavior at 64-bit boundaries and proper precision handling.
    /// </summary>
    public sealed class MathIntegerFunctionsTUnitTests
    {
        #region math.abs with Integer Boundaries

        /// <summary>
        /// Tests math.abs with integer arguments preserves integer subtype (Lua 5.3+).
        /// Per Lua 5.4 §6.7, math.abs returns an integer when given an integer argument.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", 9223372036854775807L, "maxinteger")]
        [Arguments("0", 0L, "zero")]
        [Arguments("1", 1L, "one")]
        [Arguments("-1", 1L, "negative one")]
        [Arguments("-math.maxinteger", 9223372036854775807L, "negative maxinteger")]
        [Arguments("100", 100L, "positive integer")]
        [Arguments("-100", 100L, "negative integer")]
        [Arguments("math.maxinteger - 1", 9223372036854775806L, "maxinteger - 1")]
        [Arguments("-(math.maxinteger - 1)", 9223372036854775806L, "negative (maxinteger - 1)")]
        public async Task MathAbsIntegerBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.abs({expression})");

            // Verify the value is correct
            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"math.abs({expression}) [{description}] should equal {expected}");

            // Verify integer subtype is preserved (Lua 5.3+)
            await Assert
                .That(result.IsInteger)
                .IsTrue()
                .Because($"math.abs({expression}) [{description}] should return integer subtype");
        }

        /// <summary>
        /// Tests math.abs(mininteger) - a special case because |mininteger| > maxinteger.
        /// Per Lua 5.4 §6.7 and two's complement arithmetic rules, this wraps back to mininteger.
        /// </summary>
        [Test]
        public async Task MathAbsMinintegerWraps()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.abs(math.mininteger)");

            // Diagnostic: output the actual value for debugging
            long actualValue = result.LuaNumber.AsInteger;
            string actualType = result.LuaNumber.LuaTypeName;

            // |mininteger| = 2^63 which overflows, wrapping back to mininteger
            // (same behavior as -mininteger in two's complement)
            await Assert
                .That(actualValue)
                .IsEqualTo(long.MinValue)
                .Because(
                    $"math.abs(mininteger) should wrap to mininteger (overflow). "
                        + $"Got {actualValue} (0x{actualValue:X16}), type: {actualType}"
                );

            // Verify it returns integer subtype
            await Assert
                .That(result.IsInteger)
                .IsTrue()
                .Because("math.abs(mininteger) should return integer subtype");
        }

        /// <summary>
        /// Tests math.abs with float arguments returns float subtype and correct value.
        /// </summary>
        [Test]
        [Arguments("1.5", 1.5, "positive float")]
        [Arguments("-1.5", 1.5, "negative float")]
        [Arguments("0.0", 0.0, "float zero")]
        [Arguments("-0.0", 0.0, "negative float zero")]
        [Arguments("1e10", 1e10, "scientific notation")]
        [Arguments("-1e10", 1e10, "negative scientific notation")]
        [Arguments("math.huge", double.MaxValue, "huge positive")]
        [Arguments("-math.huge", double.MaxValue, "huge negative")]
        public async Task MathAbsFloatSubtype(
            string expression,
            double expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.abs({expression})");

            // Verify the value is correct
            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.abs({expression}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests that math.abs preserves float subtype for fractional values.
        /// </summary>
        [Test]
        [Arguments("1.5", "positive float")]
        [Arguments("-1.5", "negative float")]
        [Arguments("-3.14159", "negative pi")]
        public async Task MathAbsPreservesFloatSubtype(string expression, string description)
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.abs({expression})");

            await Assert
                .That(result.IsFloat)
                .IsTrue()
                .Because($"math.abs({expression}) [{description}] should return float subtype");
        }

        /// <summary>
        /// Tests that math.abs(mininteger) behaves consistently with -mininteger.
        /// Both should wrap to mininteger due to two's complement overflow.
        /// </summary>
        [Test]
        public async Task MathAbsMinintegerConsistentWithNegation()
        {
            Script script = CreateScript();

            // Execute both operations
            DynValue absResult = script.DoString("return math.abs(math.mininteger)");
            DynValue negResult = script.DoString("return -math.mininteger");

            // Both should produce the same value (mininteger wraps to itself)
            await Assert
                .That(absResult.LuaNumber.AsInteger)
                .IsEqualTo(negResult.LuaNumber.AsInteger)
                .Because("math.abs(mininteger) should equal -mininteger (both wrap to mininteger)");

            // Both values should equal mininteger
            await Assert
                .That(absResult.LuaNumber.AsInteger)
                .IsEqualTo(long.MinValue)
                .Because("Both should wrap to mininteger");
        }

        /// <summary>
        /// Tests math.abs with values near integer boundaries to ensure no precision loss.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger - 10", 9223372036854775797L, "maxinteger - 10")]
        [Arguments("-(math.maxinteger - 10)", 9223372036854775797L, "negative (maxinteger - 10)")]
        [Arguments("math.mininteger + 10", 9223372036854775798L, "mininteger + 10")]
        [Arguments(
            "math.mininteger + 1",
            9223372036854775807L,
            "mininteger + 1 (equals maxinteger)"
        )]
        public async Task MathAbsNearBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.abs({expression})");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"math.abs({expression}) [{description}] should equal {expected}");

            await Assert
                .That(result.IsInteger)
                .IsTrue()
                .Because($"math.abs({expression}) [{description}] should preserve integer subtype");
        }

        #endregion

        #region math.floor/ceil with Integer Boundaries

        /// <summary>
        /// Tests math.floor preserves integer values at boundaries.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", 9223372036854775807L, "maxinteger")]
        [Arguments("math.mininteger", -9223372036854775808L, "mininteger")]
        [Arguments("0", 0L, "zero")]
        [Arguments("1.9", 1L, "1.9 floors to 1")]
        [Arguments("-1.1", -2L, "-1.1 floors to -2")]
        [Arguments("1e18", 1000000000000000000L, "1e18")]
        public async Task MathFloorIntegerBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.floor({expression})");

            await Assert
                .That(result.Number)
                .IsEqualTo((double)expected)
                .Because($"math.floor({expression}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests math.ceil preserves integer values at boundaries.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", 9223372036854775807L, "maxinteger")]
        [Arguments("math.mininteger", -9223372036854775808L, "mininteger")]
        [Arguments("0", 0L, "zero")]
        [Arguments("1.1", 2L, "1.1 ceils to 2")]
        [Arguments("-1.9", -1L, "-1.9 ceils to -1")]
        public async Task MathCeilIntegerBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.ceil({expression})");

            await Assert
                .That(result.Number)
                .IsEqualTo((double)expected)
                .Because($"math.ceil({expression}) [{description}] should equal {expected}");
        }

        #endregion

        #region math.fmod with Integer Boundaries

        /// <summary>
        /// Tests math.fmod with boundary values.
        /// Note: math.fmod operates on doubles, so maxinteger loses precision and becomes 2^63 (even).
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", "1", 0.0, "maxinteger % 1")]
        [Arguments("math.maxinteger", "2", 0.0, "maxinteger % 2 (rounds to 2^63, which is even)")]
        [Arguments("math.mininteger", "1", 0.0, "mininteger % 1")]
        [Arguments("math.mininteger", "2", 0.0, "mininteger % 2")]
        [Arguments("10", "3", 1.0, "10 % 3")]
        [Arguments("-10", "3", -1.0, "-10 % 3")]
        public async Task MathFmodIntegerBoundaries(
            string x,
            string y,
            double expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.fmod({x}, {y})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.fmod({x}, {y}) [{description}] should equal {expected}");
        }

        #endregion

        #region math.modf with Integer Boundaries

        /// <summary>
        /// Tests math.modf returns correct integral and fractional parts.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", 9223372036854775807.0, 0.0, "maxinteger")]
        [Arguments("math.mininteger", -9223372036854775808.0, 0.0, "mininteger")]
        [Arguments("1.5", 1.0, 0.5, "1.5")]
        [Arguments("-1.5", -1.0, -0.5, "-1.5")]
        [Arguments("0", 0.0, 0.0, "zero")]
        public async Task MathModfIntegerBoundaries(
            string expression,
            double expectedIntegral,
            double expectedFractional,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.modf({expression})");

            // modf returns two values
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(expectedIntegral)
                .Because(
                    $"math.modf({expression}) [{description}] integral should be {expectedIntegral}"
                );

            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(expectedFractional)
                .Because(
                    $"math.modf({expression}) [{description}] fractional should be {expectedFractional}"
                );
        }

        #endregion

        #region math.max/min with Integer Boundaries

        /// <summary>
        /// Tests math.max correctly compares boundary values.
        /// </summary>
        [Test]
        [Arguments(
            "math.maxinteger, math.mininteger",
            9223372036854775807.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            "math.mininteger, math.maxinteger",
            9223372036854775807.0,
            "mininteger vs maxinteger"
        )]
        [Arguments("math.maxinteger, 0", 9223372036854775807.0, "maxinteger vs 0")]
        [Arguments("0, math.mininteger", 0.0, "0 vs mininteger")]
        [Arguments(
            "math.maxinteger, math.maxinteger - 1",
            9223372036854775807.0,
            "maxinteger vs maxinteger-1"
        )]
        public async Task MathMaxIntegerBoundaries(string args, double expected, string description)
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.max({args})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.max({args}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests math.min correctly compares boundary values.
        /// </summary>
        [Test]
        [Arguments(
            "math.maxinteger, math.mininteger",
            -9223372036854775808.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            "math.mininteger, math.maxinteger",
            -9223372036854775808.0,
            "mininteger vs maxinteger"
        )]
        [Arguments("math.mininteger, 0", -9223372036854775808.0, "mininteger vs 0")]
        [Arguments("0, math.maxinteger", 0.0, "0 vs maxinteger")]
        [Arguments(
            "math.mininteger, math.mininteger + 1",
            -9223372036854775808.0,
            "mininteger vs mininteger+1"
        )]
        public async Task MathMinIntegerBoundaries(string args, double expected, string description)
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.min({args})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.min({args}) [{description}] should equal {expected}");
        }

        #endregion

        #region Integer Division (//) Edge Cases

        /// <summary>
        /// Tests integer division with boundary values.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger // 1", 9223372036854775807L, "maxinteger // 1")]
        [Arguments("math.mininteger // 1", -9223372036854775808L, "mininteger // 1")]
        [Arguments("math.maxinteger // math.maxinteger", 1L, "maxinteger // maxinteger")]
        [Arguments("math.mininteger // math.mininteger", 1L, "mininteger // mininteger")]
        [Arguments("math.maxinteger // 2", 4611686018427387903L, "maxinteger // 2")]
        [Arguments("math.mininteger // 2", -4611686018427387904L, "mininteger // 2")]
        [Arguments("-10 // 3", -4L, "-10 // 3 (floor division)")]
        [Arguments("10 // -3", -4L, "10 // -3 (floor division)")]
        public async Task IntegerDivisionBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests that mininteger // -1 throws (this is an edge case in two's complement).
        /// In Lua it would wrap to mininteger, but NovaSharp throws OverflowException.
        /// </summary>
        [Test]
        public async Task IntegerDivisionMinintegerByNegativeOneThrows()
        {
            Script script = CreateScript();

            // |mininteger| = 2^63 which overflows
            // In Lua this would wrap to mininteger, but NovaSharp throws
            await Assert
                .That(() => script.DoString("return math.mininteger // -1"))
                .Throws<System.OverflowException>()
                .Because(
                    "mininteger // -1 throws because |mininteger| > maxinteger (TODO: should wrap like Lua)"
                );
        }

        /// <summary>
        /// Tests integer division by zero throws appropriate error.
        /// </summary>
        [Test]
        public async Task IntegerDivisionByZeroThrows()
        {
            Script script = CreateScript();

            await Assert
                .That(() => script.DoString("return 1 // 0"))
                .Throws<ScriptRuntimeException>()
                .Because("integer division by zero should throw");
        }

        #endregion

        #region Modulo (%) Edge Cases

        /// <summary>
        /// Tests modulo with boundary values.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger % 2", 1L, "maxinteger % 2")]
        [Arguments("math.mininteger % 2", 0L, "mininteger % 2")]
        [Arguments("math.maxinteger % math.maxinteger", 0L, "maxinteger % maxinteger")]
        [Arguments("-10 % 3", 2L, "-10 % 3 (Lua floor mod)")]
        [Arguments("10 % -3", -2L, "10 % -3 (Lua floor mod)")]
        public async Task ModuloBoundaries(string expression, long expected, string description)
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests modulo by zero throws appropriate error in Lua 5.3+ (default).
        /// </summary>
        [Test]
        public async Task ModuloByZeroThrows()
        {
            // Default CreateScript uses Lua 5.4 which throws error
            Script script = CreateScript();

            await Assert
                .That(() => script.DoString("return 1 % 0"))
                .Throws<ScriptRuntimeException>()
                .Because("modulo by zero should throw in Lua 5.3+");
        }

        #endregion

        #region Comparison Operations with Integer Boundaries

        /// <summary>
        /// Tests comparison operators with boundary integer values.
        /// This includes critical edge cases where double precision would fail:
        /// - maxinteger and maxinteger-1 differ by 1, but (double)maxinteger == (double)(maxinteger-1)
        /// - Similar issues exist at the mininteger boundary
        /// </summary>
        [Test]
        [Arguments("math.maxinteger > math.mininteger", true, "maxinteger > mininteger")]
        [Arguments("math.mininteger < math.maxinteger", true, "mininteger < maxinteger")]
        [Arguments("math.maxinteger >= math.maxinteger", true, "maxinteger >= maxinteger")]
        [Arguments("math.mininteger <= math.mininteger", true, "mininteger <= mininteger")]
        [Arguments("math.maxinteger == 9223372036854775807", true, "maxinteger == literal")]
        [Arguments("math.mininteger == -9223372036854775808", true, "mininteger == literal")]
        [Arguments("math.maxinteger ~= math.mininteger", true, "maxinteger ~= mininteger")]
        // Critical tests for IEEE 754 precision edge cases
        [Arguments("math.maxinteger > math.maxinteger - 1", true, "maxinteger > maxinteger-1")]
        [Arguments("math.maxinteger - 1 < math.maxinteger", true, "maxinteger-1 < maxinteger")]
        [Arguments("math.maxinteger ~= math.maxinteger - 1", true, "maxinteger ~= maxinteger-1")]
        [Arguments("math.mininteger < math.mininteger + 1", true, "mininteger < mininteger+1")]
        [Arguments("math.mininteger + 1 > math.mininteger", true, "mininteger+1 > mininteger")]
        [Arguments("math.mininteger ~= math.mininteger + 1", true, "mininteger ~= mininteger+1")]
        // Boundary subtraction comparisons - tests that subtraction doesn't destroy precision
        [Arguments(
            "math.maxinteger - 1 >= math.maxinteger - 1",
            true,
            "maxinteger-1 >= maxinteger-1"
        )]
        [Arguments(
            "math.maxinteger - 1 <= math.maxinteger - 1",
            true,
            "maxinteger-1 <= maxinteger-1"
        )]
        public async Task ComparisonWithBoundaryValues(
            string expression,
            bool expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.Boolean)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should be {expected}");
        }

        /// <summary>
        /// Tests that integer and float comparisons work correctly at boundaries.
        /// </summary>
        [Test]
        [Arguments(
            "math.maxinteger == math.maxinteger + 0.0",
            true,
            "maxinteger == maxinteger as float"
        )]
        [Arguments(
            "math.mininteger == math.mininteger + 0.0",
            true,
            "mininteger == mininteger as float"
        )]
        [Arguments("0 == 0.0", true, "0 == 0.0")]
        [Arguments("1 == 1.0", true, "1 == 1.0")]
        public async Task IntegerFloatComparison(
            string expression,
            bool expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.Boolean)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should be {expected}");
        }

        #endregion

        #region String to Integer Conversion Edge Cases

        /// <summary>
        /// Tests tonumber with integer boundary strings.
        /// </summary>
        [Test]
        [Arguments("'9223372036854775807'", 9223372036854775807.0, true, "maxinteger string")]
        [Arguments("'-9223372036854775808'", -9223372036854775808.0, true, "mininteger string")]
        [Arguments(
            "'9223372036854775808'",
            9223372036854775808.0,
            true,
            "beyond maxinteger string"
        )]
        [Arguments("'0'", 0.0, true, "zero string")]
        [Arguments("'abc'", 0.0, false, "non-numeric string")]
        [Arguments("''", 0.0, false, "empty string")]
        public async Task TonumberIntegerBoundaryStrings(
            string expression,
            double expectedValue,
            bool shouldSucceed,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return tonumber({expression})");

            if (shouldSucceed)
            {
                await Assert
                    .That(result.Number)
                    .IsEqualTo(expectedValue)
                    .Because(
                        $"tonumber({expression}) [{description}] should equal {expectedValue}"
                    );
            }
            else
            {
                await Assert
                    .That(result.IsNil())
                    .IsTrue()
                    .Because($"tonumber({expression}) [{description}] should return nil");
            }
        }

        #endregion

        #region Helpers

        private static Script CreateScript()
        {
            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = LuaCompatibilityVersion.Lua54,
            };
            return new Script(CoreModules.PresetComplete, options);
        }

        #endregion
    }
}
