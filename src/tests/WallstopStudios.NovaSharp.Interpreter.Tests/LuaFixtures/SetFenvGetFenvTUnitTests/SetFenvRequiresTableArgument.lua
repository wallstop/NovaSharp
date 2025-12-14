-- Test: setfenv requires table as second argument
-- Expected: error - bad argument #2
-- Versions: 5.1 only
-- Reference: Lua 5.1 Reference Manual §5.1

-- setfenv requires a table as second argument
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.SetFenvRequiresTableArgument
local ok, err = pcall(function()
    local function testfn() end
    -- Pass a number instead of a table
    setfenv(testfn, 42)
end)

if not ok then
    -- Expected error about bad argument
    print("PASS: setfenv threw error for non-table: " .. tostring(err))
else
    print("ERROR: setfenv should have thrown error for non-table second argument")
end
