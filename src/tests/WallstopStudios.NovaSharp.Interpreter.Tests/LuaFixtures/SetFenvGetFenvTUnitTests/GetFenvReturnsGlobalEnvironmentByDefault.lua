-- Test: getfenv(0) returns the global environment
-- Expected: success - returns _G
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv(0) returns the global environment
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:82
-- @test: SetFenvGetFenvTUnitTests.GetFenvReturnsGlobalEnvironmentByDefault
local ok, err = pcall(function()
    local env = getfenv(0)
    assert(env == _G, "getfenv(0) should return _G")
    print("PASS: getfenv(0) returns _G")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
