-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.SortByStringLength
-- @compat-notes: table.sort with custom length comparator

local t = { 'a', 'bbb', 'cc', 'dddd' }
table.sort(t, function(a, b) return #a < #b end)
local result = table.concat(t, '-')

assert(result == "a-cc-bbb-dddd",
    "Expected 'a-cc-bbb-dddd', got: " .. result)
print("PASS: table.sort by length = '" .. result .. "'")