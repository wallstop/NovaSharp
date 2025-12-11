-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:186
-- @test: TableModuleTUnitTests.RemoveIgnoresExtraArguments
-- @compat-notes: Lua ignores extra arguments to table.remove (consistent across all versions)
-- Reference: Lua manual §6.6 (table.remove) - no mention of argument count validation

-- Test that table.remove silently ignores extra arguments (just like real Lua)
local values = { 1, 2, 3, 4, 5 }
local removed = table.remove(values, 1, 2, "extra", "args")

-- Verify the first element was removed
if removed == 1 and #values == 4 and values[1] == 2 then
    print("PASS")
else
    error("Expected table.remove to work normally, ignoring extra args")
end
