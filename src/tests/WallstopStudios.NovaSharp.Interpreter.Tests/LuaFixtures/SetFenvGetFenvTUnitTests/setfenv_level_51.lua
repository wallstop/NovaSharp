-- Test: setfenv with stack level
-- Expected: success
-- Description: Tests setfenv(1, table) changes current function's environment

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.setfenv_level_51
local result = nil

local function test_level()
    local new_env = { custom = true }
    setmetatable(new_env, { __index = _G })
    setfenv(1, new_env)
    -- After setfenv(1), 'custom' should be visible
    result = custom
end

test_level()
assert(result == true, "setfenv(1, env) should change current function's environment")

print("PASS")
