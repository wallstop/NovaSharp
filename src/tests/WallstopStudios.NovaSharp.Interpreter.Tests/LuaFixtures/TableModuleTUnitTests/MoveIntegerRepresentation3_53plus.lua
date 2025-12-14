-- Tests that table.move(a1, f, e, t, a2) requires integer representation for target index in Lua 5.3+
-- Per Lua 5.3 manual §6.6: table.move indices must have integer representation

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.MoveIntegerRepresentation3_53plus
local a1 = {1, 2, 3, 4, 5}
local a2 = {}
table.move(a1, 1, 3, 1.5, a2)
print("ERROR: Should have thrown")
