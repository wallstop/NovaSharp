-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\CoreLib\ErrorHandlingModuleTUnitTests.cs:344
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNilHandlerInLua53Plus
-- Test targets Lua 5.3+
return xpcall(function() end, nil)
