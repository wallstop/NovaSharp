namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    public sealed class LinqHelpersTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConvertFiltersByDataTypeAndProjectsToClrType()
        {
            IEnumerable<LuaValue> values = new List<LuaValue>
            {
                LuaValue.NewNumber(1),
                LuaValue.NewString("one"),
                LuaValue.NewNumber(2),
            };

            List<double> numbers = new(values.Convert<double>(DataType.Number));

            await Assert.That(numbers.Count).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(numbers[0]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(numbers[1]).IsEqualTo(2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task OfDataTypeReturnsOnlyMatchingValues()
        {
            IEnumerable<LuaValue> values = new List<LuaValue>
            {
                LuaValue.NewString("alpha"),
                LuaValue.NewNumber(3),
                LuaValue.NewString("beta"),
            };

            List<LuaValue> strings = new(values.OfDataType(DataType.String));
            List<string> stringValues = new();

            foreach (LuaValue value in strings)
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
            IEnumerable<LuaValue> values = new List<LuaValue>
            {
                LuaValue.NewNumber(5),
                LuaValue.NewString("value"),
            };

            List<object> objects = new(values.AsObjects());

            // Numeric values may come back as long (integer) or double depending on representation
            await Assert
                .That(Convert.ToDouble(objects[0], CultureInfo.InvariantCulture))
                .IsEqualTo(5d)
                .ConfigureAwait(false);
            await Assert.That(objects[1]).IsEqualTo("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AsObjectsGenericProjectsToRequestedType()
        {
            IEnumerable<LuaValue> values = new List<LuaValue>
            {
                LuaValue.NewString("one"),
                LuaValue.NewString("two"),
                LuaValue.NewString("three"),
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
            IEnumerable<LuaValue> values = null;

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
                _ = new List<LuaValue>(values.OfDataType(DataType.String));
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
