-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:1033
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNonFunctionHandlerInLua53Plus
-- Compatibility notes: Test targets Lua 5.1
return xpcall(function() end, 123)
