-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:322
-- @test: ErrorHandlingModuleTUnitTests.XpcallAllowsNilHandlerInLua51And52
-- @compat-notes: Test targets Lua 5.1
return xpcall(function() error('test error') end, nil)
