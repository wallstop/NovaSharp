-- Test: setfenv returns the function
-- Expected: success
-- Description: Tests that setfenv returns the function it was called with

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.setfenv_returns_function_51
local function test_func()
    return 1
end

local returned = setfenv(test_func, _G)
assert(returned == test_func, "setfenv should return the function")

print("PASS")
