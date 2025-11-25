namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.Converters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TableConversionsTests
    {
        private static readonly int[] ListIntExpectation = { 3, 4 };
        private static readonly int[] EnumerableIntExpectation = { 5, 6 };
        private static readonly object[] ObjectArrayExpectation = { 1d, "two" };
        private static readonly int[] GenericArrayExpectation = { 1, 2, 3 };

        [TestCase(typeof(Dictionary<object, object>))]
        [TestCase(typeof(Dictionary<DynValue, DynValue>))]
        [TestCase(typeof(List<object>))]
        [TestCase(typeof(List<DynValue>))]
        [TestCase(typeof(object[]))]
        [TestCase(typeof(DynValue[]))]
        public void CanConvertTableToTypeRecognizesBuiltInCollections(Type targetType)
        {
            Assert.That(TableConversions.CanConvertTableToType(table: null, targetType), Is.True);
        }

        [TestCase(typeof(List<int>))]
        [TestCase(typeof(IList<string>))]
        [TestCase(typeof(ICollection<double>))]
        [TestCase(typeof(IEnumerable<decimal>))]
        [TestCase(typeof(Dictionary<string, int>))]
        [TestCase(typeof(IDictionary<int, string>))]
        public void CanConvertTableToTypeRecognizesGenericCollections(Type targetType)
        {
            Assert.That(TableConversions.CanConvertTableToType(null, targetType), Is.True);
        }

        [Test]
        public void CanConvertTableToTypeRecognizesArrays()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TableConversions.CanConvertTableToType(null, typeof(int[])), Is.True);
                Assert.That(
                    TableConversions.CanConvertTableToType(null, typeof(string[])),
                    Is.True
                );
            });
        }

        [Test]
        public void CanConvertTableToTypeRejectsUnsupportedTypes()
        {
            Assert.That(TableConversions.CanConvertTableToType(null, typeof(ValueType)), Is.False);
        }

        [Test]
        public void ConvertIListToTableCopiesValues()
        {
            IList values = new ArrayList { 1, "two" };

            Table table = TableConversions.ConvertIListToTable(new Script(), values);

            Assert.Multiple(() =>
            {
                Assert.That(table.Length, Is.EqualTo(2));
                Assert.That(table.Get(1).Number, Is.EqualTo(1));
                Assert.That(table.Get(2).String, Is.EqualTo("two"));
            });
        }

        [Test]
        public void ConvertIDictionaryToTablePreservesEntries()
        {
            IDictionary dictionary = new Hashtable { ["name"] = "NovaSharp", ["version"] = 5 };

            Table table = TableConversions.ConvertIDictionaryToTable(new Script(), dictionary);

            Assert.Multiple(() =>
            {
                Assert.That(table.Get("name").String, Is.EqualTo("NovaSharp"));
                Assert.That(table.Get("version").Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void ConvertTableToTypeHandlesDictionaryOfObjects()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("one"), DynValue.NewNumber(1)),
                (DynValue.NewString("two"), DynValue.NewString("second"))
            );

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<object, object>)
            );

            Assert.That(result, Is.TypeOf<Dictionary<object, object>>());
            Dictionary<object, object> dictionary = (Dictionary<object, object>)result;
            Assert.Multiple(() =>
            {
                Assert.That(dictionary["one"], Is.EqualTo(1d));
                Assert.That(dictionary["two"], Is.EqualTo("second"));
            });
        }

        [Test]
        public void ConvertTableToTypeHandlesDictionaryOfDynValues()
        {
            DynValue key = DynValue.NewString("key");
            DynValue value = DynValue.NewNumber(42);
            Table table = CreateDictionaryTable((key, value));

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<DynValue, DynValue>)
            );

            Assert.That(result, Is.TypeOf<Dictionary<DynValue, DynValue>>());
            Dictionary<DynValue, DynValue> dictionary = (Dictionary<DynValue, DynValue>)result;
            Assert.That(dictionary[key], Is.SameAs(value));
        }

        [Test]
        public void ConvertTableToTypeHandlesListOfObjects()
        {
            Table table = CreateSequentialTable(
                DynValue.NewNumber(10),
                DynValue.NewString("value")
            );

            object result = TableConversions.ConvertTableToType(table, typeof(List<object>));

            Assert.That(result, Is.TypeOf<List<object>>());
            List<object> list = (List<object>)result;
            Assert.That(list, Is.EqualTo(new object[] { 10d, "value" }));
        }

        [Test]
        public void ConvertTableToTypeHandlesListOfDynValues()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");
            Table table = CreateSequentialTable(first, second);

            object result = TableConversions.ConvertTableToType(table, typeof(List<DynValue>));

            Assert.That(result, Is.TypeOf<List<DynValue>>());
            List<DynValue> list = (List<DynValue>)result;
            Assert.That(list, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void ConvertTableToTypeHandlesObjectArray()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(1), DynValue.NewString("two"));

            object[] array = (object[])TableConversions.ConvertTableToType(table, typeof(object[]));

            Assert.That(array, Is.EqualTo(ObjectArrayExpectation));
        }

        [Test]
        public void ConvertTableToTypeHandlesDynValueArray()
        {
            DynValue first = DynValue.NewNumber(7);
            DynValue second = DynValue.True;
            Table table = CreateSequentialTable(first, second);

            DynValue[] array = (DynValue[])
                TableConversions.ConvertTableToType(table, typeof(DynValue[]));

            Assert.That(array, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void ConvertTableToTypeHandlesGenericList()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(3), DynValue.NewNumber(4));

            List<int> list =
                (List<int>)TableConversions.ConvertTableToType(table, typeof(List<int>));

            Assert.That(list, Is.EqualTo(ListIntExpectation));
        }

        [Test]
        public void ConvertTableToTypeHandlesEnumerableInterface()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(5), DynValue.NewNumber(6));

            IEnumerable<int> enumerable =
                (IEnumerable<int>)
                    TableConversions.ConvertTableToType(table, typeof(IEnumerable<int>));

            Assert.That(enumerable, Is.EqualTo(EnumerableIntExpectation));
        }

        [Test]
        public void ConvertTableToTypeHandlesGenericDictionary()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("alpha"), DynValue.NewNumber(1)),
                (DynValue.NewString("beta"), DynValue.NewNumber(2))
            );

            Dictionary<string, int> dictionary =
                (Dictionary<string, int>)
                    TableConversions.ConvertTableToType(table, typeof(Dictionary<string, int>));

            Assert.That(dictionary["alpha"], Is.EqualTo(1));
            Assert.That(dictionary["beta"], Is.EqualTo(2));
        }

        [Test]
        public void ConvertTableToTypeHandlesDictionaryInterface()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("pi"), DynValue.NewNumber(3.14))
            );

            IDictionary<string, double> dictionary =
                (IDictionary<string, double>)
                    TableConversions.ConvertTableToType(table, typeof(IDictionary<string, double>));

            Assert.That(dictionary["pi"], Is.EqualTo(3.14).Within(0.0001));
        }

        [Test]
        public void ConvertTableToTypeHandlesArrayOfGenericType()
        {
            Table table = CreateSequentialTable(
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            int[] result = (int[])TableConversions.ConvertTableToType(table, typeof(int[]));

            Assert.That(result, Is.EqualTo(GenericArrayExpectation));
        }

        [Test]
        public void ConvertTableToTypeReturnsNullForUnsupportedTarget()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(1));

            object result = TableConversions.ConvertTableToType(table, typeof(ValueType));

            Assert.That(result, Is.Null);
        }

        private static Table CreateSequentialTable(params DynValue[] values)
        {
            Table table = new(new Script());
            for (int i = 0; i < values.Length; i++)
            {
                table.Set(i + 1, values[i]);
            }

            return table;
        }

        private static Table CreateDictionaryTable(params (DynValue Key, DynValue Value)[] entries)
        {
            Table table = new(new Script());
            foreach ((DynValue Key, DynValue Value) entry in entries)
            {
                table.Set(entry.Key, entry.Value);
            }

            return table;
        }
    }
}
