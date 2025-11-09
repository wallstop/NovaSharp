namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NUnit.Framework;
    using Serialization.Json;

    [TestFixture]
    public class JsonSerializationTests
    {
        private void AssertTableValues(Table t)
        {
            Assert.That(t.Get("aNumber").Type, Is.EqualTo(DataType.Number));
            Assert.That(t.Get("aNumber").Number, Is.EqualTo(1));

            Assert.That(t.Get("aString").Type, Is.EqualTo(DataType.String));
            Assert.That(t.Get("aString").String, Is.EqualTo("2"));

            Assert.That(t.Get("anObject").Type, Is.EqualTo(DataType.Table));
            Assert.That(t.Get("anArray").Type, Is.EqualTo(DataType.Table));

            Assert.That(t.Get("slash").Type, Is.EqualTo(DataType.String));
            Assert.That(t.Get("slash").String, Is.EqualTo("a/b"));

            Table o = t.Get("anObject").Table;

            Assert.That(o.Get("aNumber").Type, Is.EqualTo(DataType.Number));
            Assert.That(o.Get("aNumber").Number, Is.EqualTo(3));

            Assert.That(o.Get("aString").Type, Is.EqualTo(DataType.String));
            Assert.That(o.Get("aString").String, Is.EqualTo("4"));

            Table a = t.Get("anArray").Table;

            //				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ]

            Assert.That(a.Get(1).Type, Is.EqualTo(DataType.Number));
            Assert.That(a.Get(1).Number, Is.EqualTo(5));

            Assert.That(a.Get(2).Type, Is.EqualTo(DataType.String));
            Assert.That(a.Get(2).String, Is.EqualTo("6"));

            Assert.That(a.Get(3).Type, Is.EqualTo(DataType.Boolean));
            Assert.That(a.Get(3).Boolean, Is.True);

            Assert.That(a.Get(3).Type, Is.EqualTo(DataType.Boolean));
            Assert.That(a.Get(3).Boolean, Is.True);

            Assert.That(a.Get(4).Type, Is.EqualTo(DataType.UserData));
            Assert.That(JsonNull.IsJsonNull(a.Get(4)), Is.True);

            Assert.That(a.Get(5).Type, Is.EqualTo(DataType.Table));
            Table s = a.Get(5).Table;

            Assert.That(s.Get("aNumber").Type, Is.EqualTo(DataType.Number));
            Assert.That(s.Get("aNumber").Number, Is.EqualTo(7));

            Assert.That(s.Get("aString").Type, Is.EqualTo(DataType.String));
            Assert.That(s.Get("aString").String, Is.EqualTo("8"));

            Assert.That(t.Get("aNegativeNumber").Type, Is.EqualTo(DataType.Number));
            Assert.That(t.Get("aNegativeNumber").Number, Is.EqualTo(-9));
        }

        [Test]
        public void JsonDeserialization()
        {
            string json = @"{
				'aNumber' : 1,
				'aString' : '2',
				'anObject' : { 'aNumber' : 3, 'aString' : '4' },
				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
				'aNegativeNumber' : -9,
				'slash' : 'a\/b'
				}
			".Replace('\'', '\"');

            Table t = JsonTableConverter.JsonToTable(json);
            AssertTableValues(t);
        }

        [Test]
        public void JsonSerialization()
        {
            string json = @"{
				'aNumber' : 1,
				'aString' : '2',
				'anObject' : { 'aNumber' : 3, 'aString' : '4' },
				'anArray' : [ 5, '6', true, null, { 'aNumber' : 7, 'aString' : '8' } ],
				'aNegativeNumber' : -9,
				'slash' : 'a\/b'
				}
			".Replace('\'', '\"');

            Table t1 = JsonTableConverter.JsonToTable(json);

            string json2 = JsonTableConverter.TableToJson(t1);

            Table t = JsonTableConverter.JsonToTable(json2);

            AssertTableValues(t);
        }

        [Test]
        public void JsonObjectSerialization()
        {
            object o = new
            {
                aNumber = 1,
                aString = "2",
                anObject = new { aNumber = 3, aString = "4" },
                anArray = new object[] { 5, "6", true, null, new { aNumber = 7, aString = "8" } },
                aNegativeNumber = -9,
                slash = "a/b",
            };

            string json = JsonTableConverter.ObjectToJson(o);

            Table t = JsonTableConverter.JsonToTable(json);

            AssertTableValues(t);
        }
    }
}
