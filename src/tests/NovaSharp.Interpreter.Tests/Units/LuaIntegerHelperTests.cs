namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LuaIntegerHelperTests
    {
        [Test]
        public void TryGetIntegerDoubleRejectsNonFiniteValues()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    LuaIntegerHelper.TryGetInteger(double.NaN, out long nanResult),
                    Is.False
                );
                Assert.That(nanResult, Is.EqualTo(0));

                Assert.That(
                    LuaIntegerHelper.TryGetInteger(double.PositiveInfinity, out long infResult),
                    Is.False
                );
                Assert.That(infResult, Is.EqualTo(0));
            });
        }

        [Test]
        public void TryGetIntegerDoubleRequiresIntegralValueWithinRange()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    LuaIntegerHelper.TryGetInteger(double.MaxValue, out long overflowResult),
                    Is.False
                );
                Assert.That(overflowResult, Is.EqualTo(0));

                Assert.That(
                    LuaIntegerHelper.TryGetInteger(42.25, out long fractionalResult),
                    Is.False
                );
                Assert.That(fractionalResult, Is.EqualTo(0));

                Assert.That(
                    LuaIntegerHelper.TryGetInteger(-123.0, out long integralResult),
                    Is.True
                );
                Assert.That(integralResult, Is.EqualTo(-123));
            });
        }

        [Test]
        public void TryGetIntegerDynValueParsesNumbersAndNumericStrings()
        {
            Assert.Multiple(() =>
            {
                DynValue number = DynValue.NewNumber(64);
                Assert.That(
                    LuaIntegerHelper.TryGetInteger(number, out long numericResult),
                    Is.True
                );
                Assert.That(numericResult, Is.EqualTo(64));

                DynValue text = DynValue.NewString("1024");
                Assert.That(LuaIntegerHelper.TryGetInteger(text, out long textResult), Is.True);
                Assert.That(textResult, Is.EqualTo(1024));

                DynValue invalid = DynValue.NewString("not-a-number");
                Assert.That(
                    LuaIntegerHelper.TryGetInteger(invalid, out long invalidResult),
                    Is.False
                );
                Assert.That(invalidResult, Is.EqualTo(0));
            });
        }

        [Test]
        public void ShiftLeftHandlesNegativeAndOverflowingShifts()
        {
            Assert.Multiple(() =>
            {
                // Negative shift should delegate to ShiftRight.
                Assert.That(LuaIntegerHelper.ShiftLeft(16, -1), Is.EqualTo(8));

                // Large shift saturates to zero.
                Assert.That(LuaIntegerHelper.ShiftLeft(1, 100), Is.EqualTo(0));
            });
        }

        [Test]
        public void ShiftRightHandlesNegativeAndOverflowingShifts()
        {
            Assert.Multiple(() =>
            {
                // Negative shift should delegate to ShiftLeft.
                Assert.That(LuaIntegerHelper.ShiftRight(1, -1), Is.EqualTo(2));

                // Shifting beyond width keeps the sign bit.
                Assert.That(LuaIntegerHelper.ShiftRight(-1, 64), Is.EqualTo(-1));
                Assert.That(LuaIntegerHelper.ShiftRight(1, 64), Is.EqualTo(0));
            });
        }
    }
}
