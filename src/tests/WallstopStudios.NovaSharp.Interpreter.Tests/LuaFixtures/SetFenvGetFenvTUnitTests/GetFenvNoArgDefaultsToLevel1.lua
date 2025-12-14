-- Test: getfenv with no argument defaults to getfenv(1)
-- Expected: success - same as getfenv(1)
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv() is equivalent to getfenv(1)
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.GetFenvNoArgDefaultsToLevel1
local ok, err = pcall(function()
    local env1 = getfenv()
    local env2 = getfenv(1)
    assert(env1 == env2, "getfenv() should equal getfenv(1)")
    print("PASS: getfenv() equals getfenv(1)")
end)

if not ok then
    print("ERROR: " .. tostring(err))
end
