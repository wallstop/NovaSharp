#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.Converters;

    public sealed class StringConversionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetStringSubtypeReturnsExpectedValues()
        {
            await Assert
                .That(StringConversions.GetStringSubtype(typeof(string)))
                .IsEqualTo(StringConversions.StringSubtype.String);
            await Assert
                .That(StringConversions.GetStringSubtype(typeof(StringBuilder)))
                .IsEqualTo(StringConversions.StringSubtype.StringBuilder);
            await Assert
                .That(StringConversions.GetStringSubtype(typeof(char)))
                .IsEqualTo(StringConversions.StringSubtype.Char);
            await Assert
                .That(StringConversions.GetStringSubtype(typeof(int)))
                .IsEqualTo(default(StringConversions.StringSubtype));
        }

        [global::TUnit.Core.Test]
        public async Task ConvertStringReturnsMatchingTypes()
        {
            const string Payload = "NovaSharp";

            object stringResult = StringConversions.ConvertString(
                StringConversions.StringSubtype.String,
                Payload,
                typeof(string),
                DataType.String
            );
            object builderResult = StringConversions.ConvertString(
                StringConversions.StringSubtype.StringBuilder,
                Payload,
                typeof(StringBuilder),
                DataType.String
            );
            object charResult = StringConversions.ConvertString(
                StringConversions.StringSubtype.Char,
                Payload,
                typeof(char),
                DataType.String
            );

            await Assert.That(stringResult).IsEqualTo(Payload);
            await Assert.That(builderResult).IsTypeOf<StringBuilder>();
            await Assert.That(builderResult.ToString()).IsEqualTo(Payload);
            await Assert.That(charResult).IsTypeOf<char>();
            await Assert.That((char)charResult).IsEqualTo('N');
        }

        [global::TUnit.Core.Test]
        public async Task ConvertStringThrowsWhenCharRequestedFromEmptyString()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                StringConversions.ConvertString(
                    StringConversions.StringSubtype.Char,
                    string.Empty,
                    typeof(char),
                    DataType.String
                )
            );

            await Assert.That(exception.Message).Contains("cannot convert");
        }

        [global::TUnit.Core.Test]
        public async Task ConvertStringThrowsWhenSubtypeIsNone()
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                StringConversions.ConvertString(
                    default,
                    "fallback",
                    typeof(object),
                    DataType.String
                )
            );

            await Assert.That(exception.Message).Contains("cannot convert");
        }
    }
}
#pragma warning restore CA2007
