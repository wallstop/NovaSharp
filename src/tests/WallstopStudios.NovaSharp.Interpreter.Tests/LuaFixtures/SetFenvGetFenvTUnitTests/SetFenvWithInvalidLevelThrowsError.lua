-- Test: setfenv with invalid level throws error
-- Expected: error - 'setfenv' cannot change environment
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv with invalid level throws error
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.SetFenvWithInvalidLevelThrowsError
local ok, err = pcall(function()
    local newenv = {}
    -- Level 100 should be invalid (no function at that level)
    setfenv(100, newenv)
end)

assert(not ok, "setfenv(100, {}) should throw error")
assert(err:find("invalid level"), "Error should mention 'invalid level', got: " .. tostring(err))
print("PASS")
