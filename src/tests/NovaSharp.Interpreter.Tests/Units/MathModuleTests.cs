namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MathModuleTests
    {
        [Test]
        public void LogUsesDefaultBaseWhenOmitted()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.log(8)");

            Assert.That(result.Number, Is.EqualTo(Math.Log(8)).Within(1e-12));
        }

        [Test]
        public void LogSupportsCustomBase()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.log(8, 2)");

            Assert.That(result.Number, Is.EqualTo(3.0).Within(1e-12));
        }

        [Test]
        public void PowHandlesLargeExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.pow(10, 6)");

            Assert.That(result.Number, Is.EqualTo(1_000_000.0).Within(1e-6));
        }

        [Test]
        public void ModfSplitsIntegerAndFractionalComponents()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.modf(-3.25)");

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(-3.0));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(-0.25).Within(1e-12));
            });
        }

        [Test]
        public void MaxAggregatesAcrossArguments()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.max(-1, 5, 12, 3)");

            Assert.That(result.Number, Is.EqualTo(12.0));
        }

        [Test]
        public void MinAggregatesAcrossArguments()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.min(-1, 5, 12, 3)");

            Assert.That(result.Number, Is.EqualTo(-1.0));
        }

        [Test]
        public void LdexpCombinesMantissaAndExponent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.ldexp(0.5, 3)");

            Assert.That(result.Number, Is.EqualTo(4.0).Within(1e-12));
        }

        [Test]
        public void SqrtOfNegativeNumberReturnsNaN()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.sqrt(-1)");

            Assert.That(double.IsNaN(result.Number), Is.True);
        }

        [Test]
        public void PowWithLargeExponentReturnsInfinity()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.pow(10, 309)");

            Assert.That(double.IsPositiveInfinity(result.Number), Is.True);
        }

        [Test]
        public void RandomSeedProducesDeterministicSequence()
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

            Assert.That(firstSequence.Tuple.Length, Is.EqualTo(3));
            Assert.That(secondSequence.Tuple.Length, Is.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(
                    firstSequence.Tuple[0].Number,
                    Is.EqualTo(secondSequence.Tuple[0].Number)
                );
                Assert.That(
                    firstSequence.Tuple[1].Number,
                    Is.EqualTo(secondSequence.Tuple[1].Number)
                );
                Assert.That(
                    firstSequence.Tuple[2].Number,
                    Is.EqualTo(secondSequence.Tuple[2].Number).Within(1e-12)
                );
            });
        }

        [Test]
        public void TypeDistinguishesIntegersAndFloats()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return math.type(5), math.type(3.14)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("integer"));
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("float"));
            });
        }

        [Test]
        public void ToIntegerConvertsNumericStrings()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger('42')");

            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void ToIntegerReturnsNilWhenValueNotIntegral()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return math.tointeger(3.5)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void ToIntegerThrowsWhenArgumentMissing()
        {
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("return math.tointeger()"),
                Throws.InstanceOf<ScriptRuntimeException>().With.Message.Contains("got no value")
            );
        }

        [Test]
        public void ToIntegerThrowsWhenTypeUnsupported()
        {
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("return math.tointeger(true)"),
                Throws.InstanceOf<ScriptRuntimeException>().With.Message.Contains("boolean")
            );
        }

        [Test]
        public void UltPerformsUnsignedComparison()
        {
            Script script = CreateScript();
            DynValue tuple = script.DoString("return math.ult(0, -1), math.ult(-1, 0)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].Boolean, Is.False);
            });
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            return script;
        }
    }
}
