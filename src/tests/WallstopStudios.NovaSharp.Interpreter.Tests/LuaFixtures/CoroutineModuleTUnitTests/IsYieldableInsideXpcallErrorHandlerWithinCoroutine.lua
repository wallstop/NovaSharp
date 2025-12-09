-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:741
-- @test: CoroutineModuleTUnitTests.IsYieldableInsideXpcallErrorHandlerWithinCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
handlerYieldable = nil

                function error_handler(err)
                    handlerYieldable = coroutine.isyieldable()
                    return err
                end

                function run_xpcall_inside_coroutine()
                    return xpcall(function()
                        error('boom', 0)
                    end, error_handler)
                end
