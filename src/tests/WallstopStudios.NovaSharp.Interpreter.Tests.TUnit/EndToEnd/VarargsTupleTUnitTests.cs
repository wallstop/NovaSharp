namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class VarargsTupleTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarArgsTupleBasic(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "f(3)", "a: 3 b: nil").ConfigureAwait(false);
            await ExpectAsync(version, "f(3,4)", "a: 3 b: 4").ConfigureAwait(false);
            await ExpectAsync(version, "f(3,4,5)", "a: 3 b: 4").ConfigureAwait(false);
            await ExpectAsync(version, "f(r(),10)", "a: 1 b: 10").ConfigureAwait(false);
            await ExpectAsync(version, "f(r())", "a: 1 b: 2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarArgsTupleIntermediate(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "g(3)", "a: 3 b: nil arg: {}").ConfigureAwait(false);
            await ExpectAsync(version, "g(3,4)", "a: 3 b: 4 arg: {}").ConfigureAwait(false);
            await ExpectAsync(version, "g(3,4,5,8)", "a: 3 b: 4 arg: {5, 8, }")
                .ConfigureAwait(false);
            await ExpectAsync(version, "g(5,r())", "a: 5 b: 1 arg: {2, 3, }").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarArgsTupleAdvanced(LuaCompatibilityVersion version)
        {
            await ExpectAsync(version, "h(5,r())", "a: 5 b: 1 arg: {2, 3, }").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
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
        [AllLuaVersions]
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

        /// <summary>
        /// Tests that varargs with zero arguments returns an empty tuple, not a single nil.
        /// This was a bug where <c>select('#', ...)</c> would return 1 instead of 0 when no
        /// arguments were passed to a varargs function.
        /// </summary>
        /// <remarks>
        /// Reference Lua behavior: <c>function f(...) print(select('#', ...)) end f()</c> prints "0".
        /// The bug was in <c>ExecArgs</c> where the <c>i &gt;= argsList.Count</c> check was evaluated
        /// before checking if the symbol was a varargs symbol, causing nil to be assigned instead
        /// of creating an empty tuple.
        /// </remarks>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarargsWithZeroArgumentsReturnsEmptyTuple(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            const string lua =
                @"
                function f(...)
                    return select('#', ...)
                end
                return f()  -- Should be 0, not 1
            ";
            DynValue result = script.DoString(lua);
            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert
                .That(result.Number)
                .IsEqualTo(0)
                .Because("varargs with no arguments should have select('#', ...) return 0")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests varargs with zero arguments through pcall, which was the original symptom of the bug.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PcallVarargsWithZeroArgumentsReturnsEmptyTuple(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            const string lua =
                @"
                function f(...)
                    return select('#', ...)
                end
                local ok, count = pcall(f)
                return ok, count
            ";
            DynValue result = script.DoString(lua);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Boolean)
                .IsTrue()
                .Because("pcall should succeed")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(0)
                .Because("pcall with varargs function called with no args should have 0 varargs")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests varargs with zero arguments through xpcall, which was also affected by the bug.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task XpcallVarargsWithZeroArgumentsReturnsEmptyTuple(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            const string lua =
                @"
                function f(...)
                    return select('#', ...)
                end
                local ok, count = xpcall(f, function(err) end)
                return ok, count
            ";
            DynValue result = script.DoString(lua);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Boolean)
                .IsTrue()
                .Because("xpcall should succeed")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(0)
                .Because("xpcall with varargs function called with no args should have 0 varargs")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that varargs with exactly one nil argument is distinguishable from zero arguments.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarargsDistinguishesZeroArgsFromOneNilArg(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            const string lua =
                @"
                function f(...)
                    return select('#', ...)
                end
                local zeroArgs = f()
                local oneNilArg = f(nil)
                return zeroArgs, oneNilArg
            ";
            DynValue result = script.DoString(lua);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(0)
                .Because("f() should have 0 args")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Number)
                .IsEqualTo(1)
                .Because("f(nil) should have 1 arg (the nil)")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests the common printf pattern where zero varargs should be detected.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task VarargsZeroArgsInPrintfPattern(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            const string lua =
                @"
                function printf(fmt, ...)
                    if select('#', ...) == 0 then
                        return 'no_extra_args'
                    else
                        return 'has_extra_args'
                    end
                end
                return printf('hello')
            ";
            DynValue result = script.DoString(lua);
            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert
                .That(result.String)
                .IsEqualTo("no_extra_args")
                .Because("printf with one arg should detect zero varargs")
                .ConfigureAwait(false);
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
