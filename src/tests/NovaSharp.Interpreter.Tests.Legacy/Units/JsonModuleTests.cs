using NovaSharp.Interpreter;
using NUnit.Framework;

namespace NovaSharp.Interpreter.Tests.Units
{
    [TestFixture]
    public class JsonModuleTests
    {
        [Test]
        public void EncodeProducesCanonicalObject()
        {
            Script script = new(CoreModules.Preset_Complete);
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

            string? jsonString = script.Globals.Get("jsonString").String;
            Assert.That(jsonString, Does.Contain("\"answer\":42"));
            Assert.That(jsonString, Does.Contain("\"enabled\":true"));
        }

        [Test]
        public void DecodeBuildsLuaTable()
        {
            Script script = new(CoreModules.Preset_Complete);
            script.DoString(
                "local m = require('json'); json = { encode = m.serialize, decode = m.parse };"
            );
            DynValue? result = script.DoString(
                @"
                local data = json.decode('{""name"":""nova"",""values"":[10,20]}')
                return data.name, data.values[1], data.values[2]
            "
            );

            Assert.That(result.Tuple[0].String, Is.EqualTo("nova"));
            Assert.That(result.Tuple[1].Number, Is.EqualTo(10));
            Assert.That(result.Tuple[2].Number, Is.EqualTo(20));
        }
    }
}
