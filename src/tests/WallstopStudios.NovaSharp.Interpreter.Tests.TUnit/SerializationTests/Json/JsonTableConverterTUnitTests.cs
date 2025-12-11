namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.SerializationTests.Json
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Serialization.Json;

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

            await Assert.That(roundTrip.Length).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(roundTrip.Get("valid").String)
                .IsEqualTo("value")
                .ConfigureAwait(false);
            await Assert
                .That(JsonNull.IsJsonNull(roundTrip.Get("nullEntry")))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(roundTrip.Get("unsupported").IsNil()).IsTrue().ConfigureAwait(false);
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

            await Assert.That(roundTrip.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(roundTrip.Get(1).Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(JsonNull.IsJsonNull(roundTrip.Get(2))).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenRootIsNotObjectOrArray()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("\"orphan\"")
            );

            await AssertUnexpectedToken(exception).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenKeyValueColonIsMissing()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\" 1 }")
            );

            await AssertUnexpectedToken(exception).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenMinusIsNotFollowedByNumber()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\": -true }")
            );

            await AssertUnexpectedToken(exception).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task JsonToTableThrowsWhenValueTokenIsUnknown()
        {
            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                JsonTableConverter.JsonToTable("{ \"value\": foo }")
            );

            await AssertUnexpectedToken(exception).ConfigureAwait(false);
        }

        private static async Task AssertUnexpectedToken(SyntaxErrorException exception)
        {
            string message = exception.DecoratedMessage ?? exception.Message ?? string.Empty;
            await Assert.That(message).Contains("Unexpected token").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableToJsonPreservesIntegerSubtype()
        {
            // Arrange: Create a table with integer values (using NewInteger)
            Table table = new(null);
            table.Set("integer", DynValue.NewInteger(42));
            table.Set("large", DynValue.NewInteger(9007199254740993L)); // 2^53 + 1 - beyond double precision

            // Act: Serialize to JSON
            string json = table.TableToJson();

            // Assert: Integer values should be serialized without decimal point
            // The integer 42 should appear as "42" not "42.0"
            await Assert.That(json).Contains("\"integer\":42").ConfigureAwait(false);
            // Large integers should preserve exact value
            await Assert.That(json).Contains("\"large\":9007199254740993").ConfigureAwait(false);
            // Should NOT have ".0" appended to integers
            await Assert.That(json).DoesNotContain("42.0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableToJsonPreservesFloatSubtype()
        {
            // Arrange: Create a table with float values (using NewFloat)
            Table table = new(null);
            table.Set("pi", DynValue.NewFloat(3.14159));
            table.Set("zero", DynValue.NewFloat(0.0));

            // Act: Serialize to JSON
            string json = table.TableToJson();

            // Assert: Float values should preserve their representation
            await Assert.That(json).Contains("3.14159").ConfigureAwait(false);
            // 0.0 should be serialized as a float representation
            await Assert.That(json).Contains("\"zero\":0").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableToJsonArrayPreservesIntegerSubtype()
        {
            // Arrange: Create an array with integer values
            Table array = new(null);
            array.Append(DynValue.NewInteger(1));
            array.Append(DynValue.NewInteger(2));
            array.Append(DynValue.NewInteger(3));

            // Act: Serialize to JSON
            string json = array.TableToJson();

            // Assert: Should be a JSON array with integers (no decimal points)
            await Assert.That(json).IsEqualTo("[1,2,3]").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableToJsonLargeIntegerPrecision()
        {
            // Arrange: Create a table with large integers at precision boundaries
            Table table = new(null);
            // 2^53 = 9007199254740992 - largest integer exactly representable as double
            table.Set("maxSafeInt", DynValue.NewInteger(9007199254740992L));
            // 2^53 + 1 = 9007199254740993 - NOT exactly representable as double
            table.Set("beyondPrecision", DynValue.NewInteger(9007199254740993L));
            // Max long value
            table.Set("maxLong", DynValue.NewInteger(long.MaxValue));
            // Min long value
            table.Set("minLong", DynValue.NewInteger(long.MinValue));

            // Act: Serialize to JSON
            string json = table.TableToJson();

            // Assert: All integer values should be serialized with full precision
            await Assert.That(json).Contains("9007199254740992").ConfigureAwait(false);
            await Assert.That(json).Contains("9007199254740993").ConfigureAwait(false);
            await Assert
                .That(json)
                .Contains(long.MaxValue.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
            await Assert
                .That(json)
                .Contains(long.MinValue.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToJsonPreservesIntegerTypes()
        {
            // Arrange: A dictionary with various integer types
            Dictionary<string, object> dict = new()
            {
                { "int", 42 },
                { "long", 9007199254740993L },
                { "byte", (byte)255 },
                { "short", (short)1000 },
            };

            // Act: Serialize to JSON
            string json = JsonTableConverter.ObjectToJson(dict);

            // Assert: Integer values should be serialized without decimal point
            await Assert.That(json).Contains("\"int\":42").ConfigureAwait(false);
            await Assert.That(json).Contains("\"long\":9007199254740993").ConfigureAwait(false);
            await Assert.That(json).Contains("\"byte\":255").ConfigureAwait(false);
            await Assert.That(json).Contains("\"short\":1000").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectToJsonPreservesFloatTypes()
        {
            // Arrange: A dictionary with floating-point types
            Dictionary<string, object> dict = new() { { "double", 3.14159 }, { "float", 2.5f } };

            // Act: Serialize to JSON
            string json = JsonTableConverter.ObjectToJson(dict);

            // Assert: Float values should be serialized correctly
            await Assert.That(json).Contains("3.14159").ConfigureAwait(false);
            await Assert.That(json).Contains("2.5").ConfigureAwait(false);
        }
    }
}
