namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class ErrorHandlingTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PCallReturnsMultipleValues()
        {
            Script script = new();
            DynValue result = script.DoString("return pcall(function() return 1,2,3 end)");

            await EndToEndDynValueAssert.ExpectAsync(result, true, 1, 2, 3).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PCallSurfacesClrErrors()
        {
            string code =
                @"
                r, msg = pcall(assert, false, 'catched')
                return r, msg;
            ";

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert
                .ExpectAsync(result, false, "catched")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NestedPCallPropagatesFailures()
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

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, "!cba").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task NestedTryCatchPropagatesFailures()
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

            Script script = new(default(CoreModules))
            {
                Globals =
                {
                    ["try"] = DynValue.NewCallback(
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
                    ),
                },
            };

            DynValue executionResult = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(executionResult, "!cba").ConfigureAwait(false);
        }
    }
}
