-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:461
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNilHandlerInAllLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+
return xpcall(function() end, nil)
