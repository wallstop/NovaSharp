namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;

    public sealed class TableConversionsTUnitTests
    {
        private static readonly int[] ListIntExpectation = { 3, 4 };
        private static readonly int[] EnumerableIntExpectation = { 5, 6 };
        private static readonly object[] ObjectArrayExpectation = { 1d, "two" };
        private static readonly int[] GenericArrayExpectation = { 1, 2, 3 };

        [global::TUnit.Core.Test]
        public async Task CanConvertTableToTypeRecognizesBuiltInCollections()
        {
            Type[] targets = new[]
            {
                typeof(Dictionary<object, object>),
                typeof(Dictionary<DynValue, DynValue>),
                typeof(List<object>),
                typeof(List<DynValue>),
                typeof(object[]),
                typeof(DynValue[]),
            };

            foreach (Type target in targets)
            {
                await Assert
                    .That(TableConversions.CanConvertTableToType(null, target))
                    .IsTrue()
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CanConvertTableToTypeRecognizesGenericCollections()
        {
            Type[] targets = new[]
            {
                typeof(List<int>),
                typeof(IList<string>),
                typeof(ICollection<double>),
                typeof(IEnumerable<decimal>),
                typeof(Dictionary<string, int>),
                typeof(IDictionary<int, string>),
            };

            foreach (Type target in targets)
            {
                await Assert
                    .That(TableConversions.CanConvertTableToType(null, target))
                    .IsTrue()
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task CanConvertTableToTypeRecognizesArrays()
        {
            await Assert
                .That(TableConversions.CanConvertTableToType(null, typeof(int[])))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(TableConversions.CanConvertTableToType(null, typeof(string[])))
                .IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CanConvertTableToTypeRejectsUnsupportedTypes()
        {
            await Assert
                .That(TableConversions.CanConvertTableToType(null, typeof(ValueType)))
                .IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ConvertIListToTableCopiesValues()
        {
            IList values = new ArrayList { 1, "two" };

            Table table = TableConversions.ConvertIListToTable(new Script(), values);

            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(table.Get(1).Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(table.Get(2).String).IsEqualTo("two").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertIDictionaryToTablePreservesEntries()
        {
            IDictionary dictionary = new Hashtable { ["name"] = "NovaSharp", ["version"] = 5 };

            Table table = TableConversions.ConvertIDictionaryToTable(new Script(), dictionary);

            await Assert
                .That(table.Get("name").String)
                .IsEqualTo("NovaSharp")
                .ConfigureAwait(false);
            await Assert.That(table.Get("version").Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDictionaryOfObjects()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("one"), DynValue.NewNumber(1)),
                (DynValue.NewString("two"), DynValue.NewString("second"))
            );

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<object, object>)
            );

            await Assert.That(result).IsTypeOf<Dictionary<object, object>>().ConfigureAwait(false);
            Dictionary<object, object> dictionary = (Dictionary<object, object>)result;
            await Assert.That(dictionary["one"]).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(dictionary["two"]).IsEqualTo("second").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDictionaryOfDynValues()
        {
            DynValue key = DynValue.NewString("key");
            DynValue value = DynValue.NewNumber(42);
            Table table = CreateDictionaryTable((key, value));

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<DynValue, DynValue>)
            );

            await Assert
                .That(result)
                .IsTypeOf<Dictionary<DynValue, DynValue>>()
                .ConfigureAwait(false);
            Dictionary<DynValue, DynValue> dictionary = (Dictionary<DynValue, DynValue>)result;
            await Assert.That(dictionary[key]).IsSameReferenceAs(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesListOfObjects()
        {
            Table table = CreateSequentialTable(
                DynValue.NewNumber(10),
                DynValue.NewString("value")
            );

            object result = TableConversions.ConvertTableToType(table, typeof(List<object>));

            await Assert.That(result).IsTypeOf<List<object>>().ConfigureAwait(false);
            List<object> list = (List<object>)result;
            await AssertSequenceEqual(list, new object[] { 10d, "value" }).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesListOfDynValues()
        {
            DynValue first = DynValue.NewNumber(1);
            DynValue second = DynValue.NewString("two");
            Table table = CreateSequentialTable(first, second);

            object result = TableConversions.ConvertTableToType(table, typeof(List<DynValue>));

            await Assert.That(result).IsTypeOf<List<DynValue>>().ConfigureAwait(false);
            List<DynValue> list = (List<DynValue>)result;
            await AssertSequenceSameReferences(list, new[] { first, second }).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesObjectArray()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(1), DynValue.NewString("two"));

            object[] array = (object[])TableConversions.ConvertTableToType(table, typeof(object[]));

            await AssertSequenceEqual(array, ObjectArrayExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDynValueArray()
        {
            DynValue first = DynValue.NewNumber(7);
            DynValue second = DynValue.True;
            Table table = CreateSequentialTable(first, second);

            DynValue[] array = (DynValue[])
                TableConversions.ConvertTableToType(table, typeof(DynValue[]));

            await AssertSequenceSameReferences(array, new[] { first, second })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesGenericList()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(3), DynValue.NewNumber(4));

            List<int> list =
                (List<int>)TableConversions.ConvertTableToType(table, typeof(List<int>));

            await AssertSequenceEqual(list, ListIntExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesEnumerableInterface()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(5), DynValue.NewNumber(6));

            IEnumerable<int> enumerable =
                (IEnumerable<int>)
                    TableConversions.ConvertTableToType(table, typeof(IEnumerable<int>));

            List<int> actual = new(enumerable);
            await AssertSequenceEqual(actual, EnumerableIntExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesGenericDictionary()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("alpha"), DynValue.NewNumber(1)),
                (DynValue.NewString("beta"), DynValue.NewNumber(2))
            );

            Dictionary<string, int> dictionary =
                (Dictionary<string, int>)
                    TableConversions.ConvertTableToType(table, typeof(Dictionary<string, int>));

            await Assert.That(dictionary["alpha"]).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(dictionary["beta"]).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDictionaryInterface()
        {
            Table table = CreateDictionaryTable(
                (DynValue.NewString("pi"), DynValue.NewNumber(3.14))
            );

            IDictionary<string, double> dictionary =
                (IDictionary<string, double>)
                    TableConversions.ConvertTableToType(table, typeof(IDictionary<string, double>));

            await Assert
                .That(dictionary["pi"])
                .IsEqualTo(3.14)
                .Within(0.0001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesArrayOfGenericType()
        {
            Table table = CreateSequentialTable(
                DynValue.NewNumber(1),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            int[] result = (int[])TableConversions.ConvertTableToType(table, typeof(int[]));

            await AssertSequenceEqual(result, GenericArrayExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeReturnsNullForUnsupportedTarget()
        {
            Table table = CreateSequentialTable(DynValue.NewNumber(1));

            object result = TableConversions.ConvertTableToType(table, typeof(ValueType));

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        private static async Task AssertSequenceEqual<T>(
            IReadOnlyList<T> actual,
            IReadOnlyList<T> expected
        )
        {
            await Assert.That(actual.Count).IsEqualTo(expected.Count).ConfigureAwait(false);
            for (int i = 0; i < expected.Count; i++)
            {
                await Assert.That(actual[i]).IsEqualTo(expected[i]).ConfigureAwait(false);
            }
        }

        private static async Task AssertSequenceSameReferences(
            IReadOnlyList<DynValue> actual,
            DynValue[] expected
        )
        {
            await Assert.That(actual.Count).IsEqualTo(expected.Length).ConfigureAwait(false);
            for (int i = 0; i < expected.Length; i++)
            {
                await Assert.That(actual[i]).IsSameReferenceAs(expected[i]).ConfigureAwait(false);
            }
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
