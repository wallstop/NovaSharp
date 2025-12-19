namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ClosureTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosureOnParam(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local function g (z)
                    local function f(a)
                        return a + z;
                    end
                    return f;
                end
                return g(3)(2);
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 5)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LambdaFunctions(LuaCompatibilityVersion version)
        {
            string code =
                @"
                g = |f, x|f(x, x+1)
                f = |x, y, z|x*(y+z)
                return g(|x,y|f(x,y,1), 2)
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 8)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosureOnParamLambda(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local function g (z)
                    return |a| a + z
                end
                return g(3)(2);
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 5)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosuresCaptureIndependently(LuaCompatibilityVersion version)
        {
            string code =
                @"
                a = {}
                x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        a[i] = function () y=y+1; x = x * 10; return x+y end
                    end
                end
                container();
                x = 4000
                return a[1](), a[2](), a[3](), a[4](), a[5]()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosuresWithLocalFunctions(LuaCompatibilityVersion version)
        {
            string code =
                @"
                a = {}
                x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        local function zz() y=y+1; x = x * 10; return x+y end
                        a[i] = zz;
                    end
                end
                container();
                x = 4000
                return a[1](), a[2](), a[3](), a[4](), a[5]()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosuresWithNamedFunctions(LuaCompatibilityVersion version)
        {
            string code =
                @"
                a = {}
                x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        function zz() y=y+1; x = x * 10; return x+y end
                        a[i] = zz;
                    end
                end
                container();
                x = 4000
                return a[1](), a[2](), a[3](), a[4](), a[5]()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosureWithoutTableReuse(LuaCompatibilityVersion version)
        {
            string code =
                @"
                x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        function zz() y=y+1; x = x * 10; return x+y end
                        a1 = a2; a2 = a3; a3 = a4; a4 = a5; a5 = zz;
                    end
                end
                container();
                x = 4000
                return a1(), a2(), a3(), a4(), a5()
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NestedUpValues(LuaCompatibilityVersion version)
        {
            string code =
                @"
                local x = 0;
                local m = { };
                function m:a()
                    self.t = {
                        dojob = function()
                            if (x == 0) then return 1; else return 0; end
                        end,
                    };
                end
                m:a();
                return 10 * m.t.dojob();
                ";
            Script script = new Script(version, CoreModulePresets.Complete);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 10)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NestedOutOfScopeUpValues(LuaCompatibilityVersion version)
        {
            string code =
                @"
                function X()
                    local x = 0;
                    local m = { };
                    function m:a()
                        self.t = {
                            dojob = function()
                                if (x == 0) then return 1; else return 0; end
                            end,
                        };
                    end
                    return m;
                end
                Q = X();
                Q:a();
                return 10 * Q.t.dojob();
                ";
            Script script = new Script(version, CoreModulePresets.HardSandbox);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), 10)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LocalRedefinitionUsesLexicalScope(LuaCompatibilityVersion version)
        {
            string code =
                @"
                result = ''
                local hi = 'hello'
                local function test()
                    result = result .. hi;
                end
                test();
                hi = 'X'
                test();
                local hi = '!';
                test();
                return result;
                ";
            Script script = new Script(version, CoreModulePresets.HardSandbox);
            await EndToEndDynValueAssert
                .ExpectAsync(script.DoString(code), "helloXX")
                .ConfigureAwait(false);
        }
    }
}
