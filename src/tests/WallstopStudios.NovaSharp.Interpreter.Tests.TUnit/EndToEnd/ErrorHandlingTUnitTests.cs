namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ErrorHandlingTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PCallReturnsMultipleValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return pcall(function() return 1,2,3 end)");

            await EndToEndDynValueAssert.ExpectAsync(result, true, 1, 2, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PCallSurfacesClrErrors(LuaCompatibilityVersion version)
        {
            string code =
                @"
                r, msg = pcall(assert, false, 'caught')
                return r, msg;
            ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, false, "caught").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NestedPCallPropagatesFailures(LuaCompatibilityVersion version)
        {
            string code =
                @"
                    function try(fn)
                        local ok, value = pcall(fn)
                        if ok then
                            return value
                        end
                        return '!'
                    end

                    function a()
                        return try(b) .. 'a'
                    end

                    function b()
                        return try(c) .. 'b'
                    end

                    function c()
                        return try(d) .. 'c'
                    end

                    function d()
                        local t = { } .. 'x'
                    end

                    return a()
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, "!cba").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NestedTryCatchPropagatesFailures(LuaCompatibilityVersion version)
        {
            string code =
                @"
                    function a()
                        return try(b) .. 'a'
                    end

                    function b()
                        return try(c) .. 'b'
                    end

                    function c()
                        return try(d) .. 'c'
                    end

                    function d()
                        local t = { } .. 'x'
                    end

                    return a()
                ";

            Script script = new Script(version, default(CoreModules));
            script.Globals["try"] = DynValue.NewCallback(
                (context, args) =>
                {
                    try
                    {
                        DynValue result = args[0].Function.Call();
                        return result;
                    }
                    catch (ScriptRuntimeException)
                    {
                        return DynValue.NewString("!");
                    }
                }
            );

            DynValue executionResult = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(executionResult, "!cba").ConfigureAwait(false);
        }
    }
}
