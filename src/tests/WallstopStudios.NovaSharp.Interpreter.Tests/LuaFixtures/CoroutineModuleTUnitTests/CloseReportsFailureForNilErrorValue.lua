-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:199
-- @test: CoroutineModuleTUnitTests.CloseReportsFailureForNilErrorValue
-- Compatibility notes: Test targets Lua 5.4+
local co = coroutine.create(function() error(nil) end)
                local ok, e = coroutine.resume(co)
                local closed, closeError = coroutine.close(co)

                print('nil', ok, type(e), closed, type(closeError), closeError)

                return ok, type(e), closed, type(closeError), closeError
