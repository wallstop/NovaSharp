namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class VarargsTupleTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VarArgsTupleBasic(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "f(3)", "a: 3 b: nil").ConfigureAwait(false);
            await ExpectAsync(version, "f(3,4)", "a: 3 b: 4").ConfigureAwait(false);
            await ExpectAsync(version, "f(3,4,5)", "a: 3 b: 4").ConfigureAwait(false);
            await ExpectAsync(version, "f(r(),10)", "a: 1 b: 10").ConfigureAwait(false);
            await ExpectAsync(version, "f(r())", "a: 1 b: 2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VarArgsTupleIntermediate(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "g(3)", "a: 3 b: nil arg: {}").ConfigureAwait(false);
            await ExpectAsync(version, "g(3,4)", "a: 3 b: 4 arg: {}").ConfigureAwait(false);
            await ExpectAsync(version, "g(3,4,5,8)", "a: 3 b: 4 arg: {5, 8, }")
                .ConfigureAwait(false);
            await ExpectAsync(version, "g(5,r())", "a: 5 b: 1 arg: {2, 3, }").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VarArgsTupleAdvanced(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "h(5,r())", "a: 5 b: 1 arg: {2, 3, }").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VarArgsTupleAdvanced2(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "i(3)", "a: extra b: 3 arg: {}").ConfigureAwait(false);
            await ExpectAsync(version, "i(3,4)", "a: extra b: 3 arg: {4, }").ConfigureAwait(false);
            await ExpectAsync(version, "i(3,4,5,8)", "a: extra b: 3 arg: {4, 5, 8, }")
                .ConfigureAwait(false);
            await ExpectAsync(version, "i(5,r())", "a: extra b: 5 arg: {1, 2, 3, }")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task VarArgsTupleDontCrash(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, default(CoreModules));
            const string lua =
                @"
                function Obj(...)
                    local args = { ... }
                end
                Obj(1)
                ";
            script.DoString(lua);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private static async Task ExpectAsync(
            LuaCompatibilityVersion version,
            string code,
            string expected
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function f(a,b)
                    local debug = 'a: ' .. tostring(a) .. ' b: ' .. tostring(b)
                    return debug
                end

                function g(a, b, ...)
                    local debug = 'a: ' .. tostring(a) .. ' b: ' .. tostring(b)
                    local arg = {...}
                    debug = debug .. ' arg: {'
                    for k, v in pairs(arg) do
                        debug = debug .. tostring(v) .. ', '
                    end
                    debug = debug .. '}'
                    return debug
                end

                function r()
                    return 1, 2, 3
                end

                function h(...)
                    return g(...)
                end

                function i(...)
                    return g('extra', ...)
                end
                "
            );

            DynValue result = script.DoString("return " + code);
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo(expected).ConfigureAwait(false);
        }
    }
}
