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

if not ok then
    -- Expected error
    print("PASS: getfenv threw error for invalid level: " .. tostring(err))
else
    print("ERROR: getfenv should have thrown error for invalid level")
end
