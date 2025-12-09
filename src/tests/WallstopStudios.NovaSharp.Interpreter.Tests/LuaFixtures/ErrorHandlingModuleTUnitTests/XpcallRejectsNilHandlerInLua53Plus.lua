-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:325
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNilHandlerInLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return xpcall(function() end, nil)
