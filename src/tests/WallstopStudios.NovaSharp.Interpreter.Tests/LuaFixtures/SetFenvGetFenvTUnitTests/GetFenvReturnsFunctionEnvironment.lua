-- Test: getfenv(function) returns the function's environment
-- Expected: success - returns the function's environment table
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv(f) returns function f's environment
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.GetFenvReturnsFunctionEnvironment
local ok, err = pcall(function()
    local function testfn() end
    local env = getfenv(testfn)
    assert(type(env) == "table", "getfenv(f) should return a table, got " .. type(env))
    print("PASS: getfenv(f) returns a table")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
