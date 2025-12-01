#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class InteropTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ConverterFromObjectCoversPrimitiveAndNullableCases()
        {
            Script script = new();

            DynValue directNumber = DynValue.FromObject(script, 42);
            await Assert.That(directNumber.Type).IsEqualTo(DataType.Number);
            await Assert.That(directNumber.Number).IsEqualTo(42);

            int? nullableHasValue = 7;
            DynValue fromNullable = DynValue.FromObject(script, nullableHasValue);
            await Assert.That(fromNullable.Type).IsEqualTo(DataType.Number);
            await Assert.That(fromNullable.Number).IsEqualTo(7);

            int? nullableNull = null;
            DynValue fromNull = DynValue.FromObject(script, nullableNull);
            await Assert.That(fromNull.Type).IsEqualTo(DataType.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task ConverterFromObjectMarshalsDictionariesToLuaTables()
        {
            Script script = new();
            Dictionary<string, int> dictionary = new() { ["a"] = 1, ["b"] = 2 };

            DynValue dyn = DynValue.FromObject(script, dictionary);

            await Assert.That(dyn.Type).IsEqualTo(DataType.Table);
            await Assert.That(dyn.Table.Length).IsEqualTo(0);
            await Assert.That(dyn.Table.Get("a").Number).IsEqualTo(1);
            await Assert.That(dyn.Table.Get("b").Number).IsEqualTo(2);
        }

        [global::TUnit.Core.Test]
        public async Task TableArgumentsAreConvertedToClrDictionaryParameters()
        {
            Script script = new()
            {
                Globals =
                {
                    ["sum"] = (Func<IDictionary<string, int>, int>)(dict => dict["x"] + dict["y"]),
                },
            };

            DynValue result = script.DoString("return sum({ x = 10, y = 32 })");

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task ObjectRoundTripTableToClrObjectAndBack()
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

            await Assert.That(mutated.Table.Get("name").String).IsEqualTo("nova");
            await Assert.That(mutated.Table.Get("role").String).IsEqualTo("TESTER");
            await Assert.That(tableAfterCall.Table.Get("role").String).IsEqualTo("TESTER");
        }
    }
}
#pragma warning restore CA2007
