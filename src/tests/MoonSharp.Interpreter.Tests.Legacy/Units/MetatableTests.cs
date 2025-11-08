using MoonSharp.Interpreter;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class MetatableTests
    {
        [Test]
        public void __IndexMetatableResolvesMissingKeys()
        {
            var script = new Script();
            var table = new Table(script);
            var metatable = new Table(script);

            metatable["__index"] = DynValue.NewCallback(
                (_, args) =>
                {
                    string key = args[1].CastToString();
                    return DynValue.NewString($"missing:{key}");
                }
            );

            table.MetaTable = metatable;
            script.Globals["subject"] = table;

            DynValue result = script.DoString("return subject.someKey");

            Assert.That(result.Type, Is.EqualTo(DataType.String));
            Assert.That(result.String, Is.EqualTo("missing:someKey"));
        }

        [Test]
        public void MetatableRawAccessStillRespectsMetatable()
        {
            var script = new Script();
            var table = new Table(script);
            var metatable = new Table(script);

            table["value"] = DynValue.NewNumber(10);
            metatable["__newindex"] = DynValue.NewCallback(
                (_, args) =>
                {
                    args[0].Table.Set("value", DynValue.NewNumber(args[2].Number * 2));
                    return DynValue.Nil;
                }
            );

            table.MetaTable = metatable;
            script.Globals["subject"] = table;

            script.DoString("subject.value = 5");

            Assert.That(table.Get("value").Number, Is.EqualTo(10));
            Assert.That(table.RawGet("value").Number, Is.EqualTo(10));
            Assert.That(table.Get("value").Number, Is.EqualTo(10));

            table.Set("value", DynValue.NewNumber(7));
            Assert.That(table.Get("value").Number, Is.EqualTo(7));
        }
    }
}
