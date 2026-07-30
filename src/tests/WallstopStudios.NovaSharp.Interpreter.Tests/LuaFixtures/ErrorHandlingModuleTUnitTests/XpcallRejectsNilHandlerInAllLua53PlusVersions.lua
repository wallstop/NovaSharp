-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:1153
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNilHandlerInAllLua53PlusVersions
-- Test targets Lua 5.3+
return xpcall(function() end, nil)
