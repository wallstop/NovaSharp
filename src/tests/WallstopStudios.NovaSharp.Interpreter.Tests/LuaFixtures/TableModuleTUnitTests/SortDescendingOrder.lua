-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.SortDescendingOrder
-- @compat-notes: table.sort with descending comparator

local t = { 3, 1, 4, 1, 5, 9, 2, 6 }
table.sort(t, function(a, b) return a > b end)
local result = table.concat(t, '-')

assert(result == "9-6-5-4-3-2-1-1",
  "Expected '9-6-5-4-3-2-1-1', got: " .. result)
print("PASS: table.sort descending = '" .. result .. "'")