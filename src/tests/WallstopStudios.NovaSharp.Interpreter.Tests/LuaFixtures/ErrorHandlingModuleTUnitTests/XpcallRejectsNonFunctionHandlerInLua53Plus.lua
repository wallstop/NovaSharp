-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:363
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNonFunctionHandlerInLua53Plus
-- @compat-notes: Test targets Lua 5.1
return xpcall(function() end, 123)
