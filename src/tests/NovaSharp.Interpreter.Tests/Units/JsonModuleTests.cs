namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Serialization.Json;
    using NUnit.Framework;

    [TestFixture]
    public class JsonModuleTests
    {
        [Test]
        public void EncodeProducesCanonicalObject()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                "local m = require('json'); json = { encode = m.serialize, decode = m.parse };"
            );
            script.DoString(
                @"
                value = {
                    answer = 42,
                    enabled = true,
                    items = { 1, 2, 3 }
                }
                jsonString = json.encode(value)
            "
            );

            string jsonString = script.Globals.Get("jsonString").String;
            Assert.Multiple(() =>
            {
                Assert.That(jsonString, Does.Contain("\"answer\":42"));
                Assert.That(jsonString, Does.Contain("\"enabled\":true"));
            });
        }

        [Test]
        public void DecodeBuildsLuaTable()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                "local m = require('json'); json = { encode = m.serialize, decode = m.parse };"
            );
            DynValue result = script.DoString(
                @"
                local data = json.decode('{""name"":""nova"",""values"":[10,20]}')
                return data.name, data.values[1], data.values[2]
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].String, Is.EqualTo("nova"));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(10));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(20));
            });
        }

        [Test]
        public void ParseThrowsScriptRuntimeExceptionOnInvalidJson()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue jsonModule = script.DoString("return require('json')");
            DynValue parse = jsonModule.Table.Get("parse");

            Assert.That(
                () => script.Call(parse, DynValue.NewString("{invalid")),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void SerializeThrowsScriptRuntimeExceptionOnNonTable()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue jsonModule = script.DoString("return require('json')");
            DynValue serialize = jsonModule.Table.Get("serialize");

            Assert.That(
                () => script.Call(serialize, DynValue.NewString("oops")),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void IsNullDetectsJsonNullAndNil()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue result = script.DoString(
                @"
                local json = require('json')
                return json.isnull(json.null()),
                       json.isnull(nil),
                       json.isnull(false),
                       json.isnull({})
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Boolean, Is.True);
                Assert.That(result.Tuple[1].Boolean, Is.True);
                Assert.That(result.Tuple[2].Boolean, Is.False);
                Assert.That(result.Tuple[3].Boolean, Is.False);
            });
        }

        [Test]
        public void NullReturnsJsonNullDynValue()
        {
            DynValue value = JsonNull.Create();
            Assert.Multiple(() =>
            {
                Assert.That(value.Type, Is.EqualTo(DataType.UserData));
                Assert.That(JsonNull.IsJsonNull(value), Is.True);
            });
        }
    }
}
