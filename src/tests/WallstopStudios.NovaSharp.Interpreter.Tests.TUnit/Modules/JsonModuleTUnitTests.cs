namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Serialization.Json;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [UserDataIsolation]
    public sealed class JsonModuleTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public async Task EncodeProducesCanonicalObject(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
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
            await Assert
                .That(jsonString.Contains("\"answer\":42", System.StringComparison.Ordinal))
                .IsTrue();
            await Assert
                .That(jsonString.Contains("\"enabled\":true", System.StringComparison.Ordinal))
                .IsTrue();
        }

        [Test]
        [AllLuaVersions]
        public async Task DecodeBuildsLuaTable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.DoString(
                "local m = require('json'); json = { encode = m.serialize, decode = m.parse };"
            );
            DynValue result = script.DoString(
                @"
                local data = json.decode('{""name"":""nova"",""values"":[10,20]}')
                return data.name, data.values[1], data.values[2]
            "
            );

            await Assert.That(result.Tuple[0].String).IsEqualTo("nova");
            await Assert.That(result.Tuple[1].Number).IsEqualTo(10);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(20);
        }

        [Test]
        [AllLuaVersions]
        public async Task ParseThrowsScriptRuntimeExceptionOnInvalidJson(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue jsonModule = script.DoString("return require('json')");
            DynValue parse = jsonModule.Table.Get("parse");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(parse, DynValue.NewString("{invalid"))
            );
            await Assert.That(exception).IsNotNull();
        }

        [Test]
        [AllLuaVersions]
        public async Task SerializeThrowsScriptRuntimeExceptionOnNonTable(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            DynValue jsonModule = script.DoString("return require('json')");
            DynValue serialize = jsonModule.Table.Get("serialize");

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.Call(serialize, DynValue.NewString("oops"))
            );
            await Assert.That(exception).IsNotNull();
        }

        [Test]
        [AllLuaVersions]
        public async Task IsNullDetectsJsonNullAndNil(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local json = require('json')
                return json.isnull(json.null()),
                       json.isnull(nil),
                       json.isnull(false),
                       json.isnull({})
            "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsFalse();
        }

        [Test]
        public async Task NullReturnsJsonNullDynValue()
        {
            DynValue value = JsonNull.Create();

            await Assert.That(value.Type).IsEqualTo(DataType.UserData);
            await Assert.That(JsonNull.IsJsonNull(value)).IsTrue();
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }
    }
}
