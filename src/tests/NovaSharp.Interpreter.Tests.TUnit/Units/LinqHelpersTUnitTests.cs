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

            await Assert.That(numbers.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
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

            await Assert.That(stringValues.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(stringValues[0]).IsEqualTo("alpha").ConfigureAwait(false);
            await Assert.That(stringValues[1]).IsEqualTo("beta").ConfigureAwait(false);
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

            await Assert.That(objects[0]).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(objects[1]).IsEqualTo("value").ConfigureAwait(false);
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

            await Assert.That(strings.Count).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(strings[0]).IsEqualTo("one").ConfigureAwait(false);
            await Assert.That(strings[1]).IsEqualTo("two").ConfigureAwait(false);
            await Assert.That(strings[2]).IsEqualTo("three").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HelpersThrowWhenEnumerableIsNull()
        {
            IEnumerable<DynValue> values = null;

            ArgumentNullException convertException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<double>(values.Convert<double>(DataType.Number));
            });
            await Assert
                .That(convertException.ParamName)
                .IsEqualTo("enumerable")
                .ConfigureAwait(false);

            ArgumentNullException ofDataTypeException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<DynValue>(values.OfDataType(DataType.String));
            });
            await Assert
                .That(ofDataTypeException.ParamName)
                .IsEqualTo("enumerable")
                .ConfigureAwait(false);

            ArgumentNullException asObjectsException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<object>(values.AsObjects());
            });
            await Assert
                .That(asObjectsException.ParamName)
                .IsEqualTo("enumerable")
                .ConfigureAwait(false);

            ArgumentNullException genericException = Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new List<string>(values.AsObjects<string>());
            });
            await Assert
                .That(genericException.ParamName)
                .IsEqualTo("enumerable")
                .ConfigureAwait(false);
        }
    }
}
