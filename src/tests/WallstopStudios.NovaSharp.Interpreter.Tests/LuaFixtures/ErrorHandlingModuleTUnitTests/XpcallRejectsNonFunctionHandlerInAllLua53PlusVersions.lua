-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\CoreLib\ErrorHandlingModuleTUnitTests.cs:462
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNonFunctionHandlerInAllLua53PlusVersions
-- Test targets Lua 5.3+
return xpcall(function() end, 123)
