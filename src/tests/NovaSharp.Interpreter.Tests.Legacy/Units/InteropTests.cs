using System;
using System.Collections.Generic;
using NovaSharp.Interpreter;
using NUnit.Framework;

namespace NovaSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class InteropTests
    {
        [Test]
        public void Converter_FromObject_CoversPrimitiveAndNullableCases()
        {
            Script script = new();

            DynValue directNumber = DynValue.FromObject(script, 42);
            Assert.That(directNumber.Type, Is.EqualTo(DataType.Number));
            Assert.That(directNumber.Number, Is.EqualTo(42));

            int? nullableHasValue = 7;
            DynValue fromNullable = DynValue.FromObject(script, nullableHasValue);
            Assert.That(fromNullable.Type, Is.EqualTo(DataType.Number));
            Assert.That(fromNullable.Number, Is.EqualTo(7));

            int? nullableNull = null;
            DynValue fromNull = DynValue.FromObject(script, nullableNull);
            Assert.That(fromNull.Type, Is.EqualTo(DataType.Nil));
        }

        [Test]
        public void Converter_FromObject_MarshalsDictionariesToLuaTables()
        {
            Script script = new();
            Dictionary<string, int> dictionary = new() { ["a"] = 1, ["b"] = 2 };

            DynValue dyn = DynValue.FromObject(script, dictionary);

            Assert.That(dyn.Type, Is.EqualTo(DataType.Table));
            Assert.That(dyn.Table.Length, Is.EqualTo(0)); // dictionary entries are hash-only
            Assert.That(dyn.Table.Get("a").Number, Is.EqualTo(1));
            Assert.That(dyn.Table.Get("b").Number, Is.EqualTo(2));
        }

        [Test]
        public void TableArguments_AreConvertedToClrDictionaryParameters()
        {
            Script script = new();
            script.Globals["sum"] = (System.Func<IDictionary<string, int>, int>)(
                dict => dict["x"] + dict["y"]
            );

            DynValue result = script.DoString("return sum({ x = 10, y = 32 })");

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void ObjectRoundTrip_TableToClrObjectAndBack()
        {
            Script script = new();
            DynValue? payload = DynValue.FromObject(
                script,
                new Dictionary<string, string> { ["name"] = "nova", ["role"] = "tester" }
            );
            script.Globals["payload"] = payload;
            script.Globals["echo"] = (System.Func<
                IDictionary<string, string>,
                IDictionary<string, string>
            >)(
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

            Assert.That(mutated.Table.Get("name").String, Is.EqualTo("nova"));
            Assert.That(mutated.Table.Get("role").String, Is.EqualTo("TESTER"));
            Assert.That(tableAfterCall.Table.Get("role").String, Is.EqualTo("TESTER"));
        }
    }
}
