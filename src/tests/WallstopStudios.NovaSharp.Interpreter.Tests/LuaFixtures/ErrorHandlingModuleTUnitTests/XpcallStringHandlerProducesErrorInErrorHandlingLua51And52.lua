-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:420
-- @test: ErrorHandlingModuleTUnitTests.XpcallStringHandlerProducesErrorInErrorHandlingLua51And52
-- @compat-notes: Test targets Lua 5.1
return xpcall(function() error('test') end, 'not-a-function')
