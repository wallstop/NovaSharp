-- Test: setfenv is nil in Lua 5.2+
-- Expected: success - setfenv is nil
-- Versions: 5.2, 5.3, 5.4, 5.5
-- Reference: Lua 5.2 Reference Manual (removed setfenv/getfenv)

-- setfenv was removed in Lua 5.2+
-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:71
-- @test: SetFenvGetFenvTUnitTests.SetFenvIsNilInLua52Plus
local ok, err = pcall(function()
    local result = type(setfenv)
    assert(result == "nil", "setfenv should be nil in Lua 5.2+, got " .. tostring(result))
    print("PASS: setfenv is " .. result)
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
