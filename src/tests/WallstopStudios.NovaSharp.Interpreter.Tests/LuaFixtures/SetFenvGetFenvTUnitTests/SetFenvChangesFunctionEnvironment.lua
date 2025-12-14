-- Test: setfenv changes the environment of a function
-- Expected: success - function now uses new environment
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv(f, table) changes f's environment
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:131
-- @test: SetFenvGetFenvTUnitTests.SetFenvChangesFunctionEnvironment
local ok, err = pcall(function()
    local function testfn()
        return x
    end
    
    local newenv = { x = 42 }
    setmetatable(newenv, { __index = _G })
    
    setfenv(testfn, newenv)
    local result = testfn()
    
    assert(result == 42, "Function should see x=42 from new environment, got " .. tostring(result))
    print("PASS: setfenv changed function environment")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
