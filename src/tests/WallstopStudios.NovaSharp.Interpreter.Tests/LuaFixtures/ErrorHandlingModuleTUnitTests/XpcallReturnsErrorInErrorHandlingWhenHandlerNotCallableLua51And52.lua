-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:379
-- @test: ErrorHandlingModuleTUnitTests.XpcallReturnsErrorInErrorHandlingWhenHandlerNotCallableLua51And52
-- When main function errors and handler isn't callable in Lua 5.1/5.2, returns "error in error handling"
local ok, err = xpcall(function() error('test') end, 123)
print(ok)  -- false
print(err) -- "error in error handling"
