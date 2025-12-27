namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class DynamicTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynamicAccessEval(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return dynamic.eval('5+1');");
            await EndToEndDynValueAssert.ExpectAsync(result, 6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynamicAccessPrepare(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local prepared = dynamic.prepare('5+1');
                return dynamic.eval(prepared);
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 6).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynamicAccessScope(LuaCompatibilityVersion version)
        {
            string code =
                @"
                a = 3;
                local prepared = dynamic.prepare('a+1');
                function worker()
                    a = 5;
                    return dynamic.eval(prepared);
                end
                return worker();
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 6).ConfigureAwait(false);
        }

        // _ENV is a Lua 5.2+ feature
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task DynamicAccessScopeSecurityReturnsNil(LuaCompatibilityVersion version)
        {
            string code =
                @"
                a = 5;
                local prepared = dynamic.prepare('a');
                local eval = dynamic.eval;
                local _ENV = { }
                function worker()
                    return eval(prepared);
                end
                return worker();
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await Assert.That(result.Type).IsEqualTo(DataType.Nil).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DynamicAccessFromCSharp(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString("t = { ciao = { 'hello' } }");

            DynValue evaluation = script
                .CreateDynamicExpression("t.ciao[1] .. ' world'")
                .Evaluate();
            await Assert.That(evaluation.String).IsEqualTo("hello world").ConfigureAwait(false);
        }
    }
}
