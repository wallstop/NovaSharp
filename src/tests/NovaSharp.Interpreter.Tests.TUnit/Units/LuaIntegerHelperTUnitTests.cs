namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;

    public sealed class LuaIntegerHelperTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TryGetIntegerDoubleRejectsNonFiniteValues()
        {
            bool nanSuccess = LuaIntegerHelper.TryGetInteger(double.NaN, out long nanResult);
            bool infSuccess = LuaIntegerHelper.TryGetInteger(
                double.PositiveInfinity,
                out long infResult
            );

            await Assert.That(nanSuccess).IsFalse();
            await Assert.That(nanResult).IsEqualTo(0L);
            await Assert.That(infSuccess).IsFalse();
            await Assert.That(infResult).IsEqualTo(0L);
        }

        [global::TUnit.Core.Test]
        public async Task TryGetIntegerDoubleRequiresIntegralValueWithinRange()
        {
            bool overflowSuccess = LuaIntegerHelper.TryGetInteger(
                double.MaxValue,
                out long overflowResult
            );
            bool fractionalSuccess = LuaIntegerHelper.TryGetInteger(
                42.25,
                out long fractionalResult
            );
            bool integralSuccess = LuaIntegerHelper.TryGetInteger(-123.0, out long integralResult);

            await Assert.That(overflowSuccess).IsFalse();
            await Assert.That(overflowResult).IsEqualTo(0L);
            await Assert.That(fractionalSuccess).IsFalse();
            await Assert.That(fractionalResult).IsEqualTo(0L);
            await Assert.That(integralSuccess).IsTrue();
            await Assert.That(integralResult).IsEqualTo(-123L);
        }

        [global::TUnit.Core.Test]
        public async Task TryGetIntegerDynValueParsesNumbersAndNumericStrings()
        {
            DynValue number = DynValue.NewNumber(64);
            bool numericSuccess = LuaIntegerHelper.TryGetInteger(number, out long numericResult);
            DynValue text = DynValue.NewString("1024");
            bool textSuccess = LuaIntegerHelper.TryGetInteger(text, out long textResult);
            DynValue invalid = DynValue.NewString("not-a-number");
            bool invalidSuccess = LuaIntegerHelper.TryGetInteger(invalid, out long invalidResult);

            await Assert.That(numericSuccess).IsTrue();
            await Assert.That(numericResult).IsEqualTo(64L);
            await Assert.That(textSuccess).IsTrue();
            await Assert.That(textResult).IsEqualTo(1024L);
            await Assert.That(invalidSuccess).IsFalse();
            await Assert.That(invalidResult).IsEqualTo(0L);
        }

        [global::TUnit.Core.Test]
        public async Task ShiftLeftHandlesNegativeAndOverflowingShifts()
        {
            // Negative shift should delegate to ShiftRight.
            long negativeShift = LuaIntegerHelper.ShiftLeft(16, -1);
            // Large shift saturates to zero.
            long overflowShift = LuaIntegerHelper.ShiftLeft(1, 100);

            await Assert.That(negativeShift).IsEqualTo(8L);
            await Assert.That(overflowShift).IsEqualTo(0L);
        }

        [global::TUnit.Core.Test]
        public async Task ShiftRightHandlesNegativeAndOverflowingShifts()
        {
            // Negative shift should delegate to ShiftLeft.
            long negativeShift = LuaIntegerHelper.ShiftRight(1, -1);
            // Shifting beyond width keeps the sign bit.
            long negativeOverflow = LuaIntegerHelper.ShiftRight(-1, 64);
            long positiveOverflow = LuaIntegerHelper.ShiftRight(1, 64);

            await Assert.That(negativeShift).IsEqualTo(2L);
            await Assert.That(negativeOverflow).IsEqualTo(-1L);
            await Assert.That(positiveOverflow).IsEqualTo(0L);
        }
    }
}
