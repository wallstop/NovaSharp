namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Interop.Converters;

    public sealed class NumericConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DoubleToTypeConvertsToRequestedNumericType()
        {
            double value = 42.1;

            object intResult = NumericConversions.DoubleToType(typeof(int), value);
            object decimalResult = NumericConversions.DoubleToType(typeof(decimal), value);

            await Assert.That(intResult).IsEqualTo(42).ConfigureAwait(false);
            await Assert
                .That(decimalResult)
                .IsEqualTo(Convert.ToDecimal(value))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeReturnsOriginalWhenConversionOverflows()
        {
            double largeValue = double.MaxValue;

            object result = NumericConversions.DoubleToType(typeof(byte), largeValue);

            await Assert.That(result).IsEqualTo(largeValue).ConfigureAwait(false);
            await Assert.That(result).IsTypeOf<double>().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleConvertsNumericValues()
        {
            object intValue = 7;
            object decimalValue = 12.5m;

            double intResult = NumericConversions.TypeToDouble(typeof(int), intValue);
            double decimalResult = NumericConversions.TypeToDouble(typeof(decimal), decimalValue);

            await Assert.That(intResult).IsEqualTo(7d).ConfigureAwait(false);
            await Assert.That(decimalResult).IsEqualTo(12.5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleReturnsInputWhenTypeNotNumeric()
        {
            double result = NumericConversions.TypeToDouble(typeof(string), 3.14d);
            await Assert.That(result).IsEqualTo(3.14d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeSupportsNullableTargets()
        {
            object nullableInt = NumericConversions.DoubleToType(typeof(int?), 12d);

            await Assert.That(nullableInt).IsTypeOf<int>().ConfigureAwait(false);
            await Assert.That(nullableInt).IsEqualTo(12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleFallsBackToDirectCastForNonNumericStructs()
        {
            double direct = NumericConversions.TypeToDouble(typeof(DateTime), 5d);

            await Assert.That(direct).IsEqualTo(5d).ConfigureAwait(false);
        }
    }
}
