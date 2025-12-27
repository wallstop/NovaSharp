-- Test: setfenv(0, t) changes the global environment
-- Expected: success - global environment changes
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv(0, t) changes the running thread's global environment
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.SetFenvChangesGlobalEnvironment
local ok, err = pcall(function()
    local original_G = _G
    local newenv = { test_global = "hello" }
    setmetatable(newenv, { __index = original_G })
    
    setfenv(0, newenv)
    
    -- Now test_global should be accessible via _G (which is newenv)
    local result = getfenv(0).test_global
    assert(result == "hello", "test_global should be 'hello' in new global env, got " .. tostring(result))
    
    -- Restore original
    setfenv(0, original_G)
    print("PASS: setfenv(0, t) changed global environment")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
