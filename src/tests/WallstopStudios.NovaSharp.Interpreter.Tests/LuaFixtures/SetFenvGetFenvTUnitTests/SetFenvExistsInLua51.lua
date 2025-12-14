-- Test: setfenv exists in Lua 5.1
-- Expected: success - setfenv is a function
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv is available in Lua 5.1
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:43
-- @test: SetFenvGetFenvTUnitTests.SetFenvExistsInLua51
local ok, err = pcall(function()
    local result = type(setfenv)
    assert(result == "function", "setfenv should be a function, got " .. tostring(result))
    print("PASS: setfenv is a " .. result)
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
