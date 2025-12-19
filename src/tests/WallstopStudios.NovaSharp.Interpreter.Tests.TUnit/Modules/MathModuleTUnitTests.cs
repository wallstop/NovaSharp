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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task MaxAggregatesAcrossArguments(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.max(-1, 5, 12, 3)");

            await Assert.That(result.Number).IsEqualTo(12d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        /// math.ldexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task LdexpCombinesMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString("return math.ldexp(0.5, 3)");

            await Assert.That(result.Number).IsEqualTo(4d).Within(1e-12).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.ldexp is available in Lua 5.1 and 5.2.
        /// math.ldexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        public async Task LdexpAvailableInLua51And52(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ldexp(0.5, 3)");

            await Assert
                .That(result.Number)
                .IsEqualTo(4d)
                .Within(1e-12)
                .Because($"math.ldexp should be available in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.ldexp is NOT available in Lua 5.3+ (it was removed).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task LdexpIsNilInLua53Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ldexp");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.ldexp was removed in Lua 5.3+. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp is available in Lua 5.1 and 5.2.
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        public async Task FrexpAvailableInLua51And52(Compatibility.LuaCompatibilityVersion version)
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

        /// <summary>
        /// Tests that math.frexp is NOT available in Lua 5.3+ (it was removed).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpIsNilInLua53Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.frexp");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"math.frexp was removed in Lua 5.3+. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task ToIntegerConvertsNumericStrings(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger('42')");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task ToIntegerReturnsNilWhenValueNotIntegral(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(3.5)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task ToIntegerReturnsNilForTable(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger({})");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task ToIntegerReturnsNilForFunction(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(function() end)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task ToIntegerReturnsNilForNil(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.tointeger(nil)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpWithZeroReturnsZeroMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString("return math.frexp(0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns (0, 0) for input -0.0.
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpWithNegativeZeroReturnsZeroMantissaAndExponent(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
            DynValue result = script.DoString("return math.frexp(-0.0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that math.frexp returns negative mantissa for negative input.
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpWithNegativeNumberReturnsNegativeMantissa(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
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
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpWithSubnormalNumberHandlesExponentCorrectly(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            // Subnormal (denormalized) numbers have exponent bits = 0
            // The smallest positive subnormal is approximately 4.94e-324
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
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
        /// math.frexp was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpWithPositiveNumberReturnsMantissaInExpectedRange(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
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
        /// Both were deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FrexpAndLdexpRoundTrip(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = CreateScript(Compatibility.LuaCompatibilityVersion.Lua52);
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FloorPreservesIntegerInput(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FloorReturnsIntegerTypeReportedByMathType(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.floor(3.7))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FloorHandlesNaNAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task FloorPreservesIntegerZero(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.floor(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task CeilPreservesIntegerInput(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task CeilReturnsIntegerTypeReportedByMathType(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.type(math.ceil(3.2))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task CeilHandlesNaNAsFloat(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task CeilPreservesIntegerZero(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return math.ceil(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task RandomWithOneReturnsOne(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

            // math.random(1) should always return 1
            DynValue result = script.DoString("return math.random(1)");
            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        /// "number has no integer representation" in Lua 5.3+ and later versions.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua53,
            "math.random(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua53,
            "math.random(0/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua53,
            "math.random(1/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua53,
            "math.random(1, 2.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.random(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.random(0/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.random(1/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.random(1, 2.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.randomseed(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua54,
            "math.randomseed(0/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.random(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.random(0/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.random(1/0)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.random(1, 2.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.randomseed(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua55,
            "math.randomseed(0/0)"
        )]
        public async Task IntegerRepresentationErrorCases(
            Compatibility.LuaCompatibilityVersion version,
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
        /// Consolidated data-driven test for expressions that should succeed (accept non-integer)
        /// in Lua 5.1 and 5.2, where fractional values are silently truncated.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua51,
            "math.random(2.9)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua51,
            "math.randomseed(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua52,
            "math.random(2.9)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua52,
            "math.randomseed(1.5)"
        )]
        [global::TUnit.Core.Arguments(
            Compatibility.LuaCompatibilityVersion.Lua53,
            "math.randomseed(1.5)"
        )]
        public async Task NonIntegerAcceptedInOlderVersions(
            Compatibility.LuaCompatibilityVersion version,
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
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
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
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
