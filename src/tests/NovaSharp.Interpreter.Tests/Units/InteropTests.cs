namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using Interpreter;
    using NUnit.Framework;

    [TestFixture]
    public class InteropTests
    {
        [Test]
        public void ConverterFromObjectCoversPrimitiveAndNullableCases()
        {
            Script script = new();

            DynValue directNumber = DynValue.FromObject(script, 42);
            Assert.Multiple(() =>
            {
                Assert.That(directNumber.Type, Is.EqualTo(DataType.Number));
                Assert.That(directNumber.Number, Is.EqualTo(42));
            });

            int? nullableHasValue = 7;
            DynValue fromNullable = DynValue.FromObject(script, nullableHasValue);
            Assert.Multiple(() =>
            {
                Assert.That(fromNullable.Type, Is.EqualTo(DataType.Number));
                Assert.That(fromNullable.Number, Is.EqualTo(7));
            });

            int? nullableNull = null;
            DynValue fromNull = DynValue.FromObject(script, nullableNull);
            Assert.That(fromNull.Type, Is.EqualTo(DataType.Nil));
        }

        [Test]
        public void ConverterFromObjectMarshalsDictionariesToLuaTables()
        {
            Script script = new();
            Dictionary<string, int> dictionary = new() { ["a"] = 1, ["b"] = 2 };

            DynValue dyn = DynValue.FromObject(script, dictionary);

            Assert.Multiple(() =>
            {
                Assert.That(dyn.Type, Is.EqualTo(DataType.Table));
                Assert.That(dyn.Table.Length, Is.EqualTo(0)); // dictionary entries are hash-only
                Assert.That(dyn.Table.Get("a").Number, Is.EqualTo(1));
                Assert.That(dyn.Table.Get("b").Number, Is.EqualTo(2));
            });
        }

        [Test]
        public void TableArgumentsAreConvertedToClrDictionaryParameters()
        {
            Script script = new()
            {
                Globals =
                {
                    ["sum"] = (Func<IDictionary<string, int>, int>)(dict => dict["x"] + dict["y"]),
                },
            };

            DynValue result = script.DoString("return sum({ x = 10, y = 32 })");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void ObjectRoundTripTableToClrObjectAndBack()
        {
            Script script = new();
            DynValue payload = DynValue.FromObject(
                script,
                new Dictionary<string, string> { ["name"] = "nova", ["role"] = "tester" }
            );
            script.Globals["payload"] = payload;
            script.Globals["echo"] =
                (Func<IDictionary<string, string>, IDictionary<string, string>>)(
                    dict =>
                    {
                        dict["role"] = dict["role"].ToUpperInvariant();
                        return dict;
                    }
                );

            DynValue mutated = script.DoString("return echo(payload)");
            DynValue tableAfterCall = DynValue.FromObject(
                script,
                mutated.ToObject<IDictionary<string, string>>()
            );

            Assert.Multiple(() =>
            {
                Assert.That(mutated.Table.Get("name").String, Is.EqualTo("nova"));
                Assert.That(mutated.Table.Get("role").String, Is.EqualTo("TESTER"));
                Assert.That(tableAfterCall.Table.Get("role").String, Is.EqualTo("TESTER"));
            });
        }
    }
}
