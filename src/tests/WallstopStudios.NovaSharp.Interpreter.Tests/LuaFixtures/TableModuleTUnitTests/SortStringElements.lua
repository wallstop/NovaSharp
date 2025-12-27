-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.SortStringElements
-- @compat-notes: table.sort with string elements

local t = { 'banana', 'apple', 'cherry', 'date' }
table.sort(t)
local result = table.concat(t, '-')

assert(result == "apple-banana-cherry-date",
  "Expected 'apple-banana-cherry-date', got: " .. result)
print("PASS: table.sort strings = '" .. result .. "'")