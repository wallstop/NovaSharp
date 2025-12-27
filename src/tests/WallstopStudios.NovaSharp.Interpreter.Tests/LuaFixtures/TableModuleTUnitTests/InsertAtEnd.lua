-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.InsertAtEnd
-- @compat-notes: table.insert at end (default behavior)

local t = { 1, 2, 3 }
table.insert(t, 4)

assert(t[1] == 1, "t[1] should be 1")
assert(t[2] == 2, "t[2] should be 2")
assert(t[3] == 3, "t[3] should be 3")
assert(t[4] == 4, "t[4] should be 4")
assert(#t == 4, "table length should be 4")

print("PASS: table.insert at end works correctly")