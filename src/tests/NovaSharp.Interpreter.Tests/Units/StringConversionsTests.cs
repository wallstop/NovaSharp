namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.Converters;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringConversionsTests
    {
        [Test]
        public void GetStringSubtypeReturnsExpectedValues()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    StringConversions.GetStringSubtype(typeof(string)),
                    Is.EqualTo(StringConversions.StringSubtype.String)
                );
                Assert.That(
                    StringConversions.GetStringSubtype(typeof(StringBuilder)),
                    Is.EqualTo(StringConversions.StringSubtype.StringBuilder)
                );
                Assert.That(
                    StringConversions.GetStringSubtype(typeof(char)),
                    Is.EqualTo(StringConversions.StringSubtype.Char)
                );
                Assert.That(
                    StringConversions.GetStringSubtype(typeof(int)),
                    Is.EqualTo(default(StringConversions.StringSubtype))
                );
            });
        }

        [Test]
        public void ConvertStringReturnsMatchingTypes()
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

            Assert.Multiple(() =>
            {
                Assert.That(stringResult, Is.EqualTo(Payload));
                Assert.That(builderResult, Is.TypeOf<StringBuilder>());
                Assert.That(builderResult.ToString(), Is.EqualTo(Payload));
                Assert.That(charResult, Is.TypeOf<char>().And.EqualTo('N'));
            });
        }

        [Test]
        public void ConvertStringThrowsWhenCharRequestedFromEmptyString()
        {
            Assert.That(
                () =>
                    StringConversions.ConvertString(
                        StringConversions.StringSubtype.Char,
                        string.Empty,
                        typeof(char),
                        DataType.String
                    ),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot convert")
            );
        }

        [Test]
        public void ConvertStringThrowsWhenSubtypeIsNone()
        {
            Assert.That(
                () =>
                    StringConversions.ConvertString(
                        default,
                        "fallback",
                        typeof(object),
                        DataType.String
                    ),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("cannot convert")
            );
        }
    }
}
