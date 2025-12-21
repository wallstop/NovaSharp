namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class BinaryDumpTUnitTests
    {
        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task BinDumpChunkDump(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                "local chunk = load('return 81;'); "
                    + "local str = string.dump(chunk); "
                    + "local fn = load(str); "
                    + "return fn(9);"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 81).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task BinDumpStringDump(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString(
                "local str = string.dump(function(n) return n * n; end); "
                    + "local fn = load(str); "
                    + "return fn(9);"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 81).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpStandardDumpFunc(LuaCompatibilityVersion version)
        {
            DynValue fact = ScriptLoadFunc(
                @"
                function fact(n)
                    return n * 24;
                end
                ",
                "fact",
                version
            );
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpFactorialDumpFunc(LuaCompatibilityVersion version)
        {
            DynValue fact = ScriptLoadFunc(
                @"
                function fact(n)
                    if (n == 0) then return 1; end
                    return fact(n - 1) * n;
                end
                ",
                "fact",
                version
            );
            fact.Function.OwnerScript.Globals.Set("fact", fact);
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpFactorialDumpFuncGlobal(LuaCompatibilityVersion version)
        {
            DynValue fact = ScriptLoadFunc(
                @"
                x = 0
                function fact(n)
                    if (n == x) then return 1; end
                    return fact(n - 1) * n;
                end
                ",
                "fact",
                version
            );
            fact.Function.OwnerScript.Globals.Set("fact", fact);
            fact.Function.OwnerScript.Globals.Set("x", DynValue.NewNumber(0));
            DynValue result = fact.Function.Call(5);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpFactorialDumpFuncUpValueThrows(LuaCompatibilityVersion version)
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
                    "fact",
                    version
                );
                fact.Function.OwnerScript.Globals.Set("fact", fact);
                fact.Function.OwnerScript.Globals.Set("x", DynValue.NewNumber(0));
                _ = fact.Function.Call(5);
            });
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpFactorialClosure(LuaCompatibilityVersion version)
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
                ",
                version
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 140).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpClosureOnParam(LuaCompatibilityVersion version)
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
                ",
                version
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 5).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpNestedUpValues(LuaCompatibilityVersion version)
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
                ",
                version
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 10).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task BinDumpNestedOutOfScopeUpValues(LuaCompatibilityVersion version)
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
                ",
                version
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 10).ConfigureAwait(false);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task LoadChangeEnvWithDebugSetUpValue(LuaCompatibilityVersion version)
        {
            List<Table> captured = new();
            Script script = new(version, CoreModulePresets.Complete)
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

        private static DynValue ScriptRunString(string script, LuaCompatibilityVersion version)
        {
            Script s1 = new(version);
            DynValue proto = s1.LoadString(script);

            using MemoryStream ms = new();
            s1.Dump(proto, ms);
            ms.Position = 0;

            Script s2 = new(version);
            DynValue fn = s2.LoadStream(ms);
            return fn.Function.Call();
        }

        private static DynValue ScriptLoadFunc(
            string script,
            string functionName,
            LuaCompatibilityVersion version
        )
        {
            Script s1 = new(version);
            s1.DoString(script);
            DynValue func = s1.Globals.Get(functionName);

            using MemoryStream ms = new();
            s1.Dump(func, ms);
            ms.Position = 0;

            Script s2 = new(version);
            return s2.LoadStream(ms);
        }
    }
}
