namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

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
        public async Task ToIntegerThrowsWhenTypeUnsupported()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return math.tointeger(true)")
            );

            await Assert
                .That(exception.Message.IndexOf("boolean", StringComparison.OrdinalIgnoreCase))
                .IsGreaterThanOrEqualTo(0);
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

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
