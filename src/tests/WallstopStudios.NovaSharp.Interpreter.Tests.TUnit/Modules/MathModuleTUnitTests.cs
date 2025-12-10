namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class MathModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task LogUsesDefaultBaseWhenOmitted()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.log(8)");

            await Assert
                .That(result.Number)
                .IsEqualTo(Math.Log(8d))
                .Within(1e-12)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LogSupportsCustomBase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.log(8, 2)");

            await Assert.That(result.Number).IsEqualTo(3d).Within(1e-12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PowHandlesLargeExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.pow(10, 6)");

            await Assert
                .That(result.Number)
                .IsEqualTo(1_000_000d)
                .Within(1e-6)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ModfSplitsIntegerAndFractionalComponents()
        {
            Script script = CreateScript();
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
        public async Task MaxAggregatesAcrossArguments()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.max(-1, 5, 12, 3)");

            await Assert.That(result.Number).IsEqualTo(12d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MinAggregatesAcrossArguments()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.min(-1, 5, 12, 3)");

            await Assert.That(result.Number).IsEqualTo(-1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LdexpCombinesMantissaAndExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ldexp(0.5, 3)");

            await Assert.That(result.Number).IsEqualTo(4d).Within(1e-12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SqrtOfNegativeNumberReturnsNaN()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.sqrt(-1)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PowWithLargeExponentReturnsInfinity()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.pow(10, 309)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomSeedProducesDeterministicSequence()
        {
            Script script = CreateScript();

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
        public async Task TypeDistinguishesIntegersAndFloats()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return math.type(5), math.type(3.14)");

            await Assert.That(tuple.Tuple[0].String).IsEqualTo("integer").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("float").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerConvertsNumericStrings()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger('42')");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNilWhenValueNotIntegral()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger(3.5)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerThrowsWhenArgumentMissing()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.tointeger()")
            );

            await Assert.That(exception.Message).Contains("got no value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNilForUnsupportedType()
        {
            // Per Lua 5.3+ spec, math.tointeger returns nil for non-number/non-string types
            // (boolean, table, function, userdata, etc.) - it does NOT throw an error.
            // Reference: Lua 5.3 Manual §6.7
            Script script = CreateScript();

            DynValue result = script.DoString("return math.tointeger(true)");

            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNilForTable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger({})");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNilForFunction()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger(function() end)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ToIntegerReturnsNilForNil()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger(nil)");
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UltPerformsUnsignedComparison()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return math.ult(0, -1), math.ult(-1, 0)");

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FrexpWithZeroReturnsZeroMantissaAndExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.frexp(0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FrexpWithNegativeZeroReturnsZeroMantissaAndExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.frexp(-0.0)");

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(0d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FrexpWithNegativeNumberReturnsNegativeMantissa()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task FrexpWithSubnormalNumberHandlesExponentCorrectly()
        {
            // Subnormal (denormalized) numbers have exponent bits = 0
            // The smallest positive subnormal is approximately 4.94e-324
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task FrexpWithPositiveNumberReturnsMantissaInExpectedRange()
        {
            Script script = CreateScript();
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

        [global::TUnit.Core.Test]
        public async Task FrexpAndLdexpRoundTrip()
        {
            Script script = CreateScript();
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
        public async Task FloorReturnsIntegerForPositiveFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(3.7)");

            await Assert.That(result.Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorReturnsIntegerForNegativeFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(-3.7)");

            await Assert.That(result.Number).IsEqualTo(-4d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorPreservesIntegerInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorReturnsIntegerTypeReportedByMathType()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.type(math.floor(3.7))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorHandlesInfinityAsFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(1/0)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorHandlesNaNAsFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorPreservesIntegerZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorHandlesNegativeZeroAsInteger()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.floor(-0.0)");

            // Negative zero floored is 0, which should be returned as integer
            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        // ========================================
        // math.ceil - Integer promotion tests (Lua 5.3+)
        // ========================================

        [global::TUnit.Core.Test]
        public async Task CeilReturnsIntegerForPositiveFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(3.2)");

            await Assert.That(result.Number).IsEqualTo(4d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilReturnsIntegerForNegativeFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(-3.2)");

            await Assert.That(result.Number).IsEqualTo(-3d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilPreservesIntegerInput()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(42)");

            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilReturnsIntegerTypeReportedByMathType()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.type(math.ceil(3.2))");

            await Assert.That(result.String).IsEqualTo("integer").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilHandlesInfinityAsFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(1/0)");

            await Assert
                .That(double.IsPositiveInfinity(result.Number))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilHandlesNaNAsFloat()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(0/0)");

            await Assert.That(double.IsNaN(result.Number)).IsTrue().ConfigureAwait(false);
            await Assert.That(result.IsFloat).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilPreservesIntegerZero()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(0)");

            await Assert.That(result.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CeilHandlesFloatPointFiveCorrectly()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ceil(0.5)");

            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.IsInteger).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FloorResultCanBeUsedInStringFormat()
        {
            // Verify integer promotion integrates with string.format %d
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return string.format('%d', math.floor(math.maxinteger + 0.5))"
            );

            // maxinteger + 0.5 as double loses precision, but floor result should be maxinteger
            // (this tests the integration between floor and format)
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
        public async Task RandomAcceptsIntegralFloatLua53Plus()
        {
            // Integral floats like 2.0 should be accepted in Lua 5.3+
            Script script = CreateScript();
            script.Options.CompatibilityVersion = Compatibility.LuaCompatibilityVersion.Lua54;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            DynValue result = script.DoString("return math.random(0)");
            // Just verify it returns a number (the value is random)
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithNegativeUpperBoundThrowsError()
        {
            // All versions: math.random(-5) throws "interval is empty"
            Script script = CreateScript();

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.random(-5)")
            );
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithInvertedRangeThrowsError()
        {
            Script script = CreateScript();

            ScriptRuntimeException ex = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.random(10, 5)")
            );
            await Assert.That(ex.Message).Contains("interval is empty").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithSingleValueRangeReturnsValue()
        {
            Script script = CreateScript();

            // math.random(5, 5) should always return 5
            DynValue result = script.DoString("return math.random(5, 5)");
            await Assert.That(result.Number).IsEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithOneReturnsOne()
        {
            Script script = CreateScript();

            // math.random(1) should always return 1
            DynValue result = script.DoString("return math.random(1)");
            await Assert.That(result.Number).IsEqualTo(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithNegativeRangeReturnsValueInRange()
        {
            Script script = CreateScript();

            // math.random(-10, -5) should return value in [-10, -5]
            DynValue result = script.DoString("return math.random(-10, -5)");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(-10d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(-5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithSpanningZeroReturnsValueInRange()
        {
            Script script = CreateScript();

            // math.random(-5, 5) should return value in [-5, 5]
            DynValue result = script.DoString("return math.random(-5, 5)");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(-5d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThanOrEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomNoArgsReturnsFloatInZeroToOneRange()
        {
            Script script = CreateScript();

            // math.random() should return a float in [0, 1)
            DynValue result = script.DoString("return math.random()");
            await Assert.That(result.Number).IsGreaterThanOrEqualTo(0d).ConfigureAwait(false);
            await Assert.That(result.Number).IsLessThan(1d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RandomWithLargeRangeReturnsValueInRange()
        {
            Script script = CreateScript();

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

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

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
