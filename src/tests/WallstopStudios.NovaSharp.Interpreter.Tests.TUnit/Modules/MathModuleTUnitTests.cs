namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class MathModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LogUsesDefaultBaseWhenOmitted(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.log(8)");

            await Assert
                .That(result.Number)
                .IsEqualTo(Math.Log(8d))
                .Within(1e-12)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LogSupportsCustomBase(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.log(8, 2)");

            await Assert.That(result.Number).IsEqualTo(3d).Within(1e-12).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.pow correctly computes exponentiation.
        /// math.pow was deprecated in Lua 5.3 and removed in Lua 5.5.
        /// Use the ^ operator instead in Lua 5.5+.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionRange(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua54)]
        public async Task PowHandlesLargeExponent(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.pow(10, 6)");

            await Assert
                .That(result.Number)
                .IsEqualTo(1_000_000d)
                .Within(1e-6)
                .Because($"math.pow(10, 6) should return 1000000 in {version}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModfSplitsIntegerAndFractionalComponents(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.modf(-3.25)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(-3d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(-0.25d)
                .Within(1e-12)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MaxAggregatesAcrossArguments(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.max(-1, 5, 12, 3)");

            await Assert.That(result.Number).IsEqualTo(12d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MinAggregatesAcrossArguments(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.min(-1, 5, 12, 3)");

            await Assert.That(result.Number).IsEqualTo(-1d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.ldexp correctly computes mantissa * 2^exp.
        /// math.ldexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LdexpCombinesMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return math.ldexp(0.5, 3)");

            await Assert.That(result.Number).IsEqualTo(4d).Within(1e-12).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpAvailableInAllVersions(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.frexp(8)");

            // math.frexp(8) returns m=0.5, e=4 (8 = 0.5 * 2^4)
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(0.5d)
                .Within(1e-12)
                .Because($"math.frexp mantissa should be 0.5 in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(4d)
                .Because($"math.frexp exponent should be 4 in {version}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SqrtOfNegativeNumberReturnsNaN(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.sqrt(-1)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.pow returns infinity for exponents that exceed double range.
        /// math.pow was deprecated in Lua 5.3 and removed in Lua 5.5.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua54)]
        public async Task PowWithLargeExponentReturnsInfinity(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.pow(10, 309)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .Because($"math.pow(10, 309) should return +inf in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.pow is NOT available in Lua 5.5 (it was removed).
        /// In Lua 5.5, use the ^ operator instead: 10^6.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PowIsNilInLua55(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua55);
            DynValue result = script.DoString("return math.pow");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    "math.pow was removed in Lua 5.5. Use ^ operator instead. "
                        + $"Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that the ^ operator (exponentiation) still works in Lua 5.5.
        /// This is the replacement for the deprecated math.pow function.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ExponentiationOperatorWorksInLua55(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua55);
            DynValue result = script.DoString("return 10 ^ 6");

            await Assert
                .That(result.Number)
                .IsEqualTo(1_000_000d)
                .Within(1e-6)
                .Because("10 ^ 6 should return 1000000 in Lua 5.5")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomSeedProducesDeterministicSequence(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue firstSequence = script.DoString(
                @"
                math.randomseed(1337)
                return math.random(1, 100), math.random(1, 100), math.random()
                "
            );

            DynValue secondSequence = script.DoString(
                @"
                math.randomseed(1337)
                return math.random(1, 100), math.random(1, 100), math.random()
                "
            );

            await Assert.That(firstSequence.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(secondSequence.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(firstSequence.Tuple[0].Number)
                .IsEqualTo(secondSequence.Tuple[0].Number);
            await Assert
                .That(firstSequence.Tuple[1].Number)
                .IsEqualTo(secondSequence.Tuple[1].Number);
            await Assert
                .That(firstSequence.Tuple[2].Number)
                .IsEqualTo(secondSequence.Tuple[2].Number)
                .Within(1e-12);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task TypeDistinguishesIntegersAndFloats(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString("return math.type(5), math.type(3.14)");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("integer").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("float").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerConvertsNumericStrings(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger('42')");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerReturnsNilWhenValueNotIntegral(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(3.5)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerThrowsWhenArgumentMissing(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.tointeger()")
            );

            await Assert.That(exception.Message).Contains("got no value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerReturnsNilForUnsupportedType(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Per Lua 5.3+ spec, math.tointeger returns nil for non-number/non-string types
            // (boolean, table, function, userdata, etc.) - it does NOT throw an error.
            // Reference: Lua 5.3 Manual §6.7
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString("return math.tointeger(true)");

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerReturnsNilForTable(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger({})");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerReturnsNilForFunction(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(function() end)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ToIntegerReturnsNilForNil(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(nil)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task UltPerformsUnsignedComparison(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue tuple = script.DoString("return math.ult(0, -1), math.ult(-1, 0)");

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns (0, 0) for input 0.
        /// math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpWithZeroReturnsZeroMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return math.frexp(0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns (0, 0) for input -0.0.
        /// math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpWithNegativeZeroReturnsZeroMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return math.frexp(-0.0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns negative mantissa for negative input.
        /// math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpWithNegativeNumberReturnsNegativeMantissa(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return math.frexp(-8)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsLessThan(0d).ConfigureAwait(false);
            // m * 2^e should equal -8
            double m = result.Tuple[0].Number;
            double e = result.Tuple[1].Number;
            await Assert
                .That(m * Math.Pow(2, e))
                .IsEqualTo(-8d)
                .Within(1e-12)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp handles subnormal (denormalized) numbers correctly.
        /// math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpWithSubnormalNumberHandlesExponentCorrectly(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Subnormal (denormalized) numbers have exponent bits = 0
            // The smallest positive subnormal is approximately 4.94e-324
            Script script = CreateScript(version);
            double subnormal = double.Epsilon; // Smallest positive subnormal
            script.Globals["subnormal"] = subnormal;
            DynValue result = script.DoString("return math.frexp(subnormal)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            double m = result.Tuple[0].Number;
            double e = result.Tuple[1].Number;
            // Verify m is in range [0.5, 1)
            await Assert.That(Math.Abs(m)).IsGreaterThanOrEqualTo(0.5).ConfigureAwait(false);
            await Assert.That(Math.Abs(m)).IsLessThan(1.0).ConfigureAwait(false);
            // m * 2^e should equal the original subnormal
            await Assert
                .That(m * Math.Pow(2, e))
                .IsEqualTo(subnormal)
                .Within(1e-330)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns mantissa in [0.5, 1) for positive numbers.
        /// math.frexp is available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpWithPositiveNumberReturnsMantissaInExpectedRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return math.frexp(16)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            double m = result.Tuple[0].Number;
            double e = result.Tuple[1].Number;
            // Per Lua 5.4 spec, m is in [0.5, 1) for normal numbers
            await Assert.That(m).IsGreaterThanOrEqualTo(0.5).ConfigureAwait(false);
            await Assert.That(m).IsLessThan(1.0).ConfigureAwait(false);
            await Assert
                .That(m * Math.Pow(2, e))
                .IsEqualTo(16d)
                .Within(1e-12)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that frexp and ldexp are inverse operations.
        /// Both functions are available in all Lua versions (5.1-5.5).
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FrexpAndLdexpRoundTrip(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local m, e = math.frexp(123.456)
                return math.ldexp(m, e)
                "
            );

            await Assert.That(result.Number).IsEqualTo(123.456).Within(1e-12).ConfigureAwait(false);
        }

        // ========================================
        // math.floor - Integer promotion tests (Lua 5.3+)
        // ========================================

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorReturnsIntegerForPositiveFloat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(3.7)");

            await Assert.That(result.Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorReturnsIntegerForNegativeFloat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(-3.7)");

            await Assert.That(result.Number).IsEqualTo(-4d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorPreservesIntegerInput(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorReturnsIntegerTypeReportedByMathType(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.floor(3.7))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloorHandlesInfinityAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(1/0)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloorHandlesNaNAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorPreservesIntegerZero(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorHandlesNegativeZeroAsInteger(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(-0.0)");

            // Negative zero floored is 0, which should be returned as integer
            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        // ========================================
        // math.ceil - Integer promotion tests (Lua 5.3+)
        // ========================================

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilReturnsIntegerForPositiveFloat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(3.2)");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilReturnsIntegerForNegativeFloat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(-3.2)");

            await Assert.That(result.Number).IsEqualTo(-3d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilPreservesIntegerInput(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilReturnsIntegerTypeReportedByMathType(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.ceil(3.2))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CeilHandlesInfinityAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(1/0)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CeilHandlesNaNAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilPreservesIntegerZero(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task CeilHandlesFloatPointFiveCorrectly(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(0.5)");

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FloorResultCanBeUsedInStringFormat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Verify integer promotion integrates with string.format %d for valid integer values.
            // Note: math.maxinteger + 0.5 rounds to 2^63 which is OUTSIDE integer range,
            // so we test with a valid case instead.
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.format('%d', math.floor(3.7))");

            // math.floor(3.7) = 3 (promoted to integer subtype in Lua 5.3+)
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("3").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task FloorResultForMaxintegerPlusHalfThrowsInStringFormat(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Verify that math.floor(math.maxinteger + 0.5) correctly returns a float
            // (not an integer), and that string.format('%d', ...) correctly rejects it.
            // Reference: Lua 5.4 Manual §6.7 - math.floor returns integer only when result fits.
            // Note: math.maxinteger only exists in Lua 5.3+
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

        // ==========================================================================
        // math.random integer representation tests (Lua 5.3+ semantics)
        // ==========================================================================

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RandomErrorsOnNonIntegerArgLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(1.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task RandomTruncatesNonIntegerArgLua51And52(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(1.9) should truncate to 1, then return 1 (since random(1) returns 1)
            DynValue result = script.DoString("return math.random(1.9)");

            // Result should be 1 (truncated from 1.9)
            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RandomErrorsOnNaNLua53Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(0/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua52)]
        public async Task RandomSucceedsOnInfinityLua52Only(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Lua 5.2 ONLY: math.random(inf) SUCCEEDS because the floating-point comparison
            // 1.0 <= inf is TRUE per IEEE 754, so the interval check passes.
            // The result is garbage due to integer overflow, but no error is thrown.
            //
            // Lua 5.1 is DIFFERENT: it converts to integer FIRST via luaL_checkint(), so
            // inf → LONG_MIN, then 1 <= LONG_MIN is FALSE → error
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString("return math.random(1/0)");

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task RandomErrorsOnInfinityLua51(Compatibility.LuaCompatibilityVersion version)
        {
            // Lua 5.1: math.random(inf) ERRORS because luaL_checkint() converts to integer FIRST,
            // so inf → LONG_MIN, then 1 <= LONG_MIN is FALSE → error.
            //
            // This is different from Lua 5.2 which compares floats first.
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(1/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task RandomErrorsOnNegativeInfinityLua51And52(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Lua 5.1/5.2: math.random(-inf) THROWS because the floating-point comparison
            // 1.0 <= -inf is FALSE per IEEE 754, so the interval check fails.
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(-1/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task RandomErrorsOnNaNLua51And52(Compatibility.LuaCompatibilityVersion version)
        {
            // Lua 5.1/5.2: math.random(nan) THROWS because the floating-point comparison
            // 1.0 <= nan is FALSE per IEEE 754 (NaN comparisons always return false).
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(0/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RandomErrorsOnInfinityLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(1/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RandomErrorsOnSecondNonIntegerArgLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(1, 2.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task RandomAcceptsIntegralFloat(Compatibility.LuaCompatibilityVersion version)
        {
            // Integral floats like 2.0 should be accepted in Lua 5.3+
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(1, 2.0) should work since 2.0 has integer representation
            DynValue result = script.DoString("return math.random(1, 2.0)");

            await Assert.That(result.Number).IsGreaterThanOrEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(2d).ConfigureAwait(false);
        }

        // ==========================================================================
        // math.randomseed integer representation tests (Lua 5.4+ only)
        // ==========================================================================

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task RandomseedErrorsOnNonIntegerArgLua54Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("math.randomseed(1.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task RandomseedAcceptsNonIntegerArgLua51To53(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // Should not throw in Lua 5.1-5.3
            DynValue result = script.DoString("math.randomseed(1.5); return true");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task RandomseedErrorsOnNaNLua54Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("math.randomseed(0/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        // ==========================================================================
        // math.random edge case and boundary tests
        // ==========================================================================

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task RandomWithZeroUpperBoundThrowsErrorLua51To53(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Lua 5.1-5.3: math.random(0) throws "interval is empty"
            // Lua 5.4+: math.random(0) returns a random integer
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.random(0)")
            );
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task RandomWithZeroReturnsRandomIntegerLua54Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Lua 5.4+: math.random(0) returns a random integer (per Lua 5.4 §6.7)
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString("return math.random(0)");
            // Just verify it returns a number (the value is random)
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithNegativeUpperBoundThrowsError(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // All versions: math.random(-5) throws "interval is empty"
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.random(-5)")
            );
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithInvertedRangeThrowsError(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.random(10, 5)")
            );
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithSingleValueRangeReturnsValue(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(5, 5) should always return 5
            DynValue result = script.DoString("return math.random(5, 5)");
            await Assert.That(result.Number).IsEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithOneReturnsOne(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(1) should always return 1
            DynValue result = script.DoString("return math.random(1)");
            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithNegativeRangeReturnsValueInRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(-10, -5) should return value in [-10, -5]
            DynValue result = script.DoString("return math.random(-10, -5)");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(-10d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(-5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithSpanningZeroReturnsValueInRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(-5, 5) should return value in [-5, 5]
            DynValue result = script.DoString("return math.random(-5, 5)");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(-5d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomNoArgsReturnsFloatInZeroToOneRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random() should return a float in [0, 1)
            DynValue result = script.DoString("return math.random()");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThan(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RandomWithLargeRangeReturnsValueInRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // Test with a large but valid range
            DynValue result = script.DoString("return math.random(1, 1000000)");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(1000000d).ConfigureAwait(false);
        }

        // ==========================================================================
        // Data-driven test for version-specific integer representation errors
        // ==========================================================================

        /// <summary>
        /// Consolidated data-driven test for expressions that should error with
        /// "number has no integer representation" in Lua 5.3+.
        /// Uses LuaTestMatrix to automatically test all Lua 5.3+ versions.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            "math.random(1.5)",
            "math.random(0/0)",
            "math.random(1/0)",
            "math.random(1, 2.5)",
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task RandomIntegerRepresentationErrorCases(
            LuaCompatibilityVersion version,
            string luaExpression
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString(luaExpression)).ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"Expression '{luaExpression}' should throw integer representation error in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.randomseed integer representation errors in Lua 5.4+.
        /// math.randomseed requires integer arguments starting in Lua 5.4.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            "math.randomseed(1.5)",
            "math.randomseed(0/0)",
            MinimumVersion = LuaCompatibilityVersion.Lua54
        )]
        public async Task RandomseedIntegerRepresentationErrorCases(
            LuaCompatibilityVersion version,
            string luaExpression
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString(luaExpression)).ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"Expression '{luaExpression}' should throw integer representation error in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.random accepting non-integer arguments in Lua 5.1/5.2.
        /// In these versions, fractional values are silently truncated.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            "math.random(2.9)",
            "math.randomseed(1.5)",
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task RandomAcceptsNonIntegerInLua51And52(
            LuaCompatibilityVersion version,
            string luaExpression
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // Should not throw - expression should execute successfully
            ScriptRuntimeException caughtException = null;
            try
            {
                script.DoString(luaExpression);
            }
            catch (ScriptRuntimeException e)
            {
                caughtException = e;
            }

            await Assert
                .That(caughtException)
                .IsNull()
                .Because($"Expression '{luaExpression}' should succeed without error in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.randomseed accepting non-integer arguments in Lua 5.1-5.3.
        /// math.randomseed started requiring integers in Lua 5.4.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix("math.randomseed(1.5)", MaximumVersion = LuaCompatibilityVersion.Lua53)]
        public async Task RandomseedAcceptsNonIntegerInLua51To53(
            LuaCompatibilityVersion version,
            string luaExpression
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // Should not throw - expression should execute successfully
            ScriptRuntimeException caughtException = null;
            try
            {
                script.DoString(luaExpression);
            }
            catch (ScriptRuntimeException e)
            {
                caughtException = e;
            }

            await Assert
                .That(caughtException)
                .IsNull()
                .Because($"Expression '{luaExpression}' should succeed without error in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.log ignores the base parameter in Lua 5.1 (always returns natural log).
        /// Verified against reference Lua 5.1: math.log(100) == math.log(100, 10) == 4.605170...
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task LogIgnoresBaseInLua51(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua51);

            // In Lua 5.1, math.log always returns natural log regardless of second argument
            DynValue resultWithoutBase = script.DoString("return math.log(100)");
            DynValue resultWithBase = script.DoString("return math.log(100, 10)");

            // Both should return ln(100) ≈ 4.605
            double expectedNaturalLog = Math.Log(100);
            await Assert
                .That(resultWithoutBase.Number)
                .IsEqualTo(expectedNaturalLog)
                .Within(1e-10)
                .ConfigureAwait(false);
            await Assert
                .That(resultWithBase.Number)
                .IsEqualTo(expectedNaturalLog)
                .Within(1e-10)
                .Because("Lua 5.1 ignores the base parameter")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.log uses the base parameter in Lua 5.2+.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LogUsesBaseInLua52Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.log(100, 10) should return log base 10 of 100 = 2
            DynValue result = script.DoString("return math.log(100, 10)");

            await Assert
                .That(result.Number)
                .IsEqualTo(2d)
                .Within(1e-10)
                .Because($"math.log(100, 10) should return 2 in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.log10 is available in all Lua versions.
        /// Verified against reference Lua 5.1-5.4: math.log10(100) == 2.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task Log10AvailableInAllVersions(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.log10(100)");

            await Assert
                .That(result.Number)
                .IsEqualTo(2d)
                .Within(1e-10)
                .Because($"math.log10(100) should return 2 in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.log10 returns correct results for various inputs.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task Log10ReturnsCorrectValues(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            await Assert
                .That(script.DoString("return math.log10(1)").Number)
                .IsEqualTo(0d)
                .Within(1e-10)
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return math.log10(10)").Number)
                .IsEqualTo(1d)
                .Within(1e-10)
                .ConfigureAwait(false);
            await Assert
                .That(script.DoString("return math.log10(1000)").Number)
                .IsEqualTo(3d)
                .Within(1e-10)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.mod is available only in Lua 5.1 (removed in 5.2+).
        /// Verified against reference Lua: math.mod exists in 5.1, nil in 5.2+.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModAvailableOnlyInLua51(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua51);

            // math.mod should be available and work like fmod
            DynValue result = script.DoString("return math.mod(10, 3)");

            await Assert
                .That(result.Number)
                .IsEqualTo(Math.IEEERemainder(10, 3))
                .Within(1e-10)
                .Because("math.mod(10, 3) should work in Lua 5.1")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.mod is NOT available in Lua 5.2+ (it was removed).
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task ModIsNilInLua52Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.mod");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because($"math.mod was removed in Lua 5.2+. Actual type: {result.Type}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.modf returns integer subtype for integral part in Lua 5.3+.
        /// Verified against reference Lua 5.3/5.4: math.type returns "integer" for integral part.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ModfReturnsIntegerSubtypeInLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.modf(3.5) should return (3, 0.5) with 3 as integer subtype
            DynValue result = script.DoString(
                "local i, f = math.modf(3.5); return math.type(i), i, math.type(f), f"
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("integer")
                .Because($"Integral part should be integer subtype in {version}")
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].String)
                .IsEqualTo("float")
                .Because($"Fractional part should be float subtype in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(0.5d)
                .Within(1e-10)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.modf returns float subtype for both parts in Lua 5.1/5.2.
        /// Verified against reference Lua 5.1/5.2: type() returns "number" for both parts.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task ModfReturnsFloatSubtypeInLua51And52(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.modf(3.5) should return (3.0, 0.5) both as floats
            DynValue result = script.DoString(
                "local i, f = math.modf(3.5); return type(i), i, type(f), f"
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("number")
                .Because($"Integral part should be number in {version}")
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].String)
                .IsEqualTo("number")
                .Because($"Fractional part should be number in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(0.5d)
                .Within(1e-10)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.modf works correctly with negative numbers.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public async Task ModfWithNegativeNumbersReturnsIntegerSubtype(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.modf(-3.5) should return (-3, -0.5) with -3 as integer subtype
            DynValue result = script.DoString(
                "local i, f = math.modf(-3.5); return math.type(i), i, math.type(f), f"
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("integer")
                .Because($"Integral part should be integer subtype in {version}")
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(-3d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].String)
                .IsEqualTo("float")
                .Because($"Fractional part should be float subtype in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[3].Number)
                .IsEqualTo(-0.5d)
                .Within(1e-10)
                .ConfigureAwait(false);
        }

        // ==========================================================================
        // Data-driven tests for math.random infinity/NaN edge cases
        // ==========================================================================
        // Note: In ALL Lua versions (5.1-5.5), passing infinity or NaN to math.random
        // typically results in an error ("interval is empty" in 5.1/5.2, or
        // "number has no integer representation" in 5.3+). The specific error message
        // differs, but the behavior of rejecting these values is consistent.

        /// <summary>
        /// Data-driven test for math.random() edge cases that SHOULD throw in Lua 5.3+.
        /// In Lua 5.3+, infinity and NaN have no integer representation and must throw
        /// with a specific error message.
        /// Uses LuaTestMatrix to automatically test Lua 5.3, 5.4, and 5.5.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.random(1/0)", "positive infinity in single arg" },
            new object[] { "math.random(-1/0)", "negative infinity in single arg" },
            new object[] { "math.random(0/0)", "NaN in single arg" },
            new object[] { "math.random(1/0, 10)", "positive infinity in first arg" },
            new object[] { "math.random(1, 1/0)", "positive infinity in second arg" },
            new object[] { "math.random(0/0, 10)", "NaN in first arg" },
            new object[] { "math.random(1, 0/0)", "NaN in second arg" },
            new object[] { "math.random(-1/0, 10)", "negative infinity in first arg" },
            new object[] { "math.random(1, -1/0)", "negative infinity in second arg" },
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task RandomRejectsInfinityAndNaNLua53Plus(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString($"return {luaExpression}"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"Expression '{luaExpression}' ({description}) should throw integer representation error in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.random() edge cases that SHOULD throw in Lua 5.1/5.2.
        /// Lua 5.1/5.2 uses IEEE 754 floating-point comparison BEFORE converting to integers.
        /// These cases error because the comparison fails (NaN always false, -inf &lt; 1).
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[]
            {
                "math.random(-1/0)",
                "negative infinity in single arg (1.0 <= -inf is FALSE)",
            },
            new object[] { "math.random(0/0)", "NaN in single arg (1.0 <= nan is FALSE)" },
            new object[]
            {
                "math.random(1, -1/0)",
                "negative infinity in second arg (1 <= -inf is FALSE)",
            },
            new object[] { "math.random(1, 0/0)", "NaN in second arg (1 <= nan is FALSE)" },
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task RandomErrorsOnIntervalEmptyLua51And52(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString($"return {luaExpression}"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("interval is empty")
                .Because(
                    $"Expression '{luaExpression}' ({description}) should throw interval empty error in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.random with infinity SUCCEEDS when the interval check passes.
        ///
        /// IMPORTANT: Lua 5.1 and 5.2 have DIFFERENT behaviors due to order of operations:
        /// - Lua 5.1: converts to integer FIRST via luaL_checkint(), THEN checks m &lt;= n on integers
        /// - Lua 5.2: checks m &lt;= n on floats FIRST, THEN converts to integer
        ///
        /// This means:
        /// - math.random(-1/0, 10): Succeeds in BOTH (5.1: LONG_MIN &lt;= 10; 5.2: -inf &lt;= 10)
        /// - math.random(1, 1/0): Succeeds ONLY in 5.2 (5.2: 1 &lt;= inf; 5.1: 1 &lt;= LONG_MIN = FALSE)
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            (object)
                new object[]
                {
                    "math.random(-1/0, 10)",
                    "negative infinity first - Both: -inf <= 10 (5.2) and LONG_MIN <= 10 (5.1) are TRUE",
                },
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task RandomSucceedsWithNegativeInfinityFirstArgLua51And52(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            // Both Lua 5.1 and 5.2 succeed here because:
            // - Lua 5.1: -inf → LONG_MIN, then LONG_MIN <= 10 is TRUE
            // - Lua 5.2: -inf <= 10 is TRUE per IEEE 754
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString($"return {luaExpression}");

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Number)
                .Because(
                    $"Expression '{luaExpression}' ({description}) should succeed and return a number in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.random(1, 1/0) SUCCEEDS in Lua 5.2 only.
        /// Lua 5.2 compares floats first: 1 &lt;= inf is TRUE.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua52)]
        public async Task RandomSucceedsWithPositiveInfinitySecondArgLua52Only(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.2: compares floats first, 1 <= inf is TRUE → succeeds
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString("return math.random(1, 1/0)");

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Number)
                .Because("math.random(1, 1/0) should succeed in Lua 5.2 because 1 <= inf is TRUE")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.random(1, 1/0) ERRORS in Lua 5.1.
        /// Lua 5.1 converts to integer first: inf → LONG_MIN, then 1 &lt;= LONG_MIN is FALSE.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua51)]
        public async Task RandomErrorsWithPositiveInfinitySecondArgLua51(
            LuaCompatibilityVersion version
        )
        {
            // Lua 5.1: converts to integer FIRST, inf → LONG_MIN, then 1 <= LONG_MIN is FALSE → error
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("return math.random(1, 1/0)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("interval is empty")
                .Because(
                    "math.random(1, 1/0) should error in Lua 5.1 because 1 <= LONG_MIN is FALSE"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.random(inf, n) and math.random(nan, n) ERROR in Lua 5.2 because
        /// inf &lt;= 10 is FALSE and nan &lt;= 10 is FALSE per IEEE 754.
        ///
        /// Lua 5.1 is DIFFERENT: it converts to integer FIRST, so LONG_MIN &lt;= 10 is TRUE (no error).
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.random(1/0, 10)", "positive infinity first (inf <= 10 is FALSE)" },
            new object[] { "math.random(0/0, 10)", "NaN first (nan <= 10 is FALSE)" },
            MinimumVersion = LuaCompatibilityVersion.Lua52,
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task RandomErrorsWithInfinityNaNFirstArgLua52Only(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            // Lua 5.2: compares floats first, inf <= 10 is FALSE and nan <= 10 is FALSE per IEEE 754
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString($"return {luaExpression}"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("interval is empty")
                .Because(
                    $"Expression '{luaExpression}' ({description}) should throw interval empty error in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.random(inf, n) and math.random(nan, n) SUCCEED in Lua 5.1.
        /// Lua 5.1 converts to integer FIRST via luaL_checkint(), so inf/nan → LONG_MIN,
        /// and LONG_MIN &lt;= 10 is TRUE (no error, produces garbage).
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[]
            {
                "math.random(1/0, 10)",
                "positive infinity first → LONG_MIN <= 10 is TRUE",
            },
            new object[] { "math.random(0/0, 10)", "NaN first → LONG_MIN <= 10 is TRUE" },
            MaximumVersion = LuaCompatibilityVersion.Lua51
        )]
        public async Task RandomSucceedsWithInfinityNaNFirstArgLua51(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            // Lua 5.1: converts to integer FIRST, inf/nan → LONG_MIN, then LONG_MIN <= 10 is TRUE
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString($"return {luaExpression}");

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Number)
                .Because(
                    $"Expression '{luaExpression}' ({description}) should succeed and return a number in {version}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModfPreservesNegativeZeroForNegativeIntegers(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // math.modf(-5) should return (-5, -0), not (-5, 0)
            // The fractional part should be negative zero to preserve the sign
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local int_part, frac_part = math.modf(-5)
                -- Check if fractional part is negative zero (1/-0 = -inf)
                local is_neg_zero = (frac_part == 0 and 1/frac_part == -math.huge)
                return int_part, frac_part, is_neg_zero
            "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(-5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Boolean)
                .IsTrue()
                .Because($"math.modf(-5) fractional part should be -0 (negative zero) in {version}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModfPreservesPositiveZeroForPositiveIntegers(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // math.modf(5) should return (5, +0)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local int_part, frac_part = math.modf(5)
                -- Check if fractional part is positive zero (1/+0 = +inf)
                local is_pos_zero = (frac_part == 0 and 1/frac_part == math.huge)
                return int_part, frac_part, is_pos_zero
            "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Boolean)
                .IsTrue()
                .Because($"math.modf(5) fractional part should be +0 (positive zero) in {version}")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ModfNegativeInfinityFractionalPartIsNegativeZero(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // math.modf(-inf) should return (-inf, -0)
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                @"
                local int_part, frac_part = math.modf(-math.huge)
                local is_neg_zero = (frac_part == 0 and 1/frac_part == -math.huge)
                return int_part, frac_part, is_neg_zero
            "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(double.IsNegativeInfinity(result.Tuple[0].Number))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Boolean)
                .IsTrue()
                .Because(
                    $"math.modf(-inf) fractional part should be -0 (negative zero) in {version}"
                )
                .ConfigureAwait(false);
        }

        // ==========================================================================
        // Comprehensive data-driven math.random edge case tests
        // These tests consolidate various edge cases with improved diagnostics
        // ==========================================================================

        /// <summary>
        /// Data-driven test for math.random expressions that should SUCCEED across all Lua versions.
        /// Each test case includes the expression to evaluate and expected numeric result type.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.random()", "returns float in [0,1)" },
            new object[] { "math.random(1)", "returns 1 (single value)" },
            new object[] { "math.random(100)", "returns integer in [1,100]" },
            new object[] { "math.random(5, 5)", "returns 5 (single value range)" },
            new object[] { "math.random(-10, -5)", "returns integer in [-10,-5]" },
            new object[] { "math.random(-5, 5)", "returns integer in [-5,5]" },
            new object[] { "math.random(1, 1000000)", "returns integer in large range" }
        )]
        public async Task RandomSucceedsDataDriven(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString($"return {luaExpression}");

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Number)
                .Because($"'{luaExpression}' ({description}) should return a number in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.random expressions that should ERROR with "interval is empty"
        /// in Lua 5.1 and 5.2. These versions use integer conversion first, which can cause
        /// different behavior than Lua 5.3+ which validates integer representation upfront.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.random(10, 5)", "reversed range (10 > 5)" },
            new object[] { "math.random(-1/0)", "negative infinity single arg" },
            new object[] { "math.random(0/0)", "NaN single arg" },
            MaximumVersion = LuaCompatibilityVersion.Lua52
        )]
        public async Task RandomErrorsIntervalEmptyLua51And52DataDriven(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString($"return {luaExpression}"))
                        .ConfigureAwait(false)
                )
                .Because($"'{luaExpression}' ({description}) should throw in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("interval is empty")
                .Because(
                    $"'{luaExpression}' error message should indicate interval is empty in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.random expressions that should ERROR with
        /// "number has no integer representation" in Lua 5.3+.
        /// Lua 5.3+ validates integer representation before performing range checks.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.random(1.5)", "non-integral float single arg" },
            new object[] { "math.random(0/0)", "NaN single arg" },
            new object[] { "math.random(1/0)", "positive infinity single arg" },
            new object[] { "math.random(-1/0)", "negative infinity single arg" },
            new object[] { "math.random(1, 2.5)", "non-integral float second arg" },
            new object[] { "math.random(1.5, 10)", "non-integral float first arg" },
            new object[] { "math.random(1, 0/0)", "NaN second arg" },
            new object[] { "math.random(0/0, 10)", "NaN first arg" },
            MinimumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task RandomErrorsNoIntegerRepresentationLua53PlusDataDriven(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString($"return {luaExpression}"))
                        .ConfigureAwait(false)
                )
                .Because($"'{luaExpression}' ({description}) should throw in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"'{luaExpression}' error message should indicate no integer representation in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.randomseed expressions that should ERROR in Lua 5.4+.
        /// Lua 5.4 introduced integer requirement for randomseed first argument.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.randomseed(1.5)", "non-integral float" },
            new object[] { "math.randomseed(0/0)", "NaN" },
            new object[] { "math.randomseed(1/0)", "positive infinity" },
            new object[] { "math.randomseed(-1/0)", "negative infinity" },
            MinimumVersion = LuaCompatibilityVersion.Lua54
        )]
        public async Task RandomseedErrorsNoIntegerRepresentationLua54PlusDataDriven(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString(luaExpression)).ConfigureAwait(false)
                )
                .Because($"'{luaExpression}' ({description}) should throw in {version}")
                .ConfigureAwait(false);

            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .Because(
                    $"'{luaExpression}' error message should indicate no integer representation in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Data-driven test for math.randomseed expressions that should SUCCEED in Lua 5.1-5.3.
        /// These versions accept non-integer arguments to randomseed.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "math.randomseed(1.5)", "non-integral float" },
            new object[] { "math.randomseed(1/0)", "positive infinity" },
            new object[] { "math.randomseed(-1/0)", "negative infinity" },
            MaximumVersion = LuaCompatibilityVersion.Lua53
        )]
        public async Task RandomseedSucceedsLua51To53DataDriven(
            LuaCompatibilityVersion version,
            string luaExpression,
            string description
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // Should not throw - returns nothing (nil)
            DynValue result = script.DoString($"{luaExpression}; return true");

            await Assert
                .That(result.Boolean)
                .IsTrue()
                .Because(
                    $"'{luaExpression}' ({description}) should succeed without error in {version}"
                )
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }

        private static Script CreateScript(Compatibility.LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }
    }
}
