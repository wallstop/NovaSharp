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
            var script = new Script(CoreModules.Metatables | CoreModules.Basic);

            script.DoString(
                @"
                subject = {}
                setmetatable(subject, {
                    __newindex = function(t, key, value)
                        rawset(t, key, value * 2)
                    end
                })

                subject.value = 5
            "
            );

            var subject = script.Globals.Get("subject").Table;
            Assert.That(subject.Get("value").Number, Is.EqualTo(10));

            subject.Set("value", DynValue.NewNumber(7));
            Assert.That(subject.Get("value").Number, Is.EqualTo(7));
        }
    }
}
