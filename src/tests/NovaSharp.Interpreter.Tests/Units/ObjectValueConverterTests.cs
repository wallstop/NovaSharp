namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ObjectValueConverterTests
    {
        [Test]
        public void SerializeObjectToDynValueUsesCustomNullReplacement()
        {
            Script script = new();
            DynValue fallback = DynValue.NewString("missing");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(script, null, fallback);

            Assert.That(result, Is.SameAs(fallback));
        }

        [Test]
        public void SerializeObjectToDynValueCapturesInstanceAndStaticProperties()
        {
            Script script = new();
            SampleObject sample = new("value");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(script, sample);
            Table serialized = result.Table;

            Assert.Multiple(() =>
            {
                Assert.That(serialized.Get(nameof(SampleObject.InstanceValue)).String, Is.EqualTo("value"));
                Assert.That(serialized.Get(nameof(SampleObject.StaticNumber)).Number, Is.EqualTo(SampleObject.StaticNumber));
            });
        }

        [Test]
        public void SerializeObjectToDynValueEnumeratesListsAndEnums()
        {
            Script script = new();
            List<object> payload = new()
            {
                SampleEnum.Second,
                null,
                "tail",
            };
            DynValue fallback = DynValue.NewString("missing");

            DynValue result = ObjectValueConverter.SerializeObjectToDynValue(script, payload, fallback);
            Table serialized = result.Table;

            Assert.Multiple(() =>
            {
                Assert.That(serialized.Length, Is.EqualTo(3));
                Assert.That(serialized.Get(1).Number, Is.EqualTo((double)SampleEnum.Second));
                Assert.That(serialized.Get(2).String, Is.EqualTo("missing"));
                Assert.That(serialized.Get(3).String, Is.EqualTo("tail"));
            });
        }

        private sealed class SampleObject
        {
            public SampleObject(string value)
            {
                InstanceValue = value;
            }

            public string InstanceValue { get; }

            public static int StaticNumber => 42;
        }

        private enum SampleEnum
        {
            First = 1,
            Second = 2,
        }
    }
}
