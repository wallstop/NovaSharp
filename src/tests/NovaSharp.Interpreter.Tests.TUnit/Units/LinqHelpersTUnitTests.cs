#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class LinqHelpersTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConvertFiltersByDataTypeAndProjectsToClrType()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewNumber(1),
                DynValue.NewString("one"),
                DynValue.NewNumber(2),
            };

            List<double> numbers = new(values.Convert<double>(DataType.Number));

            await Assert.That(numbers.Count).IsEqualTo(2);
            await Assert.That(numbers[0]).IsEqualTo(1d);
            await Assert.That(numbers[1]).IsEqualTo(2d);
        }

        [global::TUnit.Core.Test]
        public async Task OfDataTypeReturnsOnlyMatchingValues()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewString("alpha"),
                DynValue.NewNumber(3),
                DynValue.NewString("beta"),
            };

            List<DynValue> strings = new(values.OfDataType(DataType.String));
            List<string> stringValues = new();

            foreach (DynValue value in strings)
            {
                stringValues.Add(value.String);
            }

            await Assert.That(stringValues.Count).IsEqualTo(2);
            await Assert.That(stringValues[0]).IsEqualTo("alpha");
            await Assert.That(stringValues[1]).IsEqualTo("beta");
        }

        [global::TUnit.Core.Test]
        public async Task AsObjectsProjectsToRawObjects()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewNumber(5),
                DynValue.NewString("value"),
            };

            List<object> objects = new(values.AsObjects());

            await Assert.That(objects[0]).IsEqualTo(5d);
            await Assert.That(objects[1]).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task AsObjectsGenericProjectsToRequestedType()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewString("one"),
                DynValue.NewString("two"),
                DynValue.NewString("three"),
            };

            List<string> strings = new(values.AsObjects<string>());

            await Assert.That(strings.Count).IsEqualTo(3);
            await Assert.That(strings[0]).IsEqualTo("one");
            await Assert.That(strings[1]).IsEqualTo("two");
            await Assert.That(strings[2]).IsEqualTo("three");
        }

        [global::TUnit.Core.Test]
        public async Task HelpersThrowWhenEnumerableIsNull()
        {
            IEnumerable<DynValue> values = null;

            ArgumentNullException convertException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<double>(values.Convert<double>(DataType.Number));
            });
            await Assert.That(convertException.ParamName).IsEqualTo("enumerable");

            ArgumentNullException ofDataTypeException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<DynValue>(values.OfDataType(DataType.String));
            });
            await Assert.That(ofDataTypeException.ParamName).IsEqualTo("enumerable");

            ArgumentNullException asObjectsException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<object>(values.AsObjects());
            });
            await Assert.That(asObjectsException.ParamName).IsEqualTo("enumerable");

            ArgumentNullException genericException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<string>(values.AsObjects<string>());
            });
            await Assert.That(genericException.ParamName).IsEqualTo("enumerable");
        }
    }
}
#pragma warning restore CA2007
