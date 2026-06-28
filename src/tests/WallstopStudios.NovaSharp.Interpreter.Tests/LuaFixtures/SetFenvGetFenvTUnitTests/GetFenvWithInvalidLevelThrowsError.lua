-- Test: getfenv with invalid level throws error
-- Expected: error - invalid level
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- getfenv with invalid level throws error
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.GetFenvWithInvalidLevelThrowsError
local ok, err = pcall(function()
    -- Level 100 should be invalid (no function at that level)
    local env = getfenv(100)
end)

assert(not ok, "getfenv(100) should throw error")
assert(err:find("invalid level"), "Error should mention 'invalid level', got: " .. tostring(err))
print("PASS")
