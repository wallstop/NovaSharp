-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.RemoveDefaultPosition
-- @compat-notes: table.remove with no position removes last element

local t = { 'a', 'b', 'c' }
local removed = table.remove(t)
local remaining = table.concat(t, '-')

assert(removed == 'c', "removed should be 'c', got: " .. tostring(removed))
assert(remaining == "a-b", "remaining should be 'a-b', got: " .. remaining)
assert(#t == 2, "table length should be 2, got: " .. #t)

print("PASS: table.remove() removes last element correctly")