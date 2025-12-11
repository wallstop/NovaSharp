namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ClosureTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ClosureOnParam()
        {
            string script =
                @"
                local function g (z)
                    local function f(a)
                        return a + z;
                    end
                    return f;
                end
                return g(3)(2);
                ";
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 5)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LambdaFunctions()
        {
            string script =
                @"
                g = |f, x|f(x, x+1)
                f = |x, y, z|x*(y+z)
                return g(|x,y|f(x,y,1), 2)
                ";
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 8)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClosureOnParamLambda()
        {
            string script =
                @"
                local function g (z)
                    return |a| a + z
                end
                return g(3)(2);
                ";
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 5)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClosuresCaptureIndependently()
        {
            string script =
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
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClosuresWithLocalFunctions()
        {
            string script =
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
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClosuresWithNamedFunctions()
        {
            string script =
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
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ClosureWithoutTableReuse()
        {
            string script =
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
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 201, 2001, 20001, 200001, 2000001)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NestedUpValues()
        {
            string script =
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
            await EndToEndDynValueAssert
                .ExpectAsync(Script.RunString(script), 10)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NestedOutOfScopeUpValues()
        {
            string script =
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
            DynValue result = new Script(CoreModulePresets.HardSandbox).DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LocalRedefinitionUsesLexicalScope()
        {
            string script =
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
            DynValue result = new Script(CoreModulePresets.HardSandbox).DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "helloXX").ConfigureAwait(false);
        }
    }
}
