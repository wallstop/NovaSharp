-- Test: getfenv exists in Lua 5.1
-- Expected: success - getfenv is a function
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv is available in Lua 5.1
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:32
-- @test: SetFenvGetFenvTUnitTests.GetFenvExistsInLua51
local ok, err = pcall(function()
    local result = type(getfenv)
    assert(result == "function", "getfenv should be a function, got " .. tostring(result))
    print("PASS: getfenv is a " .. result)
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
