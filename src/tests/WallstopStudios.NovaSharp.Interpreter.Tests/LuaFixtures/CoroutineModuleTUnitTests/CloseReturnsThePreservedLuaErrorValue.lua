-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:173
-- @test: CoroutineModuleTUnitTests.CloseReturnsThePreservedLuaErrorValue
-- Compatibility notes: Test targets Lua 5.4+
local original = {}
                local co = coroutine.create(function() error(original) end)
                local ok, e = coroutine.resume(co)
                local closed, closeError = coroutine.close(co)

                print('close', ok, rawequal(original, e), closed, rawequal(original, closeError), type(closeError))

                return ok, rawequal(original, e), closed, rawequal(original, closeError), type(closeError)
