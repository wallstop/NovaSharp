-- Tests that table.remove(t, pos) requires integer representation for position in Lua 5.3+
-- Per Lua 5.3 manual §6.6: table.remove position must have integer representation

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.RemoveIntegerRepresentation_53plus
local t = {1, 2, 3}
table.remove(t, 1.5)
print("ERROR: Should have thrown")
