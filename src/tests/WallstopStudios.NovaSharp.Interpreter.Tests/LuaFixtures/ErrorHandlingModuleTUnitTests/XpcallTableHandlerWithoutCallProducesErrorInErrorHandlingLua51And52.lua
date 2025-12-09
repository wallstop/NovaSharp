-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:419
-- @test: ErrorHandlingModuleTUnitTests.XpcallTableHandlerWithoutCallProducesErrorInErrorHandlingLua51And52
-- In Lua 5.1/5.2, xpcall with table handler (no __call) produces "error in error handling"
local ok, err = xpcall(function() error('test') end, {})
print(ok)  -- false
print(err) -- "error in error handling"
