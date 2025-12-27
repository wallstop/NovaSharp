-- Test: Basic setfenv functionality
-- Expected: success
-- Description: Tests setfenv changes function environment

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.setfenv_basic_51
local function test_func()
    return x  -- 'x' will be looked up in environment
end

-- Create custom environment with x defined
local custom_env = { x = 42 }
setmetatable(custom_env, { __index = _G })

-- Set environment
setfenv(test_func, custom_env)

-- Verify environment changed
local result = test_func()
assert(result == 42, "setfenv should change function environment, got: " .. tostring(result))

-- Verify getfenv reflects change
local env = getfenv(test_func)
assert(env == custom_env, "getfenv should return new environment")
assert(env.x == 42, "Environment should have x = 42")

print("PASS")
