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

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeConvertsToSbyte()
        {
            double value = 42d;

            object result = NumericConversions.DoubleToType(typeof(sbyte), value);

            await Assert.That(result).IsEqualTo((sbyte)42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeConvertsToUshort()
        {
            double value = 1000d;

            object result = NumericConversions.DoubleToType(typeof(ushort), value);

            await Assert.That(result).IsEqualTo((ushort)1000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeConvertsToUint()
        {
            double value = 100000d;

            object result = NumericConversions.DoubleToType(typeof(uint), value);

            await Assert.That(result).IsEqualTo((uint)100000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoubleToTypeConvertsToUlong()
        {
            double value = 1000000d;

            object result = NumericConversions.DoubleToType(typeof(ulong), value);

            await Assert.That(result).IsEqualTo((ulong)1000000).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleConvertsFromSbyte()
        {
            object sbyteValue = (sbyte)42;

            double result = NumericConversions.TypeToDouble(typeof(sbyte), sbyteValue);

            await Assert.That(result).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleConvertsFromUshort()
        {
            object ushortValue = (ushort)1000;

            double result = NumericConversions.TypeToDouble(typeof(ushort), ushortValue);

            await Assert.That(result).IsEqualTo(1000d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleConvertsFromUint()
        {
            object uintValue = (uint)100000;

            double result = NumericConversions.TypeToDouble(typeof(uint), uintValue);

            await Assert.That(result).IsEqualTo(100000d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TypeToDoubleConvertsFromUlong()
        {
            object ulongValue = (ulong)1000000;

            double result = NumericConversions.TypeToDouble(typeof(ulong), ulongValue);

            await Assert.That(result).IsEqualTo(1000000d).ConfigureAwait(false);
        }
    }
}
