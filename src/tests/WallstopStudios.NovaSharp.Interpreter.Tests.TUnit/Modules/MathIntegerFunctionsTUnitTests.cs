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
        /// Tests math.abs with boundary integer values.
        /// </summary>
        [Test]
        [Arguments("math.maxinteger", 9223372036854775807L, "maxinteger")]
        [Arguments("0", 0L, "zero")]
        [Arguments("1", 1L, "one")]
        [Arguments("-1", 1L, "negative one")]
        [Arguments("-math.maxinteger", 9223372036854775807L, "negative maxinteger")]
        public async Task MathAbsIntegerBoundaries(
            string expression,
            long expected,
            string description
        )
        {
            Script script = CreateScript();
            DynValue result = script.DoString($"return math.abs({expression})");

            await Assert
                .That(result.Number)
                .IsEqualTo((double)expected)
                .Because($"math.abs({expression}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests math.abs(mininteger) - a special case because |mininteger| > maxinteger.
        /// In Lua, this wraps back to mininteger for integer mode.
        /// </summary>
        [Test]
        public async Task MathAbsMinintegerWraps()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.abs(math.mininteger)");

            // |mininteger| = 2^63 which overflows, wrapping back to mininteger
            // (same behavior as -mininteger)
            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(long.MinValue)
                .Because("math.abs(mininteger) should wrap to mininteger (overflow)");
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
        /// Tests modulo by zero throws appropriate error.
        /// </summary>
        [Test]
        public async Task ModuloByZeroThrows()
        {
            Script script = CreateScript();

            await Assert
                .That(() => script.DoString("return 1 % 0"))
                .Throws<ScriptRuntimeException>()
                .Because("modulo by zero should throw");
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
