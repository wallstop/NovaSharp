-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:398
-- @test: ErrorHandlingModuleTUnitTests.XpcallStringHandlerProducesErrorInErrorHandlingLua51And52
-- In Lua 5.1/5.2, xpcall with string handler produces "error in error handling"
local ok, err = xpcall(function() error('test') end, 'not-a-function')
print(ok)  -- false
print(err) -- "error in error handling"
