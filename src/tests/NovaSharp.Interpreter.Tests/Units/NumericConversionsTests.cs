namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Interop.Converters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class NumericConversionsTests
    {
        [Test]
        public void DoubleToTypeConvertsToRequestedNumericType()
        {
            double value = 42.1;

            object intResult = NumericConversions.DoubleToType(typeof(int), value);
            object decimalResult = NumericConversions.DoubleToType(typeof(decimal), value);

            Assert.Multiple(() =>
            {
                Assert.That(intResult, Is.EqualTo(42));
                Assert.That(decimalResult, Is.EqualTo(Convert.ToDecimal(value)));
            });
        }

        [Test]
        public void DoubleToTypeReturnsOriginalWhenConversionOverflows()
        {
            double largeValue = double.MaxValue;

            object result = NumericConversions.DoubleToType(typeof(byte), largeValue);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(largeValue));
                Assert.That(result, Is.TypeOf<double>());
            });
        }

        [Test]
        public void TypeToDoubleConvertsNumericValues()
        {
            object intValue = 7;
            object decimalValue = 12.5m;

            double intResult = NumericConversions.TypeToDouble(typeof(int), intValue);
            double decimalResult = NumericConversions.TypeToDouble(typeof(decimal), decimalValue);

            Assert.Multiple(() =>
            {
                Assert.That(intResult, Is.EqualTo(7d));
                Assert.That(decimalResult, Is.EqualTo(12.5d));
            });
        }

        [Test]
        public void TypeToDoubleReturnsInputWhenTypeNotNumeric()
        {
            object directValue = 3.14d;

            double result = NumericConversions.TypeToDouble(typeof(string), directValue);

            Assert.That(result, Is.EqualTo(3.14d));
        }

        [Test]
        public void DoubleToTypeSupportsNullableTargets()
        {
            double value = 12d;

            object nullableInt = NumericConversions.DoubleToType(typeof(int?), value);

            Assert.Multiple(() =>
            {
                Assert.That(nullableInt, Is.TypeOf<int>());
                Assert.That(nullableInt, Is.EqualTo(12));
            });
        }

        [Test]
        public void TypeToDoubleFallsBackToDirectCastForNonNumericStructs()
        {
            double direct = NumericConversions.TypeToDouble(typeof(DateTime), 5d);

            Assert.That(direct, Is.EqualTo(5d));
        }
    }
}
