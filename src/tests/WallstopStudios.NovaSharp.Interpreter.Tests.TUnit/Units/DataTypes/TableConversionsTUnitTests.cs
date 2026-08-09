namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableConversionsTUnitTests
    {
        private static readonly int[] ListIntExpectation = { 3, 4 };
        private static readonly int[] EnumerableIntExpectation = { 5, 6 };
        private static readonly int[] GenericArrayExpectation = { 1, 2, 3 };

        [global::TUnit.Core.Test]
        public async Task CanConvertTableToTypeRecognizesBuiltInCollections()
        {
            Type[] targets = new[]
            {
                typeof(Dictionary<object, object>),
                typeof(Dictionary<LuaValue, LuaValue>),
                typeof(List<object>),
                typeof(List<LuaValue>),
                typeof(object[]),
                typeof(LuaValue[]),
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
        [AllLuaVersions]
        public async Task ConvertIListToTableCopiesValues(LuaCompatibilityVersion version)
        {
            IList values = new ArrayList { 1, "two" };

            Table table = TableConversions.ConvertIListToTable(new Script(version), values);

            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(table.Get(1).Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(table.Get(2).String).IsEqualTo("two").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConvertIDictionaryToTablePreservesEntries(LuaCompatibilityVersion version)
        {
            IDictionary dictionary = new Hashtable { ["name"] = "NovaSharp", ["version"] = 5 };

            Table table = TableConversions.ConvertIDictionaryToTable(
                new Script(version),
                dictionary
            );

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
                (LuaValue.NewString("one"), LuaValue.NewNumber(1)),
                (LuaValue.NewString("two"), LuaValue.NewString("second"))
            );

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<object, object>)
            );

            await Assert.That(result).IsTypeOf<Dictionary<object, object>>().ConfigureAwait(false);
            Dictionary<object, object> dictionary = (Dictionary<object, object>)result;
            // Numeric values may come back as long or double depending on internal representation
            await Assert
                .That(Convert.ToDouble(dictionary["one"], CultureInfo.InvariantCulture))
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert.That(dictionary["two"]).IsEqualTo("second").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDictionaryOfDynValues()
        {
            LuaValue key = LuaValue.NewString("key");
            LuaValue value = LuaValue.NewNumber(42);
            Table table = CreateDictionaryTable((key, value));

            object result = TableConversions.ConvertTableToType(
                table,
                typeof(Dictionary<LuaValue, LuaValue>)
            );

            await Assert
                .That(result)
                .IsTypeOf<Dictionary<LuaValue, LuaValue>>()
                .ConfigureAwait(false);
            Dictionary<LuaValue, LuaValue> dictionary = (Dictionary<LuaValue, LuaValue>)result;
            await Assert.That(dictionary[key]).IsEqualTo(value).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesListOfObjects()
        {
            Table table = CreateSequentialTable(
                LuaValue.NewNumber(10),
                LuaValue.NewString("value")
            );

            object result = TableConversions.ConvertTableToType(table, typeof(List<object>));

            await Assert.That(result).IsTypeOf<List<object>>().ConfigureAwait(false);
            List<object> list = (List<object>)result;
            await Assert.That(list.Count).IsEqualTo(2).ConfigureAwait(false);
            // Numeric values may come back as long or double depending on internal representation
            await Assert
                .That(Convert.ToDouble(list[0], CultureInfo.InvariantCulture))
                .IsEqualTo(10d)
                .ConfigureAwait(false);
            await Assert.That(list[1]).IsEqualTo("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesListOfDynValues()
        {
            LuaValue first = LuaValue.NewNumber(1);
            LuaValue second = LuaValue.NewString("two");
            Table table = CreateSequentialTable(first, second);

            object result = TableConversions.ConvertTableToType(table, typeof(List<LuaValue>));

            await Assert.That(result).IsTypeOf<List<LuaValue>>().ConfigureAwait(false);
            List<LuaValue> list = (List<LuaValue>)result;
            await AssertSequenceSameReferences(list, new[] { first, second }).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesObjectArray()
        {
            Table table = CreateSequentialTable(LuaValue.NewNumber(1), LuaValue.NewString("two"));

            object[] array = (object[])TableConversions.ConvertTableToType(table, typeof(object[]));

            await Assert.That(array.Length).IsEqualTo(2).ConfigureAwait(false);
            // Numeric values may come back as long or double depending on internal representation
            await Assert
                .That(Convert.ToDouble(array[0], CultureInfo.InvariantCulture))
                .IsEqualTo(1d)
                .ConfigureAwait(false);
            await Assert.That(array[1]).IsEqualTo("two").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesDynValueArray()
        {
            LuaValue first = LuaValue.NewNumber(7);
            LuaValue second = LuaValue.True;
            Table table = CreateSequentialTable(first, second);

            LuaValue[] array = (LuaValue[])
                TableConversions.ConvertTableToType(table, typeof(LuaValue[]));

            await AssertSequenceSameReferences(array, new[] { first, second })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesGenericList()
        {
            Table table = CreateSequentialTable(LuaValue.NewNumber(3), LuaValue.NewNumber(4));

            List<int> list =
                (List<int>)TableConversions.ConvertTableToType(table, typeof(List<int>));

            await AssertSequenceEqual(list, ListIntExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeHandlesEnumerableInterface()
        {
            Table table = CreateSequentialTable(LuaValue.NewNumber(5), LuaValue.NewNumber(6));

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
                (LuaValue.NewString("alpha"), LuaValue.NewNumber(1)),
                (LuaValue.NewString("beta"), LuaValue.NewNumber(2))
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
                (LuaValue.NewString("pi"), LuaValue.NewNumber(3.14))
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
                LuaValue.NewNumber(1),
                LuaValue.NewNumber(2),
                LuaValue.NewNumber(3)
            );

            int[] result = (int[])TableConversions.ConvertTableToType(table, typeof(int[]));

            await AssertSequenceEqual(result, GenericArrayExpectation).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConvertTableToTypeReturnsNullForUnsupportedTarget()
        {
            Table table = CreateSequentialTable(LuaValue.NewNumber(1));

            object result = TableConversions.ConvertTableToType(table, typeof(ValueType));

            await Assert.That(result).IsNull().ConfigureAwait(false);
        }

        private static async Task AssertSequenceEqual<T>(IReadOnlyList<T> actual, T[] expected)
        {
            await Assert.That(actual.Count).IsEqualTo(expected.Length).ConfigureAwait(false);
            for (int i = 0; i < expected.Length; i++)
            {
                await Assert.That(actual[i]).IsEqualTo(expected[i]).ConfigureAwait(false);
            }
        }

        private static async Task AssertSequenceSameReferences(
            IReadOnlyList<LuaValue> actual,
            LuaValue[] expected
        )
        {
            await Assert.That(actual.Count).IsEqualTo(expected.Length).ConfigureAwait(false);
            for (int i = 0; i < expected.Length; i++)
            {
                await Assert.That(actual[i]).IsEqualTo(expected[i]).ConfigureAwait(false);
            }
        }

        private static Table CreateSequentialTable(params LuaValue[] values)
        {
            Table table = new(new Script());
            for (int i = 0; i < values.Length; i++)
            {
                table.Set(i + 1, values[i]);
            }

            return table;
        }

        private static Table CreateDictionaryTable(params (LuaValue Key, LuaValue Value)[] entries)
        {
            Table table = new(new Script());
            foreach ((LuaValue Key, LuaValue Value) entry in entries)
            {
                table.Set(entry.Key, entry.Value);
            }

            return table;
        }
    }
}
