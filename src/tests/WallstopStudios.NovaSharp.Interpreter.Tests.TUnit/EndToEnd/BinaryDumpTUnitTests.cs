namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class BinaryDumpTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task BinDumpChunkDump()
        {
            DynValue result = Script.RunString(
                "local chunk = load('return 81;'); "
                    + "local str = string.dump(chunk); "
                    + "local fn = load(str); "
                    + "return fn(9);"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 81).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpStringDump()
        {
            DynValue result = Script.RunString(
                "local str = string.dump(function(n) return n * n; end); "
                    + "local fn = load(str); "
                    + "return fn(9);"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 81).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpStandardDumpFunc()
        {
            DynValue fact = ScriptLoadFunc(
                @"
                function fact(n)
                    return n * 24;
                end
                ",
                "fact"
            );
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpFactorialDumpFunc()
        {
            DynValue fact = ScriptLoadFunc(
                @"
                function fact(n)
                    if (n == 0) then return 1; end
                    return fact(n - 1) * n;
                end
                ",
                "fact"
            );
            fact.Function.OwnerScript.Globals.Set("fact", fact);
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpFactorialDumpFuncGlobal()
        {
            DynValue fact = ScriptLoadFunc(
                @"
                x = 0
                function fact(n)
                    if (n == x) then return 1; end
                    return fact(n - 1) * n;
                end
                ",
                "fact"
            );
            fact.Function.OwnerScript.Globals.Set("fact", fact);
            fact.Function.OwnerScript.Globals.Set("x", DynValue.NewNumber(0));
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpFactorialDumpFuncUpValueThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                DynValue fact = ScriptLoadFunc(
                    @"
                    local x = 0
                    function fact(n)
                        if (n == x) then return 1; end
                        return fact(n - 1) * n;
                    end
                    ",
                    "fact"
                );
                fact.Function.OwnerScript.Globals.Set("fact", fact);
                fact.Function.OwnerScript.Globals.Set("x", DynValue.NewNumber(0));
                _ = fact.Function.Call(5);
            });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpFactorialClosure()
        {
            DynValue result = ScriptRunString(
                @"
                local x = 5;
                function fact(n)
                    if (n == x) then return 1; end
                    return fact(n - 1) * n;
                end
                x = 0;
                y = fact(5);
                x = 3;
                y = y + fact(5);
                return y;
                "
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 140).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpClosureOnParam()
        {
            DynValue result = ScriptRunString(
                @"
                local function g(z)
                    local function f(a)
                        return a + z;
                    end
                    return f;
                end
                return g(3)(2);
                "
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpNestedUpValues()
        {
            DynValue result = ScriptRunString(
                @"
                local x = 0;
                local m = {};
                function m:a()
                    self.t = {
                        dojob = function()
                            if (x == 0) then return 1; else return 0; end
                        end,
                    };
                end
                m:a();
                return 10 * m.t.dojob();
                "
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BinDumpNestedOutOfScopeUpValues()
        {
            DynValue result = ScriptRunString(
                @"
                function X()
                    local x = 0;
                    local m = {};
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
                "
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 10).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task LoadChangeEnvWithDebugSetUpValue()
        {
            List<Table> captured = new();
            Script script = new(CoreModulePresets.Complete)
            {
                Globals = { ["print"] = (Action<Table>)(captured.Add) },
            };

            script.DoString(
                @"
                function print_env()
                    print(_ENV)
                end

                function sandbox()
                    print(_ENV)
                    _ENV = { print = print, print_env = print_env, debug = debug, load = load }
                    print(_ENV)
                    print_env()
                    local code1 = load('print(_ENV)')
                    code1()
                    debug.setupvalue(code1, 0, _ENV)
                    debug.setupvalue(code1, 1, _ENV)
                    code1()
                    local code2 = load('print(_ENV)', nil, nil, _ENV)
                    code2()
                end

                sandbox()
                "
            );

            await Assert.That(captured.Count).IsEqualTo(6).ConfigureAwait(false);
            int[] expected = { 0, 1, 1, 0, 1, 1 };
            for (int i = 0; i < captured.Count; i++)
            {
                await Assert
                    .That(ReferenceEquals(captured[i], captured[expected[i]]))
                    .IsTrue()
                    .ConfigureAwait(false);
            }
        }

        private static DynValue ScriptRunString(string script)
        {
            Script s1 = new();
            DynValue proto = s1.LoadString(script);

            using MemoryStream ms = new();
            s1.Dump(proto, ms);
            ms.Position = 0;

            Script s2 = new();
            DynValue fn = s2.LoadStream(ms);
            return fn.Function.Call();
        }

        private static DynValue ScriptLoadFunc(string script, string functionName)
        {
            Script s1 = new();
            s1.DoString(script);
            DynValue func = s1.Globals.Get(functionName);

            using MemoryStream ms = new();
            s1.Dump(func, ms);
            ms.Position = 0;

            Script s2 = new();
            return s2.LoadStream(ms);
        }
    }
}
