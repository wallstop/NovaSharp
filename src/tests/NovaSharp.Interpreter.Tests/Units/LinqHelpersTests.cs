namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LinqHelpersTests
    {
        [Test]
        public void ConvertFiltersByDataTypeAndProjectsToClrType()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewNumber(1),
                DynValue.NewString("one"),
                DynValue.NewNumber(2),
            };

            List<double> numbers = new List<double>(values.Convert<double>(DataType.Number));

            Assert.That(numbers, Is.EqualTo(new List<double> { 1.0, 2.0 }));
        }

        [Test]
        public void OfDataTypeReturnsOnlyMatchingValues()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewString("alpha"),
                DynValue.NewNumber(3),
                DynValue.NewString("beta"),
            };

            List<DynValue> strings = new List<DynValue>(values.OfDataType(DataType.String));
            List<string> stringValues = new List<string>();
            foreach (DynValue value in strings)
            {
                stringValues.Add(value.String);
            }

            Assert.That(stringValues, Is.EqualTo(new List<string> { "alpha", "beta" }));
        }

        [Test]
        public void AsObjectsProjectsToRawObjects()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewNumber(5),
                DynValue.NewString("value"),
            };

            List<object> objects = new List<object>(values.AsObjects());

            Assert.Multiple(() =>
            {
                Assert.That(objects[0], Is.EqualTo(5.0));
                Assert.That(objects[1], Is.EqualTo("value"));
            });
        }

        [Test]
        public void AsObjectsGenericProjectsToRequestedType()
        {
            IEnumerable<DynValue> values = new List<DynValue>
            {
                DynValue.NewString("one"),
                DynValue.NewString("two"),
                DynValue.NewString("three"),
            };

            List<string> strings = new List<string>(values.AsObjects<string>());

            Assert.That(strings, Is.EqualTo(new List<string> { "one", "two", "three" }));
        }
    }
}
