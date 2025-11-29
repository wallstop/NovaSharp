#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.SerializationTests.Json
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Serialization.Json;

    public sealed class JsonTableConverterTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TableToJsonSkipsIncompatibleObjectEntries()
        {
            Table table = new(null);
            table.Set("valid", DynValue.NewString("value"));
            table.Set("nullEntry", JsonNull.Create());
            table.Set("unsupported", DynValue.NewCallback((_, _) => DynValue.True));

            string json = JsonTableConverter.TableToJson(table);
            Table roundTrip = JsonTableConverter.JsonToTable(json);

            await Assert.That(roundTrip.Length).IsEqualTo(0);
            await Assert.That(roundTrip.Get("valid").String).IsEqualTo("value");
            await Assert.That(JsonNull.IsJsonNull(roundTrip.Get("nullEntry"))).IsTrue();
            await Assert.That(roundTrip.Get("unsupported").IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task TableToJsonSkipsIncompatibleArrayEntries()
        {
            Table array = new(null);
            array.Append(DynValue.NewNumber(1));
            array.Append(DynValue.NewCallback((_, _) => DynValue.Nil));
            array.Append(JsonNull.Create());

            string json = JsonTableConverter.TableToJson(array);
            Table roundTrip = JsonTableConverter.JsonToTable(json);

            await Assert.That(roundTrip.Length).IsEqualTo(2);
            await Assert.That(roundTrip.Get(1).Number).IsEqualTo(1);
            await Assert.That(JsonNull.IsJsonNull(roundTrip.Get(2))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenRootIsNotObjectOrArray()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("\"orphan\"")
            );

            await AssertUnexpectedToken(exception);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenKeyValueColonIsMissing()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\" 1 }")
            );

            await AssertUnexpectedToken(exception);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenMinusIsNotFollowedByNumber()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\": -true }")
            );

            await AssertUnexpectedToken(exception);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenValueTokenIsUnknown()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\": foo }")
            );

            await AssertUnexpectedToken(exception);
        }

        private static async Task AssertUnexpectedToken(SyntaxErrorException exception)
        {
            string message = exception.DecoratedMessage ?? exception.Message ?? string.Empty;
            await Assert.That(message).Contains("Unexpected token");
        }
    }
}
#pragma warning restore CA2007
