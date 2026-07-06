-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:755
-- @test: ErrorHandlingModuleTUnitTests.XpcallMessageHandlerErrorDoesNotReenterMessageHandler
-- Compatibility notes: Test targets Lua 5.4+
return xpcall(function()
                    error('boom', 0)
                end, function()
                    error('handlererr', 0)
                end)
