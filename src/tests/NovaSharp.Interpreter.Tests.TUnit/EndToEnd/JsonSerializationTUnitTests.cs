#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using Serialization.Json;

    public sealed class JsonSerializationTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task JsonDeserialization()
        {
            string json = @"{
                'aNumber' : 1,
                'aString' : '2',
                'anObject' : { 'aNumber' : 3, 'aString' : '4' },
                'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
                'aNegativeNumber' : -9,
                'slash' : 'a\/b'
                }".Replace('\'', '\"');

            Table table = JsonTableConverter.JsonToTable(json);
            await AssertTableValues(table);
        }

        [global::TUnit.Core.Test]
        public async Task JsonSerializationRoundTrip()
        {
            string json = @"{
                'aNumber' : 1,
                'aString' : '2',
                'anObject' : { 'aNumber' : 3, 'aString' : '4' },
                'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
                'aNegativeNumber' : -9,
                'slash' : 'a\/b'
                }".Replace('\'', '\"');

            Table original = JsonTableConverter.JsonToTable(json);
            string serialized = JsonTableConverter.TableToJson(original);
            Table roundTrip = JsonTableConverter.JsonToTable(serialized);
            await AssertTableValues(roundTrip);
        }

        [global::TUnit.Core.Test]
        public async Task JsonObjectSerialization()
        {
            object payload = new
            {
                aNumber = 1,
                aString = "2",
                anObject = new { aNumber = 3, aString = "4" },
                anArray = new object[] { 5, "6", true, null, new { aNumber = 7, aString = "8" } },
                aNegativeNumber = -9,
                slash = "a/b",
            };

            string json = JsonTableConverter.ObjectToJson(payload);
            Table parsed = JsonTableConverter.JsonToTable(json);
            await AssertTableValues(parsed);
        }

        private static async Task AssertTableValues(Table table)
        {
            await Assert.That(table.Get("aNumber").Number).IsEqualTo(1);
            await Assert.That(table.Get("aString").String).IsEqualTo("2");
            await Assert.That(table.Get("slash").String).IsEqualTo("a/b");

            Table nested = table.Get("anObject").Table;
            await Assert.That(nested.Get("aNumber").Number).IsEqualTo(3);
            await Assert.That(nested.Get("aString").String).IsEqualTo("4");

            Table array = table.Get("anArray").Table;
            await Assert.That(array.Get(1).Number).IsEqualTo(5);
            await Assert.That(array.Get(2).String).IsEqualTo("6");
            await Assert.That(array.Get(3).Boolean).IsTrue();
            await Assert.That(JsonNull.IsJsonNull(array.Get(4))).IsTrue();

            Table obj = array.Get(5).Table;
            await Assert.That(obj.Get("aNumber").Number).IsEqualTo(7);
            await Assert.That(obj.Get("aString").String).IsEqualTo("8");

            await Assert.That(table.Get("aNegativeNumber").Number).IsEqualTo(-9);
        }
    }
}
#pragma warning restore CA2007
