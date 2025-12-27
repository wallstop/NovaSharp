-- Tests that table.remove(t, pos) accepts fractional position in Lua 5.1/5.2
-- These versions silently truncate via floor

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.RemoveFractional_51_52
local t = {1, 2, 3}
local removed = table.remove(t, 2.9)  -- Should remove at position 2
assert(removed == 2, "Removed element should be 2")
assert(#t == 2, "Table should have 2 elements")
assert(t[1] == 1, "First element should be 1")
assert(t[2] == 3, "Second element should be 3")
print("PASS: table.remove with fractional position accepted")
