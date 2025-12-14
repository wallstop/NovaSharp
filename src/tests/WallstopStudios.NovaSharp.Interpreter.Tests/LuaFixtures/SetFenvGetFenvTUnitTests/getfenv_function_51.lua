-- Test: getfenv on a function
-- Expected: success
-- Description: Tests getfenv(func) returns the function's environment

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.getfenv_function_51
local function test_func()
    return 1
end

-- Default environment should be _G
local env = getfenv(test_func)
assert(env == _G, "getfenv(func) should return _G by default")

print("PASS")
