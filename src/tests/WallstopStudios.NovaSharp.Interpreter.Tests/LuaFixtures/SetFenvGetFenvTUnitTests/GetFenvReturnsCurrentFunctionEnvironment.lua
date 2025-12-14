-- Test: getfenv(1) returns the calling function's environment
-- Expected: success - returns current function's environment
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv(1) returns the calling function's environment
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.GetFenvReturnsCurrentFunctionEnvironment
local ok, err = pcall(function()
    local env = getfenv(1)
    -- For a top-level function, this should be _G
    assert(type(env) == "table", "getfenv(1) should return a table")
    print("PASS: getfenv(1) returns a table")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
