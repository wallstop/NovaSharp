-- Test: setfenv returns the function it modified
-- Expected: success - returns the modified function
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv(f, table) returns f
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.SetFenvReturnsFunction
local ok, err = pcall(function()
    local function testfn() end
    local newenv = {}
    setmetatable(newenv, { __index = _G })
    
    local returned = setfenv(testfn, newenv)
    
    assert(returned == testfn, "setfenv should return the function")
    print("PASS: setfenv returns the function")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
