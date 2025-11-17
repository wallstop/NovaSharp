namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Serialization.Json;
    using NUnit.Framework;

    [TestFixture]
    public sealed class JsonTableConverterTests
    {
        [Test]
        public void TableToJsonSkipsIncompatibleObjectEntries()
        {
            Table table = new(null);
            table.Set("valid", DynValue.NewString("value"));
            table.Set("nullEntry", JsonNull.Create());
            table.Set("unsupported", DynValue.NewCallback((_, _) => DynValue.True));

            string json = JsonTableConverter.TableToJson(table);
            Table roundTrip = JsonTableConverter.JsonToTable(json);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.Length, Is.EqualTo(0));
                Assert.That(roundTrip.Get("valid").String, Is.EqualTo("value"));
                Assert.That(JsonNull.IsJsonNull(roundTrip.Get("nullEntry")), Is.True);
                Assert.That(
                    roundTrip.Get("unsupported").IsNil(),
                    Is.True,
                    "Incompatible types are omitted."
                );
            });
        }

        [Test]
        public void TableToJsonSkipsIncompatibleArrayEntries()
        {
            Table array = new(null);
            array.Append(DynValue.NewNumber(1));
            array.Append(DynValue.NewCallback((_, _) => DynValue.Nil));
            array.Append(JsonNull.Create());

            string json = JsonTableConverter.TableToJson(array);
            Table roundTrip = JsonTableConverter.JsonToTable(json);

            Assert.Multiple(() =>
            {
                Assert.That(roundTrip.Length, Is.EqualTo(2));
                Assert.That(roundTrip.Get(1).Number, Is.EqualTo(1));
                Assert.That(JsonNull.IsJsonNull(roundTrip.Get(2)), Is.True);
            });
        }

        [Test]
        public void JsonToTableThrowsWhenRootIsNotObjectOrArray()
        {
            const string json = "\"orphan\"";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable(json)
            )!;

            AssertUnexpectedToken(exception);
        }

        [Test]
        public void JsonToTableThrowsWhenKeyValueColonIsMissing()
        {
            const string json = "{ \"value\" 1 }";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable(json)
            )!;

            AssertUnexpectedToken(exception);
        }

        [Test]
        public void JsonToTableThrowsWhenMinusIsNotFollowedByNumber()
        {
            const string json = "{ \"value\": -true }";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable(json)
            )!;

            AssertUnexpectedToken(exception);
        }

        [Test]
        public void JsonToTableThrowsWhenValueTokenIsUnknown()
        {
            const string json = "{ \"value\": foo }";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable(json)
            )!;

            AssertUnexpectedToken(exception);
        }

        private static void AssertUnexpectedToken(SyntaxErrorException exception)
        {
            string message = exception.DecoratedMessage ?? exception.Message ?? string.Empty;
            Assert.That(message, Does.Contain("Unexpected token"));
        }
    }
}
