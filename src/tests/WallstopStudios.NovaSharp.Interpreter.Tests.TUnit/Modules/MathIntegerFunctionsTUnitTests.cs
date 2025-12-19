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
    /// These tests require Lua 5.3+ for math.maxinteger, math.mininteger, and integer subtype.
    /// </summary>
    public sealed class MathIntegerFunctionsTUnitTests
    {
        /// <summary>
        /// Tests math.abs with integer arguments preserves integer subtype (Lua 5.3+).
        /// Per Lua 5.4 §6.7, math.abs returns an integer when given an integer argument.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1", 1L, "one")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1", 1L, "negative one")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-math.maxinteger",
            9223372036854775807L,
            "negative maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "100", 100L, "positive integer")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-100", 100L, "negative integer")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger - 1",
            9223372036854775806L,
            "maxinteger - 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-(math.maxinteger - 1)",
            9223372036854775806L,
            "negative (maxinteger - 1)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1", 1L, "one")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1", 1L, "negative one")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-math.maxinteger",
            9223372036854775807L,
            "negative maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "100", 100L, "positive integer")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-100", 100L, "negative integer")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger - 1",
            9223372036854775806L,
            "maxinteger - 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-(math.maxinteger - 1)",
            9223372036854775806L,
            "negative (maxinteger - 1)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1", 1L, "one")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1", 1L, "negative one")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-math.maxinteger",
            9223372036854775807L,
            "negative maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "100", 100L, "positive integer")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-100", 100L, "negative integer")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger - 1",
            9223372036854775806L,
            "maxinteger - 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-(math.maxinteger - 1)",
            9223372036854775806L,
            "negative (maxinteger - 1)"
        )]
        public async Task MathAbsIntegerBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathAbsMinintegerWraps(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua53, "1.5", 1.5, "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.5", 1.5, "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua53, "0.0", 0.0, "float zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-0.0", 0.0, "negative float zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1e10", 1e10, "scientific notation")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1e10", 1e10, "negative scientific notation")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.huge",
            double.PositiveInfinity,
            "huge positive"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-math.huge",
            double.PositiveInfinity,
            "huge negative"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.5", 1.5, "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.5", 1.5, "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua54, "0.0", 0.0, "float zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-0.0", 0.0, "negative float zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1e10", 1e10, "scientific notation")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1e10", 1e10, "negative scientific notation")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.huge",
            double.PositiveInfinity,
            "huge positive"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-math.huge",
            double.PositiveInfinity,
            "huge negative"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.5", 1.5, "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.5", 1.5, "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua55, "0.0", 0.0, "float zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-0.0", 0.0, "negative float zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1e10", 1e10, "scientific notation")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1e10", 1e10, "negative scientific notation")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.huge",
            double.PositiveInfinity,
            "huge positive"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-math.huge",
            double.PositiveInfinity,
            "huge negative"
        )]
        public async Task MathAbsFloatSubtype(
            LuaCompatibilityVersion version,
            string expression,
            double expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return math.abs({expression})");

            // Verify the value is correct
            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.abs({expression}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests that math.abs preserves float subtype for fractional values.
        /// IsFloat is a Lua 5.3+ concept for numeric subtype distinction.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "1.5", "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.5", "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-3.14159", "negative pi")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.5", "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.5", "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-3.14159", "negative pi")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.5", "positive float")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.5", "negative float")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-3.14159", "negative pi")]
        public async Task MathAbsPreservesFloatSubtype(
            LuaCompatibilityVersion version,
            string expression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MathAbsMinintegerConsistentWithNegation(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger - 10",
            9223372036854775797L,
            "maxinteger - 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "-(math.maxinteger - 10)",
            9223372036854775797L,
            "negative (maxinteger - 10)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger + 10",
            9223372036854775798L,
            "mininteger + 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger + 1",
            9223372036854775807L,
            "mininteger + 1 (equals maxinteger)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger - 10",
            9223372036854775797L,
            "maxinteger - 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "-(math.maxinteger - 10)",
            9223372036854775797L,
            "negative (maxinteger - 10)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger + 10",
            9223372036854775798L,
            "mininteger + 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger + 1",
            9223372036854775807L,
            "mininteger + 1 (equals maxinteger)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger - 10",
            9223372036854775797L,
            "maxinteger - 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "-(math.maxinteger - 10)",
            9223372036854775797L,
            "negative (maxinteger - 10)"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger + 10",
            9223372036854775798L,
            "mininteger + 10"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger + 1",
            9223372036854775807L,
            "mininteger + 1 (equals maxinteger)"
        )]
        public async Task MathAbsNearBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

        /// <summary>
        /// Tests math.floor preserves integer values at boundaries.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1.9", 1L, "1.9 floors to 1")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.1", -2L, "-1.1 floors to -2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1e18", 1000000000000000000L, "1e18")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.9", 1L, "1.9 floors to 1")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.1", -2L, "-1.1 floors to -2")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1e18", 1000000000000000000L, "1e18")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.9", 1L, "1.9 floors to 1")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.1", -2L, "-1.1 floors to -2")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1e18", 1000000000000000000L, "1e18")]
        public async Task MathFloorIntegerBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1.1", 2L, "1.1 ceils to 2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.9", -1L, "-1.9 ceils to -1")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.1", 2L, "1.1 ceils to 2")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.9", -1L, "-1.9 ceils to -1")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            9223372036854775807L,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger",
            -9223372036854775808L,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0", 0L, "zero")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.1", 2L, "1.1 ceils to 2")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.9", -1L, "-1.9 ceils to -1")]
        public async Task MathCeilIntegerBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return math.ceil({expression})");

            await Assert
                .That(result.Number)
                .IsEqualTo((double)expected)
                .Because($"math.ceil({expression}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests math.fmod with boundary values.
        /// Note: math.fmod operates on doubles, so maxinteger loses precision and becomes 2^63 (even).
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.maxinteger", "1", 0.0, "maxinteger % 1")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            "2",
            0.0,
            "maxinteger % 2 (rounds to 2^63, which is even)"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.mininteger", "1", 0.0, "mininteger % 1")]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.mininteger", "2", 0.0, "mininteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "10", "3", 1.0, "10 % 3")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-10", "3", -1.0, "-10 % 3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.maxinteger", "1", 0.0, "maxinteger % 1")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            "2",
            0.0,
            "maxinteger % 2 (rounds to 2^63, which is even)"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.mininteger", "1", 0.0, "mininteger % 1")]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.mininteger", "2", 0.0, "mininteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua54, "10", "3", 1.0, "10 % 3")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-10", "3", -1.0, "-10 % 3")]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.maxinteger", "1", 0.0, "maxinteger % 1")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            "2",
            0.0,
            "maxinteger % 2 (rounds to 2^63, which is even)"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.mininteger", "1", 0.0, "mininteger % 1")]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.mininteger", "2", 0.0, "mininteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua55, "10", "3", 1.0, "10 % 3")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-10", "3", -1.0, "-10 % 3")]
        public async Task MathFmodIntegerBoundaries(
            LuaCompatibilityVersion version,
            string x,
            string y,
            double expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return math.fmod({x}, {y})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.fmod({x}, {y}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests math.modf returns correct integral and fractional parts.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger",
            9223372036854775807.0,
            0.0,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger",
            -9223372036854775808.0,
            0.0,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "1.5", 1.0, 0.5, "1.5")]
        [Arguments(LuaCompatibilityVersion.Lua53, "-1.5", -1.0, -0.5, "-1.5")]
        [Arguments(LuaCompatibilityVersion.Lua53, "0", 0.0, 0.0, "zero")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger",
            9223372036854775807.0,
            0.0,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger",
            -9223372036854775808.0,
            0.0,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "1.5", 1.0, 0.5, "1.5")]
        [Arguments(LuaCompatibilityVersion.Lua54, "-1.5", -1.0, -0.5, "-1.5")]
        [Arguments(LuaCompatibilityVersion.Lua54, "0", 0.0, 0.0, "zero")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger",
            9223372036854775807.0,
            0.0,
            "maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger",
            -9223372036854775808.0,
            0.0,
            "mininteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "1.5", 1.0, 0.5, "1.5")]
        [Arguments(LuaCompatibilityVersion.Lua55, "-1.5", -1.0, -0.5, "-1.5")]
        [Arguments(LuaCompatibilityVersion.Lua55, "0", 0.0, 0.0, "zero")]
        public async Task MathModfIntegerBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            double expectedIntegral,
            double expectedFractional,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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

        /// <summary>
        /// Tests math.max correctly compares boundary values.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger, math.mininteger",
            9223372036854775807.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger, math.maxinteger",
            9223372036854775807.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger, 0",
            9223372036854775807.0,
            "maxinteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0, math.mininteger", 0.0, "0 vs mininteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger, math.maxinteger - 1",
            9223372036854775807.0,
            "maxinteger vs maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger, math.mininteger",
            9223372036854775807.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger, math.maxinteger",
            9223372036854775807.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger, 0",
            9223372036854775807.0,
            "maxinteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0, math.mininteger", 0.0, "0 vs mininteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger, math.maxinteger - 1",
            9223372036854775807.0,
            "maxinteger vs maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger, math.mininteger",
            9223372036854775807.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger, math.maxinteger",
            9223372036854775807.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger, 0",
            9223372036854775807.0,
            "maxinteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0, math.mininteger", 0.0, "0 vs mininteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger, math.maxinteger - 1",
            9223372036854775807.0,
            "maxinteger vs maxinteger-1"
        )]
        public async Task MathMaxIntegerBoundaries(
            LuaCompatibilityVersion version,
            string args,
            double expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger, math.mininteger",
            -9223372036854775808.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger, math.maxinteger",
            -9223372036854775808.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger, 0",
            -9223372036854775808.0,
            "mininteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0, math.maxinteger", 0.0, "0 vs maxinteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger, math.mininteger + 1",
            -9223372036854775808.0,
            "mininteger vs mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger, math.mininteger",
            -9223372036854775808.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger, math.maxinteger",
            -9223372036854775808.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger, 0",
            -9223372036854775808.0,
            "mininteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0, math.maxinteger", 0.0, "0 vs maxinteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger, math.mininteger + 1",
            -9223372036854775808.0,
            "mininteger vs mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger, math.mininteger",
            -9223372036854775808.0,
            "maxinteger vs mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger, math.maxinteger",
            -9223372036854775808.0,
            "mininteger vs maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger, 0",
            -9223372036854775808.0,
            "mininteger vs 0"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0, math.maxinteger", 0.0, "0 vs maxinteger")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger, math.mininteger + 1",
            -9223372036854775808.0,
            "mininteger vs mininteger+1"
        )]
        public async Task MathMinIntegerBoundaries(
            LuaCompatibilityVersion version,
            string args,
            double expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return math.min({args})");

            await Assert
                .That(result.Number)
                .IsEqualTo(expected)
                .Because($"math.min({args}) [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests integer division with boundary values.
        /// Integer division (//) was introduced in Lua 5.3.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger // 1",
            9223372036854775807L,
            "maxinteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger // 1",
            -9223372036854775808L,
            "mininteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger // math.maxinteger",
            1L,
            "maxinteger // maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger // math.mininteger",
            1L,
            "mininteger // mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger // 2",
            4611686018427387903L,
            "maxinteger // 2"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger // 2",
            -4611686018427387904L,
            "mininteger // 2"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "-10 // 3", -4L, "-10 // 3 (floor division)")]
        [Arguments(LuaCompatibilityVersion.Lua53, "10 // -3", -4L, "10 // -3 (floor division)")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger // 1",
            9223372036854775807L,
            "maxinteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger // 1",
            -9223372036854775808L,
            "mininteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger // math.maxinteger",
            1L,
            "maxinteger // maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger // math.mininteger",
            1L,
            "mininteger // mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger // 2",
            4611686018427387903L,
            "maxinteger // 2"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger // 2",
            -4611686018427387904L,
            "mininteger // 2"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "-10 // 3", -4L, "-10 // 3 (floor division)")]
        [Arguments(LuaCompatibilityVersion.Lua54, "10 // -3", -4L, "10 // -3 (floor division)")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger // 1",
            9223372036854775807L,
            "maxinteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger // 1",
            -9223372036854775808L,
            "mininteger // 1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger // math.maxinteger",
            1L,
            "maxinteger // maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger // math.mininteger",
            1L,
            "mininteger // mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger // 2",
            4611686018427387903L,
            "maxinteger // 2"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger // 2",
            -4611686018427387904L,
            "mininteger // 2"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "-10 // 3", -4L, "-10 // 3 (floor division)")]
        [Arguments(LuaCompatibilityVersion.Lua55, "10 // -3", -4L, "10 // -3 (floor division)")]
        public async Task IntegerDivisionBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.LuaNumber.AsInteger)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should equal {expected}");
        }

        /// <summary>
        /// Tests that mininteger // -1 wraps to mininteger (two's complement behavior).
        /// In two's complement, -mininteger overflows back to mininteger.
        /// Integer division (//) was introduced in Lua 5.3.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IntegerDivisionMinintegerByNegativeOneWraps(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // mininteger // -1 = mininteger due to two's complement wrapping
            // |mininteger| would be 2^63 which can't be represented, so it wraps back
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
                .Because("mininteger // -1 wraps to mininteger per Lua spec")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests integer division by zero throws appropriate error.
        /// Integer division (//) was introduced in Lua 5.3.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IntegerDivisionByZeroThrows(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(() => script.DoString("return 1 // 0"))
                .Throws<ScriptRuntimeException>()
                .Because("integer division by zero should throw");
        }

        /// <summary>
        /// Tests modulo with boundary values.
        /// <summary>
        /// Tests modulo with boundary values.
        /// </summary>
        [Test]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.maxinteger % 2", 1L, "maxinteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua53, "math.mininteger % 2", 0L, "mininteger % 2")]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger % math.maxinteger",
            0L,
            "maxinteger % maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "-10 % 3", 2L, "-10 % 3 (Lua floor mod)")]
        [Arguments(LuaCompatibilityVersion.Lua53, "10 % -3", -2L, "10 % -3 (Lua floor mod)")]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.maxinteger % 2", 1L, "maxinteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua54, "math.mininteger % 2", 0L, "mininteger % 2")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger % math.maxinteger",
            0L,
            "maxinteger % maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "-10 % 3", 2L, "-10 % 3 (Lua floor mod)")]
        [Arguments(LuaCompatibilityVersion.Lua54, "10 % -3", -2L, "10 % -3 (Lua floor mod)")]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.maxinteger % 2", 1L, "maxinteger % 2")]
        [Arguments(LuaCompatibilityVersion.Lua55, "math.mininteger % 2", 0L, "mininteger % 2")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger % math.maxinteger",
            0L,
            "maxinteger % maxinteger"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "-10 % 3", 2L, "-10 % 3 (Lua floor mod)")]
        [Arguments(LuaCompatibilityVersion.Lua55, "10 % -3", -2L, "10 % -3 (Lua floor mod)")]
        public async Task ModuloBoundaries(
            LuaCompatibilityVersion version,
            string expression,
            long expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [Arguments(LuaCompatibilityVersion.Lua53)]
        [Arguments(LuaCompatibilityVersion.Lua54)]
        [Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ModuloByZeroThrows(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(() => script.DoString("return 1 % 0"))
                .Throws<ScriptRuntimeException>()
                .Because("modulo by zero should throw in Lua 5.3+");
        }

        /// <summary>
        /// Tests comparison operators with boundary integer values.
        /// This includes critical edge cases where double precision would fail:
        /// - maxinteger and maxinteger-1 differ by 1, but (double)maxinteger == (double)(maxinteger-1)
        /// - Similar issues exist at the mininteger boundary
        /// math.maxinteger/mininteger require Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger > math.mininteger",
            true,
            "maxinteger > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger < math.maxinteger",
            true,
            "mininteger < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger >= math.maxinteger",
            true,
            "maxinteger >= maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger <= math.mininteger",
            true,
            "mininteger <= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger == 9223372036854775807",
            true,
            "maxinteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger == -9223372036854775808",
            true,
            "mininteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger ~= math.mininteger",
            true,
            "maxinteger ~= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger > math.maxinteger - 1",
            true,
            "maxinteger > maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger - 1 < math.maxinteger",
            true,
            "maxinteger-1 < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger ~= math.maxinteger - 1",
            true,
            "maxinteger ~= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger < math.mininteger + 1",
            true,
            "mininteger < mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger + 1 > math.mininteger",
            true,
            "mininteger+1 > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger ~= math.mininteger + 1",
            true,
            "mininteger ~= mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger - 1 >= math.maxinteger - 1",
            true,
            "maxinteger-1 >= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger - 1 <= math.maxinteger - 1",
            true,
            "maxinteger-1 <= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger > math.mininteger",
            true,
            "maxinteger > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger < math.maxinteger",
            true,
            "mininteger < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger >= math.maxinteger",
            true,
            "maxinteger >= maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger <= math.mininteger",
            true,
            "mininteger <= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger == 9223372036854775807",
            true,
            "maxinteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger == -9223372036854775808",
            true,
            "mininteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger ~= math.mininteger",
            true,
            "maxinteger ~= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger > math.maxinteger - 1",
            true,
            "maxinteger > maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger - 1 < math.maxinteger",
            true,
            "maxinteger-1 < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger ~= math.maxinteger - 1",
            true,
            "maxinteger ~= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger < math.mininteger + 1",
            true,
            "mininteger < mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger + 1 > math.mininteger",
            true,
            "mininteger+1 > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger ~= math.mininteger + 1",
            true,
            "mininteger ~= mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger - 1 >= math.maxinteger - 1",
            true,
            "maxinteger-1 >= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger - 1 <= math.maxinteger - 1",
            true,
            "maxinteger-1 <= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger > math.mininteger",
            true,
            "maxinteger > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger < math.maxinteger",
            true,
            "mininteger < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger >= math.maxinteger",
            true,
            "maxinteger >= maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger <= math.mininteger",
            true,
            "mininteger <= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger == 9223372036854775807",
            true,
            "maxinteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger == -9223372036854775808",
            true,
            "mininteger == literal"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger ~= math.mininteger",
            true,
            "maxinteger ~= mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger > math.maxinteger - 1",
            true,
            "maxinteger > maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger - 1 < math.maxinteger",
            true,
            "maxinteger-1 < maxinteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger ~= math.maxinteger - 1",
            true,
            "maxinteger ~= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger < math.mininteger + 1",
            true,
            "mininteger < mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger + 1 > math.mininteger",
            true,
            "mininteger+1 > mininteger"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger ~= math.mininteger + 1",
            true,
            "mininteger ~= mininteger+1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger - 1 >= math.maxinteger - 1",
            true,
            "maxinteger-1 >= maxinteger-1"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger - 1 <= math.maxinteger - 1",
            true,
            "maxinteger-1 <= maxinteger-1"
        )]
        public async Task ComparisonWithBoundaryValues(
            LuaCompatibilityVersion version,
            string expression,
            bool expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.Boolean)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should be {expected}");
        }

        /// <summary>
        /// Tests that integer and float comparisons work correctly at boundaries.
        /// math.maxinteger/mininteger require Lua 5.3+.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.maxinteger == math.maxinteger + 0.0",
            true,
            "maxinteger == maxinteger as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "math.mininteger == math.mininteger + 0.0",
            true,
            "mininteger == mininteger as float"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "0 == 0.0", true, "0 == 0.0")]
        [Arguments(LuaCompatibilityVersion.Lua53, "1 == 1.0", true, "1 == 1.0")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.maxinteger == math.maxinteger + 0.0",
            true,
            "maxinteger == maxinteger as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "math.mininteger == math.mininteger + 0.0",
            true,
            "mininteger == mininteger as float"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "0 == 0.0", true, "0 == 0.0")]
        [Arguments(LuaCompatibilityVersion.Lua54, "1 == 1.0", true, "1 == 1.0")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.maxinteger == math.maxinteger + 0.0",
            true,
            "maxinteger == maxinteger as float"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "math.mininteger == math.mininteger + 0.0",
            true,
            "mininteger == mininteger as float"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "0 == 0.0", true, "0 == 0.0")]
        [Arguments(LuaCompatibilityVersion.Lua55, "1 == 1.0", true, "1 == 1.0")]
        public async Task IntegerFloatComparison(
            LuaCompatibilityVersion version,
            string expression,
            bool expected,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString($"return {expression}");

            await Assert
                .That(result.Boolean)
                .IsEqualTo(expected)
                .Because($"{expression} [{description}] should be {expected}");
        }

        /// <summary>
        /// Tests tonumber with integer boundary strings.
        /// These boundary tests use values at the limits of Lua 5.3+ integer range.
        /// </summary>
        [Test]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "'9223372036854775807'",
            9223372036854775807.0,
            true,
            "maxinteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "'-9223372036854775808'",
            -9223372036854775808.0,
            true,
            "mininteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua53,
            "'9223372036854775808'",
            9223372036854775808.0,
            true,
            "beyond maxinteger string"
        )]
        [Arguments(LuaCompatibilityVersion.Lua53, "'0'", 0.0, true, "zero string")]
        [Arguments(LuaCompatibilityVersion.Lua53, "'abc'", 0.0, false, "non-numeric string")]
        [Arguments(LuaCompatibilityVersion.Lua53, "''", 0.0, false, "empty string")]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "'9223372036854775807'",
            9223372036854775807.0,
            true,
            "maxinteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "'-9223372036854775808'",
            -9223372036854775808.0,
            true,
            "mininteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua54,
            "'9223372036854775808'",
            9223372036854775808.0,
            true,
            "beyond maxinteger string"
        )]
        [Arguments(LuaCompatibilityVersion.Lua54, "'0'", 0.0, true, "zero string")]
        [Arguments(LuaCompatibilityVersion.Lua54, "'abc'", 0.0, false, "non-numeric string")]
        [Arguments(LuaCompatibilityVersion.Lua54, "''", 0.0, false, "empty string")]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "'9223372036854775807'",
            9223372036854775807.0,
            true,
            "maxinteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "'-9223372036854775808'",
            -9223372036854775808.0,
            true,
            "mininteger string"
        )]
        [Arguments(
            LuaCompatibilityVersion.Lua55,
            "'9223372036854775808'",
            9223372036854775808.0,
            true,
            "beyond maxinteger string"
        )]
        [Arguments(LuaCompatibilityVersion.Lua55, "'0'", 0.0, true, "zero string")]
        [Arguments(LuaCompatibilityVersion.Lua55, "'abc'", 0.0, false, "non-numeric string")]
        [Arguments(LuaCompatibilityVersion.Lua55, "''", 0.0, false, "empty string")]
        public async Task TonumberIntegerBoundaryStrings(
            LuaCompatibilityVersion version,
            string expression,
            double expectedValue,
            bool shouldSucceed,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
    }
}
